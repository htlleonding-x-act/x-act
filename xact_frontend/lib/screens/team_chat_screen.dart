import 'dart:async';

import 'package:flutter/material.dart';

import '../api/api_service.dart';
import '../api/models.dart';
import '../services/app_session.dart';
import '../widgets/chat_input_bar.dart';
import '../widgets/chat_message_list.dart';
import '../widgets/xact_branding.dart';

class TeamChatScreen extends StatefulWidget {
  const TeamChatScreen({super.key});

  @override
  State<TeamChatScreen> createState() => _TeamChatScreenState();
}

class _TeamChatScreenState extends State<TeamChatScreen> {
  final List<ChatMessage> _messages = [];
  StreamSubscription<RealtimeEventEnvelope>? _eventSubscription;

  int? _teamId;
  TeamChatHeaderData? _header;
  bool _loading = true;
  bool _failed = false;

  @override
  void initState() {
    super.initState();
    _teamId = AppSession.instance.currentTeamId;
    _eventSubscription = ApiService.instance.realtimeEvents.listen(_onEvent);
    unawaited(_init());
  }

  @override
  void dispose() {
    _eventSubscription?.cancel();
    super.dispose();
  }

  Future<void> _init() async {
    final teamId = _teamId;
    if (teamId == null) {
      setState(() {
        _loading = false;
        _failed = true;
      });
      return;
    }

    // Header failure should not block the chat itself.
    try {
      final header = await ApiService.instance.loadTeamChatHeader();
      if (mounted) {
        setState(() => _header = header);
      }
    } catch (_) {
      // ignore, keep fallback header
    }

    try {
      await ApiService.instance.ensureTeamChannelSubscription(teamId: teamId);
    } catch (_) {
      // realtime is best-effort; history still loads below
    }

    await _loadHistory(teamId);
  }

  Future<void> _loadHistory(int teamId) async {
    try {
      final messages = await ApiService.instance.loadTeamChatMessages(
        teamId: teamId,
      );
      if (!mounted) {
        return;
      }
      setState(() {
        _messages
          ..clear()
          ..addAll(messages);
        _loading = false;
        _failed = false;
      });
    } catch (_) {
      if (!mounted) {
        return;
      }
      setState(() {
        _loading = false;
        _failed = true;
      });
    }
  }

  void _onEvent(RealtimeEventEnvelope envelope) {
    if (envelope.type != RealtimeEvents.chatMessagePosted) {
      return;
    }

    final message = ChatMessage.fromJson(envelope.payload);
    if (message.teamId == null ||
        message.teamId != _teamId ||
        _messages.any((m) => m.id == message.id)) {
      return;
    }
    if (!mounted) {
      return;
    }

    setState(() => _messages.add(message));
  }

  void _handleSend(String text) {
    unawaited(_send(text));
  }

  Future<void> _send(String text) async {
    final teamId = _teamId;
    if (teamId == null) {
      return;
    }

    try {
      final message = await ApiService.instance.sendTeamChatMessage(
        teamId: teamId,
        content: text,
      );
      if (mounted && !_messages.any((m) => m.id == message.id)) {
        setState(() => _messages.add(message));
      }
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Failed to send message.')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final header = _header;
    final teamName = header?.teamName ?? (_failed ? 'Team Chat' : 'Loading…');
    final teamColor = header == null
        ? XActColors.roleDetective
        : XActColors.roleColor(header.role);
    final subtitle = header == null
        ? (_failed ? 'Team info unavailable' : 'Loading team info…')
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
          Expanded(child: _buildBody()),
          ChatInputBar(
            hintText: 'Message your team…',
            leadingIcon: Icons.location_on_rounded,
            onSend: _teamId == null ? null : _handleSend,
          ),
        ],
      ),
    );
  }

  Widget _buildBody() {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_teamId == null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Text(
            'Join a team to use team chat.',
            style: XActText.bodySm.copyWith(color: XActColors.text3),
            textAlign: TextAlign.center,
          ),
        ),
      );
    }

    if (_failed && _messages.isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Text(
            'Could not load team chat. Reopen this tab to retry.',
            style: XActText.bodySm.copyWith(color: XActColors.text3),
            textAlign: TextAlign.center,
          ),
        ),
      );
    }

    return ChatMessageList(
      messages: _messages,
      currentMemberId: AppSession.instance.currentMemberId,
      emptyLabel: 'No team messages yet. Coordinate with your team!',
    );
  }
}
