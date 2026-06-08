import 'dart:async';

import 'package:flutter/material.dart';

import '../api/api_service.dart';
import '../api/models.dart';
import '../services/app_session.dart';
import '../services/chat_notification_service.dart';
import '../widgets/chat_input_bar.dart';
import '../widgets/chat_message_list.dart';
import '../widgets/xact_branding.dart';

class AllChatScreen extends StatefulWidget {
  const AllChatScreen({super.key});

  @override
  State<AllChatScreen> createState() => _AllChatScreenState();
}

class _AllChatScreenState extends State<AllChatScreen> {
  final List<ChatMessage> _messages = [];
  StreamSubscription<RealtimeEventEnvelope>? _eventSubscription;
  bool _loading = true;
  bool _failed = false;

  @override
  void initState() {
    super.initState();
    _eventSubscription = ApiService.instance.realtimeEvents.listen(_onEvent);
    unawaited(_init());
    ChatNotificationService.instance.markAllChatRead();
  }

  @override
  void dispose() {
    _eventSubscription?.cancel();
    super.dispose();
  }

  Future<void> _init() async {
    // Make sure the realtime connection is started and subscribed to the
    // session group before we rely on _onEvent for live updates.
    try {
      await ApiService.instance.ensureSessionChannelSubscription();
    } catch (_) {
      // Realtime is best-effort; history still loads below.
    }
    await _loadHistory();
  }

  Future<void> _loadHistory() async {
    try {
      final messages = await ApiService.instance.loadAllChatMessages();
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
    if (!message.isGlobal || _messages.any((m) => m.id == message.id)) {
      return;
    }
    if (!mounted) {
      return;
    }

    setState(() => _messages.add(message));
    ChatNotificationService.instance.markAllChatRead();
  }

  void _handleSend(String text) {
    unawaited(_send(text));
  }

  Future<void> _send(String text) async {
    try {
      final message = await ApiService.instance.sendAllChatMessage(text);
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
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'All Chat',
                        style: XActText.heading.copyWith(fontSize: 17),
                      ),
                      const SizedBox(height: 2),
                      Row(
                        children: [
                          Container(
                            width: 6,
                            height: 6,
                            decoration: const BoxDecoration(
                              shape: BoxShape.circle,
                              color: XActColors.success,
                            ),
                          ),
                          const SizedBox(width: 6),
                          Text(
                            'Everyone sees this',
                            style: XActText.caption.copyWith(fontSize: 12),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          Expanded(child: _buildBody()),
          ChatInputBar(
            hintText: 'Message everyone…',
            onSend: _handleSend,
          ),
        ],
      ),
    );
  }

  Widget _buildBody() {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_failed && _messages.isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Text(
            'Could not load chat. Reopen this tab to retry.',
            style: XActText.bodySm.copyWith(color: XActColors.text3),
            textAlign: TextAlign.center,
          ),
        ),
      );
    }

    return ChatMessageList(
      messages: _messages,
      currentMemberId: AppSession.instance.currentMemberId,
      emptyLabel: 'No messages yet. Say hello to everyone!',
    );
  }
}
