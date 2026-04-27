import 'package:flutter/material.dart';
import 'dart:async';
import '../api/api_service.dart';
import 'start/start_screen.dart';
import 'team_screen.dart';
import 'all_chat_screen.dart';
import 'team_chat_screen.dart';
import 'report_screen.dart';
import '../widgets/map_area.dart';
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
  Timer? _trackingRetryTimer;

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
    _trackingRetryTimer = Timer.periodic(const Duration(seconds: 2), (_) {
      if (!mounted || _trackingInitCancelled) {
        return;
      }

      if (!LocationService.instance.isTracking) {
        unawaited(_startLocationTrackingSafely());
      }
    });
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
    LocationService.instance.stopTracking();
    super.dispose();
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

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: _allowDirectPop || !_hasActiveGameSession,
      onPopInvokedWithResult: _onPopInvokedWithResult,
      child: Scaffold(
        body: _isMapFullscreen
            ? MapArea(onFullscreenToggle: _toggleFullscreen, isFullscreen: true)
            : Column(
                children: [
                  Expanded(
                    flex: 5,
                    child: MapArea(onFullscreenToggle: _toggleFullscreen),
                  ),
                  Expanded(flex: 4, child: _screens[_selectedIndex]),
                ],
              ),
        bottomNavigationBar: _isMapFullscreen
            ? null
            : BottomNavigationBar(
                currentIndex: _selectedIndex,
                onTap: (index) {
                  setState(() {
                    _selectedIndex = index;
                  });
                },
                type: BottomNavigationBarType.fixed,
                backgroundColor: const Color(0xFF0F172A),
                selectedItemColor: Colors.blue.shade400,
                unselectedItemColor: Colors.white54,
                items: const [
                  BottomNavigationBarItem(
                    icon: Icon(Icons.groups),
                    label: 'Team',
                  ),
                  BottomNavigationBarItem(
                    icon: Icon(Icons.forum),
                    label: 'All Chat',
                  ),
                  BottomNavigationBarItem(
                    icon: Icon(Icons.chat),
                    label: 'Team Chat',
                  ),
                  BottomNavigationBarItem(
                    icon: Icon(Icons.warning),
                    label: 'Report',
                  ),
                ],
              ),
      ),
    );
  }
}
