import 'package:latlong2/latlong.dart';

/// Holds the game-area polygon set by the host during lobby setup.
/// Points survive navigation (they live for the app session) so the game
/// map can display the polygon without any backend round-trip.
class GeofenceStore {
  GeofenceStore._();
  static final GeofenceStore instance = GeofenceStore._();

  List<LatLng> _points = const [];

  /// The saved polygon vertices in sequence order. Unmodifiable.
  List<LatLng> get points => List.unmodifiable(_points);

  /// Replaces the stored polygon with [points].
  void setPoints(List<LatLng> points) => _points = List.of(points);

  /// Clears all stored points.
  void clear() => _points = const [];
}
