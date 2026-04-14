import 'package:flutter/material.dart';
import 'dart:async';
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
    unawaited(_startLocationTracking());
    _trackingRetryTimer = Timer.periodic(const Duration(seconds: 2), (_) {
      if (!mounted || _trackingInitCancelled) {
        return;
      }

      if (!LocationService.instance.isTracking) {
        unawaited(_startLocationTracking());
      }
    });
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

  @override
  Widget build(BuildContext context) {
    return Scaffold(
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
    );
  }
}
