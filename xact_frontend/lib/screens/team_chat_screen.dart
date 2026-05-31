import 'package:flutter/material.dart';
import '../widgets/chat_input_bar.dart';
import '../widgets/xact_branding.dart';

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

        final teamName = header?.teamName ?? (hasError ? 'Team Chat' : 'Loading…');
        final teamColor = header == null
            ? XActColors.roleDetective
            : XActColors.roleColor(header.role);

        final subtitle = header == null
            ? (hasError ? 'Failed to load team info' : 'Loading team info…')
            : '${header.memberCount} teammates · private';

        return Container(
          color: XActColors.bg,
          child: Column(
            children: [
              Container(
                padding: const EdgeInsets.fromLTRB(16, 12, 16, 14),
                decoration: BoxDecoration(
                  border: Border(
                    bottom: BorderSide(color: XActColors.hairlineSoft),
                  ),
                ),
                child: Row(
                  children: [
                    Container(
                      width: 8,
                      height: 8,
                      decoration: BoxDecoration(
                        shape: BoxShape.circle,
                        color: teamColor,
                        boxShadow: [
                          BoxShadow(
                            color: teamColor.withValues(alpha: .6),
                            blurRadius: 8,
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            teamName,
                            style: XActText.heading.copyWith(fontSize: 17),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                          const SizedBox(height: 2),
                          Text(
                            subtitle,
                            style: XActText.caption.copyWith(fontSize: 12),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
              Expanded(
                child: Center(
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Text(
                      'Team-chat is waiting for backend realtime integration.',
                      style: XActText.bodySm.copyWith(color: XActColors.text3),
                      textAlign: TextAlign.center,
                    ),
                  ),
                ),
              ),
              ChatInputBar(
                hintText: 'Message your team…',
                leadingIcon: Icons.location_on_rounded,
                onSend: null,
              ),
            ],
          ),
        );
      },
    );
  }
}
