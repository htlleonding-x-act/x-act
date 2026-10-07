import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:xact_frontend/api/api_service.dart';
import 'package:xact_frontend/api/models.dart';
import 'package:xact_frontend/screens/end_match/end_match_format.dart';
import 'package:xact_frontend/screens/end_match/end_match_screen.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class MatchHistoryScreen extends StatefulWidget {
  const MatchHistoryScreen({super.key});

  @override
  State<MatchHistoryScreen> createState() => _MatchHistoryScreenState();
}

class _MatchHistoryScreenState extends State<MatchHistoryScreen> {
  // the backend's upper limit; each match loads its full results there
  static const _limit = 50;

  List<MatchSummary>? _matches;
  bool _failed = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _matches = null;
      _failed = false;
    });

    try {
      final matches = await ApiService.instance.loadMyMatches(limit: _limit);
      if (mounted) setState(() => _matches = matches);
    } catch (_) {
      if (mounted) setState(() => _failed = true);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: XActColors.bg,
      body: Stack(
        children: [
          Positioned.fill(child: XActBranding.aurora()),
          SafeArea(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                XActBranding.buildTopBar(
                  context: context,
                  eyebrow: 'Stats',
                  title: 'Match history',
                ),
                Expanded(child: _buildBody()),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildBody() {
    final matches = _matches;

    if (_failed) {
      return _buildMessage(
        'Could not load your matches.',
        action: XActBranding.buildSecondaryButton(
          text: 'Try again',
          onPressed: _load,
        ),
      );
    }
    if (matches == null) {
      return const Center(child: CircularProgressIndicator());
    }
    if (matches.isEmpty) {
      return _buildMessage(
        'No finished matches yet. They show up here once a match ends.',
      );
    }

    return RefreshIndicator(
      onRefresh: _load,
      child: ListView.separated(
        padding: const EdgeInsets.fromLTRB(20, 0, 20, 32),
        itemCount: matches.length,
        separatorBuilder: (_, _) => const SizedBox(height: XActSpace.s3),
        itemBuilder: (_, index) => _buildMatch(matches[index]),
      ),
    );
  }

  Widget _buildMessage(String text, {Widget? action}) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              text,
              textAlign: TextAlign.center,
              style: XActText.body.copyWith(color: XActColors.text3),
            ),
            if (action != null) ...[const SizedBox(height: 20), action],
          ],
        ),
      ),
    );
  }

  Widget _buildMatch(MatchSummary match) {
    final accent = match.won ? XActColors.success : XActColors.text3;
    final duration = formatShortDuration(
      match.endTime.difference(match.startTime),
    );

    return Material(
      color: XActColors.surface,
      borderRadius: XActRadius.lg,
      child: InkWell(
        borderRadius: XActRadius.lg,
        onTap: () => Navigator.of(context).push(
          MaterialPageRoute(
            builder: (_) => EndMatchScreen(
              sessionId: match.sessionId,
              reviewMemberId: match.memberId,
            ),
          ),
        ),
        child: Ink(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            borderRadius: XActRadius.lg,
            border: Border.all(color: XActColors.hairlineSoft),
          ),
          child: Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      match.sessionName,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: XActText.bodySm.copyWith(
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      '${formatDateTime(match.startTime)} · $duration',
                      style: XActText.caption,
                    ),
                    const SizedBox(height: 6),
                    Text(
                      '${match.teamName} · ${formatDistance(match.distanceMeters)}',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: XActText.caption.copyWith(color: XActColors.text3),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 12),
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 4,
                ),
                decoration: BoxDecoration(
                  color: accent.withValues(alpha: .16),
                  borderRadius: XActRadius.pill,
                ),
                child: Text(
                  match.won ? 'WON' : 'LOST',
                  style: GoogleFonts.inter(
                    fontSize: 11,
                    fontWeight: FontWeight.w700,
                    color: accent,
                    letterSpacing: 1.2,
                  ),
                ),
              ),
              const SizedBox(width: 4),
              Icon(
                Icons.chevron_right_rounded,
                size: 18,
                color: XActColors.text5,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
