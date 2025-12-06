import 'package:flutter/material.dart';

class AllChatScreen extends StatelessWidget {
  const AllChatScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Container(
      color: const Color(0xFF1E293B),
      child: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.forum, size: 64, color: Colors.white54),
            const SizedBox(height: 16),
            Text('All Chat Screen', style: Theme.of(context).textTheme.headlineSmall?.copyWith(color: Colors.white54)),
          ],
        ),
      ),
    );
  }
}
