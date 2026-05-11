import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:xact_frontend/api/api_service.dart';
import 'package:xact_frontend/screens/start/start_screen.dart';
import 'package:xact_frontend/widgets/game_start_overlay.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

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
    final base = ThemeData(
      colorScheme: ColorScheme.fromSeed(
        seedColor: XActColors.secondary,
        brightness: Brightness.dark,
        surface: XActColors.surface,
      ),
      useMaterial3: true,
      scaffoldBackgroundColor: XActColors.bg,
      brightness: Brightness.dark,
    );

    return MaterialApp(
      title: 'X-ACT',
      theme: base.copyWith(
        textTheme: GoogleFonts.interTextTheme(base.textTheme).apply(
          bodyColor: XActColors.text1,
          displayColor: XActColors.text1,
        ),
        snackBarTheme: SnackBarThemeData(
          backgroundColor: XActColors.surface2,
          contentTextStyle: XActText.bodySm,
          behavior: SnackBarBehavior.floating,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(14),
            side: BorderSide(color: XActColors.hairlineSoft),
          ),
        ),
        dialogTheme: DialogThemeData(
          backgroundColor: XActColors.surface,
          surfaceTintColor: Colors.transparent,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(20),
            side: BorderSide(color: XActColors.hairlineSoft),
          ),
          titleTextStyle: XActText.heading.copyWith(fontSize: 18),
          contentTextStyle: XActText.bodySm.copyWith(color: XActColors.text2),
        ),
        progressIndicatorTheme: const ProgressIndicatorThemeData(
          color: XActColors.secondary,
        ),
      ),
      builder: (context, child) {
        return Stack(
          children: [
            ?child,
            const GameStartOverlay(),
          ],
        );
      },
      home: const StartScreen(),
    );
  }
}
