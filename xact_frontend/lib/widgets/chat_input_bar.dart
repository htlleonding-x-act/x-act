import 'package:flutter/material.dart';

import 'xact_branding.dart';

class ChatInputBar extends StatefulWidget {
  final String hintText;

  /// Called with the trimmed message text when the user sends. When `null`,
  /// the input is shown in a disabled state.
  final ValueChanged<String>? onSend;
  final IconData leadingIcon;

  const ChatInputBar({
    super.key,
    required this.hintText,
    this.onSend,
    this.leadingIcon = Icons.add_rounded,
  });

  @override
  State<ChatInputBar> createState() => _ChatInputBarState();
}

class _ChatInputBarState extends State<ChatInputBar> {
  final TextEditingController _controller = TextEditingController();
  bool _hasText = false;

  @override
  void initState() {
    super.initState();
    _controller.addListener(_handleTextChanged);
  }

  @override
  void dispose() {
    _controller.removeListener(_handleTextChanged);
    _controller.dispose();
    super.dispose();
  }

  void _handleTextChanged() {
    final hasText = _controller.text.trim().isNotEmpty;
    if (hasText != _hasText) {
      setState(() => _hasText = hasText);
    }
  }

  void _send() {
    final onSend = widget.onSend;
    final text = _controller.text.trim();
    if (onSend == null || text.isEmpty) {
      return;
    }

    onSend(text);
    _controller.clear();
  }

  @override
  Widget build(BuildContext context) {
    final enabled = widget.onSend != null;
    final canSend = enabled && _hasText;

    return Container(
      padding: const EdgeInsets.fromLTRB(12, 10, 12, 12),
      decoration: BoxDecoration(
        color: XActColors.bg,
        border: Border(top: BorderSide(color: XActColors.hairlineSoft)),
      ),
      child: SafeArea(
        top: false,
        child: Row(
          children: [
            XActBranding.circleIconButton(
              icon: widget.leadingIcon,
              onPressed: () {},
            ),
            const SizedBox(width: 8),
            Expanded(
              child: SizedBox(
                height: 44,
                child: TextField(
                  controller: _controller,
                  enabled: enabled,
                  style: XActText.body.copyWith(fontSize: 15),
                  cursorColor: XActColors.secondary,
                  textInputAction: TextInputAction.send,
                  onSubmitted: (_) => _send(),
                  decoration: InputDecoration(
                    hintText: widget.hintText,
                    hintStyle: XActText.body.copyWith(
                      fontSize: 15,
                      color: XActColors.text4,
                    ),
                    filled: true,
                    fillColor: Colors.white.withValues(alpha: .03),
                    contentPadding: const EdgeInsets.symmetric(
                      horizontal: 16,
                      vertical: 0,
                    ),
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(14),
                      borderSide: BorderSide(color: XActColors.hairlineSoft),
                    ),
                    enabledBorder: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(14),
                      borderSide: BorderSide(color: XActColors.hairlineSoft),
                    ),
                    focusedBorder: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(14),
                      borderSide: const BorderSide(
                        color: XActColors.secondary,
                        width: 2,
                      ),
                    ),
                  ),
                ),
              ),
            ),
            const SizedBox(width: 8),
            Opacity(
              opacity: canSend ? 1 : .4,
              child: Container(
                width: 44,
                height: 44,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  gradient: const LinearGradient(
                    begin: Alignment.topCenter,
                    end: Alignment.bottomCenter,
                    colors: [
                      XActColors.secondaryLight,
                      XActColors.secondaryDark,
                    ],
                  ),
                  boxShadow: canSend ? XActElevation.glowBlue : null,
                ),
                child: IconButton(
                  icon: const Icon(Icons.send_rounded, color: Colors.white),
                  iconSize: 18,
                  onPressed: canSend ? _send : null,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
