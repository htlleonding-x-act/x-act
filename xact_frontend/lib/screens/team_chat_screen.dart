import 'package:flutter/material.dart';
import '../widgets/chat_input_bar.dart';

class TeamChatScreen extends StatelessWidget {
  const TeamChatScreen({super.key});

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
                  'Detectives Alpha',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 20,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  'Private team chat • 3 members',
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
                  senderName: 'Mike',
                  message: 'Keep your eyes open for any movement',
                  timeAgo: 6,
                  senderColor: Colors.blue,
                ),
                const SizedBox(height: 12),
                _ChatMessage(
                  message: "Sounds good, I'll head east",
                  timeAgo: 3,
                  isCurrentUser: true,
                ),
              ],
            ),
          ),
          ChatInputBar(hintText: 'Message your team...', onSend: () {}),
        ],
      ),
    );
  }
}

class _ChatMessage extends StatelessWidget {
  final String? senderName;
  final String message;
  final int timeAgo;
  final bool isCurrentUser;
  final Color? senderColor;

  const _ChatMessage({
    this.senderName,
    required this.message,
    required this.timeAgo,
    this.isCurrentUser = false,
    this.senderColor,
  });

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: isCurrentUser ? Alignment.centerRight : Alignment.centerLeft,
      child: Container(
        constraints: BoxConstraints(
          maxWidth: MediaQuery.of(context).size.width * 0.75,
        ),
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: isCurrentUser
              ? const Color(0xFF3B82F6)
              : const Color(0xFF334155),
          borderRadius: BorderRadius.circular(16),
        ),
        child: Column(
          crossAxisAlignment: isCurrentUser
              ? CrossAxisAlignment.end
              : CrossAxisAlignment.start,
          children: [
            if (!isCurrentUser && senderName != null) ...[
              Text(
                senderName!,
                style: TextStyle(
                  color: senderColor ?? const Color(0xFF60A5FA),
                  fontWeight: FontWeight.bold,
                  fontSize: 14,
                ),
              ),
              const SizedBox(height: 8),
            ],
            Text(
              message,
              style: const TextStyle(color: Colors.white, fontSize: 15),
            ),
            const SizedBox(height: 4),
            Text(
              '${timeAgo}m ago',
              style: TextStyle(
                color: isCurrentUser ? Colors.white70 : Colors.white54,
                fontSize: 12,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
