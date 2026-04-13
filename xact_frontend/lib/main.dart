import 'package:flutter/material.dart';
import 'package:xact_frontend/api/api_service.dart';
import 'package:xact_frontend/screens/start/start_screen.dart';
import 'package:xact_frontend/widgets/game_start_overlay.dart';

void main() {
  runApp(const MainApp());
}

class MainApp extends StatefulWidget {
  const MainApp({super.key});

  @override
  State<MainApp> createState() => _MainAppState();
}

class _MainAppState extends State<MainApp> with WidgetsBindingObserver {
  bool _isClosingSession = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.detached && !_isClosingSession) {
      _isClosingSession = true;
      ApiService.instance.closeCurrentSession().whenComplete(() {
        _isClosingSession = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'X-ACT',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.blue, brightness: Brightness.dark),
        useMaterial3: true,
      ),
      builder: (context, child) {
        return Stack(
          children: [
            if (child != null) child,
            const GameStartOverlay(),
          ],
        );
      },
      home: const StartScreen(),
    );           
  }
}
