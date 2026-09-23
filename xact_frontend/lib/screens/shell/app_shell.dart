import 'package:flutter/material.dart';
import 'package:xact_frontend/screens/settings/profile_screen.dart';
import 'package:xact_frontend/screens/start/start_screen.dart';
import 'package:xact_frontend/services/app_session.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class AppShell extends StatefulWidget {
  const AppShell({super.key});

  @override
  State<AppShell> createState() => _AppShellState();
}

class _AppShellState extends State<AppShell> {
  int _index = 0;

  void _onTabChanged(int index) {
    if (index == 1 && AppSession.instance.currentUserId == null) return;
    setState(() => _index = index);
  }

  @override
  Widget build(BuildContext context) {
    final isLoggedIn = AppSession.instance.currentUserId != null;

    return Scaffold(
      backgroundColor: XActColors.bg,
      body: _index == 0 ? const StartScreen() : const ProfileScreen(),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _index,
        onDestinationSelected: _onTabChanged,
        backgroundColor: XActColors.surface,
        surfaceTintColor: Colors.transparent,
        shadowColor: Colors.transparent,
        indicatorColor: XActColors.primarySoft,
        labelBehavior: NavigationDestinationLabelBehavior.alwaysShow,
        destinations: [
          const NavigationDestination(
            icon: Icon(Icons.home_outlined),
            selectedIcon: Icon(Icons.home_rounded),
            label: 'Home',
          ),
          NavigationDestination(
            icon: Icon(
              Icons.person_outline_rounded,
              color: isLoggedIn ? null : XActColors.text4,
            ),
            selectedIcon: const Icon(Icons.person_rounded),
            label: 'Profile',
          ),
        ],
      ),
    );
  }
}
