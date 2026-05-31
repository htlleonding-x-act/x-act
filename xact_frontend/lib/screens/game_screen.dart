import 'package:flutter/material.dart';
import 'dart:async';
import '../api/api_service.dart';
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
        bottomNavigationBar: _isMapFullscreen ? null : _buildBottomNav(),
      ),
    );
  }

  Widget _buildBottomNav() {
    const items = [
      (Icons.groups_rounded, 'Team'),
      (Icons.forum_rounded, 'All Chat'),
      (Icons.chat_bubble_rounded, 'Team Chat'),
      (Icons.flag_rounded, 'Report'),
    ];

    return Container(
      decoration: BoxDecoration(
        color: XActColors.bg,
        border: Border(
          top: BorderSide(color: XActColors.hairlineSoft),
        ),
      ),
      padding: const EdgeInsets.fromLTRB(16, 6, 16, 6),
      child: SafeArea(
        top: false,
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
