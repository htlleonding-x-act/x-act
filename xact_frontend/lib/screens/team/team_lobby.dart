import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:xact_frontend/api/api_service.dart';
import 'package:xact_frontend/api/models.dart';
import 'package:xact_frontend/screens/game_screen.dart';
import 'package:xact_frontend/screens/team/add_team.dart';
import 'package:xact_frontend/widgets/team/add_team_button.dart';
import 'package:xact_frontend/widgets/team/lobby_bottom_buttons.dart';
import 'package:xact_frontend/widgets/team/lobby_code_card.dart';
import 'package:xact_frontend/widgets/team/lobby_header.dart';
import 'package:xact_frontend/widgets/team/lobby_team_card.dart';
import 'package:xact_frontend/widgets/team/spectators_card.dart';
import 'package:xact_frontend/widgets/team/team_data.dart';
import 'package:xact_frontend/widgets/team/team_overview_card.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class TeamLobbyScreen extends StatefulWidget {
  final int sessionId;
  final String lobbyCode;
  final bool isLeader;

  const TeamLobbyScreen({
    super.key,
    required this.sessionId,
    required this.lobbyCode,
    required this.isLeader,
  });

  @override
  State<TeamLobbyScreen> createState() => _TeamLobbyScreenState();
}

class _TeamLobbyScreenState extends State<TeamLobbyScreen> {
  bool _loading = true;
  bool _working = false;

  List<LobbyPlayer> _spectators = [];
  List<TeamData> _teams = [];
  int? _spectatorTeamId;

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
  }

  Future<void> _refreshLobby() async {
    setState(() => _loading = true);

    try {
      final snapshot = await ApiService.instance.loadLobbySnapshot(
        widget.sessionId,
      );

      final usersById = snapshot.usersById;
      final teams = <TeamData>[];
      final spectators = <LobbyPlayer>[];
      int? spectatorTeamId;

      for (final team in snapshot.teams) {
        final teamColor = tryParseHexColor(team.colorCode) ?? Colors.blueGrey;
        final members = (snapshot.membersByTeamId[team.teamId] ?? const [])
            .map((m) {
              final displayName = m.userId != null
                  ? (usersById[m.userId!]?.username ?? 'User ${m.userId}')
                  : (m.guestName ?? 'Guest');

              return LobbyPlayer(
                memberId: m.memberId,
                teamId: team.teamId,
                userId: m.userId,
                name: displayName,
                isCurrentUser: false,
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
            maxPlayers: 6,
            players: members,
            isDeletable: team.role != TeamRole.mrX,
          ),
        );
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
      ).showSnackBar(SnackBar(content: Text('Failed to load lobby: $error')));
    } finally {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  void _copyLobbyCode() {
    Clipboard.setData(ClipboardData(text: widget.lobbyCode));
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(const SnackBar(content: Text('Lobby code copied!')));
  }

  Future<void> _addTeam() async {
    final result = await showDialog<AddTeamResult>(
      context: context,
      builder: (_) => const AddTeamDialog(),
    );

    if (result == null) return;

    setState(() => _working = true);
    try {
      await ApiService.instance.addTeam(
        sessionId: widget.sessionId,
        teamName: result.name,
        role: TeamRole.detective,
        colorCode: _toHexColor(result.color),
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
    var draftName = team.name;

    final name = await showDialog<String>(
      context: context,
      useRootNavigator: true,
      builder: (ctx) {
        return AlertDialog(
          backgroundColor: XActBranding.cardColor,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
          title: const Text('Edit Team', style: TextStyle(color: Colors.white)),
          content: TextFormField(
            initialValue: team.name,
            onChanged: (value) => draftName = value,
            autofocus: false,
            style: const TextStyle(color: Colors.white, fontSize: 16),
            decoration: InputDecoration(
              labelText: 'Team Name',
              labelStyle: const TextStyle(color: Colors.white70),
              filled: true,
              fillColor: XActBranding.backgroundColor,
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10),
                borderSide: const BorderSide(color: Colors.white24),
              ),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(ctx, rootNavigator: true).pop(),
              child: const Text(
                'Cancel',
                style: TextStyle(color: Colors.white54),
              ),
            ),
            ElevatedButton(
              onPressed: () =>
                  Navigator.of(ctx, rootNavigator: true).pop(draftName.trim()),
              style: ElevatedButton.styleFrom(
                backgroundColor: XActBranding.primaryBlue,
                foregroundColor: Colors.white,
              ),
              child: const Text('Save'),
            ),
          ],
        );
      },
    );

    if (name == null || name.isEmpty || name == team.name) return;

    setState(() => _working = true);
    try {
      await ApiService.instance.updateTeam(
        sessionId: widget.sessionId,
        teamId: team.teamId,
        teamName: name,
        role: team.role,
        colorCode: _toHexColor(team.color),
        isCaught: false,
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
      if (!mounted) return;
      Navigator.push(
        context,
        MaterialPageRoute(builder: (_) => const GameScreen()),
      );
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
      await _refreshLobby();
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
    final players = [..._spectators, ..._teams.expand((team) => team.players)];
    players.shuffle();

    final targets = _teams.where((t) => !t.isMisterX).toList(growable: false);
    if (targets.isEmpty) {
      return;
    }

    for (final player in players) {
      final team = targets[players.indexOf(player) % targets.length];
      _movePlayerToTeam(player, team);
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
            LobbyHeader(
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
                      LobbyCodeCard(
                        lobbyCode: widget.lobbyCode,
                        onCopy: _copyLobbyCode,
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
}
