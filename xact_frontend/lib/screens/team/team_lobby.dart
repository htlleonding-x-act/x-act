import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
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

// ─── Team Lobby Screen ──────────────────────────────────────────────────────

class TeamLobbyScreen extends StatefulWidget {
  /// TODO: accept real session data (lobby code, session id, etc.)
  final String lobbyCode;

  const TeamLobbyScreen({super.key, this.lobbyCode = 'Q7AMJW'});

  @override
  State<TeamLobbyScreen> createState() => _TeamLobbyScreenState();
}

class _TeamLobbyScreenState extends State<TeamLobbyScreen> {
  // ── Pseudo function: lobby leader check ─────────────────────────────────
  // TODO: Replace with real logic – check if current user is the lobby leader
  //       (e.g. compare current userId with session.creatorId).
  bool isLobbyLeader() => true;

  // ── Mock player list (Spectators) ─────────────────────────────────────────
  // TODO: Replace with real player list from backend / websocket
  final List<String> _spectators = [
    'You',
    'Alex',
    'Sarah',
    'Mike',
    'Emma',
    'Lucas',
    'Mia',
    'Noah',
  ];

  // ── Default teams ─────────────────────────────────────────────────────────
  late final List<TeamData> _teams = [
    TeamData(
      name: 'Mister X',
      color: Colors.red,
      maxPlayers: 2,
      isMisterX: true,
      isDeletable: false,
    ),
    TeamData(
      name: 'Detectives 1',
      color: Colors.purple,
      maxPlayers: 3,
      isDeletable: false, // at least one detective team must exist
    ),
    TeamData(name: 'Detectives 2', color: Colors.green, maxPlayers: 3),
  ];

  // ── Helper: can the game start? ───────────────────────────────────────────
  // TODO: Replace with real validation
  bool get _canStartGame {
    final hasMisterX = _teams.any((t) => t.isMisterX && t.players.isNotEmpty);
    final hasDetective = _teams.any(
      (t) => !t.isMisterX && t.players.isNotEmpty,
    );
    return hasMisterX && hasDetective;
  }

  // ── Team overview helper ──────────────────────────────────────────────────

  int get _totalPlayers =>
      _spectators.length +
      _teams.fold<int>(0, (sum, t) => sum + t.players.length);

  // ── Actions ───────────────────────────────────────────────────────────────

  void _copyLobbyCode() {
    Clipboard.setData(ClipboardData(text: widget.lobbyCode));
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(const SnackBar(content: Text('Lobby code copied!')));
  }

  void _addTeam() async {
    // TODO: Hook up to backend – POST /api/teams
    final result = await showDialog<AddTeamResult>(
      context: context,
      builder: (_) => const AddTeamDialog(),
    );
    if (result != null) {
      setState(() {
        _teams.add(
          TeamData(
            name: result.name,
            color: result.color,
            maxPlayers: result.maxPlayers,
          ),
        );
      });
    }
  }

  void _deleteTeam(int index) {
    // TODO: Hook up to backend – DELETE /api/teams/:id
    setState(() {
      // Move players back to spectators
      _spectators.addAll(_teams[index].players);
      _teams.removeAt(index);
    });
  }

  void _renameTeam(int index) async {
    final team = _teams[index];
    final controller = TextEditingController(text: team.name);

    final newName = await showDialog<String>(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: XActBranding.cardColor,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: const Text('Rename Team', style: TextStyle(color: Colors.white)),
        content: TextField(
          controller: controller,
          autofocus: true,
          style: const TextStyle(color: Colors.white, fontSize: 16),
          decoration: InputDecoration(
            hintText: 'New team name…',
            hintStyle: const TextStyle(color: Colors.white38),
            filled: true,
            fillColor: XActBranding.backgroundColor,
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(10),
              borderSide: const BorderSide(color: Colors.white24),
            ),
            enabledBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(10),
              borderSide: const BorderSide(color: Colors.white24),
            ),
            focusedBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(10),
              borderSide: const BorderSide(color: Colors.blueAccent),
            ),
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(),
            child: const Text(
              'Cancel',
              style: TextStyle(color: Colors.white54),
            ),
          ),
          ElevatedButton(
            onPressed: () {
              final text = controller.text.trim();
              if (text.isNotEmpty) Navigator.of(ctx).pop(text);
            },
            style: ElevatedButton.styleFrom(
              backgroundColor: XActBranding.primaryBlue,
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(10),
              ),
            ),
            child: const Text('Save'),
          ),
        ],
      ),
    );

    controller.dispose();

    if (newName != null && newName != team.name) {
      setState(() => team.name = newName);
      _updateTeamNameOnBackend(index, newName);
    }
  }

  /// Sends the updated team name to the backend.
  // TODO: Replace placeholder with real API call (e.g. PUT /api/teams/:id)
  Future<void> _updateTeamNameOnBackend(int index, String newName) async {
    // TODO: final teamId = _teams[index].id;
    // TODO: await ApiService.instance.updateTeamName(teamId, newName);
    debugPrint(
      'TODO → update team name on backend: index=$index, name=$newName',
    );
  }

  void _randomizeTeams() {
    // TODO: Implement real randomization logic
    setState(() {
      // Gather all players
      final allPlayers = <String>[..._spectators];
      for (final t in _teams) {
        allPlayers.addAll(t.players);
        t.players.clear();
      }
      allPlayers.shuffle();

      // Distribute round-robin across teams
      var teamIdx = 0;
      for (final player in allPlayers) {
        if (_teams[teamIdx].players.length >= _teams[teamIdx].maxPlayers) {
          teamIdx = (teamIdx + 1) % _teams.length;
        }
        if (_teams[teamIdx].players.length < _teams[teamIdx].maxPlayers) {
          _teams[teamIdx].players.add(player);
          teamIdx = (teamIdx + 1) % _teams.length;
        } else {
          // Overflow → back to spectators
          _spectators.add(player);
        }
      }
      // Remove distributed players from spectators
      for (final t in _teams) {
        for (final p in t.players) {
          _spectators.remove(p);
        }
      }
    });
  }

  void _startGame() {
    // TODO: Call backend to start the session before navigating
    Navigator.push(
      context,
      MaterialPageRoute(builder: (_) => const GameScreen()),
    );
  }

  // ── Drag & Drop helpers ─────────────────────────────────────────────────

  /// Removes [playerName] from wherever it currently lives (spectators or a team).
  void _removePlayerFromSource(String playerName) {
    _spectators.remove(playerName);
    for (final t in _teams) {
      t.players.remove(playerName);
    }
  }

  /// Moves a player into the spectators list.
  void _movePlayerToSpectators(String playerName) {
    setState(() {
      _removePlayerFromSource(playerName);
      _spectators.add(playerName);
    });
    // TODO: notify backend about the assignment change
  }

  /// Moves a player into [team]. Returns `false` if the team is full.
  bool _movePlayerToTeam(String playerName, TeamData team) {
    if (team.players.length >= team.maxPlayers) return false;
    setState(() {
      _removePlayerFromSource(playerName);
      team.players.add(playerName);
    });
    // TODO: notify backend about the assignment change
    return true;
  }

  // ══════════════════════════════════════════════════════════════════════════
  // BUILD
  // ══════════════════════════════════════════════════════════════════════════

  @override
  Widget build(BuildContext context) {
    final leader = isLobbyLeader();

    return Scaffold(
      backgroundColor: XActBranding.backgroundColor,
      body: SafeArea(
        child: Column(
          children: [
            // ── Header ────────────────────────────────────────────────────
            LobbyHeader(
              totalPlayers: _totalPlayers,
              isLeader: leader,
              onQrPressed: () {
                // TODO: Show QR code dialog
              },
            ),

            // ── Scrollable content ────────────────────────────────────────
            Expanded(
              child: SingleChildScrollView(
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
                    const SizedBox(height: 6),
                    const Text(
                      'Drag players to assign teams',
                      style: TextStyle(color: Colors.white38, fontSize: 13),
                    ),
                    const SizedBox(height: 16),

                    // ── Team Overview ──────────────────────────────────────
                    TeamOverviewCard(
                      spectatorCount: _spectators.length,
                      teams: _teams,
                    ),
                    const SizedBox(height: 16),

                    // ── Spectators ─────────────────────────────────────────
                    SpectatorsCard(
                      spectators: _spectators,
                      onPlayerDropped: _movePlayerToSpectators,
                    ),
                    const SizedBox(height: 16),

                    // ── Team cards ─────────────────────────────────────────
                    ..._teams.asMap().entries.map(
                      (e) => Padding(
                        padding: const EdgeInsets.only(bottom: 12),
                        child: LobbyTeamCard(
                          team: e.value,
                          isLeader: leader,
                          onRename: () => _renameTeam(e.key),
                          onDelete: () => _deleteTeam(e.key),
                          onPlayerDropped: (name) =>
                              _movePlayerToTeam(name, e.value),
                        ),
                      ),
                    ),

                    // ── Add team button ────────────────────────────────────
                    if (leader) ...[
                      AddTeamButton(onPressed: _addTeam),
                      const SizedBox(height: 12),
                    ],

                    const SizedBox(height: 8),
                  ],
                ),
              ),
            ),

            // ── Bottom buttons ────────────────────────────────────────────
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
}
