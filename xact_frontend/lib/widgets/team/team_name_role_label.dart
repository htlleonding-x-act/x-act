import 'package:flutter/material.dart';
import 'package:xact_frontend/api/models.dart';

import '../xact_branding.dart';

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

    final roleColor = XActColors.roleColor(role);
    final resolvedRoleStyle = roleStyle ??
        TextStyle(
          color: roleColor,
          fontSize: (resolvedTeamStyle.fontSize ?? 14) - 2,
          fontWeight: FontWeight.w700,
          height: 1.1,
        );

    return Wrap(
      spacing: XActSpace.s2,
      runSpacing: XActSpace.s1,
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
          padding: const EdgeInsets.symmetric(
            horizontal: XActSpace.s2,
            vertical: 3,
          ),
          decoration: BoxDecoration(
            color: roleColor.withValues(alpha: .16),
            borderRadius: XActRadius.pill,
          ),
          child: Text(roleLabel.toUpperCase(), style: resolvedRoleStyle),
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

  WrapAlignment _wrapAlignment(TextAlign align) {
    return switch (align) {
      TextAlign.center => WrapAlignment.center,
      TextAlign.end || TextAlign.right => WrapAlignment.end,
      _ => WrapAlignment.start,
    };
  }
}
