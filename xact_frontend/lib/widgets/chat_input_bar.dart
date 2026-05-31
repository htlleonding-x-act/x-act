import 'package:flutter/material.dart';

import 'xact_branding.dart';

class ChatInputBar extends StatelessWidget {
  final String hintText;
  final VoidCallback? onSend;
  final IconData leadingIcon;

  const ChatInputBar({
    super.key,
    required this.hintText,
    this.onSend,
    this.leadingIcon = Icons.add_rounded,
  });

  @override
  Widget build(BuildContext context) {
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
              icon: leadingIcon,
              onPressed: () {},
            ),
            const SizedBox(width: 8),
            Expanded(
              child: SizedBox(
                height: 44,
                child: TextField(
                  style: XActText.body.copyWith(fontSize: 15),
                  cursorColor: XActColors.secondary,
                  decoration: InputDecoration(
                    hintText: hintText,
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
                  textInputAction: TextInputAction.send,
                ),
              ),
            ),
            const SizedBox(width: 8),
            Container(
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
                boxShadow: XActElevation.glowBlue,
              ),
              child: IconButton(
                icon: const Icon(Icons.send_rounded, color: Colors.white),
                iconSize: 18,
                onPressed: onSend,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
