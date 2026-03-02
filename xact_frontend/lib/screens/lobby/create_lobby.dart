import 'package:flutter/material.dart';
import 'package:xact_frontend/screens/game_screen.dart';
import 'package:xact_frontend/screens/lobby/define_game_area_screen.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class CreateLobbyScreen extends StatefulWidget {
  const CreateLobbyScreen({super.key});

  @override
  State<CreateLobbyScreen> createState() => _CreateLobbyScreenState();
}

class _CreateLobbyScreenState extends State<CreateLobbyScreen> {
  final _lobbyNameController = TextEditingController();

  @override
  void dispose() {
    _lobbyNameController.dispose();
    super.dispose();
  }

  void _onCreate() async {
    final lobbyName = _lobbyNameController.text.trim();

    if (lobbyName.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please enter a lobby name')),
      );
      return;
    }

    // TODO: Call POST /api/gamesessions to create a real session and get
    // the returned sessionId. Using a placeholder of 1 for now.
    const sessionId = 1;

    // Step 1: Let the host define the game area.
    final areaSaved = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (_) => DefineGameAreaScreen(sessionId: sessionId),
      ),
    );

    if (areaSaved != true || !mounted) return;

    // Step 2: Enter the game.
    Navigator.push(
      context,
      MaterialPageRoute(builder: (context) => const GameScreen()),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: XActBranding.backgroundColor,
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 24.0),
          child: Column(
            children: [
              const SizedBox(height: 40),
              XActBranding.buildHeader(),
              const Spacer(),
              _buildCreateForm(),
              const Spacer(),
              XActBranding.buildFooter(),
              const SizedBox(height: 16),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildCreateForm() {
    return XActBranding.buildFormCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Create New Lobby',
            style: TextStyle(
              color: Colors.white,
              fontSize: 24,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 20),
          XActBranding.buildTextField(
            label: 'Lobby Name',
            hintText: 'Enter lobby name...',
            controller: _lobbyNameController,
          ),
          const SizedBox(height: 24),
          Row(
            children: [
              Expanded(
                child: XActBranding.buildSecondaryButton(
                  text: 'Create',
                  onPressed: _onCreate,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: XActBranding.buildCancelButton(
                  text: 'Cancel',
                  onPressed: () => Navigator.of(context).pop(),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
