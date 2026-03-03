import 'package:flutter/material.dart';

/// Data model for a team in the lobby.
class TeamData {
  String name;
  Color color;
  int maxPlayers;
  List<String> players;
  bool isMisterX;

  /// When `false` the team cannot be deleted (Mister X + first detective team).
  bool isDeletable;

  TeamData({
    required this.name,
    required this.color,
    this.maxPlayers = 3,
    List<String>? players,
    this.isMisterX = false,
    this.isDeletable = true,
  }) : players = players ?? [];
}
