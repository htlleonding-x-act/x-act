import 'package:flutter/material.dart';

import '../../../widgets/xact_branding.dart';
import 'replay_controller.dart';
import 'replay_tracks.dart';

/// a legend over the map. tap a player to follow them, long press to hide them
class ReplayMemberChips extends StatelessWidget {
  const ReplayMemberChips({
    super.key,
    required this.tracks,
    required this.controller,
  });

  final ReplayTrackSet tracks;
  final ReplayController controller;

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: controller,
      builder: (context, _) => SizedBox(
        height: 34,
        child: ListView.separated(
          scrollDirection: Axis.horizontal,
          padding: const EdgeInsets.symmetric(horizontal: XActSpace.s3),
          itemCount: tracks.tracks.length,
          separatorBuilder: (_, _) => const SizedBox(width: XActSpace.s2),
          itemBuilder: (context, index) {
            final track = tracks.tracks[index];
            final memberId = track.member.memberId;
            final focused = controller.focusedMemberId == memberId;
            final hidden = controller.hiddenMemberIds.contains(memberId);

            return Opacity(
              opacity: hidden ? .4 : 1,
              child: Material(
                color: focused ? track.color.withValues(alpha: .3) : XActColors.glass,
                shape: StadiumBorder(
                  side: BorderSide(
                    color: focused ? track.color : XActColors.hairlineSoft,
                  ),
                ),
                child: InkWell(
                  customBorder: const StadiumBorder(),
                  onTap: () => controller.focusMember(focused ? null : memberId),
                  onLongPress: () => controller.toggleHidden(memberId),
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: XActSpace.s3),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Container(
                          width: 8,
                          height: 8,
                          decoration: BoxDecoration(
                            color: track.color,
                            shape: BoxShape.circle,
                          ),
                        ),
                        const SizedBox(width: 6),
                        Text(
                          track.member.displayName,
                          style: XActText.caption.copyWith(
                            color: XActColors.text1,
                            decoration: hidden ? TextDecoration.lineThrough : null,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            );
          },
        ),
      ),
    );
  }
}
