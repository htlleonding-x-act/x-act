import 'package:flutter/material.dart';
import 'package:xact_frontend/api/models.dart';

class LobbyPlayer {
  final int memberId;
  final int teamId;
  final int? userId;
  final String name;
  final bool isCurrentUser;
  final bool isTeamLeader;

  const LobbyPlayer({
    required this.memberId,
    required this.teamId,
    required this.userId,
    required this.name,
    required this.isCurrentUser,
    required this.isTeamLeader,
  });
}

/// Data model for a team in the lobby.
class TeamData {
  final int teamId;
  final TeamRole role;
  String name;
  Color color;
  int maxPlayers;
  List<LobbyPlayer> players;

  /// When `false` the team cannot be deleted (Mister X + first detective team).
  bool isDeletable;

  TeamData({
    required this.teamId,
    required this.role,
    required this.name,
    required this.color,
    this.maxPlayers = 3,
    List<LobbyPlayer>? players,
    this.isDeletable = true,
  }) : players = players ?? [];

  bool get isMisterX => role == TeamRole.mrX;
  bool get isSpectator => role == TeamRole.spectator;
}
