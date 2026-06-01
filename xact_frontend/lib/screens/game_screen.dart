import 'package:flutter/material.dart';
import 'dart:async';
import '../api/api_service.dart';
import '../api/models.dart';
import 'end_match_screen.dart';
import 'start/start_screen.dart';
import 'team_screen.dart';
import 'all_chat_screen.dart';
import 'team_chat_screen.dart';
import 'report_screen.dart';
import '../widgets/map_area.dart';
import '../widgets/xact_branding.dart';
import '../services/app_session.dart';
import '../services/location_service.dart';

class GameScreen extends StatefulWidget {
  const GameScreen({super.key});

  @override
  State<GameScreen> createState() => _GameScreenState();
}

class _GameScreenState extends State<GameScreen> {
  int _selectedIndex = 0;
  bool _isMapFullscreen = false;
  bool _trackingInitCancelled = false;
  bool _allowDirectPop = false;
  bool _endMatchNavigationStarted = false;
  bool _endingGame = false;
  Timer? _trackingRetryTimer;
  Timer? _sessionStatusPollTimer;
  StreamSubscription<RealtimeEventEnvelope>? _realtimeEventSub;
  GameSessionDetails? _sessionDetails;

  final List<Widget> _screens = const [
    TeamScreen(),
    AllChatScreen(),
    TeamChatScreen(),
    ReportScreen(),
  ];

  @override
  void initState() {
    super.initState();
    unawaited(_startLocationTrackingSafely());
    unawaited(_initRealtimeAnnouncements());
    unawaited(_loadSessionDetails());
    unawaited(_checkForFinishedSession());
    _sessionStatusPollTimer = Timer.periodic(const Duration(seconds: 4), (_) {
      if (!mounted || _endMatchNavigationStarted) {
        return;
      }

      unawaited(_checkForFinishedSession());
      unawaited(_loadSessionDetails());
    });
    _trackingRetryTimer = Timer.periodic(const Duration(seconds: 2), (_) {
      if (!mounted || _trackingInitCancelled) {
        return;
      }

      if (!LocationService.instance.isTracking) {
        unawaited(_startLocationTrackingSafely());
      }
    });
  }

  Future<void> _initRealtimeAnnouncements() async {
    final sessionId = AppSession.instance.currentSessionId;
    if (sessionId == null) {
      return;
    }

    try {
      await ApiService.instance.ensureRealtimeSessionSubscription(sessionId);
      _realtimeEventSub = ApiService.instance.realtimeEvents.listen((event) {
        if (event.type == RealtimeEvents.mrXCaught) {
          _onMrXCaught(MrXCaughtPayload.fromJson(event.payload));
        }
      });
    } catch (_) {
      // Announcements are best-effort; the game keeps working without them.
    }
  }

  Future<void> _loadSessionDetails() async {
    final sessionId = AppSession.instance.currentSessionId;
    if (sessionId == null) {
      return;
    }

    try {
      final details = await ApiService.instance.getGameSession(sessionId);
      if (!mounted) {
        return;
      }

      setState(() => _sessionDetails = details);
    } catch (_) {
      // Best-effort polling; the button can remain hidden until a successful fetch.
    }
  }

  void _onMrXCaught(MrXCaughtPayload payload) {
    if (!mounted) {
      return;
    }

    final currentTeamId = AppSession.instance.currentTeamId;
    final String message;
    if (currentTeamId == payload.newMrXTeamId) {
      message = 'Your team caught Mister X — you are now Mister X!';
    } else if (currentTeamId == payload.formerMrXTeamId) {
      message = 'You were caught! ${payload.newMrXTeamName} is now Mister X.';
    } else {
      message =
          '${payload.newMrXTeamName} caught Mister X — they are the new Mister X!';
    }

    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          content: Text(message, style: XActText.bodySm),
          backgroundColor: XActColors.surface2,
          behavior: SnackBarBehavior.floating,
          duration: const Duration(seconds: 4),
        ),
      );
  }

  Future<void> _startLocationTrackingSafely() async {
    try {
      await _startLocationTracking();
    } catch (error, stackTrace) {
      debugPrint('Failed to start location tracking: $error');
      debugPrintStack(stackTrace: stackTrace);
    }
  }

  @override
  void dispose() {
    _trackingInitCancelled = true;
    _trackingRetryTimer?.cancel();
    _sessionStatusPollTimer?.cancel();
    _realtimeEventSub?.cancel();
    LocationService.instance.stopTracking();
    super.dispose();
  }

  Future<void> _checkForFinishedSession() async {
    if (_endMatchNavigationStarted || !mounted) {
      return;
    }

    final sessionId = AppSession.instance.currentSessionId;
    if (sessionId == null) {
      return;
    }

    try {
      final details = await ApiService.instance.getGameSession(sessionId);
      if (!mounted || _endMatchNavigationStarted) {
        return;
      }

      setState(() => _sessionDetails = details);

      if (details.status == SessionStatus.finished) {
        await _openEndMatchScreen();
      }
    } catch (_) {
      // Best-effort poll; transient failures should not interrupt play.
    }
  }

  Future<void> _startLocationTracking() async {
    const maxAttempts = 6;
    for (var attempt = 0; attempt < maxAttempts; attempt++) {
      if (_trackingInitCancelled || !mounted) {
        return;
      }

      final session = AppSession.instance;
      final sessionId = session.currentSessionId;
      final memberId = session.currentMemberId;
      final teamId = session.currentTeamId;

      if (sessionId != null && memberId != null && teamId != null) {
        await LocationService.instance.startTracking(
          sessionId: sessionId,
          memberId: memberId,
          teamId: teamId,
        );
        return;
      }

      await Future<void>.delayed(const Duration(milliseconds: 500));
    }

    // Fallback: still show own GPS on map even if upload identity is incomplete.
    await LocationService.instance.startWatching();
  }

  void _toggleFullscreen() {
    setState(() => _isMapFullscreen = !_isMapFullscreen);
  }

  bool get _hasActiveGameSession =>
      AppSession.instance.currentSessionId != null;

  Future<void> _onPopInvokedWithResult(bool didPop, Object? result) async {
    if (didPop || _allowDirectPop || !_hasActiveGameSession) {
      return;
    }

    final shouldQuit = await _showQuitConfirmationDialog();
    if (!shouldQuit) {
      return;
    }

    await _quitGameAndNavigateBack();
  }

  Future<bool> _showQuitConfirmationDialog() async {
    final result = await showDialog<bool>(
      context: context,
      builder: (dialogContext) {
        return AlertDialog(
          title: const Text('Quit Game'),
          content: const Text('Are you sure you want to quit?'),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(dialogContext).pop(false),
              child: const Text('Cancel'),
            ),
            TextButton(
              onPressed: () => Navigator.of(dialogContext).pop(true),
              child: const Text('Quit'),
            ),
          ],
        );
      },
    );

    return result ?? false;
  }

  Future<void> _quitGameAndNavigateBack() async {
    try {
      await ApiService.instance.closeCurrentSession();
    } catch (error, stackTrace) {
      debugPrint('Failed to close current session while quitting: $error');
      debugPrintStack(stackTrace: stackTrace);
      return;
    }

    if (!mounted) {
      return;
    }

    setState(() => _allowDirectPop = true);
    await WidgetsBinding.instance.endOfFrame;
    if (!mounted) {
      return;
    }

    final navigator = Navigator.of(context);
    if (navigator.canPop()) {
      navigator.pop();
      return;
    }

    if (mounted) {
      setState(() => _allowDirectPop = false);
      navigator.pushAndRemoveUntil(
        MaterialPageRoute(builder: (_) => const StartScreen()),
        (route) => false,
      );
    }
  }

  Future<void> _openEndMatchScreen() async {
    if (_endMatchNavigationStarted || !mounted) {
      return;
    }

    final sessionId = AppSession.instance.currentSessionId;
    if (sessionId == null) {
      return;
    }

    _endMatchNavigationStarted = true;
    _trackingInitCancelled = true;
    _trackingRetryTimer?.cancel();
    _sessionStatusPollTimer?.cancel();
    _realtimeEventSub?.cancel();

    await WidgetsBinding.instance.endOfFrame;
    if (!mounted) {
      return;
    }

    await Navigator.of(context).pushReplacement(
      MaterialPageRoute(builder: (_) => EndMatchScreen(sessionId: sessionId)),
    );
  }

  bool get _isHost {
    final details = _sessionDetails;
    final currentUserId = AppSession.instance.currentUserId;
    return details != null &&
        currentUserId != null &&
        details.hostUserId == currentUserId;
  }

  Future<void> _endGameAsHost() async {
    final sessionId = AppSession.instance.currentSessionId;
    if (sessionId == null || _endingGame) {
      return;
    }

    setState(() => _endingGame = true);
    try {
      await ApiService.instance.endGameSession(sessionId);
      await _openEndMatchScreen();
    } catch (error, stackTrace) {
      debugPrint('Failed to end game session: $error');
      debugPrintStack(stackTrace: stackTrace);

      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text('Could not end game: $error')));
      }
    } finally {
      if (mounted) {
        setState(() => _endingGame = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: _allowDirectPop || !_hasActiveGameSession,
      onPopInvokedWithResult: _onPopInvokedWithResult,
      child: Scaffold(
        backgroundColor: XActColors.bg,
        body: _isMapFullscreen
            ? MapArea(onFullscreenToggle: _toggleFullscreen, isFullscreen: true)
            : SafeArea(
                bottom: false,
                child: Column(
                  children: [
                    Expanded(
                      flex: 5,
                      child: MapArea(onFullscreenToggle: _toggleFullscreen),
                    ),
                    Expanded(flex: 4, child: _screens[_selectedIndex]),
                  ],
                ),
              ),
        bottomNavigationBar: _isMapFullscreen ? null : _buildBottomBar(),
      ),
    );
  }

  Widget _buildBottomBar() {
    const items = [
      (Icons.groups_rounded, 'Team'),
      (Icons.forum_rounded, 'All Chat'),
      (Icons.chat_bubble_rounded, 'Team Chat'),
      (Icons.flag_rounded, 'Report'),
    ];

    return SafeArea(
      top: false,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (_isHost && !_endMatchNavigationStarted) ...[
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 10, 16, 8),
              child: XActBranding.buildPrimaryButton(
                text: _endingGame ? 'Ending game…' : 'End Game',
                icon: Icons.stop_circle_rounded,
                height: 52,
                onPressed: _endingGame ? null : _endGameAsHost,
              ),
            ),
          ],
          Container(
            decoration: BoxDecoration(
              color: XActColors.bg,
              border: Border(top: BorderSide(color: XActColors.hairlineSoft)),
            ),
            padding: const EdgeInsets.fromLTRB(16, 6, 16, 6),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                for (int i = 0; i < items.length; i++)
                  _buildNavItem(
                    icon: items[i].$1,
                    label: items[i].$2,
                    active: _selectedIndex == i,
                    onTap: () => setState(() => _selectedIndex = i),
                  ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildNavItem({
    required IconData icon,
    required String label,
    required bool active,
    required VoidCallback onTap,
  }) {
    return InkResponse(
      onTap: onTap,
      radius: 36,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 6),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            SizedBox(
              height: 3,
              width: 28,
              child: active
                  ? Container(
                      decoration: BoxDecoration(
                        color: XActColors.secondary,
                        borderRadius: BorderRadius.circular(2),
                      ),
                    )
                  : null,
            ),
            const SizedBox(height: 4),
            Icon(
              icon,
              size: 22,
              color: active ? XActColors.text1 : XActColors.text4,
            ),
            const SizedBox(height: 4),
            Text(
              label,
              style: XActText.caption.copyWith(
                fontSize: 10,
                fontWeight: FontWeight.w600,
                color: active ? XActColors.text1 : XActColors.text4,
                letterSpacing: .4,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
