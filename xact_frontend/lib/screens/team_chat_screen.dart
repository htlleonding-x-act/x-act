import 'package:flutter/material.dart';

class TeamChatScreen extends StatelessWidget {
  const TeamChatScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Container(
      color: const Color(0xFF1E293B),
      child: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.chat, size: 64, color: Colors.white54),
            const SizedBox(height: 16),
            Text('Team Chat Screen', style: Theme.of(context).textTheme.headlineSmall?.copyWith(color: Colors.white54)),
          ],
        ),
      ),
    );
  }
}
