import 'package:app_links/app_links.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:xact_frontend/services/chat_notification_service.dart';
import 'package:xact_frontend/services/game_haptics_service.dart';
import 'package:xact_frontend/services/preferences_service.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:xact_frontend/api/api_service.dart';
import 'package:xact_frontend/auth/auth_config.dart';
import 'package:xact_frontend/screens/auth/login_screen.dart';
import 'package:xact_frontend/screens/start/start_screen.dart';
import 'package:xact_frontend/widgets/game_start_overlay.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await PreferencesService.instance.load();
  await ChatNotificationService.instance.init();
  GameHapticsService.instance.init();
  await ApiService.instance.restoreLogin();
  runApp(MainApp(loginCallback: await _loginCallbackThatStartedApp()));
}

/// on web the login redirect is a fresh load of the app. on mobile the os can kill
/// the app while the user is in the browser, so the redirect cold-starts it
Future<Uri?> _loginCallbackThatStartedApp() async {
  Uri? link;
  if (kIsWeb) {
    link = Uri.base;
  } else if (!AuthConfig.isDesktop) {
    try {
      link = await AppLinks().getInitialLink();
    } catch (_) {}
  }

  return link != null && link.queryParameters.containsKey('code')
      ? link
      : null;
}

class MainApp extends StatefulWidget {
  const MainApp({super.key, this.loginCallback});

  final Uri? loginCallback;

  @override
  State<MainApp> createState() => _MainAppState();
}

class _MainAppState extends State<MainApp> {
  final _messengerKey = GlobalKey<ScaffoldMessengerState>();
  late final _snackBarCleaner = _ClearSnackBarsOnPageChange(_messengerKey);

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
      scaffoldMessengerKey: _messengerKey,
      navigatorObservers: [_snackBarCleaner],
      theme: base.copyWith(
        textTheme: GoogleFonts.interTextTheme(base.textTheme).apply(
          bodyColor: XActColors.text1,
          displayColor: XActColors.text1,
        ),
        snackBarTheme: SnackBarThemeData(
          backgroundColor: XActColors.surface2,
          contentTextStyle: XActText.bodySm,
          behavior: SnackBarBehavior.floating,
          showCloseIcon: true,
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
      home: widget.loginCallback != null
          ? LoginScreen(callback: widget.loginCallback)
          : const StartScreen(),
    );
  }
}

/// the messenger sits above the navigator, so without this a snackbar stays
/// on the next screen and covers its buttons. dialogs are routes too, but a
/// message shown under a dialog still belongs to the same screen
class _ClearSnackBarsOnPageChange extends NavigatorObserver {
  _ClearSnackBarsOnPageChange(this._messengerKey);

  final GlobalKey<ScaffoldMessengerState> _messengerKey;

  void _clearFor(Route<dynamic>? route) {
    if (route is PageRoute) {
      _messengerKey.currentState?.clearSnackBars();
    }
  }

  @override
  void didPush(Route<dynamic> route, Route<dynamic>? previousRoute) =>
      _clearFor(route);

  @override
  void didPop(Route<dynamic> route, Route<dynamic>? previousRoute) =>
      _clearFor(route);

  @override
  void didReplace({Route<dynamic>? newRoute, Route<dynamic>? oldRoute}) =>
      _clearFor(newRoute);

  @override
  void didRemove(Route<dynamic> route, Route<dynamic>? previousRoute) =>
      _clearFor(route);
}
