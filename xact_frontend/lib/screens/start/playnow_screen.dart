import 'package:flutter/material.dart';
import 'package:xact_frontend/screens/lobby/create_lobby.dart';
import 'package:xact_frontend/screens/lobby/join_lobby.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class PlayNowScreen extends StatelessWidget {
  const PlayNowScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: XActColors.bg,
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: XActSpace.s6),
          child: Column(
            children: [
              const SizedBox(height: XActSpace.s8),
              XActBranding.buildHeader(),
              const SizedBox(height: XActSpace.s7),
              XActBranding.buildActionCard(
                icon: Icons.add,
                title: 'Start New Game',
                subtitle:
                    'Create a game and open its lobby so friends can join with your game code',
                bg: XActColors.secondary,
                onTap: () {
                  Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (context) => const CreateGameScreen(),
                    ),
                  );
                },
              ),
              const SizedBox(height: XActSpace.s4),
              XActBranding.buildActionCard(
                icon: Icons.arrow_forward,
                title: "Join Friend's Game",
                subtitle:
                    'Enter a game code to join your friends in the lobby',
                onTap: () {
                  Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (context) => const JoinGameScreen(),
                    ),
                  );
                },
              ),
              const Spacer(),
              _buildBackButton(context),
              const SizedBox(height: XActSpace.s6),
              XActBranding.buildFooter(),
              const SizedBox(height: XActSpace.s4),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildBackButton(BuildContext context) {
    return TextButton(
      onPressed: () {
        Navigator.of(context).pop();
      },
      child: Text(
        'Back',
        style: XActText.body.copyWith(color: XActColors.text2),
      ),
    );
  }
}
