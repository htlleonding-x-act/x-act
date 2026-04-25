import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:xact_frontend/api/api_service.dart';
import 'package:xact_frontend/api/models.dart';
import 'package:xact_frontend/screens/game_screen.dart';
import 'package:xact_frontend/screens/team/add_team.dart';
import 'package:xact_frontend/services/app_session.dart';
import 'package:xact_frontend/services/game_start_transition_service.dart';
import 'package:xact_frontend/widgets/team/add_team_button.dart';
import 'package:xact_frontend/widgets/team/lobby_bottom_buttons.dart';
import 'package:xact_frontend/widgets/team/lobby_code_card.dart';
import 'package:xact_frontend/widgets/team/lobby_header.dart';
import 'package:xact_frontend/widgets/team/lobby_team_card.dart';
import 'package:xact_frontend/widgets/team/spectators_card.dart';
import 'package:xact_frontend/widgets/team/team_data.dart';
import 'package:xact_frontend/widgets/team/team_overview_card.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class GameLobbyScreen extends StatefulWidget {
  final int sessionId;
  final String gameCode;
  final String gameName;
  final bool isLeader;

  const GameLobbyScreen({
    super.key,
    required this.sessionId,
    required this.gameCode,
    required this.gameName,
    required this.isLeader,
  });

  @override
  State<GameLobbyScreen> createState() => _GameLobbyScreenState();
}

class _GameLobbyScreenState extends State<GameLobbyScreen> {
  bool _loading = true;
  bool _working = false;
  bool _gameTransitionStarted = false;

  StreamSubscription<RealtimeEventEnvelope>? _realtimeEventSub;
  StreamSubscription<GameSessionSnapshot>? _realtimeSnapshotSub;
  Timer? _realtimeRefreshDebounce;

  List<LobbyPlayer> _spectators = [];
  List<TeamData> _teams = [];
  int? _spectatorTeamId;
  final Map<int, _TeamUiConfig> _teamUiConfigById = {};

  bool isLobbyLeader() => widget.isLeader;

  bool get _canStartGame {
    final hasMisterX = _teams.any((t) => t.isMisterX && t.players.isNotEmpty);
    final hasDetective = _teams.any(
      (t) => !t.isMisterX && t.players.isNotEmpty,
    );
    return hasMisterX && hasDetective;
  }

  int get _totalPlayers =>
      _spectators.length +
      _teams.fold<int>(0, (sum, t) => sum + t.players.length);

  @override
  void initState() {
    super.initState();
    _refreshLobby();
    _initRealtime();
  }

  @override
  void dispose() {
    _realtimeEventSub?.cancel();
    _realtimeSnapshotSub?.cancel();
    _realtimeRefreshDebounce?.cancel();
    super.dispose();
  }

  Future<void> _refreshLobby({bool silent = false}) async {
    if (!silent) {
      setState(() => _loading = true);
    }

    try {
      final snapshot = await ApiService.instance.loadLobbySnapshot(
        widget.sessionId,
      );

      final usersById = snapshot.usersById;
      final currentUserId = AppSession.instance.currentUserId;
      final teams = <TeamData>[];
      final spectators = <LobbyPlayer>[];
      int? spectatorTeamId;
      TeamMemberDetails? currentMembership;

      for (final team in snapshot.teams) {
        final configuredUi = _teamUiConfigById[team.teamId];
        final teamColor =
            configuredUi?.color ??
            (tryParseHexColor(team.colorCode) ?? Colors.blueGrey);
        final maxPlayers = configuredUi?.maxPlayers ?? team.maxPlayerCount;
        final members = (snapshot.membersByTeamId[team.teamId] ?? const [])
            .map((m) {
              if (currentUserId != null && m.userId == currentUserId) {
                currentMembership = m;
              }

              final displayName = m.userId != null
                  ? (usersById[m.userId!]?.username ?? 'User ${m.userId}')
                  : (m.guestName ?? 'Guest');

              return LobbyPlayer(
                memberId: m.memberId,
                teamId: team.teamId,
                userId: m.userId,
                name: displayName,
                isCurrentUser: currentUserId != null && m.userId == currentUserId,
                isTeamLeader: m.isTeamLeader,
              );
            })
            .toList(growable: false);

        if (team.role == TeamRole.spectator) {
          spectatorTeamId ??= team.teamId;
          spectators.addAll(members);
          continue;
        }

        teams.add(
          TeamData(
            teamId: team.teamId,
            role: team.role ?? TeamRole.detective,
            name: team.teamName,
            color: teamColor,
            maxPlayers: maxPlayers,
            players: members,
            isDeletable: team.role != TeamRole.mrX,
          ),
        );
      }

      final currentTeamIds = teams.map((team) => team.teamId).toSet();
      _teamUiConfigById.removeWhere(
        (teamId, _) => !currentTeamIds.contains(teamId),
      );

      if (currentMembership != null) {
        AppSession.instance.setMembership(
          teamId: currentMembership!.teamId,
          memberId: currentMembership!.memberId,
          teamLeader: currentMembership!.isTeamLeader,
        );

        try {
          await ApiService.instance.registerCurrentMemberPresence();
        } catch (_) {
        }
      }

      setState(() {
        _teams = teams;
        _spectators = spectators;
        _spectatorTeamId = spectatorTeamId;
      });
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(
        SnackBar(content: Text('Failed to load game lobby: $error')),
      );
    } finally {
      if (mounted && !silent) {
        setState(() => _loading = false);
      }
    }
  }

  Future<void> _initRealtime() async {
    try {
      await ApiService.instance.ensureRealtimeSessionSubscription(
        widget.sessionId,
      );
      await ApiService.instance.registerCurrentMemberPresence();

      _realtimeEventSub = ApiService.instance.realtimeEvents.listen((event) {
        if (event.type == RealtimeEvents.gameSessionStarted) {
          unawaited(_openGameForAll());
        }

        if (_isLobbyRealtimeEvent(event.type)) {
          _queueRealtimeRefresh();
        }
      });

      _realtimeSnapshotSub = ApiService.instance.realtimeSnapshots.listen((
        snapshot,
      ) {
        if (snapshot.sessionId == widget.sessionId) {
          if (snapshot.status == SessionStatus.active) {
            unawaited(_openGameForAll());
          }

          _queueRealtimeRefresh();
        }
      });
    } catch (_) {
      // Lobby still works with manual refresh and HTTP fallback.
    }
  }

  bool _isLobbyRealtimeEvent(String eventType) {
    return eventType == RealtimeEvents.teamMemberJoined ||
        eventType == RealtimeEvents.teamMemberUpdated ||
        eventType == RealtimeEvents.teamMemberLeft ||
        eventType == RealtimeEvents.gameSessionStarted ||
        eventType == RealtimeEvents.locationLogRecorded;
  }

  void _queueRealtimeRefresh() {
    if (!mounted || _working) {
      return;
    }

    _realtimeRefreshDebounce?.cancel();
    _realtimeRefreshDebounce = Timer(const Duration(milliseconds: 250), () {
      if (!mounted) {
        return;
      }

      _refreshLobby(silent: true);
    });
  }

  void _copyGameCode() {
    Clipboard.setData(ClipboardData(text: widget.gameCode));
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(const SnackBar(content: Text('Game code copied!')));
  }

  Future<void> _addTeam() async {
    final result = await showDialog<AddTeamResult>(
      context: context,
      builder: (_) => const AddTeamDialog.create(),
    );

    if (result == null) return;

    setState(() => _working = true);
    try {
      final created = await ApiService.instance.addTeam(
        sessionId: widget.sessionId,
        teamName: result.name,
        role: TeamRole.detective,
        colorCode: _toHexColor(result.color),
        maxPlayerCount: result.maxPlayers,
      );
      _teamUiConfigById[created.teamId] = _TeamUiConfig(
        color: result.color,
        maxPlayers: result.maxPlayers,
      );
      await _refreshLobby();
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Could not create team: $error')));
    } finally {
      if (mounted) {
        setState(() => _working = false);
      }
    }
  }

  Future<void> _deleteTeam(int index) async {
    final team = _teams[index];

    setState(() => _working = true);
    try {
      await ApiService.instance.deleteTeam(
        sessionId: widget.sessionId,
        teamId: team.teamId,
      );
      _teamUiConfigById.remove(team.teamId);
      await _refreshLobby();
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Could not delete team: $error')));
    } finally {
      if (mounted) {
        setState(() => _working = false);
      }
    }
  }

  Future<void> _renameTeam(int index) async {
    final team = _teams[index];
    final edited = await showDialog<AddTeamResult>(
      context: context,
      builder: (_) => AddTeamDialog.edit(
        initialName: team.name,
        initialMaxPlayers: team.maxPlayers,
        initialColor: team.color,
      ),
    );

    if (edited == null) return;

    final hasChanged =
        edited.name != team.name ||
        edited.maxPlayers != team.maxPlayers ||
        edited.color.toARGB32() != team.color.toARGB32();
    if (!hasChanged) return;

    setState(() => _working = true);
    try {
      await ApiService.instance.updateTeam(
        sessionId: widget.sessionId,
        teamId: team.teamId,
        teamName: edited.name,
        role: team.role,
        colorCode: _toHexColor(edited.color),
        isCaught: false,
        maxPlayerCount: edited.maxPlayers,
      );
      _teamUiConfigById[team.teamId] = _TeamUiConfig(
        color: edited.color,
        maxPlayers: edited.maxPlayers,
      );
      await _refreshLobby();
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Could not rename team: $error')));
    } finally {
      if (mounted) {
        setState(() => _working = false);
      }
    }
  }

  Future<void> _startGame() async {
    setState(() => _working = true);
    try {
      await ApiService.instance.startGameSession(widget.sessionId);
      await _openGameForAll();
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Could not start game: $error')));
    } finally {
      if (mounted) {
        setState(() => _working = false);
      }
    }
  }

  Future<void> _movePlayerToSpectators(LobbyPlayer player) async {
    final spectatorTeamId = _spectatorTeamId;
    if (spectatorTeamId == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('No spectator team is available.')),
      );
      return;
    }

    await _movePlayer(player: player, targetTeamId: spectatorTeamId);
  }

  Future<void> _movePlayerToTeam(LobbyPlayer player, TeamData team) async {
    await _movePlayer(player: player, targetTeamId: team.teamId);
  }

  Future<void> _movePlayer({
    required LobbyPlayer player,
    required int targetTeamId,
    bool refreshAfterMove = true,
  }) async {
    setState(() => _working = true);
    try {
      await ApiService.instance.moveMemberToTeam(
        sessionId: widget.sessionId,
        member: TeamMemberDetails(
          memberId: player.memberId,
          teamId: player.teamId,
          sessionId: widget.sessionId,
          userId: player.userId,
          guestName: player.userId == null ? player.name : null,
          isTeamLeader: player.isTeamLeader,
          currentLatitude: null,
          currentLongitude: null,
          lastUpdated: null,
        ),
        sourceTeamId: player.teamId,
        targetTeamId: targetTeamId,
      );
      if (refreshAfterMove) {
        await _refreshLobby();
      }
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Could not move player: $error')));
    } finally {
      if (mounted) {
        setState(() => _working = false);
      }
    }
  }

  void _randomizeTeams() {
    final misterXTeams = _teams.where((team) => team.isMisterX).toList(growable: false);
    final detectiveTeams = _teams
        .where((team) => !team.isMisterX && !team.isSpectator)
        .toList(growable: false);

    if (misterXTeams.isEmpty || detectiveTeams.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Randomize requires one Mister X and one detective team.'),
        ),
      );
      return;
    }

    final misterXTeam = misterXTeams.first;

    final players = [..._spectators, ..._teams.expand((team) => team.players)]
        .toList(growable: true);

    if (players.length < 2) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Not enough players to randomize teams.')),
      );
      return;
    }

    players.shuffle();

    final moves = <_PlannedMove>[];

    final mrXPlayer = players.removeAt(0);
    moves.add(_PlannedMove(player: mrXPlayer, targetTeamId: misterXTeam.teamId));

    for (var i = 0; i < players.length; i++) {
      final targetTeam = detectiveTeams[i % detectiveTeams.length];
      moves.add(
        _PlannedMove(player: players[i], targetTeamId: targetTeam.teamId),
      );
    }

    _randomizeTeamsAsync(moves);
  }

  Future<void> _randomizeTeamsAsync(List<_PlannedMove> moves) async {
    setState(() => _working = true);
    try {
      for (final move in moves) {
        if (move.player.teamId == move.targetTeamId) {
          continue;
        }

        await ApiService.instance.moveMemberToTeam(
          sessionId: widget.sessionId,
          member: TeamMemberDetails(
            memberId: move.player.memberId,
            teamId: move.player.teamId,
            sessionId: widget.sessionId,
            userId: move.player.userId,
            guestName: move.player.userId == null ? move.player.name : null,
            isTeamLeader: move.player.isTeamLeader,
            currentLatitude: null,
            currentLongitude: null,
            lastUpdated: null,
          ),
          sourceTeamId: move.player.teamId,
          targetTeamId: move.targetTeamId,
        );
      }

      await _refreshLobby();
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Could not randomize teams: $error')),
      );
    } finally {
      if (mounted) {
        setState(() => _working = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final leader = isLobbyLeader();

    if (_loading) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }

    return Scaffold(
      backgroundColor: XActBranding.backgroundColor,
      body: SafeArea(
        child: Column(
          children: [
            GameLobbyHeader(
              gameName: widget.gameName,
              totalPlayers: _totalPlayers,
              isLeader: leader,
              onQrPressed: () {},
            ),
            Expanded(
              child: RefreshIndicator(
                onRefresh: _refreshLobby,
                child: SingleChildScrollView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  padding: const EdgeInsets.symmetric(
                    horizontal: 16,
                    vertical: 8,
                  ),
                  child: Column(
                    children: [
                      GameCodeCard(
                        gameCode: widget.gameCode,
                        codeLabel: 'Game Code',
                        onCopy: _copyGameCode,
                      ),
                      if (_working) ...[
                        const SizedBox(height: 8),
                        const LinearProgressIndicator(),
                      ],
                      const SizedBox(height: 6),
                      const Text(
                        'Drag players to assign teams',
                        style: TextStyle(color: Colors.white38, fontSize: 13),
                      ),
                      const SizedBox(height: 16),
                      TeamOverviewCard(
                        spectatorCount: _spectators.length,
                        teams: _teams,
                      ),
                      const SizedBox(height: 16),
                      SpectatorsCard(
                        spectators: _spectators,
                        onPlayerDropped: _movePlayerToSpectators,
                      ),
                      const SizedBox(height: 16),
                      ..._teams.asMap().entries.map(
                        (entry) => Padding(
                          padding: const EdgeInsets.only(bottom: 12),
                          child: LobbyTeamCard(
                            team: entry.value,
                            isLeader: leader,
                            onRename: () => _renameTeam(entry.key),
                            onDelete: entry.value.isDeletable
                                ? () => _deleteTeam(entry.key)
                                : null,
                            onPlayerDropped: (player) =>
                                _movePlayerToTeam(player, entry.value),
                          ),
                        ),
                      ),
                      if (leader) ...[
                        AddTeamButton(onPressed: _addTeam),
                        const SizedBox(height: 12),
                      ],
                    ],
                  ),
                ),
              ),
            ),
            if (leader)
              LobbyBottomButtons(
                canStartGame: _canStartGame,
                onRandomize: _randomizeTeams,
                onStartGame: _startGame,
              ),
          ],
        ),
      ),
    );
  }

  String _toHexColor(Color color) {
    final rgb = color.toARGB32() & 0x00FFFFFF;
    return '#${rgb.toRadixString(16).padLeft(6, '0').toUpperCase()}';
  }

  Future<void> _openGameForAll() async {
    if (!mounted || _gameTransitionStarted) {
      return;
    }

    _gameTransitionStarted = true;
    await GameStartTransitionService.instance.playCountdown(seconds: 3);

    if (!mounted) {
      return;
    }

    Navigator.pushReplacement(
      context,
      MaterialPageRoute(builder: (_) => const GameScreen()),
    );
  }
}

class _TeamUiConfig {
  final Color color;
  final int maxPlayers;

  const _TeamUiConfig({required this.color, required this.maxPlayers});
}

class _PlannedMove {
  final LobbyPlayer player;
  final int targetTeamId;

  const _PlannedMove({required this.player, required this.targetTeamId});
}
