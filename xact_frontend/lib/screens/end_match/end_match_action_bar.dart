import 'package:flutter/material.dart';

import '../../widgets/xact_branding.dart';

class EndMatchActionBar extends StatelessWidget {
  const EndMatchActionBar({
    super.key,
    required this.isHost,
    required this.loading,
    required this.busy,
    required this.onRematch,
    required this.onLeave,
  });

  final bool isHost;
  final bool loading;

  /// a rematch or leave is running, so both buttons wait
  final bool busy;
  final VoidCallback onRematch;
  final VoidCallback onLeave;

  // below this the two buttons stack, their labels don't fit side by side
  static const double _rowBreakpoint = 360;

  @override
  Widget build(BuildContext context) {
    final rematchButton = XActBranding.buildSecondaryButton(
      text: 'Back to the Lobby',
      icon: Icons.meeting_room_rounded,
      onPressed: busy ? null : onRematch,
      height: 52,
    );
    final leaveButton = XActBranding.buildGhostButton(
      text: 'Leave Lobby',
      icon: Icons.logout_rounded,
      onPressed: busy ? null : onLeave,
      height: 52,
    );

    return Container(
      width: double.infinity,
      decoration: BoxDecoration(
        color: XActColors.bg2.withValues(alpha: .96),
        border: Border(top: BorderSide(color: XActColors.hairlineSoft)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: .35),
            blurRadius: 24,
            offset: const Offset(0, -8),
          ),
        ],
      ),
      child: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.all(XActSpace.s4),
          child: LayoutBuilder(
            builder: (context, constraints) {
              final sideBySide = constraints.maxWidth >= _rowBreakpoint;

              return Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                mainAxisSize: MainAxisSize.min,
                children: [
                  if (busy)
                    const Padding(
                      padding: EdgeInsets.only(bottom: XActSpace.s3),
                      child: LinearProgressIndicator(
                        minHeight: 3,
                        backgroundColor: Colors.transparent,
                        color: XActColors.secondary,
                      ),
                    ),
                  if (!isHost && !loading)
                    Padding(
                      padding: const EdgeInsets.only(bottom: XActSpace.s3),
                      child: Text(
                        'Waiting for the host to start a new lobby…',
                        textAlign: TextAlign.center,
                        style: XActText.caption.copyWith(
                          color: XActColors.text4,
                        ),
                      ),
                    ),
                  if (isHost && sideBySide)
                    Row(
                      children: [
                        Expanded(child: rematchButton),
                        const SizedBox(width: XActSpace.s3),
                        Expanded(child: leaveButton),
                      ],
                    )
                  else ...[
                    if (isHost) ...[
                      rematchButton,
                      const SizedBox(height: XActSpace.s3),
                    ],
                    leaveButton,
                  ],
                ],
              );
            },
          ),
        ),
      ),
    );
  }
}
