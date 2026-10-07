import 'package:flutter/material.dart';

import '../../../widgets/xact_branding.dart';
import 'replay_tracks.dart';
import 'replay_visibility.dart';

Future<void> showReplayLayersSheet(
  BuildContext context, {
  required ReplayTrackSet tracks,
  required ReplayVisibility visibility,
}) {
  return showModalBottomSheet<void>(
    context: context,
    backgroundColor: XActColors.surface,
    isScrollControlled: true,
    useSafeArea: true,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
    ),
    builder: (_) => _ReplayLayersSheet(tracks: tracks, visibility: visibility),
  );
}

class _ReplayLayersSheet extends StatelessWidget {
  const _ReplayLayersSheet({required this.tracks, required this.visibility});

  // the map behind stays visible, so toggling something shows its effect
  static const double _maxHeightShare = .6;

  final ReplayTrackSet tracks;
  final ReplayVisibility visibility;

  static (String, String, IconData) _describe(ReplayLayer layer) =>
      switch (layer) {
        ReplayLayer.gameArea => (
          'Game area',
          'Border of the playing field',
          Icons.pentagon_outlined,
        ),
        ReplayLayer.wholeRoutes => (
          'Whole routes',
          'Faded preview of every route',
          Icons.timeline_rounded,
        ),
        ReplayLayer.trails => (
          'Trails',
          'The way walked so far',
          Icons.route_rounded,
        ),
        ReplayLayer.sightings => (
          'Mister X sightings',
          'Where the hunters saw Mister X',
          Icons.visibility_rounded,
        ),
        ReplayLayer.catches => (
          'Catches',
          'Where Mister X was caught',
          Icons.back_hand_rounded,
        ),
      };

  @override
  Widget build(BuildContext context) {
    return ConstrainedBox(
      constraints: BoxConstraints(
        maxHeight: MediaQuery.sizeOf(context).height * _maxHeightShare,
      ),
      child: ListenableBuilder(
        listenable: visibility,
        builder: (context, _) {
          final memberIds = [for (final t in tracks.tracks) t.member.memberId];
          final allHidden = memberIds.every(visibility.isMemberHidden);

          return ListView(
            shrinkWrap: true,
            padding: const EdgeInsets.fromLTRB(
              XActSpace.s2,
              XActSpace.s3,
              XActSpace.s2,
              XActSpace.s5,
            ),
            children: [
              Center(
                child: Container(
                  width: 36,
                  height: 4,
                  decoration: BoxDecoration(
                    color: XActColors.hairlineSoft,
                    borderRadius: BorderRadius.circular(2),
                  ),
                ),
              ),
              const SizedBox(height: XActSpace.s4),
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: XActSpace.s4),
                child: XActBranding.buildEyebrow('Map'),
              ),
              for (final layer in ReplayLayer.values) _buildLayerTile(layer),
              const SizedBox(height: XActSpace.s3),
              Padding(
                padding: const EdgeInsets.only(left: XActSpace.s4),
                child: Row(
                  children: [
                    Expanded(child: XActBranding.buildEyebrow('Players')),
                    TextButton(
                      onPressed: () => visibility.setAllMembersHidden(
                        memberIds,
                        hidden: !allHidden,
                      ),
                      child: Text(allHidden ? 'Show all' : 'Hide all'),
                    ),
                  ],
                ),
              ),
              for (final track in tracks.tracks) _buildPlayerTile(track),
            ],
          );
        },
      ),
    );
  }

  Widget _buildLayerTile(ReplayLayer layer) {
    final (title, subtitle, icon) = _describe(layer);
    return SwitchListTile(
      value: visibility.isLayerVisible(layer),
      onChanged: (_) => visibility.toggleLayer(layer),
      secondary: Icon(icon, color: XActColors.text2),
      title: Text(title, style: XActText.bodySm),
      subtitle: Text(
        subtitle,
        style: XActText.caption.copyWith(color: XActColors.text4),
      ),
    );
  }

  Widget _buildPlayerTile(MemberTrack track) {
    final memberId = track.member.memberId;
    return SwitchListTile(
      value: !visibility.isMemberHidden(memberId),
      onChanged: (_) => visibility.toggleMember(memberId),
      secondary: Container(
        width: 12,
        height: 12,
        decoration: BoxDecoration(color: track.color, shape: BoxShape.circle),
      ),
      title: Text(
        track.member.displayName,
        style: XActText.bodySm,
        maxLines: 1,
        overflow: TextOverflow.ellipsis,
      ),
    );
  }
}
