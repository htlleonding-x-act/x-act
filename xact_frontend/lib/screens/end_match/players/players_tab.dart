import 'package:flutter/material.dart';

import '../../../api/game_results.dart';
import '../../../api/models.dart';
import '../../../widgets/xact_branding.dart';
import '../end_match_format.dart';
import 'leaderboard_row.dart';
import 'team_totals_card.dart';

enum LeaderboardMetric {
  distance('Distance'),
  topSpeed('Top speed'),
  pace('Pace'),
  mrXTime('Mister X time'),
  outOfBounds('Out of area'),
  powerUps('Power-ups');

  const LeaderboardMetric(this.label);

  final String label;
}

class PlayersTab extends StatefulWidget {
  const PlayersTab({
    super.key,
    required this.results,
    required this.currentMemberId,
    this.onWatchRoute,
  });

  final GameResults results;
  final int? currentMemberId;
  final ValueChanged<ResultMember>? onWatchRoute;

  @override
  State<PlayersTab> createState() => _PlayersTabState();
}

class _PlayersTabState extends State<PlayersTab> {
  LeaderboardMetric _metric = LeaderboardMetric.distance;
  bool _byTeam = false;

  @override
  Widget build(BuildContext context) {
    final results = widget.results;
    final teams = results.teams
        .where((t) => t.finalRole != TeamRole.spectator && t.memberCount > 0)
        .toList();
    final players = _sorted(results.players);

    return ListView(
      padding: const EdgeInsets.fromLTRB(
        XActSpace.s4,
        XActSpace.s3,
        XActSpace.s4,
        XActSpace.s6,
      ),
      children: [
        XActBranding.buildEyebrow('Teams'),
        const SizedBox(height: XActSpace.s3),
        SizedBox(
          height: 190,
          child: ListView.separated(
            scrollDirection: Axis.horizontal,
            itemCount: teams.length,
            separatorBuilder: (_, _) => const SizedBox(width: XActSpace.s3),
            itemBuilder: (context, index) => TeamTotalsCard(
              team: teams[index],
              accent: teamAccent(results, teams[index].teamId),
              isWinner: teams[index].teamId == results.winnerTeamId,
            ),
          ),
        ),
        const SizedBox(height: XActSpace.s5),
        Row(
          children: [
            Expanded(child: XActBranding.buildEyebrow('Leaderboard')),
            Text('By team', style: XActText.caption),
            Switch(
              value: _byTeam,
              onChanged: (value) => setState(() => _byTeam = value),
            ),
          ],
        ),
        const SizedBox(height: XActSpace.s2),
        SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: Row(
            children: [
              for (final metric in LeaderboardMetric.values)
                Padding(
                  padding: const EdgeInsets.only(right: XActSpace.s2),
                  child: ChoiceChip(
                    label: Text(metric.label),
                    selected: _metric == metric,
                    onSelected: (_) => setState(() => _metric = metric),
                  ),
                ),
            ],
          ),
        ),
        const SizedBox(height: XActSpace.s3),
        if (players.isEmpty)
          Text(
            'No players took part in this match.',
            style: XActText.bodySm.copyWith(color: XActColors.text3),
          )
        else if (_byTeam)
          ..._buildGroupedRows(teams, players)
        else
          ..._buildRows(players),
      ],
    );
  }

  List<Widget> _buildGroupedRows(List<ResultTeam> teams, List<ResultMember> players) {
    return [
      for (final team in teams) ...[
        Padding(
          padding: const EdgeInsets.only(top: XActSpace.s2, bottom: XActSpace.s2),
          child: Text(
            team.teamName,
            style: XActText.caption.copyWith(
              color: teamAccent(widget.results, team.teamId),
            ),
          ),
        ),
        ..._buildRows(players.where((p) => p.teamId == team.teamId).toList()),
      ],
    ];
  }

  List<Widget> _buildRows(List<ResultMember> players) {
    return [
      for (final (index, player) in players.indexed)
        Padding(
          padding: const EdgeInsets.only(bottom: XActSpace.s2),
          child: LeaderboardRow(
            key: ValueKey(player.memberId),
            rank: index + 1,
            member: player,
            team: widget.results.teamById(player.teamId),
            accent: teamAccent(widget.results, player.teamId),
            metricValue: _formatMetric(player.stats),
            isMe: player.memberId == widget.currentMemberId,
            onWatchRoute: widget.onWatchRoute == null
                ? null
                : () => widget.onWatchRoute!(player),
          ),
        ),
    ];
  }

  List<ResultMember> _sorted(List<ResultMember> players) {
    final sorted = [...players];
    if (_metric == LeaderboardMetric.pace) {
      // lower pace is faster, players without a pace go last
      sorted.sort((a, b) {
        final paceA = a.stats.avgPaceSecondsPerKm ?? double.infinity;
        final paceB = b.stats.avgPaceSecondsPerKm ?? double.infinity;
        return paceA.compareTo(paceB);
      });
    } else {
      sorted.sort((a, b) => _valueOf(b.stats).compareTo(_valueOf(a.stats)));
    }
    return sorted;
  }

  num _valueOf(MemberStats stats) => switch (_metric) {
    LeaderboardMetric.distance => stats.distanceMeters,
    LeaderboardMetric.topSpeed => stats.topSpeedKmh,
    LeaderboardMetric.pace => stats.avgPaceSecondsPerKm ?? 0,
    LeaderboardMetric.mrXTime => stats.mrXTime.inSeconds,
    LeaderboardMetric.outOfBounds => stats.outOfBounds.inSeconds,
    LeaderboardMetric.powerUps => stats.powerUpsUsed,
  };

  String _formatMetric(MemberStats stats) => switch (_metric) {
    LeaderboardMetric.distance => formatDistance(stats.distanceMeters),
    LeaderboardMetric.topSpeed => formatSpeed(stats.topSpeedKmh),
    LeaderboardMetric.pace => formatPace(stats.avgPaceSecondsPerKm),
    LeaderboardMetric.mrXTime => formatShortDuration(stats.mrXTime),
    LeaderboardMetric.outOfBounds => formatShortDuration(stats.outOfBounds),
    LeaderboardMetric.powerUps => '${stats.powerUpsUsed}',
  };
}
