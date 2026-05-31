import 'dart:async';

import 'package:flutter/material.dart';

import '../api/api_service.dart';
import '../api/models.dart';
import '../services/app_session.dart';
import '../widgets/team/team_card.dart';
import '../widgets/xact_branding.dart';

class TeamScreen extends StatefulWidget {
  const TeamScreen({super.key});

  @override
  State<TeamScreen> createState() => _TeamScreenState();
}

class _TeamScreenState extends State<TeamScreen> {
  late Future<List<TeamCardData>> _load;
  StreamSubscription<RealtimeEventEnvelope>? _realtimeEventSub;
  Timer? _refreshDebounce;
  bool _markingCaught = false;

  @override
  void initState() {
    super.initState();
    _load = ApiService.instance.loadTeamCards();
    _initRealtime();
  }

  @override
  void dispose() {
    _realtimeEventSub?.cancel();
    _refreshDebounce?.cancel();
    super.dispose();
  }

  Future<void> _initRealtime() async {
    final sessionId = AppSession.instance.currentSessionId;
    if (sessionId == null) {
      return;
    }

    try {
      await ApiService.instance.ensureRealtimeSessionSubscription(sessionId);
      _realtimeEventSub = ApiService.instance.realtimeEvents.listen((event) {
        if (_isTeamRealtimeEvent(event.type)) {
          _queueReload();
        }
      });
    } catch (_) {
      // Screen still shows the initial load if realtime is unavailable.
    }
  }

  bool _isTeamRealtimeEvent(String type) {
    // A Mr. X catch arrives as two team_updated events (the role swap); reloading
    // on these keeps the button visibility and team labels in sync for everyone.
    return type == RealtimeEvents.teamAdded ||
        type == RealtimeEvents.teamUpdated ||
        type == RealtimeEvents.teamDeleted ||
        type == RealtimeEvents.teamMemberJoined ||
        type == RealtimeEvents.teamMemberUpdated ||
        type == RealtimeEvents.teamMemberLeft;
  }

  void _queueReload() {
    _refreshDebounce?.cancel();
    _refreshDebounce = Timer(const Duration(milliseconds: 250), () {
      if (!mounted) {
        return;
      }
      setState(() {
        _load = ApiService.instance.loadTeamCards();
      });
    });
  }

  bool _isCurrentTeamMrX(List<TeamCardData> teams) {
    final currentTeamId = AppSession.instance.currentTeamId;
    if (currentTeamId == null) {
      return false;
    }
    return teams.any(
      (team) => team.teamId == currentTeamId && team.role == TeamRole.mrX,
    );
  }

  Future<void> _showCaughtDialog(List<TeamCardData> teams) async {
    final detectiveTeams = teams
        .where((team) => team.role == TeamRole.detective)
        .toList(growable: false);

    if (detectiveTeams.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('No detective team to hand Mister X over to.')),
      );
      return;
    }

    final selected = await showDialog<TeamCardData>(
      context: context,
      builder: (dialogContext) {
        return Dialog(
          backgroundColor: XActColors.surface,
          shape: const RoundedRectangleBorder(borderRadius: XActRadius.lg),
          child: Padding(
            padding: const EdgeInsets.all(XActSpace.s5),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Who caught you?', style: XActText.heading),
                const SizedBox(height: XActSpace.s2),
                Text(
                  'Hand over the Mister X role to the detective team that caught you.',
                  style: XActText.caption.copyWith(color: XActColors.text3),
                ),
                const SizedBox(height: XActSpace.s4),
                for (final team in detectiveTeams) ...[
                  XActBranding.buildActionCard(
                    icon: Icons.groups_rounded,
                    title: team.teamName,
                    subtitle: '${team.members.length} player(s)',
                    onTap: () => Navigator.pop(dialogContext, team),
                  ),
                  const SizedBox(height: XActSpace.s2),
                ],
                const SizedBox(height: XActSpace.s2),
                XActBranding.buildCancelButton(
                  text: 'Cancel',
                  onPressed: () => Navigator.pop(dialogContext),
                ),
              ],
            ),
          ),
        );
      },
    );

    if (selected == null) {
      return;
    }

    await _markCaught(selected);
  }

  Future<void> _markCaught(TeamCardData catchingTeam) async {
    if (_markingCaught) {
      return;
    }

    final sessionId = AppSession.instance.currentSessionId;
    if (sessionId == null) {
      return;
    }

    setState(() => _markingCaught = true);
    try {
      await ApiService.instance.markMrXCaught(
        sessionId: sessionId,
        catchingTeamId: catchingTeam.teamId,
      );
      // The resulting role swap arrives via realtime team_updated events, which
      // reload this screen (and flip the map view) automatically.
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Failed to hand over Mister X: $error')),
        );
      }
    } finally {
      if (mounted) {
        setState(() => _markingCaught = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      color: XActColors.bg,
      child: FutureBuilder<List<TeamCardData>>(
        future: _load,
        builder: (context, snapshot) {
          if (snapshot.hasError) {
            return Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      'Failed to load teams',
                      style: XActText.subheading,
                      textAlign: TextAlign.center,
                    ),
                    const SizedBox(height: 8),
                    Text(
                      snapshot.error.toString(),
                      style: XActText.caption.copyWith(
                        color: XActColors.text3,
                        fontSize: 13,
                      ),
                      textAlign: TextAlign.center,
                    ),
                  ],
                ),
              ),
            );
          }

          final teams = snapshot.data;
          if (teams == null) {
            return const Center(
              child: Padding(
                padding: EdgeInsets.all(24),
                child: CircularProgressIndicator(color: XActColors.secondary),
              ),
            );
          }

          final visibleTeams = teams
              .where((team) => team.members.isNotEmpty)
              .toList(growable: false);

          final showCaughtButton = _isCurrentTeamMrX(teams);

          return Column(
            children: [
              if (showCaughtButton)
                Padding(
                  padding: const EdgeInsets.fromLTRB(16, 16, 16, 0),
                  child: XActBranding.buildPrimaryButton(
                    text: _markingCaught ? 'Handing over…' : "I've been caught",
                    icon: Icons.pan_tool_rounded,
                    height: 52,
                    onPressed:
                        _markingCaught ? null : () => _showCaughtDialog(teams),
                  ),
                ),
              Expanded(
                child: visibleTeams.isEmpty
                    ? Center(
                        child: Padding(
                          padding: const EdgeInsets.all(24),
                          child: Text(
                            'No teams found',
                            style: XActText.bodySm
                                .copyWith(color: XActColors.text3),
                          ),
                        ),
                      )
                    : ListView.separated(
                        padding: const EdgeInsets.all(16),
                        itemCount: visibleTeams.length,
                        separatorBuilder: (context, index) =>
                            const SizedBox(height: 12),
                        itemBuilder: (context, index) {
                          final team = visibleTeams[index];
                          return TeamCard(
                            teamName: team.teamName,
                            role: team.role,
                            color: team.color,
                            members: team.members,
                            isMisterX: team.isMisterX,
                          );
                        },
                      ),
              ),
            ],
          );
        },
      ),
    );
  }
}
