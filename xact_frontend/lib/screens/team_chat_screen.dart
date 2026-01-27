import 'package:flutter/material.dart';
import '../widgets/chat_input_bar.dart';

import '../api/api_service.dart';

class TeamChatScreen extends StatefulWidget {
  const TeamChatScreen({super.key});

  @override
  State<TeamChatScreen> createState() => _TeamChatScreenState();
}

class _TeamChatScreenState extends State<TeamChatScreen> {
  late final Future<TeamChatHeaderData> _loadHeader;

  @override
  void initState() {
    super.initState();
    _loadHeader = ApiService.instance.loadTeamChatHeader();
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<TeamChatHeaderData>(
      future: _loadHeader,
      builder: (context, snapshot) {
        final header = snapshot.data;
        final hasError = snapshot.hasError;

        final teamName =
            header?.teamName ?? (hasError ? 'Team Chat' : 'Loading...');

        final memberCountText = header == null
            ? (hasError ? 'Failed to load team info' : 'Loading team info...')
            : 'Private team chat • ${header.memberCount} members';

        final teamColor = header?.teamColor;

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
                      teamName,
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 20,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      memberCountText,
                      style: const TextStyle(
                        color: Colors.white54,
                        fontSize: 14,
                      ),
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
                      senderColor: teamColor,
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
      },
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
