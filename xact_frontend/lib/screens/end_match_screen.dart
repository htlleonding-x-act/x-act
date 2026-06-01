import 'package:flutter/material.dart';
import 'package:xact_frontend/api/api_service.dart';
import 'package:xact_frontend/api/models.dart';
import 'package:xact_frontend/screens/start/start_screen.dart';
import 'package:xact_frontend/screens/team/team_lobby.dart';
import 'package:xact_frontend/services/app_session.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class EndMatchScreen extends StatefulWidget {
  final int sessionId;

  const EndMatchScreen({super.key, required this.sessionId});

  @override
  State<EndMatchScreen> createState() => _EndMatchScreenState();
}

class _EndMatchScreenState extends State<EndMatchScreen>
    with SingleTickerProviderStateMixin {
  bool _loading = true;
  bool _working = false;
  GameSessionDetails? _sessionDetails;
  LobbySnapshot? _snapshot;
  late final TabController _tabController;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 3, vsync: this);
    _loadSummary();
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  Future<void> _loadSummary() async {
    try {
      final results = await Future.wait<Object?>([
        ApiService.instance.getGameSession(widget.sessionId),
        ApiService.instance.loadLobbySnapshot(widget.sessionId),
      ]);

      if (!mounted) {
        return;
      }

      setState(() {
        _sessionDetails = results[0] as GameSessionDetails;
        _snapshot = results[1] as LobbySnapshot;
        _loading = false;
      });
    } catch (_) {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  String? get _winnerTeamName {
    final snapshot = _snapshot;
    if (snapshot == null) {
      return null;
    }

    final playableTeams = snapshot.teams
        .where((team) => team.role != TeamRole.spectator)
        .toList(growable: false);
    if (playableTeams.isEmpty) {
      return null;
    }

    final mrXTeam = playableTeams
        .where((team) => team.role == TeamRole.mrX)
        .toList(growable: false);
    if (mrXTeam.length == 1 && !mrXTeam.first.isCaught) {
      return mrXTeam.first.teamName;
    }

    if (playableTeams.length == 1) {
      return playableTeams.first.teamName;
    }

    return null;
  }

  String? get _matchDurationText {
    final details = _sessionDetails;
    final startTime = details?.startTime;
    final endTime = details?.endTime;

    if (startTime == null || endTime == null) {
      return null;
    }

    final duration = endTime.difference(startTime);
    if (duration.isNegative) {
      return null;
    }

    return _formatDuration(duration);
  }

  String get _summaryText {
    final details = _sessionDetails;
    final winner = _winnerTeamName;
    final duration = _matchDurationText;

    if (details?.status == SessionStatus.finished &&
        winner != null &&
        duration != null) {
      return 'The match has ended with a recorded result.';
    }

    if (winner != null || duration != null) {
      return 'Partial match summary available.';
    }

    return 'No detailed match summary is available yet.';
  }

  int get _teamCount {
    final snapshot = _snapshot;
    if (snapshot == null) {
      return 0;
    }

    return snapshot.teams
        .where((team) => team.role != TeamRole.spectator)
        .length;
  }

  int get _playerCount {
    final snapshot = _snapshot;
    if (snapshot == null) {
      return 0;
    }

    return snapshot.membersByTeamId.values.fold<int>(
      0,
      (sum, members) => sum + members.length,
    );
  }

  int get _caughtTeamCount {
    final snapshot = _snapshot;
    if (snapshot == null) {
      return 0;
    }

    return snapshot.teams
        .where((team) => team.role != TeamRole.spectator && team.isCaught)
        .length;
  }

  List<TeamDetails> get _playableTeams {
    final snapshot = _snapshot;
    if (snapshot == null) {
      return const [];
    }

    return snapshot.teams
        .where((team) => team.role != TeamRole.spectator)
        .toList(growable: false);
  }

  String _formatDuration(Duration duration) {
    final totalMinutes = duration.inMinutes;
    final hours = totalMinutes ~/ 60;
    final minutes = totalMinutes % 60;

    if (hours > 0 && minutes > 0) {
      return '${hours}h ${minutes}m';
    }

    if (hours > 0) {
      return '${hours}h';
    }

    if (minutes > 0) {
      return '${minutes}m';
    }

    return '< 1m';
  }

  String _formatDateTime(DateTime? value) {
    if (value == null) {
      return 'Not available';
    }

    final local = value.toLocal();
    final day = local.day.toString().padLeft(2, '0');
    final month = local.month.toString().padLeft(2, '0');
    final year = local.year;
    final hour = local.hour.toString().padLeft(2, '0');
    final minute = local.minute.toString().padLeft(2, '0');
    return '$day.$month.$year $hour:$minute';
  }

  String _roleLabel(TeamRole? role) => switch (role) {
    TeamRole.mrX => 'Mister X',
    TeamRole.detective => 'Detective',
    TeamRole.spectator => 'Spectator',
    null => 'Team',
  };

  Color _roleColor(TeamRole? role) => XActColors.roleColor(role);

  Future<void> _backToLobby() async {
    setState(() => _working = true);
    try {
      final details =
          _sessionDetails ??
          await ApiService.instance.getGameSession(widget.sessionId);
      if (!mounted) {
        return;
      }

      final currentUserId = AppSession.instance.currentUserId;
      await Navigator.of(context).pushReplacement(
        MaterialPageRoute(
          builder: (_) => GameLobbyScreen(
            sessionId: widget.sessionId,
            gameCode: details.joinCode,
            gameName: details.sessionName,
            isLeader:
                currentUserId != null && currentUserId == details.hostUserId,
          ),
        ),
      );
    } catch (error) {
      if (!mounted) {
        return;
      }

      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Could not reopen lobby: $error')));
    } finally {
      if (mounted) {
        setState(() => _working = false);
      }
    }
  }

  Future<void> _leaveLobby() async {
    setState(() => _working = true);
    try {
      await ApiService.instance.closeCurrentSession();
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Could not leave lobby cleanly: $error')),
        );
      }
    }

    if (!mounted) {
      return;
    }

    await Navigator.of(context).pushAndRemoveUntil(
      MaterialPageRoute(builder: (_) => const StartScreen()),
      (route) => false,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: XActColors.bg,
      body: Stack(
        children: [
          Positioned.fill(child: XActBranding.aurora()),
          SafeArea(
            child: LayoutBuilder(
              builder: (context, constraints) {
                final contentWidth = constraints.maxWidth < 1080
                    ? constraints.maxWidth
                    : 1080.0;

                return Center(
                  child: ConstrainedBox(
                    constraints: BoxConstraints(maxWidth: contentWidth),
                    child: Column(
                      children: [
                        Padding(
                          padding: const EdgeInsets.fromLTRB(
                            XActSpace.s4,
                            XActSpace.s3,
                            XActSpace.s4,
                            0,
                          ),
                          child: XActBranding.buildTopBar(
                            context: context,
                            eyebrow: 'Match result',
                            title: 'Match beendet',
                            showBack: false,
                          ),
                        ),
                        Padding(
                          padding: const EdgeInsets.fromLTRB(
                            XActSpace.s4,
                            XActSpace.s2,
                            XActSpace.s4,
                            XActSpace.s4,
                          ),
                          child: _buildHeaderCard(),
                        ),
                        Padding(
                          padding: const EdgeInsets.symmetric(
                            horizontal: XActSpace.s4,
                          ),
                          child: _buildTabBar(),
                        ),
                        const SizedBox(height: XActSpace.s3),
                        Expanded(
                          child: TabBarView(
                            controller: _tabController,
                            children: [
                              _buildOverviewTab(),
                              _buildTeamsTab(),
                              _buildSessionTab(),
                            ],
                          ),
                        ),
                        _buildActionArea(),
                      ],
                    ),
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildHeaderCard() {
    return XActBranding.buildFormCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Container(
                width: 56,
                height: 56,
                decoration: BoxDecoration(
                  color: XActColors.primarySoft,
                  borderRadius: XActRadius.md,
                  border: Border.all(color: XActColors.hairlineSoft),
                ),
                child: const Icon(
                  Icons.flag_rounded,
                  color: XActColors.primary,
                  size: 30,
                ),
              ),
              const SizedBox(width: XActSpace.s4),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Match beendet',
                      style: XActText.displaySm.copyWith(fontSize: 30),
                    ),
                    const SizedBox(height: XActSpace.s1),
                    Text(
                      _summaryText,
                      style: XActText.body.copyWith(color: XActColors.text2),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: XActSpace.s4),
          if (_loading)
            const Padding(
              padding: EdgeInsets.symmetric(vertical: XActSpace.s4),
              child: Center(child: CircularProgressIndicator()),
            )
          else ...[
            Row(
              children: [
                Expanded(
                  child: _MiniStatCard(
                    label: 'Winner',
                    value: _winnerTeamName ?? 'Not available',
                    accent: _roleColor(TeamRole.mrX),
                  ),
                ),
                const SizedBox(width: XActSpace.s3),
                Expanded(
                  child: _MiniStatCard(
                    label: 'Duration',
                    value: _matchDurationText ?? 'Not available',
                    accent: XActColors.secondary,
                  ),
                ),
              ],
            ),
            const SizedBox(height: XActSpace.s3),
            Row(
              children: [
                Expanded(
                  child: _MiniStatCard(
                    label: 'Teams',
                    value: '$_teamCount',
                    accent: XActColors.success,
                  ),
                ),
                const SizedBox(width: XActSpace.s3),
                Expanded(
                  child: _MiniStatCard(
                    label: 'Players',
                    value: '$_playerCount',
                    accent: XActColors.warning,
                  ),
                ),
              ],
            ),
            const SizedBox(height: XActSpace.s3),
            _DetailRow(
              label: 'Status',
              value: _sessionDetails?.status == SessionStatus.finished
                  ? 'Finished'
                  : 'Summary loaded',
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildTabBar() {
    return Container(
      padding: const EdgeInsets.all(XActSpace.s1),
      decoration: BoxDecoration(
        color: XActColors.surface,
        borderRadius: XActRadius.lg,
        border: Border.all(color: XActColors.hairlineSoft),
        boxShadow: XActElevation.e1,
      ),
      child: TabBar(
        controller: _tabController,
        isScrollable: false,
        indicatorSize: TabBarIndicatorSize.tab,
        labelPadding: const EdgeInsets.symmetric(
          horizontal: XActSpace.s3,
          vertical: XActSpace.s2,
        ),
        indicatorPadding: const EdgeInsets.all(2),
        labelColor: Colors.white,
        unselectedLabelColor: XActColors.text3,
        indicator: BoxDecoration(
          borderRadius: XActRadius.md,
          gradient: const LinearGradient(
            begin: Alignment.topCenter,
            end: Alignment.bottomCenter,
            colors: [XActColors.secondaryLight, XActColors.secondaryDark],
          ),
        ),
        dividerColor: Colors.transparent,
        labelStyle: XActText.bodySm.copyWith(fontWeight: FontWeight.w700),
        unselectedLabelStyle: XActText.bodySm.copyWith(
          fontWeight: FontWeight.w600,
        ),
        tabs: const [
          Tab(text: 'Overview'),
          Tab(text: 'Teams'),
          Tab(text: 'Session'),
        ],
      ),
    );
  }

  Widget _buildOverviewTab() {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }

    final snapshot = _snapshot;
    final details = _sessionDetails;

    return ListView(
      padding: const EdgeInsets.fromLTRB(
        XActSpace.s4,
        XActSpace.s1,
        XActSpace.s4,
        XActSpace.s5,
      ),
      children: [
        _SectionCard(
          title: 'Result snapshot',
          subtitle: 'A compact recap using the current session data.',
          child: Column(
            children: [
              _DetailRow(
                label: 'Winning Team',
                value: _winnerTeamName ?? 'Not available',
              ),
              const SizedBox(height: 12),
              _DetailRow(
                label: 'Match Duration',
                value: _matchDurationText ?? 'Not available',
              ),
              const SizedBox(height: 12),
              _DetailRow(
                label: 'Planned Duration',
                value: '${details?.plannedDurationMinutes ?? 0} min',
              ),
              const SizedBox(height: XActSpace.s3),
              _DetailRow(
                label: 'Reveal Interval',
                value: '${details?.mrXRevealInterval ?? 0} min',
              ),
            ],
          ),
        ),
        const SizedBox(height: XActSpace.s3),
        _SectionCard(
          title: 'Status summary',
          subtitle: 'Useful end-of-match signals from the backend.',
          child: Column(
            children: [
              _DetailRow(
                label: 'Session Status',
                value: _sessionDetails?.status?.name ?? 'Unknown',
              ),
              const SizedBox(height: XActSpace.s3),
              _DetailRow(label: 'Players in Session', value: '$_playerCount'),
              const SizedBox(height: XActSpace.s3),
              _DetailRow(
                label: 'Latest Location Points',
                value: '${snapshot?.latestLocations.length ?? 0}',
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildTeamsTab() {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }

    final teams = _playableTeams;
    if (teams.isEmpty) {
      return Center(
        child: Text(
          'No team data available',
          style: XActText.bodySm.copyWith(color: XActColors.text3),
        ),
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.fromLTRB(
        XActSpace.s4,
        XActSpace.s1,
        XActSpace.s4,
        XActSpace.s5,
      ),
      itemCount: teams.length,
      separatorBuilder: (context, index) =>
          const SizedBox(height: XActSpace.s3),
      itemBuilder: (context, index) {
        final team = teams[index];
        final members = _snapshot!.membersByTeamId[team.teamId] ?? const [];

        return _SectionCard(
          title: team.teamName,
          subtitle: _roleLabel(team.role),
          leadingColor: _roleColor(team.role),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  _TagChip(
                    text: team.isCaught ? 'Caught' : 'Active',
                    color: team.isCaught
                        ? XActColors.primary
                        : XActColors.success,
                  ),
                  const SizedBox(width: 8),
                  _TagChip(
                    text: '${members.length} players',
                    color: XActColors.secondary,
                  ),
                ],
              ),
              if (members.isNotEmpty) ...[
                const SizedBox(height: XActSpace.s3),
                Wrap(
                  spacing: XActSpace.s2,
                  runSpacing: XActSpace.s2,
                  children: [
                    for (final member in members)
                      _TagChip(
                        text: _memberLabel(member),
                        color: member.isTeamLeader
                            ? XActColors.warning
                            : XActColors.text4,
                      ),
                  ],
                ),
              ] else ...[
                const SizedBox(height: XActSpace.s3),
                Text(
                  'No players assigned to this team.',
                  style: XActText.caption.copyWith(color: XActColors.text4),
                ),
              ],
            ],
          ),
        );
      },
    );
  }

  Widget _buildSessionTab() {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }

    final details = _sessionDetails;
    return ListView(
      padding: const EdgeInsets.fromLTRB(
        XActSpace.s4,
        XActSpace.s1,
        XActSpace.s4,
        XActSpace.s5,
      ),
      children: [
        _SectionCard(
          title: 'Match timing',
          subtitle:
              'The backend only provides the session timing fields below.',
          child: Column(
            children: [
              _DetailRow(
                label: 'Session Name',
                value: details?.sessionName ?? 'Not available',
              ),
              const SizedBox(height: 12),
              _DetailRow(
                label: 'Session ID',
                value: '${details?.sessionId ?? widget.sessionId}',
              ),
              const SizedBox(height: XActSpace.s3),
              _DetailRow(
                label: 'Start Time',
                value: _formatDateTime(details?.startTime),
              ),
              const SizedBox(height: XActSpace.s3),
              _DetailRow(
                label: 'End Time',
                value: _formatDateTime(details?.endTime),
              ),
              const SizedBox(height: XActSpace.s3),
              _DetailRow(
                label: 'Match Duration',
                value: _matchDurationText ?? 'Not available',
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        _SectionCard(
          title: 'Match configuration',
          subtitle: 'Useful settings already known at end of play.',
          child: Column(
            children: [
              _DetailRow(
                label: 'Planned Duration',
                value: '${details?.plannedDurationMinutes ?? 0} min',
              ),
              const SizedBox(height: XActSpace.s3),
              _DetailRow(
                label: 'Reveal Interval',
                value: '${details?.mrXRevealInterval ?? 0} min',
              ),
              const SizedBox(height: XActSpace.s3),
              _DetailRow(label: 'Caught Teams', value: '$_caughtTeamCount'),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildActionArea() {
    return Container(
      width: double.infinity,
      decoration: BoxDecoration(
        color: XActColors.bg2.withValues(alpha: .96),
        border: Border(top: BorderSide(color: XActColors.hairlineSoft)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: .35),
            blurRadius: 24,
            offset: const Offset(0, -8),
          ),
        ],
      ),
      child: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(
            XActSpace.s4,
            XActSpace.s4,
            XActSpace.s4,
            XActSpace.s4,
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (_working) ...[
                const Padding(
                  padding: EdgeInsets.only(bottom: XActSpace.s3),
                  child: LinearProgressIndicator(
                    minHeight: 3,
                    backgroundColor: Colors.transparent,
                    color: XActColors.secondary,
                  ),
                ),
              ],
              Text(
                'Session actions',
                style: XActText.caption.copyWith(color: XActColors.text4),
              ),
              const SizedBox(height: XActSpace.s2),
              XActBranding.buildSecondaryButton(
                text: 'Back to Lobby',
                icon: Icons.meeting_room_rounded,
                onPressed: _working ? null : _backToLobby,
                height: 54,
              ),
              const SizedBox(height: XActSpace.s3),
              XActBranding.buildGhostButton(
                text: 'Leave Lobby',
                icon: Icons.logout_rounded,
                onPressed: _working ? null : _leaveLobby,
                height: 54,
              ),
            ],
          ),
        ),
      ),
    );
  }

  String _memberLabel(TeamMemberDetails member) {
    if (member.guestName != null && member.guestName!.trim().isNotEmpty) {
      return member.guestName!.trim();
    }

    if (member.userId != null) {
      return 'User ${member.userId}';
    }

    return 'Player ${member.memberId}';
  }
}

class _DetailRow extends StatelessWidget {
  final String label;
  final String value;

  const _DetailRow({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: XActColors.surface2,
        borderRadius: XActRadius.md,
        border: Border.all(color: XActColors.hairlineSoft),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            label,
            style: XActText.caption.copyWith(color: XActColors.text4),
          ),
          const SizedBox(height: 6),
          Text(
            value,
            style: XActText.body.copyWith(fontWeight: FontWeight.w600),
          ),
        ],
      ),
    );
  }
}

class _MiniStatCard extends StatelessWidget {
  final String label;
  final String value;
  final Color accent;

  const _MiniStatCard({
    required this.label,
    required this.value,
    required this.accent,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      constraints: const BoxConstraints(minHeight: 92),
      padding: const EdgeInsets.all(XActSpace.s4),
      decoration: BoxDecoration(
        color: XActColors.surface2,
        borderRadius: XActRadius.lg,
        border: Border.all(color: accent.withValues(alpha: .45)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Text(
            label,
            style: XActText.caption.copyWith(color: XActColors.text4),
          ),
          const SizedBox(height: XActSpace.s2),
          Text(
            value,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: XActText.body.copyWith(
              fontWeight: FontWeight.w700,
              color: accent,
            ),
          ),
        ],
      ),
    );
  }
}

class _SectionCard extends StatelessWidget {
  final String title;
  final String subtitle;
  final Widget child;
  final Color? leadingColor;

  const _SectionCard({
    required this.title,
    required this.subtitle,
    required this.child,
    this.leadingColor,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(XActSpace.s4),
      decoration: BoxDecoration(
        color: XActColors.surface,
        borderRadius: XActRadius.xl,
        border: Border.all(color: XActColors.hairlineSoft),
        boxShadow: XActElevation.e1,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 10,
                height: 10,
                margin: const EdgeInsets.only(top: 6, right: XActSpace.s2),
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: leadingColor ?? XActColors.secondary,
                ),
              ),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(title, style: XActText.heading),
                    const SizedBox(height: XActSpace.s1),
                    Text(
                      subtitle,
                      style: XActText.caption.copyWith(color: XActColors.text3),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          child,
        ],
      ),
    );
  }
}

class _TagChip extends StatelessWidget {
  final String text;
  final Color color;

  const _TagChip({required this.text, required this.color});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: XActSpace.s3,
        vertical: XActSpace.s2,
      ),
      decoration: BoxDecoration(
        color: color.withValues(alpha: .14),
        borderRadius: XActRadius.pill,
        border: Border.all(color: color.withValues(alpha: .35)),
      ),
      child: Text(
        text,
        style: XActText.caption.copyWith(
          color: color,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}
