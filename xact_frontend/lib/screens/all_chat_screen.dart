import 'package:flutter/material.dart';
import '../widgets/chat_input_bar.dart';

class AllChatScreen extends StatelessWidget {
  const AllChatScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Container(
      color: const Color(0xFF1E293B),
      child: Column(
        children: [
          Container(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.center,
              children: [
                Text(
                  'All Chat',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 20,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  'Messages visible to everyone',
                  style: TextStyle(color: Colors.white54, fontSize: 14),
                ),
              ],
            ),
          ),
          Expanded(
            child: ListView(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              children: [
                const SizedBox(height: 12),
                _ChatMessage(
                  senderName: 'Mister X',
                  teamName: 'Mister X',
                  message: 'You\'ll never catch me! 😎',
                  timeAgo: 3,
                  nameColor: Colors.red,
                ),
                const SizedBox(height: 12),
                _ChatMessage(
                  senderName: 'Sophie',
                  teamName: 'Detectives Beta',
                  message: 'Challenge accepted!',
                  timeAgo: 1,
                  nameColor: Colors.green,
                ),
                const SizedBox(height: 16),
              ],
            ),
          ),
          ChatInputBar(hintText: 'Message all chat...', onSend: () {}),
        ],
      ),
    );
  }
}

class _ChatMessage extends StatelessWidget {
  final String senderName;
  final String teamName;
  final String message;
  final int timeAgo;
  final Color nameColor;

  const _ChatMessage({
    required this.senderName,
    required this.teamName,
    required this.message,
    required this.timeAgo,
    required this.nameColor,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: const Color(0xFF334155),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Text(
                senderName,
                style: TextStyle(
                  color: nameColor,
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(width: 8),
              Text(
                teamName,
                style: TextStyle(color: Colors.white54, fontSize: 14),
              ),
              const Spacer(),
              Text(
                '${timeAgo}m ago',
                style: TextStyle(color: Colors.white38, fontSize: 13),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(message, style: TextStyle(color: Colors.white, fontSize: 15)),
        ],
      ),
    );
  }
}
