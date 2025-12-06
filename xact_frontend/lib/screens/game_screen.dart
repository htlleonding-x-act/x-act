import 'package:flutter/material.dart';
import 'team_screen.dart';
import 'all_chat_screen.dart';
import 'team_chat_screen.dart';
import 'report_screen.dart';
import '../widgets/map_area.dart';

class GameScreen extends StatefulWidget {
  const GameScreen({super.key});

  @override
  State<GameScreen> createState() => _GameScreenState();
}

class _GameScreenState extends State<GameScreen> {
  int _selectedIndex = 0;

  final List<Widget> _screens = const [TeamScreen(), AllChatScreen(), TeamChatScreen(), ReportScreen()];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Column(
        children: [
          const Expanded(flex: 2, child: MapArea()),
          Expanded(flex: 3, child: _screens[_selectedIndex]),
        ],
      ),
      bottomNavigationBar: BottomNavigationBar(
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
          BottomNavigationBarItem(icon: Icon(Icons.groups), label: 'Team'),
          BottomNavigationBarItem(icon: Icon(Icons.forum), label: 'All Chat'),
          BottomNavigationBarItem(icon: Icon(Icons.chat), label: 'Team Chat'),
          BottomNavigationBarItem(icon: Icon(Icons.warning), label: 'Report'),
        ],
      ),
    );
  }
}
