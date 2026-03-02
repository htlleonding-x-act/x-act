import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:xact_frontend/screens/game_screen.dart';
import 'package:xact_frontend/screens/team/add_team.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

// ─── Data model for a team in the lobby ─────────────────────────────────────

class _TeamData {
  String name;
  Color color;
  int maxPlayers;
  List<String> players;
  bool isMisterX;

  /// When `false` the team cannot be deleted (Mister X + first detective team).
  bool isDeletable;

  _TeamData({
    required this.name,
    required this.color,
    this.maxPlayers = 3,
    List<String>? players,
    this.isMisterX = false,
    this.isDeletable = true,
  }) : players = players ?? [];
}

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
  late final List<_TeamData> _teams = [
    _TeamData(
      name: 'Mister X',
      color: Colors.red,
      maxPlayers: 2,
      isMisterX: true,
      isDeletable: false,
    ),
    _TeamData(
      name: 'Detectives 1',
      color: Colors.purple,
      maxPlayers: 3,
      isDeletable: false, // at least one detective team must exist
    ),
    _TeamData(name: 'Detectives 2', color: Colors.green, maxPlayers: 3),
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
          _TeamData(
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
            _buildHeader(),

            // ── Scrollable content ────────────────────────────────────────
            Expanded(
              child: SingleChildScrollView(
                padding: const EdgeInsets.symmetric(
                  horizontal: 16,
                  vertical: 8,
                ),
                child: Column(
                  children: [
                    _buildLobbyCodeCard(),
                    const SizedBox(height: 6),
                    const Text(
                      'Drag players to assign teams',
                      style: TextStyle(color: Colors.white38, fontSize: 13),
                    ),
                    const SizedBox(height: 16),

                    // ── Team Overview ──────────────────────────────────────
                    _buildTeamOverview(),
                    const SizedBox(height: 16),

                    // ── Spectators ─────────────────────────────────────────
                    _buildSpectatorsCard(),
                    const SizedBox(height: 16),

                    // ── Team cards ─────────────────────────────────────────
                    ..._teams.asMap().entries.map(
                      (e) => Padding(
                        padding: const EdgeInsets.only(bottom: 12),
                        child: _buildTeamCard(e.key, e.value, leader),
                      ),
                    ),

                    // ── Add team button ────────────────────────────────────
                    if (leader) ...[
                      _buildAddTeamButton(),
                      const SizedBox(height: 12),
                    ],

                    const SizedBox(height: 8),
                  ],
                ),
              ),
            ),

            // ── Bottom buttons ────────────────────────────────────────────
            if (leader) _buildBottomButtons(),
          ],
        ),
      ),
    );
  }

  // ── Header row ──────────────────────────────────────────────────────────

  Widget _buildHeader() {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Game Lobby',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 22,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  '$_totalPlayers players in lobby'
                  '${isLobbyLeader() ? '  •  Lobby Leader' : ''}',
                  style: const TextStyle(color: Colors.white60, fontSize: 13),
                ),
              ],
            ),
          ),
          // TODO: QR code button → show QR dialog for lobby code
          IconButton(
            icon: const Icon(Icons.qr_code, color: Colors.white70),
            onPressed: () {
              // TODO: Show QR code dialog
            },
          ),
        ],
      ),
    );
  }

  // ── Lobby code card ─────────────────────────────────────────────────────

  Widget _buildLobbyCodeCard() {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: XActBranding.cardColor,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Lobby Code',
                  style: TextStyle(color: Colors.white54, fontSize: 12),
                ),
                const SizedBox(height: 4),
                Text(
                  widget.lobbyCode,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 28,
                    fontWeight: FontWeight.bold,
                    letterSpacing: 4,
                  ),
                ),
              ],
            ),
          ),
          ElevatedButton.icon(
            onPressed: _copyLobbyCode,
            icon: const Icon(Icons.copy, size: 16),
            label: const Text('Copy'),
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.white12,
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(8),
              ),
            ),
          ),
        ],
      ),
    );
  }

  // ── Team Overview ───────────────────────────────────────────────────────

  Widget _buildTeamOverview() {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: XActBranding.cardColor,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Row(
            children: [
              Icon(Icons.people, color: Colors.white70, size: 18),
              SizedBox(width: 6),
              Text(
                'Team Overview',
                style: TextStyle(
                  color: Colors.white,
                  fontSize: 15,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Wrap(
            spacing: 16,
            runSpacing: 6,
            children: [
              _overviewChip(
                'Spectators',
                Colors.grey,
                '${_spectators.length}/∞',
              ),
              ..._teams.map(
                (t) => _overviewChip(
                  t.name,
                  t.color,
                  '${t.players.length}/${t.maxPlayers}',
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _overviewChip(String name, Color color, String count) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 10,
          height: 10,
          decoration: BoxDecoration(color: color, shape: BoxShape.circle),
        ),
        const SizedBox(width: 6),
        Text(name, style: const TextStyle(color: Colors.white70, fontSize: 13)),
        const SizedBox(width: 4),
        Text(
          count,
          style: const TextStyle(color: Colors.white38, fontSize: 13),
        ),
      ],
    );
  }

  // ── Spectators card ─────────────────────────────────────────────────────

  Widget _buildSpectatorsCard() {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: XActBranding.cardColor,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: Colors.blueAccent.withAlpha(120), width: 1.5),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Text(
                'Spectators',
                style: TextStyle(
                  color: Colors.white,
                  fontSize: 16,
                  fontWeight: FontWeight.w600,
                ),
              ),
              const SizedBox(width: 8),
              // TODO: Toggle visibility icon
              const Icon(Icons.visibility, color: Colors.white38, size: 18),
              const SizedBox(width: 6),
              // TODO: Edit spectators icon
              const Icon(Icons.edit, color: Colors.white38, size: 18),
              const Spacer(),
              Text(
                '${_spectators.length}/∞',
                style: const TextStyle(color: Colors.white54, fontSize: 13),
              ),
            ],
          ),
          const SizedBox(height: 10),
          ..._spectators.map((name) => _buildPlayerTile(name)),
        ],
      ),
    );
  }

  Widget _buildPlayerTile(String name) {
    final isYou = name == 'You';
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
        decoration: BoxDecoration(
          color: XActBranding.backgroundColor,
          borderRadius: BorderRadius.circular(8),
        ),
        child: Row(
          children: [
            // TODO: Drag handle → implement drag & drop to assign players
            const Icon(Icons.drag_indicator, color: Colors.white24, size: 20),
            const SizedBox(width: 8),
            Container(
              width: 10,
              height: 10,
              decoration: const BoxDecoration(
                color: Colors.grey,
                shape: BoxShape.circle,
              ),
            ),
            const SizedBox(width: 10),
            Text(
              name,
              style: const TextStyle(color: Colors.white, fontSize: 14),
            ),
            if (isYou) ...[
              const SizedBox(width: 6),
              const Text(
                '(you)',
                style: TextStyle(color: Colors.amber, fontSize: 13),
              ),
            ],
          ],
        ),
      ),
    );
  }

  // ── Team card ───────────────────────────────────────────────────────────

  Widget _buildTeamCard(int index, _TeamData team, bool isLeader) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: XActBranding.cardColor,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: team.color.withAlpha(150), width: 1.5),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  team.name,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 16,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
              if (isLeader) ...[
                GestureDetector(
                  onTap: () => _renameTeam(index),
                  child: Icon(Icons.edit, color: team.color, size: 18),
                ),
                const SizedBox(width: 10),
                Text(
                  '${team.players.length}/${team.maxPlayers}',
                  style: TextStyle(color: team.color, fontSize: 14),
                ),
                // Only show delete icon when team is deletable
                if (team.isDeletable) ...[
                  const SizedBox(width: 10),
                  GestureDetector(
                    onTap: () => _deleteTeam(index),
                    child: const Icon(
                      Icons.delete,
                      color: Colors.white38,
                      size: 18,
                    ),
                  ),
                ],
              ] else
                Text(
                  '${team.players.length}/${team.maxPlayers}',
                  style: TextStyle(color: team.color, fontSize: 14),
                ),
            ],
          ),
          const SizedBox(height: 8),
          if (team.players.isEmpty)
            const Text(
              'No players',
              style: TextStyle(color: Colors.white38, fontSize: 13),
            )
          else
            ...team.players.map((p) => _buildPlayerTile(p)),
        ],
      ),
    );
  }

  // ── Add team button ─────────────────────────────────────────────────────

  Widget _buildAddTeamButton() {
    return SizedBox(
      width: double.infinity,
      child: OutlinedButton.icon(
        onPressed: _addTeam,
        icon: const Icon(Icons.add, color: Colors.white54),
        label: const Text(
          'Add New Team',
          style: TextStyle(color: Colors.white54),
        ),
        style: OutlinedButton.styleFrom(
          side: const BorderSide(color: Colors.white24),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          padding: const EdgeInsets.symmetric(vertical: 14),
        ),
      ),
    );
  }

  // ── Bottom buttons (Randomize + Start) ──────────────────────────────────

  Widget _buildBottomButtons() {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 4, 16, 12),
      child: Column(
        children: [
          // ── Randomize Teams ───────────────────────────────────────────
          SizedBox(
            width: double.infinity,
            child: ElevatedButton.icon(
              onPressed: _randomizeTeams,
              icon: const Icon(Icons.shuffle, size: 18),
              label: const Text('Randomize Teams'),
              style: ElevatedButton.styleFrom(
                backgroundColor: XActBranding.primaryBlue,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
                padding: const EdgeInsets.symmetric(vertical: 14),
              ),
            ),
          ),
          const SizedBox(height: 8),

          // ── Start Game ────────────────────────────────────────────────
          SizedBox(
            width: double.infinity,
            child: ElevatedButton.icon(
              onPressed: _canStartGame ? _startGame : null,
              icon: const Icon(Icons.play_arrow, size: 20),
              label: const Text(
                'Start Game',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
              ),
              style: ElevatedButton.styleFrom(
                backgroundColor: _canStartGame ? Colors.green : Colors.white10,
                foregroundColor: _canStartGame ? Colors.white : Colors.white38,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
                padding: const EdgeInsets.symmetric(vertical: 14),
              ),
            ),
          ),
          if (!_canStartGame) ...[
            const SizedBox(height: 6),
            const Text(
              'Need at least 1 Mister X and 1 Detective to start',
              style: TextStyle(color: Colors.white38, fontSize: 12),
            ),
          ],
        ],
      ),
    );
  }
}
