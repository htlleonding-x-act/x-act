import 'package:flutter/material.dart';
import 'package:xact_frontend/screens/game_screen.dart';
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

  void _onCreate() {
    final lobbyName = _lobbyNameController.text.trim();

    if (lobbyName.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please enter a lobby name')),
      );
      return;
    }

    Navigator.push(
      context,
      MaterialPageRoute(builder: (context) => const GameScreen()),
    );

    // TODO: Implement join lobby logic

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
