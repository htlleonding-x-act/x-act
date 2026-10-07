import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';

// luminance weights, so the osm tiles turn grey and player colors stand out
const ColorFilter _greyscaleTileFilter = ColorFilter.matrix(<double>[
  0.2126, 0.7152, 0.0722, 0, 0, //
  0.2126, 0.7152, 0.0722, 0, 0, //
  0.2126, 0.7152, 0.0722, 0, 0, //
  0, 0, 0, 1, 0, //
]);

TileLayer buildGreyscaleTileLayer() => TileLayer(
  urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
  userAgentPackageName: 'com.xact.app',
  tileBuilder: (context, tileWidget, tile) =>
      ColorFiltered(colorFilter: _greyscaleTileFilter, child: tileWidget),
);
