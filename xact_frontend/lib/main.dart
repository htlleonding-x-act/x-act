import 'package:flutter/material.dart';
import 'package:xact_frontend/screens/startscreen/start_screen.dart';
import 'screens/game_screen.dart';

void main() {
  runApp(const MainApp());
}

class MainApp extends StatelessWidget {
  const MainApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'X-ACT',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.blue, brightness: Brightness.dark),
        useMaterial3: true,
      ),
      home: const StartScreen(),
    );
  }
}
