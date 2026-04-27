import 'package:flutter/material.dart';
import '../widgets/chat_input_bar.dart';
import '../widgets/team/team_name_role_label.dart';

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

        final fallbackTeamName = hasError ? 'Team Chat' : 'Loading...';

        final memberCountText = header == null
            ? (hasError ? 'Failed to load team info' : 'Loading team info...')
            : 'Private team chat • ${header.memberCount} members';

        return Container(
          color: const Color(0xFF1E293B),
          child: Column(
            children: [
              Container(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.center,
                  children: [
                    if (header == null)
                      Text(
                        fallbackTeamName,
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 20,
                          fontWeight: FontWeight.bold,
                        ),
                      )
                    else
                      TeamNameRoleLabel(
                        teamName: header.teamName,
                        role: header.role,
                        teamNameStyle: const TextStyle(
                          color: Colors.white,
                          fontSize: 20,
                          fontWeight: FontWeight.bold,
                        ),
                        roleStyle: const TextStyle(
                          color: Color(0xFFBFDBFE),
                          fontSize: 18,
                          fontWeight: FontWeight.w700,
                        ),
                        textAlign: TextAlign.center,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
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
                child: const Center(
                  child: Padding(
                    padding: EdgeInsets.all(24),
                    child: Text(
                      'Team-chat is waiting for backend realtime integration.',
                      style: TextStyle(color: Colors.white70, fontSize: 14),
                      textAlign: TextAlign.center,
                    ),
                  ),
                ),
              ),
              ChatInputBar(hintText: 'Message your team...', onSend: null),
            ],
          ),
        );
      },
    );
  }
}
