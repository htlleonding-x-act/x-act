import 'dart:async';

import 'package:flutter/material.dart';

import '../api/api_service.dart';
import '../api/models.dart';
import '../services/app_session.dart';
import '../widgets/xact_branding.dart';

/// One player in the report roster, enriched with role/host/self flags.
class _PlayerRow {
  final int memberId;
  final int teamId;
  final int? userId;
  final String name;
  final Color teamColor;
  final TeamRole? role;
  final bool isHost;
  final bool isSelf;

  const _PlayerRow({
    required this.memberId,
    required this.teamId,
    required this.userId,
    required this.name,
    required this.teamColor,
    required this.role,
    required this.isHost,
    required this.isSelf,
  });
}

class ReportScreen extends StatefulWidget {
  const ReportScreen({super.key});

  @override
  State<ReportScreen> createState() => _ReportScreenState();
}

class _ReportScreenState extends State<ReportScreen> {
  final List<_PlayerRow> _players = [];
  final Map<int, MemberOffense> _offensesByMember = {};
  final Set<int> _votedVoteIds = {};

  KickVote? _vote;
  bool _loading = true;
  bool _failed = false;
  bool _busy = false;

  StreamSubscription<RealtimeEventEnvelope>? _eventSub;
  Timer? _rosterReloadDebounce;
  Timer? _countdownTimer;

  int? get _currentMemberId => AppSession.instance.currentMemberId;

  @override
  void initState() {
    super.initState();
    _eventSub = ApiService.instance.realtimeEvents.listen(_onEvent);
    _countdownTimer = Timer.periodic(const Duration(seconds: 1), (_) {
      // Keep the open-vote countdown ticking.
      if (mounted && _vote != null) {
        setState(() {});
      }
    });
    unawaited(_init());
  }

  @override
  void dispose() {
    _eventSub?.cancel();
    _rosterReloadDebounce?.cancel();
    _countdownTimer?.cancel();
    super.dispose();
  }

  Future<void> _init() async {
    final sessionId = AppSession.instance.currentSessionId;
    if (sessionId == null) {
      setState(() {
        _loading = false;
        _failed = true;
      });
      return;
    }

    try {
      await ApiService.instance.ensureRealtimeSessionSubscription(sessionId);
    } catch (_) {
      // Realtime is best-effort; the initial REST load still populates the tab.
    }

    try {
      final results = await Future.wait([
        _buildRoster(sessionId),
        ApiService.instance.loadOpenKickVote(sessionId: sessionId),
        ApiService.instance.loadActiveOffenses(sessionId: sessionId),
      ]);

      if (!mounted) {
        return;
      }

      final roster = results[0] as List<_PlayerRow>;
      final vote = results[1] as KickVote?;
      final offenses = results[2] as List<MemberOffense>;

      setState(() {
        _players
          ..clear()
          ..addAll(roster);
        _vote = (vote != null && vote.isOpen) ? vote : null;
        _offensesByMember
          ..clear()
          ..addEntries(offenses.map((o) => MapEntry(o.memberId, o)));
        _loading = false;
        _failed = false;
      });
    } catch (_) {
      if (!mounted) {
        return;
      }
      setState(() {
        _loading = false;
        _failed = true;
      });
    }
  }

  Future<List<_PlayerRow>> _buildRoster(int sessionId) async {
    final snapshot = await ApiService.instance.loadLobbySnapshot(sessionId);
    final details = await ApiService.instance.getGameSession(sessionId);
    final hostUserId = details.hostUserId;
    final currentMemberId = _currentMemberId;

    final rows = <_PlayerRow>[];
    for (final team in snapshot.teams) {
      final color = tryParseHexColor(team.colorCode) ?? XActColors.roleSpectator;
      final members = snapshot.membersByTeamId[team.teamId] ?? const [];
      for (final member in members) {
        final name = member.userId != null
            ? (snapshot.usersById[member.userId!]?.username ??
                  'User ${member.userId}')
            : (member.guestName ?? 'Guest');

        rows.add(
          _PlayerRow(
            memberId: member.memberId,
            teamId: team.teamId,
            userId: member.userId,
            name: name,
            teamColor: color,
            role: team.role,
            isHost: member.userId != null && member.userId == hostUserId,
            isSelf: member.memberId == currentMemberId,
          ),
        );
      }
    }

    rows.sort((a, b) {
      if (a.isSelf != b.isSelf) {
        return a.isSelf ? -1 : 1;
      }
      return a.name.toLowerCase().compareTo(b.name.toLowerCase());
    });
    return rows;
  }

  void _queueRosterReload() {
    final sessionId = AppSession.instance.currentSessionId;
    if (sessionId == null) {
      return;
    }
    _rosterReloadDebounce?.cancel();
    _rosterReloadDebounce = Timer(const Duration(milliseconds: 300), () async {
      try {
        final roster = await _buildRoster(sessionId);
        if (!mounted) {
          return;
        }
        setState(() {
          _players
            ..clear()
            ..addAll(roster);
        });
      } catch (_) {
        // Keep the previous roster on a transient failure.
      }
    });
  }

  void _onEvent(RealtimeEventEnvelope envelope) {
    switch (envelope.type) {
      case RealtimeEvents.kickVoteStarted:
      case RealtimeEvents.kickVoteUpdated:
        final vote = KickVote.fromJson(envelope.payload);
        if (!mounted) {
          return;
        }
        setState(() => _vote = vote.isOpen ? vote : null);
        break;

      case RealtimeEvents.kickVoteResolved:
        final vote = KickVote.fromJson(envelope.payload);
        if (!mounted) {
          return;
        }
        setState(() {
          _votedVoteIds.remove(vote.voteId);
          if (_vote?.voteId == vote.voteId || _vote == null) {
            _vote = null;
          }
        });
        _announceVoteOutcome(vote);
        break;

      case RealtimeEvents.memberKicked:
        final payload = MemberKickedPayload.fromJson(envelope.payload);
        if (!mounted) {
          return;
        }
        setState(() => _offensesByMember.remove(payload.memberId));
        // Vote kicks are already announced via kick_vote_resolved; only the host
        // sudo kick needs its own notice here to avoid a duplicate toast.
        if (payload.byHost && payload.memberId != _currentMemberId) {
          _toast('${payload.memberName} was kicked by the host.');
        }
        _queueRosterReload();
        break;

      case RealtimeEvents.memberOffenseRaised:
        final offense = MemberOffense.fromJson(envelope.payload);
        if (!mounted) {
          return;
        }
        setState(() => _offensesByMember[offense.memberId] = offense);
        break;

      case RealtimeEvents.memberOffenseCleared:
        final offense = MemberOffense.fromJson(envelope.payload);
        if (!mounted) {
          return;
        }
        setState(() => _offensesByMember.remove(offense.memberId));
        break;

      case RealtimeEvents.teamMemberJoined:
      case RealtimeEvents.teamMemberLeft:
      case RealtimeEvents.teamMemberUpdated:
      case RealtimeEvents.teamAdded:
      case RealtimeEvents.teamUpdated:
      case RealtimeEvents.teamDeleted:
        _queueRosterReload();
        break;

      default:
        break;
    }
  }

  void _announceVoteOutcome(KickVote vote) {
    final message = switch (vote.status) {
      KickVoteStatus.passed => 'Vote passed — ${vote.targetName} was kicked.',
      KickVoteStatus.rejected => 'Vote to kick ${vote.targetName} failed.',
      KickVoteStatus.cancelled => 'The vote against ${vote.targetName} was cancelled.',
      KickVoteStatus.expired => 'The vote against ${vote.targetName} expired.',
      _ => null,
    };
    if (message != null) {
      _toast(message);
    }
  }

  void _toast(String message) {
    if (!mounted) {
      return;
    }
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          content: Text(message, style: XActText.bodySm),
          backgroundColor: XActColors.surface2,
          behavior: SnackBarBehavior.floating,
          duration: const Duration(seconds: 3),
        ),
      );
  }

  bool get _isHost {
    final currentUserId = AppSession.instance.currentUserId;
    if (currentUserId == null) {
      return false;
    }
    return _players.any((p) => p.isSelf && p.userId == currentUserId && p.isHost);
  }

  bool get _hasOpenVote => _vote != null;

  Future<void> _startVote(_PlayerRow row) async {
    final reason = await _promptReason(
      title: 'Start a vote to kick',
      subject: row.name,
      actionLabel: 'Start vote',
      destructive: false,
    );
    if (reason == null || !mounted) {
      return;
    }

    // If the player is currently flagged and the initiator typed nothing, record
    // the out-of-bounds offense as the reason so it persists even if they return.
    final effectiveReason = reason.isNotEmpty
        ? reason
        : (_offensesByMember.containsKey(row.memberId)
              ? 'Outside the game area'
              : null);

    await _run(() async {
      final vote = await ApiService.instance.startKickVote(
        targetMemberId: row.memberId,
        reason: effectiveReason,
      );
      if (!mounted) {
        return;
      }
      setState(() => _vote = vote.isOpen ? vote : null);
      if (!vote.isOpen) {
        _announceVoteOutcome(vote);
      }
    });
  }

  Future<void> _castBallot(bool approve) async {
    final vote = _vote;
    if (vote == null || _busy) {
      return;
    }

    setState(() => _votedVoteIds.add(vote.voteId));
    await _run(() async {
      final updated = await ApiService.instance.castKickBallot(
        voteId: vote.voteId,
        approve: approve,
      );
      if (!mounted) {
        return;
      }
      setState(() => _vote = updated.isOpen ? updated : null);
      if (!updated.isOpen) {
        _announceVoteOutcome(updated);
      }
    }, onError: () {
      if (mounted) {
        setState(() => _votedVoteIds.remove(vote.voteId));
      }
    });
  }

  Future<void> _cancelVote() async {
    final vote = _vote;
    if (vote == null) {
      return;
    }
    await _run(() async {
      await ApiService.instance.cancelKickVote(voteId: vote.voteId);
      if (mounted) {
        setState(() => _vote = null);
      }
    });
  }

  Future<void> _hostKick(_PlayerRow row) async {
    final reason = await _promptReason(
      title: 'Kick this player',
      subject: row.name,
      actionLabel: 'Kick now',
      destructive: true,
    );
    if (reason == null || !mounted) {
      return;
    }

    await _run(() async {
      await ApiService.instance.hostKickMember(
        targetMemberId: row.memberId,
        reason: reason.isEmpty ? null : reason,
      );
      _toast('${row.name} was kicked.');
    });
  }

  /// Runs an action with a busy guard and a generic error toast.
  Future<void> _run(Future<void> Function() action, {VoidCallback? onError}) async {
    if (_busy) {
      return;
    }
    setState(() => _busy = true);
    try {
      await action();
    } catch (_) {
      onError?.call();
      _toast('Something went wrong. Please try again.');
    } finally {
      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }

  Future<String?> _promptReason({
    required String title,
    required String subject,
    required String actionLabel,
    required bool destructive,
  }) {
    // The dialog owns its TextEditingController via _ReasonDialog's State so the
    // controller is disposed in dispose() — after the dialog's exit animation,
    // in correct element-teardown order. Disposing it here (e.g. in a finally
    // after `await showDialog`) would run while the TextField is still mounted,
    // since showDialog's future completes before the pop animation, and trips
    // the framework's `_dependents.isEmpty` assertion during teardown.
    return showDialog<String>(
      context: context,
      builder: (dialogContext) => _ReasonDialog(
        title: title,
        subject: subject,
        actionLabel: actionLabel,
        destructive: destructive,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      color: XActColors.bg,
      child: Column(
        children: [
          _buildHeader(),
          Expanded(child: _buildBody()),
        ],
      ),
    );
  }

  Widget _buildHeader() {
    return Container(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 14),
      decoration: BoxDecoration(
        border: Border(bottom: BorderSide(color: XActColors.hairlineSoft)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Report', style: XActText.heading.copyWith(fontSize: 17)),
          const SizedBox(height: 2),
          Text(
            _isHost
                ? 'Vote out rule-breakers, or use host powers to kick.'
                : 'Vote to remove players who break the rules.',
            style: XActText.caption.copyWith(fontSize: 12),
          ),
        ],
      ),
    );
  }

  Widget _buildBody() {
    if (_loading) {
      return const Center(
        child: CircularProgressIndicator(color: XActColors.secondary),
      );
    }

    if (_failed) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Text(
            'Could not load the report tool. Reopen this tab to retry.',
            style: XActText.bodySm.copyWith(color: XActColors.text3),
            textAlign: TextAlign.center,
          ),
        ),
      );
    }

    final flagged = _players
        .where((p) => _offensesByMember.containsKey(p.memberId))
        .toList(growable: false);

    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 14, 16, 20),
      children: [
        if (_vote != null) ...[
          _buildVoteCard(_vote!),
          const SizedBox(height: XActSpace.s5),
        ],
        if (flagged.isNotEmpty) ...[
          XActBranding.buildEyebrow('Flagged players'),
          const SizedBox(height: XActSpace.s2),
          for (final row in flagged) ...[
            _buildPlayerTile(row, flagged: true),
            const SizedBox(height: XActSpace.s2),
          ],
          const SizedBox(height: XActSpace.s4),
        ],
        XActBranding.buildEyebrow('All players'),
        const SizedBox(height: XActSpace.s2),
        if (_players.isEmpty)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 24),
            child: Text(
              'No players found.',
              style: XActText.bodySm.copyWith(color: XActColors.text3),
              textAlign: TextAlign.center,
            ),
          )
        else
          for (final row in _players) ...[
            _buildPlayerTile(row, flagged: false),
            const SizedBox(height: XActSpace.s2),
          ],
      ],
    );
  }

  Widget _buildVoteCard(KickVote vote) {
    final isTarget = vote.targetMemberId == _currentMemberId;
    final isInitiator = vote.initiatorMemberId == _currentMemberId;
    final alreadyVoted = _votedVoteIds.contains(vote.voteId);
    final secondsLeft = _secondsLeft(vote);

    return Container(
      padding: const EdgeInsets.all(XActSpace.s4),
      decoration: BoxDecoration(
        color: XActColors.surface,
        borderRadius: XActRadius.md,
        border: Border.all(color: XActColors.primary.withValues(alpha: .55)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.how_to_vote_rounded,
                  color: XActColors.primary, size: 18),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  'Kick vote: ${vote.targetName}',
                  style: XActText.subheading.copyWith(fontSize: 15),
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              Text(
                secondsLeft > 0 ? '${secondsLeft}s' : '—',
                style: XActText.caption.copyWith(color: XActColors.text2),
              ),
            ],
          ),
          const SizedBox(height: 6),
          _buildVoteWhy(vote),
          const SizedBox(height: XActSpace.s3),
          Row(
            children: [
              Expanded(
                child: Text(
                  '${vote.approveCount}/${vote.approvalsNeeded} to kick',
                  style: XActText.bodySm.copyWith(color: XActColors.text2),
                  overflow: TextOverflow.ellipsis,
                  maxLines: 1,
                ),
              ),
              const SizedBox(width: XActSpace.s2),
              Text(
                '${vote.rejectCount} against',
                style: XActText.caption.copyWith(fontSize: 12),
              ),
            ],
          ),
          const SizedBox(height: XActSpace.s3),
          _buildVoteActions(
            isTarget: isTarget,
            isInitiator: isInitiator,
            alreadyVoted: alreadyVoted,
          ),
        ],
      ),
    );
  }

  /// Explains why the vote is running: who started it, an out-of-bounds flag on
  /// the target (if any), and the typed reason (if any).
  Widget _buildVoteWhy(KickVote vote) {
    final targetOffense = vote.targetMemberId != null
        ? _offensesByMember[vote.targetMemberId]
        : null;
    final offenseLabel =
        targetOffense != null ? _offenseReasonLabel(targetOffense.type) : null;
    final reason = vote.reason?.trim();
    // Show the typed reason unless it just repeats the live offense chip above it.
    final showReason =
        reason != null && reason.isNotEmpty && reason != offenseLabel;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Started by ${vote.initiatorName}',
          style: XActText.caption.copyWith(fontSize: 12),
        ),
        if (offenseLabel != null) ...[
          const SizedBox(height: 5),
          Row(
            children: [
              const Icon(Icons.warning_amber_rounded,
                  color: XActColors.primary, size: 14),
              const SizedBox(width: 5),
              Expanded(
                child: Text(
                  offenseLabel,
                  style: XActText.bodySm
                      .copyWith(color: XActColors.primary, fontSize: 12.5),
                ),
              ),
            ],
          ),
        ],
        if (showReason) ...[
          const SizedBox(height: 5),
          Text(
            '“$reason”',
            style: XActText.bodySm
                .copyWith(color: XActColors.text2, fontSize: 12.5),
          ),
        ],
        if (offenseLabel == null && !showReason) ...[
          const SizedBox(height: 5),
          Text(
            'No reason given.',
            style: XActText.caption.copyWith(fontSize: 12, color: XActColors.text4),
          ),
        ],
      ],
    );
  }

  String _offenseReasonLabel(OffenseType? type) => type == OffenseType.outOfBounds
      ? 'Outside the game area'
      : offenseTypeLabel(type);

  Widget _buildVoteActions({
    required bool isTarget,
    required bool isInitiator,
    required bool alreadyVoted,
  }) {
    if (isTarget) {
      return Container(
        width: double.infinity,
        padding: const EdgeInsets.symmetric(vertical: 10, horizontal: 12),
        decoration: BoxDecoration(
          color: XActColors.primary.withValues(alpha: .12),
          borderRadius: XActRadius.sm,
        ),
        child: Text(
          'You are being voted out — get back in the game!',
          style: XActText.bodySm.copyWith(color: XActColors.primary, fontSize: 12.5),
        ),
      );
    }

    // The initiator already approved by starting the vote, so they only see the
    // host/initiator "Cancel vote" control, not a ballot.
    final showBallot = !isInitiator;
    final canCancel = isInitiator || _isHost;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (showBallot)
          Row(
            children: [
              Expanded(
                child: _miniButton(
                  label: alreadyVoted ? 'Voted' : 'Approve',
                  icon: Icons.check_rounded,
                  color: XActColors.success,
                  onTap: (alreadyVoted || _busy) ? null : () => _castBallot(true),
                ),
              ),
              const SizedBox(width: XActSpace.s2),
              Expanded(
                child: _miniButton(
                  label: 'Keep',
                  icon: Icons.shield_outlined,
                  color: XActColors.text2,
                  onTap: (alreadyVoted || _busy) ? null : () => _castBallot(false),
                ),
              ),
            ],
          ),
        if (isInitiator)
          Text(
            'You started this vote.',
            style: XActText.caption.copyWith(fontSize: 12, color: XActColors.text3),
          ),
        if (canCancel) ...[
          if (showBallot || isInitiator) const SizedBox(height: XActSpace.s2),
          XActBranding.buildGhostButton(
            text: 'Cancel vote',
            icon: Icons.stop_rounded,
            height: 44,
            foreground: XActColors.text1,
            onPressed: _busy ? null : _cancelVote,
          ),
        ],
      ],
    );
  }

  Widget _buildPlayerTile(_PlayerRow row, {required bool flagged}) {
    final offense = _offensesByMember[row.memberId];
    final canVote = !row.isSelf && !row.isHost && !_hasOpenVote;
    final canHostKick = _isHost && !row.isSelf && !row.isHost;

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: XActSpace.s3,
        vertical: XActSpace.s3,
      ),
      decoration: BoxDecoration(
        color: XActColors.surface,
        borderRadius: XActRadius.md,
        border: Border.all(
          color: flagged
              ? XActColors.primary.withValues(alpha: .45)
              : XActColors.hairlineSoft,
        ),
      ),
      child: Row(
        children: [
          Container(
            width: 10,
            height: 10,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              color: row.teamColor,
            ),
          ),
          const SizedBox(width: XActSpace.s3),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        row.name,
                        style: XActText.bodySm,
                        overflow: TextOverflow.ellipsis,
                        maxLines: 1,
                      ),
                    ),
                    // The viewer's own row reads "You"; everyone else sees the
                    // host's row marked "Host". Showing at most one tag keeps the
                    // line from overflowing on narrow layouts.
                    if (row.isSelf)
                      _tag('You', XActColors.secondary)
                    else if (row.isHost)
                      _tag('Host', XActColors.warning),
                  ],
                ),
                if (offense != null) ...[
                  const SizedBox(height: 3),
                  Row(
                    children: [
                      const Icon(Icons.warning_amber_rounded,
                          color: XActColors.primary, size: 13),
                      const SizedBox(width: 4),
                      Expanded(
                        child: Text(
                          offenseTypeLabel(offense.type),
                          style: XActText.caption.copyWith(
                            color: XActColors.primary,
                            fontSize: 11,
                          ),
                          overflow: TextOverflow.ellipsis,
                          maxLines: 1,
                        ),
                      ),
                    ],
                  ),
                ],
              ],
            ),
          ),
          if (canVote)
            _miniButton(
              label: 'Vote',
              icon: Icons.how_to_vote_rounded,
              color: XActColors.secondary,
              compact: true,
              onTap: _busy ? null : () => _startVote(row),
            ),
          if (canHostKick) ...[
            const SizedBox(width: XActSpace.s2),
            _miniButton(
              label: 'Kick',
              icon: Icons.gavel_rounded,
              color: XActColors.primary,
              compact: true,
              onTap: _busy ? null : () => _hostKick(row),
            ),
          ],
        ],
      ),
    );
  }

  Widget _tag(String label, Color color) {
    return Padding(
      padding: const EdgeInsets.only(left: 6),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 2),
        decoration: BoxDecoration(
          color: color.withValues(alpha: .16),
          borderRadius: XActRadius.pill,
        ),
        child: Text(
          label,
          style: XActText.caption.copyWith(
            color: color,
            fontSize: 10,
            fontWeight: FontWeight.w700,
          ),
        ),
      ),
    );
  }

  Widget _miniButton({
    required String label,
    required IconData icon,
    required Color color,
    required VoidCallback? onTap,
    bool compact = false,
  }) {
    final enabled = onTap != null;
    return Material(
      color: enabled ? color.withValues(alpha: .16) : Colors.white.withValues(alpha: .03),
      borderRadius: XActRadius.sm,
      child: InkWell(
        onTap: onTap,
        borderRadius: XActRadius.sm,
        child: Padding(
          padding: EdgeInsets.symmetric(
            horizontal: compact ? 10 : 12,
            vertical: compact ? 7 : 10,
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(icon, size: 15, color: enabled ? color : XActColors.text4),
              const SizedBox(width: 6),
              Text(
                label,
                style: XActText.caption.copyWith(
                  color: enabled ? color : XActColors.text4,
                  fontSize: 12,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  int _secondsLeft(KickVote vote) {
    final expiresAt = vote.expiresAt;
    if (expiresAt == null) {
      return 0;
    }
    final diff = expiresAt.toUtc().difference(DateTime.now().toUtc()).inSeconds;
    return diff > 0 ? diff : 0;
  }
}

/// Reason-prompt dialog used for starting a kick vote / host kick. Owns its own
/// [TextEditingController] so it is disposed in [State.dispose] (after the exit
/// animation), avoiding a premature dispose that corrupts the element tree.
class _ReasonDialog extends StatefulWidget {
  const _ReasonDialog({
    required this.title,
    required this.subject,
    required this.actionLabel,
    required this.destructive,
  });

  final String title;
  final String subject;
  final String actionLabel;
  final bool destructive;

  @override
  State<_ReasonDialog> createState() => _ReasonDialogState();
}

class _ReasonDialogState extends State<_ReasonDialog> {
  final TextEditingController _controller = TextEditingController();

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      backgroundColor: XActColors.surface,
      shape: const RoundedRectangleBorder(borderRadius: XActRadius.lg),
      title: Text(widget.title, style: XActText.heading),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            widget.subject,
            style: XActText.subheading.copyWith(
              color:
                  widget.destructive ? XActColors.primary : XActColors.secondary,
            ),
          ),
          const SizedBox(height: XActSpace.s4),
          TextField(
            controller: _controller,
            maxLength: 200,
            style: XActText.bodySm,
            cursorColor: XActColors.secondary,
            decoration: InputDecoration(
              hintText: 'Reason (optional)',
              hintStyle: XActText.bodySm.copyWith(color: XActColors.text4),
              counterText: '',
              filled: true,
              fillColor: Colors.white.withValues(alpha: .03),
              border: const OutlineInputBorder(
                borderRadius: XActRadius.md,
              ),
            ),
          ),
        ],
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: Text(
            'Cancel',
            style: XActText.bodySm.copyWith(color: XActColors.text3),
          ),
        ),
        TextButton(
          onPressed: () => Navigator.of(context).pop(_controller.text.trim()),
          child: Text(
            widget.actionLabel,
            style: XActText.bodySm.copyWith(
              color:
                  widget.destructive ? XActColors.primary : XActColors.secondary,
              fontWeight: FontWeight.w700,
            ),
          ),
        ),
      ],
    );
  }
}
