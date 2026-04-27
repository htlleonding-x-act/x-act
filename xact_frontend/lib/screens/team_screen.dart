import 'package:flutter/material.dart';

import '../api/api_service.dart';
import '../widgets/team/team_card.dart';

class TeamScreen extends StatefulWidget {
  const TeamScreen({super.key});

  @override
  State<TeamScreen> createState() => _TeamScreenState();
}

class _TeamScreenState extends State<TeamScreen> {
  late final Future<List<TeamCardData>> _load;

  @override
  void initState() {
    super.initState();
    _load = ApiService.instance.loadTeamCards();
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      color: const Color(0xFF0F172A),
      child: FutureBuilder<List<TeamCardData>>(
        future: _load,
        builder: (context, snapshot) {
          if (snapshot.hasError) {
            return Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const Text(
                      'Failed to load teams',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 16,
                        fontWeight: FontWeight.w600,
                      ),
                      textAlign: TextAlign.center,
                    ),
                    const SizedBox(height: 8),
                    Text(
                      snapshot.error.toString(),
                      style: const TextStyle(
                        color: Colors.white70,
                        fontSize: 13,
                      ),
                      textAlign: TextAlign.center,
                    ),
                  ],
                ),
              ),
            );
          }

          final teams = snapshot.data;
          if (teams == null) {
            return const Center(
              child: Padding(
                padding: EdgeInsets.all(24),
                child: CircularProgressIndicator(),
              ),
            );
          }

          final visibleTeams = teams
              .where((team) => team.members.isNotEmpty)
              .toList(growable: false);

          if (visibleTeams.isEmpty) {
            return const Center(
              child: Padding(
                padding: EdgeInsets.all(24),
                child: Text(
                  'No teams found',
                  style: TextStyle(color: Colors.white70),
                ),
              ),
            );
          }

          return ListView.separated(
            padding: const EdgeInsets.all(16),
            itemCount: visibleTeams.length,
            separatorBuilder: (context, index) => const SizedBox(height: 12),
            itemBuilder: (context, index) {
              final team = visibleTeams[index];
              return TeamCard(
                teamName: team.teamName,
                role: team.role,
                color: team.color,
                members: team.members,
                isMisterX: team.isMisterX,
              );
            },
          );
        },
      ),
    );
  }
}
