import 'package:latlong2/latlong.dart';

/// holds the game area polygon the host set during lobby setup. the points
/// live for the whole app session, so the game map can show the polygon
/// without asking the backend
class GeofenceStore {
  GeofenceStore._();
  static final GeofenceStore instance = GeofenceStore._();

  List<LatLng> _points = const [];

  /// in sequence order
  List<LatLng> get points => List.unmodifiable(_points);

  void setPoints(List<LatLng> points) => _points = List.of(points);

  void clear() => _points = const [];
}
