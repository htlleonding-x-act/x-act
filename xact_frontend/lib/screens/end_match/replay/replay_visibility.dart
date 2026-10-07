import 'package:flutter/foundation.dart';

enum ReplayLayer { gameArea, wholeRoutes, trails, sightings, catches }

/// which players and map layers the replay draws. it notifies only when the
/// viewer toggles something, so the layers that never move can listen to it
/// without rebuilding on every frame of the playback
class ReplayVisibility extends ChangeNotifier {
  final Set<int> _hiddenMemberIds = {};
  final Set<ReplayLayer> _hiddenLayers = {};

  bool isMemberHidden(int memberId) => _hiddenMemberIds.contains(memberId);

  bool isLayerVisible(ReplayLayer layer) => !_hiddenLayers.contains(layer);

  void toggleMember(int memberId) {
    if (!_hiddenMemberIds.remove(memberId)) {
      _hiddenMemberIds.add(memberId);
    }
    notifyListeners();
  }

  void showMember(int memberId) {
    if (_hiddenMemberIds.remove(memberId)) {
      notifyListeners();
    }
  }

  void setAllMembersHidden(Iterable<int> memberIds, {required bool hidden}) {
    if (hidden) {
      _hiddenMemberIds.addAll(memberIds);
    } else {
      _hiddenMemberIds.removeAll(memberIds);
    }
    notifyListeners();
  }

  void toggleLayer(ReplayLayer layer) {
    if (!_hiddenLayers.remove(layer)) {
      _hiddenLayers.add(layer);
    }
    notifyListeners();
  }
}
