import 'package:flutter/material.dart';
import '../widgets/chat_input_bar.dart';
import '../widgets/xact_branding.dart';

class AllChatScreen extends StatelessWidget {
  const AllChatScreen({super.key});

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
          Expanded(
            child: Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Text(
                  'All-chat is waiting for backend realtime integration.',
                  style: XActText.bodySm.copyWith(color: XActColors.text3),
                  textAlign: TextAlign.center,
                ),
              ),
            ),
          ),
          const ChatInputBar(hintText: 'Message everyone…', onSend: null),
        ],
      ),
    );
  }
}
