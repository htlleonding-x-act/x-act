import 'package:flutter/material.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class JoinLobbyScreen extends StatefulWidget {
  const JoinLobbyScreen({super.key});

  @override
  State<JoinLobbyScreen> createState() => _JoinLobbyScreenState();
}

class _JoinLobbyScreenState extends State<JoinLobbyScreen> {
  final _lobbyCodeController = TextEditingController();
  final _usernameController = TextEditingController();

  @override
  void dispose() {
    _lobbyCodeController.dispose();
    _usernameController.dispose();
    super.dispose();
  }

  void _onJoin() {
    final lobbyCode = _lobbyCodeController.text.trim();
    final username = _usernameController.text.trim();

    if (lobbyCode.isEmpty || username.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please fill in all fields')),
      );
      return;
    }

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
              _buildJoinForm(),
              const Spacer(),
              XActBranding.buildFooter(),
              const SizedBox(height: 16),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildJoinForm() {
    return XActBranding.buildFormCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Join Lobby',
            style: TextStyle(
              color: Colors.white,
              fontSize: 24,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 20),
          XActBranding.buildTextField(
            label: 'Lobby Code',
            hintText: 'Enter 6-digit code...',
            controller: _lobbyCodeController,
            keyboardType: TextInputType.number,
            maxLength: 6,
          ),
          const SizedBox(height: 16),
          XActBranding.buildTextField(
            label: 'Username',
            hintText: 'Enter your username...',
            controller: _usernameController,
          ),
          const SizedBox(height: 24),
          Row(
            children: [
              Expanded(
                child: XActBranding.buildSecondaryButton(
                  text: 'Join',
                  onPressed: _onJoin,
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
