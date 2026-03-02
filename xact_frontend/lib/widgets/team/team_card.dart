import 'package:flutter/material.dart';

/// A team card displayed on the in-game Team tab.
class TeamCard extends StatelessWidget {
  final String teamName;
  final Color color;
  final List<String> members;
  final bool isMisterX;

  const TeamCard({
    super.key,
    required this.teamName,
    required this.color,
    required this.members,
    this.isMisterX = false,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: const Color(0xFF1E293B),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: const Color(0xFF334155), width: 1),
      ),
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: 12,
                height: 12,
                decoration: BoxDecoration(color: color, shape: BoxShape.circle),
              ),
              const SizedBox(width: 12),
              Text(
                teamName,
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                ),
              ),
              if (isMisterX) ...[
                const SizedBox(width: 8),
                const Text('👑', style: TextStyle(fontSize: 18)),
              ],
              const Spacer(),
              Row(
                children: [
                  const Icon(Icons.people, color: Colors.white54, size: 18),
                  const SizedBox(width: 4),
                  Text(
                    members.length.toString(),
                    style: const TextStyle(color: Colors.white54, fontSize: 16),
                  ),
                ],
              ),
            ],
          ),
          const SizedBox(height: 12),
          ...members.map(
            (member) => Padding(
              padding: const EdgeInsets.only(bottom: 4, left: 24),
              child: Text(
                member,
                style: const TextStyle(color: Colors.white70, fontSize: 16),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
