import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:xact_frontend/api/api_service.dart';
import 'package:xact_frontend/api/models.dart';
import 'package:xact_frontend/screens/lobby/scan_game_code.dart';
import 'package:xact_frontend/screens/team/team_lobby.dart';
import 'package:xact_frontend/services/app_session.dart';
import 'package:xact_frontend/services/location_service.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class JoinGameScreen extends StatefulWidget {
  const JoinGameScreen({super.key});

  @override
  State<JoinGameScreen> createState() => _JoinGameScreenState();
}

class _JoinGameScreenState extends State<JoinGameScreen> {
  static const int _codeLength = 6;

  final _codeController = TextEditingController();
  final _codeFocus = FocusNode();
  final _usernameController = TextEditingController();
  bool _joining = false;

  @override
  void initState() {
    super.initState();
    _codeController.addListener(() => setState(() {}));
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _codeFocus.requestFocus();
    });
  }

  @override
  void dispose() {
    _codeController.dispose();
    _codeFocus.dispose();
    _usernameController.dispose();
    super.dispose();
  }

  Future<void> _onScan() async {
    final scanned = await Navigator.of(context).push<String>(
      MaterialPageRoute(builder: (_) => const ScanGameCodeScreen()),
    );

    if (!mounted || scanned == null) return;

    _codeController.text = scanned.toUpperCase();
    _codeController.selection = TextSelection.collapsed(
      offset: scanned.length,
    );

    if (_usernameController.text.trim().isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Game code scanned. Enter a name to join.'),
        ),
      );
      return;
    }

    _onJoin();
  }

  Future<void> _onJoin() async {
    final gameCode = _codeController.text.trim().toUpperCase();
    final username = _usernameController.text.trim();

    if (gameCode.isEmpty || username.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please fill in all fields')),
      );
      return;
    }

    if (!RegExp(r'^[A-Z0-9]{6}$').hasMatch(gameCode)) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text(
            'Game code must be 6 characters using uppercase letters and numbers',
          ),
        ),
      );
      return;
    }

    setState(() => _joining = true);

    try {
      final userId = await ApiService.instance.ensureMvpUser(
        preferredName: username,
      );
      AppSession.instance.setIdentity(userId: userId, username: username);

      final session = await ApiService.instance.joinLobbyByCode(gameCode);
      final snapshot = await ApiService.instance.loadLobbySnapshot(
        session.sessionId,
      );
      TeamMemberDetails? existingMembership;
      for (final members in snapshot.membersByTeamId.values) {
        for (final member in members) {
          if (member.userId == userId) {
            existingMembership = member;
            break;
          }
        }
        if (existingMembership != null) break;
      }

      TeamDetails? spectatorTeam;
      for (final team in snapshot.teams) {
        if (team.role == TeamRole.spectator) {
          spectatorTeam = team;
          break;
        }
      }

      spectatorTeam ??= await ApiService.instance.addTeam(
        sessionId: session.sessionId,
        teamName: 'Unassigned',
        role: TeamRole.spectator,
        colorCode: '#64748B',
      );

      final member = existingMembership ??
          await ApiService.instance.addUserMember(
            sessionId: session.sessionId,
            teamId: spectatorTeam.teamId,
            userId: userId,
          );

      AppSession.instance.setMembership(
        teamId: spectatorTeam.teamId,
        memberId: member.memberId,
        teamLeader: member.isTeamLeader,
      );

      final locationReady = await _ensureLocationReadyBeforeLobby();
      if (!locationReady || !mounted) return;

      Navigator.push(
        context,
        MaterialPageRoute(
          builder: (context) => GameLobbyScreen(
            sessionId: session.sessionId,
            gameCode: session.joinCode,
            gameName: session.sessionName,
            isLeader: false,
          ),
        ),
      );
    } catch (error) {
      if (!mounted) return;
      final message = error.toString().contains('HTTP 404')
          ? 'No game found with code "$gameCode". Ask the host to share a current code.'
          : 'Could not join game: $error';
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(message)));
    } finally {
      if (mounted) setState(() => _joining = false);
    }
  }

  Future<bool> _ensureLocationReadyBeforeLobby() async {
    while (mounted) {
      final position = await LocationService.instance.getCurrentPosition(
        timeLimit: const Duration(seconds: 10),
      );
      if (!mounted) return false;
      if (position != null) return true;

      final retry = await showDialog<bool>(
        context: context,
        barrierDismissible: false,
        builder: (dialogContext) {
          return AlertDialog(
            title: const Text('Location required'),
            content: const Text(
              'Please allow location access and ensure GPS is enabled before joining the lobby.',
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.of(dialogContext).pop(false),
                child: const Text('Cancel'),
              ),
              TextButton(
                onPressed: () => Navigator.of(dialogContext).pop(true),
                child: const Text('Retry'),
              ),
            ],
          );
        },
      );

      if (retry != true) return false;
    }

    return false;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: XActColors.bg,
      resizeToAvoidBottomInset: true,
      body: SafeArea(
        child: Column(
          children: [
            XActBranding.buildTopBar(
              context: context,
              eyebrow: 'With code',
              title: 'Join a Game',
            ),
            Expanded(
              child: GestureDetector(
                onTap: () => _codeFocus.requestFocus(),
                behavior: HitTestBehavior.opaque,
                child: SingleChildScrollView(
                  keyboardDismissBehavior:
                      ScrollViewKeyboardDismissBehavior.onDrag,
                  padding: EdgeInsets.fromLTRB(
                    24,
                    8,
                    24,
                    24 + MediaQuery.of(context).viewInsets.bottom,
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      const SizedBox(height: 10),
                      Center(
                        child: Column(
                          children: [
                            Text(
                              'Enter the code',
                              style: XActText.displaySm,
                            ),
                            const SizedBox(height: 6),
                            Text(
                              'Six characters · letters & numbers',
                              style: XActText.caption.copyWith(
                                fontSize: 14,
                                color: XActColors.text3,
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 22),
                      _CodeCells(
                        value: _codeController.text,
                        length: _codeLength,
                      ),
                      // Hidden field that drives the visible cells.
                      SizedBox(
                        height: 0,
                        child: Offstage(
                          child: TextField(
                            controller: _codeController,
                            focusNode: _codeFocus,
                            autofocus: false,
                            maxLength: _codeLength,
                            textCapitalization: TextCapitalization.characters,
                            keyboardType: TextInputType.visiblePassword,
                            inputFormatters: [
                              FilteringTextInputFormatter.allow(
                                RegExp(r'[A-Za-z0-9]'),
                              ),
                              _UpperCaseTextFormatter(),
                            ],
                          ),
                        ),
                      ),
                      const SizedBox(height: 18),
                      XActBranding.buildGhostButton(
                        text: 'Scan QR instead',
                        icon: Icons.qr_code_scanner_rounded,
                        height: 48,
                        onPressed: _joining ? null : _onScan,
                      ),
                      const SizedBox(height: XActSpace.s6),
                      XActBranding.buildTextField(
                        label: 'Your display name',
                        hintText: 'Enter your name…',
                        controller: _usernameController,
                      ),
                      const SizedBox(height: XActSpace.s4),
                      _InfoBanner(
                        text:
                            'We use your location only during a match. Your name is visible to teammates.',
                      ),
                    ],
                  ),
                ),
              ),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(24, 0, 24, 28),
              child: XActBranding.buildPrimaryButton(
                text: _joining ? 'Joining…' : 'Join Game',
                onPressed: _joining ? null : _onJoin,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _CodeCells extends StatelessWidget {
  final String value;
  final int length;

  const _CodeCells({required this.value, required this.length});

  @override
  Widget build(BuildContext context) {
    final chars = value.split('');
    final focusedIndex = chars.length.clamp(0, length - 1);

    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: List.generate(length, (i) {
        final filled = i < chars.length;
        final focused = i == focusedIndex && !filled;
        return Padding(
          padding: EdgeInsets.only(right: i == length - 1 ? 0 : 8),
          child: Container(
            width: 48,
            height: 60,
            decoration: BoxDecoration(
              color: filled
                  ? XActColors.surface2
                  : Colors.white.withValues(alpha: .03),
              borderRadius: BorderRadius.circular(14),
              border: Border.all(
                color: focused
                    ? XActColors.secondary
                    : XActColors.hairlineSoft,
                width: focused ? 2 : 1,
              ),
              boxShadow: focused
                  ? [
                      BoxShadow(
                        color: XActColors.secondary.withValues(alpha: .18),
                        blurRadius: 0,
                        spreadRadius: 2,
                      ),
                    ]
                  : null,
            ),
            child: Center(
              child: filled
                  ? Text(
                      chars[i],
                      style: XActText.mono.copyWith(
                        fontSize: 26,
                        letterSpacing: 0,
                        color: XActColors.text1,
                      ),
                    )
                  : (focused
                      ? const _BlinkingCaret()
                      : const SizedBox.shrink()),
            ),
          ),
        );
      }),
    );
  }
}

class _BlinkingCaret extends StatefulWidget {
  const _BlinkingCaret();

  @override
  State<_BlinkingCaret> createState() => _BlinkingCaretState();
}

class _BlinkingCaretState extends State<_BlinkingCaret>
    with SingleTickerProviderStateMixin {
  late final AnimationController _ctrl;

  @override
  void initState() {
    super.initState();
    _ctrl = AnimationController(
      duration: const Duration(milliseconds: 1000),
      vsync: this,
    )..repeat(reverse: true);
  }

  @override
  void dispose() {
    _ctrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return FadeTransition(
      opacity: _ctrl,
      child: Container(
        width: 2,
        height: 26,
        decoration: BoxDecoration(
          color: XActColors.secondary,
          borderRadius: BorderRadius.circular(1),
        ),
      ),
    );
  }
}

class _InfoBanner extends StatelessWidget {
  final String text;
  const _InfoBanner({required this.text});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: XActColors.secondarySoft,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(
          color: XActColors.secondary.withValues(alpha: .2),
        ),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Icon(
            Icons.info_outline_rounded,
            size: 18,
            color: XActColors.secondary,
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Text(
              text,
              style: XActText.bodySm.copyWith(
                color: XActColors.text2,
                height: 1.45,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _UpperCaseTextFormatter extends TextInputFormatter {
  @override
  TextEditingValue formatEditUpdate(
    TextEditingValue oldValue,
    TextEditingValue newValue,
  ) {
    return newValue.copyWith(text: newValue.text.toUpperCase());
  }
}

// Kept for back-compat with any external imports.
class UpperCaseTextFormatter extends _UpperCaseTextFormatter {}
