import 'package:flutter/material.dart';

import '../api/models.dart';
import 'xact_branding.dart';

/// Scrollable list of chat bubbles. Own messages align right, others align
/// left with a sender label. Auto-scrolls to the newest message.
class ChatMessageList extends StatefulWidget {
  final List<ChatMessage> messages;
  final int? currentMemberId;
  final String emptyLabel;

  /// Optional accent colour per message (e.g. by sender team in the All chat).
  final Color Function(ChatMessage message)? senderColorResolver;

  const ChatMessageList({
    super.key,
    required this.messages,
    required this.currentMemberId,
    this.emptyLabel = 'No messages yet. Say hello!',
    this.senderColorResolver,
  });

  @override
  State<ChatMessageList> createState() => _ChatMessageListState();
}

class _ChatMessageListState extends State<ChatMessageList> {
  final ScrollController _scrollController = ScrollController();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _jumpToBottom());
  }

  @override
  void didUpdateWidget(covariant ChatMessageList oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.messages.length != oldWidget.messages.length) {
      WidgetsBinding.instance.addPostFrameCallback((_) => _animateToBottom());
    }
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  void _jumpToBottom() {
    if (!_scrollController.hasClients) {
      return;
    }
    _scrollController.jumpTo(_scrollController.position.maxScrollExtent);
  }

  void _animateToBottom() {
    if (!_scrollController.hasClients) {
      return;
    }
    _scrollController.animateTo(
      _scrollController.position.maxScrollExtent,
      duration: const Duration(milliseconds: 220),
      curve: Curves.easeOut,
    );
  }

  @override
  Widget build(BuildContext context) {
    if (widget.messages.isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Text(
            widget.emptyLabel,
            style: XActText.bodySm.copyWith(color: XActColors.text3),
            textAlign: TextAlign.center,
          ),
        ),
      );
    }

    return ListView.builder(
      controller: _scrollController,
      padding: const EdgeInsets.fromLTRB(12, 12, 12, 8),
      itemCount: widget.messages.length,
      itemBuilder: (context, index) {
        final message = widget.messages[index];
        final isOwn =
            widget.currentMemberId != null &&
            message.senderMemberId == widget.currentMemberId;
        return _ChatBubble(
          message: message,
          isOwn: isOwn,
          accent: widget.senderColorResolver?.call(message),
        );
      },
    );
  }
}

class _ChatBubble extends StatelessWidget {
  final ChatMessage message;
  final bool isOwn;
  final Color? accent;

  const _ChatBubble({
    required this.message,
    required this.isOwn,
    this.accent,
  });

  @override
  Widget build(BuildContext context) {
    final bubbleColor = isOwn
        ? XActColors.secondary.withValues(alpha: .22)
        : Colors.white.withValues(alpha: .05);
    final borderColor = isOwn
        ? XActColors.secondary.withValues(alpha: .45)
        : XActColors.hairlineSoft;
    final nameColor = accent ?? (isOwn ? XActColors.secondary : XActColors.text2);

    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        mainAxisAlignment:
            isOwn ? MainAxisAlignment.end : MainAxisAlignment.start,
        children: [
          ConstrainedBox(
            constraints: BoxConstraints(
              maxWidth: MediaQuery.of(context).size.width * .72,
            ),
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
              decoration: BoxDecoration(
                color: bubbleColor,
                borderRadius: BorderRadius.only(
                  topLeft: const Radius.circular(14),
                  topRight: const Radius.circular(14),
                  bottomLeft: Radius.circular(isOwn ? 14 : 4),
                  bottomRight: Radius.circular(isOwn ? 4 : 14),
                ),
                border: Border.all(color: borderColor),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  if (!isOwn)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 2),
                      child: Text(
                        message.senderName,
                        style: XActText.caption.copyWith(
                          color: nameColor,
                          fontWeight: FontWeight.w700,
                          fontSize: 11,
                        ),
                      ),
                    ),
                  Text(
                    message.content,
                    style: XActText.body.copyWith(fontSize: 14, height: 1.3),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    _formatTime(message.sentAt.toLocal()),
                    style: XActText.caption.copyWith(
                      fontSize: 10,
                      color: XActColors.text4,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  static String _formatTime(DateTime time) {
    final hh = time.hour.toString().padLeft(2, '0');
    final mm = time.minute.toString().padLeft(2, '0');
    return '$hh:$mm';
  }
}
