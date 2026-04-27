import 'package:flutter/material.dart';
import 'package:xact_frontend/api/models.dart';

/// Displays a team name followed by a role badge.
class TeamNameRoleLabel extends StatelessWidget {
  final String teamName;
  final TeamRole? role;
  final TextStyle? teamNameStyle;
  final TextStyle? roleStyle;
  final bool includeSpectatorRole;
  final TextAlign textAlign;
  final int? maxLines;
  final TextOverflow? overflow;

  const TeamNameRoleLabel({
    super.key,
    required this.teamName,
    required this.role,
    this.teamNameStyle,
    this.roleStyle,
    this.includeSpectatorRole = false,
    this.textAlign = TextAlign.start,
    this.maxLines,
    this.overflow,
  });

  @override
  Widget build(BuildContext context) {
    final normalizedName = teamName.trim().isEmpty ? 'Team' : teamName.trim();
    final resolvedTeamStyle =
        teamNameStyle ?? DefaultTextStyle.of(context).style;
    final roleLabel = _resolveRoleLabel(role, includeSpectatorRole);

    if (roleLabel == null) {
      return Text(
        normalizedName,
        style: resolvedTeamStyle,
        textAlign: textAlign,
        maxLines: maxLines,
        overflow: overflow,
      );
    }

    final badgeColor = _roleColor(role);
    final resolvedRoleStyle = roleStyle ??
        TextStyle(
          color: badgeColor,
          fontSize: (resolvedTeamStyle.fontSize ?? 14) - 2,
          fontWeight: FontWeight.w700,
          height: 1.1,
        );

    return Wrap(
      spacing: 8,
      runSpacing: 4,
      crossAxisAlignment: WrapCrossAlignment.center,
      alignment: _wrapAlignment(textAlign),
      children: [
        Text(
          normalizedName,
          style: resolvedTeamStyle,
          textAlign: textAlign,
          maxLines: maxLines,
          overflow: overflow,
        ),
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
          decoration: BoxDecoration(
            color: badgeColor.withAlpha(38),
            borderRadius: BorderRadius.circular(999),
            border: Border.all(color: badgeColor.withAlpha(140), width: 1),
          ),
          child: Text(roleLabel, style: resolvedRoleStyle),
        ),
      ],
    );
  }

  String? _resolveRoleLabel(TeamRole? currentRole, bool includeSpectator) {
    if (currentRole == null) {
      return null;
    }

    if (!includeSpectator && currentRole == TeamRole.spectator) {
      return null;
    }

    return teamRoleDisplayLabel(currentRole);
  }

  Color _roleColor(TeamRole? currentRole) {
    return switch (currentRole) {
      TeamRole.mrX => const Color(0xFFFCA5A5),
      TeamRole.detective => const Color(0xFF93C5FD),
      TeamRole.spectator => const Color(0xFFCBD5E1),
      null => Colors.white70,
    };
  }

  WrapAlignment _wrapAlignment(TextAlign align) {
    return switch (align) {
      TextAlign.center => WrapAlignment.center,
      TextAlign.end || TextAlign.right => WrapAlignment.end,
      _ => WrapAlignment.start,
    };
  }
}
