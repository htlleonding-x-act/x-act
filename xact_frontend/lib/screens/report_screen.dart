import 'package:flutter/material.dart';

class ReportScreen extends StatefulWidget {
  const ReportScreen({super.key});

  @override
  State<ReportScreen> createState() => _ReportScreenState();
}

class _ReportScreenState extends State<ReportScreen> {
  int _kickVotes = 1;
  int _keepVotes = 1;
  final int _totalTeams = 3;
  bool _hasVoted = false;
  String? _userVote;

  void _voteKick() {
    if (_hasVoted) return;
    setState(() {
      _kickVotes++;
      _hasVoted = true;
      _userVote = 'kick';
    });
  }

  void _voteKeep() {
    if (_hasVoted) return;
    setState(() {
      _keepVotes++;
      _hasVoted = true;
      _userVote = 'keep';
    });
  }

  @override
  Widget build(BuildContext context) {
    final totalVotes = _kickVotes + _keepVotes;
    final kickPercentage = totalVotes > 0 ? _kickVotes / totalVotes : 0.0;

    return Container(
      color: const Color(0xFF1E293B),
      child: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(16.0),
          child: ListView(
            padding: EdgeInsets.zero,
            children: [
              const Padding(
                padding: EdgeInsets.only(left: 8.0, bottom: 12.0),
                child: Text(
                  'Active Vote Kicks',
                  style: TextStyle(
                    color: Color(0xFF94A3B8),
                    fontSize: 16,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ),
              Container(
                decoration: BoxDecoration(
                  color: const Color(0xFF0F172A),
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(color: const Color(0xFF334155), width: 1),
                ),
                padding: const EdgeInsets.all(20),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text(
                          'Detectives Gamma',
                          style: TextStyle(
                            color: Colors.white,
                            fontSize: 20,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        Container(
                          padding: const EdgeInsets.all(8),
                          decoration: BoxDecoration(
                            color: const Color(
                              0xFFFF6B00,
                            ).withValues(alpha: 0.15),
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child: const Icon(
                            Icons.warning_rounded,
                            color: Color(0xFFFF6B00),
                            size: 24,
                          ),
                        ),
                      ],
                    ),
                    const Text(
                      'Reported by Detectives Alpha',
                      style: TextStyle(color: Color(0xFF64748B), fontSize: 14),
                    ),
                    const SizedBox(height: 32),
                    const Text(
                      'Left the playable area',
                      style: TextStyle(
                        color: Color(0xFFE2E8F0),
                        fontSize: 16,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                    const SizedBox(height: 32),
                    Text(
                      'Votes: $_kickVotes / $_totalTeams',
                      style: const TextStyle(
                        color: Color(0xFF94A3B8),
                        fontSize: 14,
                      ),
                    ),
                    const SizedBox(height: 12),
                    ClipRRect(
                      borderRadius: BorderRadius.circular(4),
                      child: LinearProgressIndicator(
                        value: kickPercentage,
                        minHeight: 8,
                        backgroundColor: const Color(0xFF334155),
                        valueColor: const AlwaysStoppedAnimation<Color>(
                          Color(0xFFFF6B00),
                        ),
                      ),
                    ),
                    const SizedBox(height: 20),
                    Row(
                      children: [
                        Expanded(
                          child: ElevatedButton.icon(
                            onPressed: _hasVoted ? null : _voteKick,
                            icon: const Icon(Icons.warning_rounded),
                            label: const Text('Vote Kick'),
                            style: ElevatedButton.styleFrom(
                              backgroundColor: _userVote == 'kick'
                                  ? const Color(0xFFEA580C)
                                  : const Color(0xFFFF6B00),
                              foregroundColor: Colors.white,
                              disabledBackgroundColor: const Color(
                                0xFFEA580C,
                              ).withValues(alpha: 0.5),
                              disabledForegroundColor: Colors.white70,
                              padding: const EdgeInsets.symmetric(vertical: 16),
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(12),
                              ),
                              elevation: 0,
                            ),
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: ElevatedButton.icon(
                            onPressed: _hasVoted ? null : _voteKeep,
                            icon: const Icon(Icons.check_circle_outline),
                            label: const Text('Vote Keep'),
                            style: ElevatedButton.styleFrom(
                              backgroundColor: _userVote == 'keep'
                                  ? const Color(0xFF059669)
                                  : const Color(0xFF10B981),
                              foregroundColor: Colors.white,
                              disabledBackgroundColor: const Color(
                                0xFF059669,
                              ).withValues(alpha: 0.5),
                              disabledForegroundColor: Colors.white70,
                              padding: const EdgeInsets.symmetric(vertical: 16),
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(12),
                              ),
                              elevation: 0,
                            ),
                          ),
                        ),
                      ],
                    ),
                    if (_hasVoted)
                      Padding(
                        padding: const EdgeInsets.only(top: 12),
                        child: Text(
                          _userVote == 'kick'
                              ? 'You voted to kick'
                              : 'You voted to keep',
                          style: TextStyle(
                            color: _userVote == 'kick'
                                ? const Color(0xFFFF6B00)
                                : const Color(0xFF10B981),
                            fontSize: 14,
                            fontWeight: FontWeight.w500,
                          ),
                          textAlign: TextAlign.center,
                        ),
                      ),
                  ],
                ),
              ),
              const SizedBox(height: 24),
            ],
          ),
        ),
      ),
    );
  }
}
