import 'dart:async';
import 'dart:math';

import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';

import '../../api/api_service.dart';
import '../../api/game_results.dart';
import '../../api/models.dart';
import '../../services/app_session.dart';
import '../../widgets/xact_branding.dart';
import '../start/start_screen.dart';
import '../team/team_lobby.dart';
import 'end_match_action_bar.dart';
import 'end_match_tab_bar.dart';
import 'overview/overview_tab.dart';
import 'players/players_tab.dart';
import 'replay/replay_controller.dart';
import 'replay/replay_tab.dart';
import 'replay/replay_tracks.dart';
import 'share/share_match_result.dart';
import 'winner_hero.dart';

class EndMatchScreen extends StatefulWidget {
  final int sessionId;

  const EndMatchScreen({super.key, required this.sessionId});

  @override
  State<EndMatchScreen> createState() => _EndMatchScreenState();
}

class _EndMatchScreenState extends State<EndMatchScreen>
    with TickerProviderStateMixin {
  static const double _maxContentWidth = 1080;

  static const _tabs = [
    EndMatchTab('Overview', Icons.dashboard_rounded),
    EndMatchTab('Replay', Icons.route_rounded),
    EndMatchTab('Players', Icons.leaderboard_rounded),
  ];
  static const int _overviewTab = 0;
  static const int _replayTab = 1;

  // a jump lands a moment before the event, so the replay shows it happen
  static const int _jumpLeadSeconds = 3;
  static const double _jumpZoom = 16;
  static const Duration _highlightDuration = Duration(seconds: 2);

  late final TabController _tabController = TabController(
    length: _tabs.length,
    vsync: this,
  );
  final MapController _mapController = MapController();
  final ValueNotifier<TimelineEvent?> _highlightedEvent = ValueNotifier(null);

  bool _loading = true;
  bool _working = false;
  bool _migrating = false;
  GameResults? _results;
  ReplayTrackSet? _tracks;
  ReplayController? _replay;
  Object? _loadError;
  String? _hostUserId;
  StreamSubscription<RealtimeEventEnvelope>? _rematchSub;
  bool _mapReady = false;
  LatLng? _pendingCameraTarget;
  Timer? _highlightTimer;

  @override
  void initState() {
    super.initState();
    _tabController.addListener(_onTabChanged);
    _loadResults();
    _initRematchListener();
  }

  @override
  void dispose() {
    _rematchSub?.cancel();
    _highlightTimer?.cancel();
    _tabController.dispose();
    _replay?.dispose();
    _highlightedEvent.dispose();
    _mapController.dispose();
    super.dispose();
  }

  void _onTabChanged() {
    if (_tabController.index != _replayTab) {
      _replay?.pause();
    }
    if (!_tabController.indexIsChanging) {
      setState(() {});
    }
  }

  void _onMapReady() {
    _mapReady = true;
    final target = _pendingCameraTarget;
    if (target != null) {
      _pendingCameraTarget = null;
      _mapController.move(target, _jumpZoom);
    }
  }

  /// the map only exists once the replay tab was opened, until then the
  /// target waits for it
  void _moveCamera(LatLng target) {
    if (_mapReady) {
      _mapController.move(target, max(_mapController.camera.zoom, _jumpZoom));
    } else {
      _pendingCameraTarget = target;
    }
  }

  void _jumpToEvent(TimelineEvent event) {
    final replay = _replay;
    if (replay == null) return;

    replay.pause();
    replay.seek(max(0, event.offsetSeconds - _jumpLeadSeconds).toDouble());
    if (event.position case final position?) {
      _moveCamera(position);
    }

    _highlightedEvent.value = event;
    _highlightTimer?.cancel();
    _highlightTimer = Timer(
      _highlightDuration,
      () => _highlightedEvent.value = null,
    );
  }

  void _watchRoute(ResultMember member) {
    final replay = _replay;
    if (replay == null || member.route.isEmpty) return;

    _tabController.animateTo(_replayTab);
    replay.focusMember(member.memberId);
    replay.seek(member.route.first.offsetSeconds.toDouble());
    _moveCamera(member.route.first.position);
    replay.play();
  }

  /// catches and offenses have a moment to jump to, the other awards are about
  /// a whole route
  void _showAward(MatchAward award) {
    final results = _results;
    if (results == null || award.memberIds.isEmpty) return;

    final eventType = switch (award.type) {
      AwardType.hunter => TimelineEventType.mrXCaught,
      AwardType.ruleBender => TimelineEventType.leftGameArea,
      _ => null,
    };
    final event = results.timeline
        .where((e) => e.type == eventType && award.memberIds.contains(e.memberId))
        .firstOrNull;

    if (event != null) {
      _tabController.animateTo(_replayTab);
      _jumpToEvent(event);
      return;
    }

    final member = results.memberById(award.memberIds.first);
    if (member != null) {
      _watchRoute(member);
    }
  }

  Future<void> _loadResults() async {
    setState(() {
      _loading = true;
      _loadError = null;
    });

    try {
      final results = await ApiService.instance.loadGameResults(
        widget.sessionId,
      );
      if (!mounted) return;
      _replay?.dispose();
      setState(() {
        _results = results;
        _tracks = ReplayTrackSet.of(results);
        _replay = ReplayController(
          vsync: this,
          matchSeconds: results.durationSeconds,
        );
        _hostUserId = results.hostUserId;
        _loading = false;
      });
    } catch (error) {
      // the rematch button still needs to know who the host is
      String? hostUserId;
      try {
        hostUserId = (await ApiService.instance.getGameSession(
          widget.sessionId,
        )).hostUserId;
      } catch (_) {}

      if (!mounted) return;
      setState(() {
        _loadError = error;
        _hostUserId = hostUserId ?? _hostUserId;
        _loading = false;
      });
    }
  }

  bool get _isHost {
    final currentUserId = AppSession.instance.currentUserId;
    return _hostUserId != null && currentUserId == _hostUserId;
  }

  /// keeps listening on the finished session's channel so every client, the
  /// host included, moves into the new lobby when the host starts a rematch
  Future<void> _initRematchListener() async {
    try {
      await ApiService.instance.ensureRealtimeSessionSubscription(
        widget.sessionId,
      );
      // mark this player as present on the finished session. the rematch only
      // copies connected members, so a player who leaves this screen and
      // unregisters isn't carried over into the new lobby as a ghost
      await ApiService.instance.registerCurrentMemberPresence();
    } catch (_) {
      // the subscription usually carries over from the lobby, so the listener
      // below still works
    }

    _rematchSub = ApiService.instance.realtimeEvents.listen((event) {
      if (event.type == RealtimeEvents.rematchCreated) {
        final payload = RematchCreatedPayload.fromJson(event.payload);
        if (payload.finishedSessionId == widget.sessionId) {
          _navigateToRematch(
            sessionId: payload.newSessionId,
            joinCode: payload.newJoinCode,
            sessionName: payload.sessionName,
            hostUserId: payload.hostUserId,
          );
        }
      }
    });
  }

  Future<void> _startRematch() async {
    setState(() => _working = true);
    try {
      final rematch = await ApiService.instance.createRematch(widget.sessionId);
      // navigate from the response right away, other clients follow the
      // broadcast. the guard in _navigateToRematch stops a second push when the
      // host gets its own broadcast
      _navigateToRematch(
        sessionId: rematch.sessionId,
        joinCode: rematch.joinCode,
        sessionName: rematch.sessionName,
        hostUserId: rematch.hostUserId,
      );
    } catch (error) {
      if (!mounted) {
        return;
      }
      setState(() => _working = false);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            'Could not start a new lobby. ${describeApiError(error)}',
          ),
        ),
      );
    }
  }

  void _navigateToRematch({
    required int sessionId,
    required String joinCode,
    required String sessionName,
    required String hostUserId,
  }) {
    if (_migrating || !mounted) {
      return;
    }
    _migrating = true;

    openRematchLobby(
      context,
      sessionId: sessionId,
      joinCode: joinCode,
      sessionName: sessionName,
      hostUserId: hostUserId,
    );
  }

  Future<void> _leaveLobby() async {
    setState(() => _working = true);
    try {
      await ApiService.instance.closeCurrentSession();
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              'Could not leave lobby cleanly. ${describeApiError(error)}',
            ),
          ),
        );
      }
    }

    if (!mounted) {
      return;
    }

    await Navigator.of(context).pushAndRemoveUntil(
      MaterialPageRoute(builder: (_) => const StartScreen()),
      (route) => false,
    );
  }

  int? get _currentMemberId => AppSession.instance.currentMemberId;

  bool _isWinner(GameResults results) {
    final me = results.memberById(_currentMemberId);
    return me != null && me.teamId == results.winnerTeamId;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: XActColors.bg,
      body: Stack(
        children: [
          Positioned.fill(child: XActBranding.aurora()),
          SafeArea(
            bottom: false,
            child: Center(
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: _maxContentWidth),
                child: Column(
                  children: [
                    Expanded(child: _buildBody()),
                    EndMatchActionBar(
                      isHost: _isHost,
                      loading: _loading,
                      busy: _working || _migrating,
                      onRematch: _startRematch,
                      onLeave: _leaveLobby,
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildBody() {
    final results = _results;
    final tracks = _tracks;
    final replay = _replay;
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (results == null || tracks == null || replay == null) {
      return _buildError();
    }

    final compactHero = _tabController.index != _overviewTab;

    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(
            XActSpace.s4,
            XActSpace.s3,
            XActSpace.s4,
            XActSpace.s3,
          ),
          child: WinnerHero(
            results: results,
            isWinner: _isWinner(results),
            compact: compactHero,
            onShare: () => showMatchShareSheet(context, results),
          ),
        ),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: XActSpace.s4),
          child: EndMatchTabBar(controller: _tabController, tabs: _tabs),
        ),
        Expanded(
          child: TabBarView(
            controller: _tabController,
            // a horizontal drag on the map must pan it, not switch tabs
            physics: const NeverScrollableScrollPhysics(),
            children: [
              OverviewTab(
                results: results,
                currentMemberId: _currentMemberId,
                onShowAward: _showAward,
                onShare: () => showMatchShareSheet(context, results),
              ),
              ReplayTab(
                results: results,
                tracks: tracks,
                controller: replay,
                mapController: _mapController,
                highlightedEvent: _highlightedEvent,
                onEventTap: _jumpToEvent,
                onMapReady: _onMapReady,
              ),
              PlayersTab(
                results: results,
                currentMemberId: _currentMemberId,
                onWatchRoute: _watchRoute,
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildError() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(XActSpace.s5),
        child: XActBranding.buildFormCard(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(Icons.cloud_off_rounded, size: 40, color: XActColors.text3),
              const SizedBox(height: XActSpace.s3),
              Text(
                'Could not load the match results',
                style: XActText.heading,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: XActSpace.s2),
              Text(
                describeApiError(_loadError ?? 'unknown error'),
                style: XActText.bodySm.copyWith(color: XActColors.text3),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: XActSpace.s4),
              XActBranding.buildSecondaryButton(
                text: 'Try again',
                icon: Icons.refresh_rounded,
                onPressed: _loadResults,
                height: 48,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
