import 'package:flutter/material.dart';
import 'package:xact_frontend/api/api_service.dart';
import 'package:xact_frontend/api/models.dart';
import 'package:xact_frontend/screens/lobby/define_game_area_screen.dart';
import 'package:xact_frontend/screens/team/team_lobby.dart';
import 'package:xact_frontend/services/app_session.dart';
import 'package:xact_frontend/services/geofence_store.dart';
import 'package:xact_frontend/services/location_service.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class CreateGameScreen extends StatefulWidget {
  const CreateGameScreen({super.key});

  @override
  State<CreateGameScreen> createState() => _CreateGameScreenState();
}

class _CreateGameScreenState extends State<CreateGameScreen> {
  final _gameNameController = TextEditingController();
  bool _creating = false;
  bool _finalizingLobby = false;

  @override
  void dispose() {
    _gameNameController.dispose();
    super.dispose();
  }

  void _onCreate() async {
    final gameName = _gameNameController.text.trim();

    if (gameName.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please enter a game name')),
      );
      return;
    }

    setState(() => _creating = true);

    try {
      final hostUserId = await ApiService.instance.ensureMvpUser(
        preferredName: 'Host',
        reuseByName: true,
      );

      final session = await ApiService.instance.createLobby(
        lobbyName: gameName,
      );
      final sessionId = session.sessionId;

      final snapshot = await ApiService.instance.loadLobbySnapshot(sessionId);
      TeamMemberDetails? hostMember;
      for (final members in snapshot.membersByTeamId.values) {
        for (final member in members) {
          if (member.userId == hostUserId) {
            hostMember = member;
            break;
          }
        }
        if (hostMember != null) {
          break;
        }
      }

      if (hostMember != null) {
        AppSession.instance.setMembership(
          teamId: hostMember.teamId,
          memberId: hostMember.memberId,
          teamLeader: hostMember.isTeamLeader,
        );
      }

      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Game created! Share code: ${session.joinCode}'),
        ),
      );

      // While the host is defining the game area, the create-game form is
      // still mounted underneath; flipping this flag now means the moment
      // the area screen pops we reveal a loading view instead of the form
      // briefly reappearing.
      setState(() => _finalizingLobby = true);

      // Step 1: Let the host define the game area.
      final areaSaved = await Navigator.push<bool>(
        context,
        MaterialPageRoute(
          builder: (_) => DefineGameAreaScreen(
            sessionId: sessionId,
            gameName: session.sessionName,
          ),
        ),
      );

      if (areaSaved != true || !mounted) {
        if (mounted) setState(() => _finalizingLobby = false);
        return;
      }

      await ApiService.instance.saveGeofenceArea(
        sessionId: sessionId,
        points: GeofenceStore.instance.points,
      );

      final locationReady = await _ensureLocationReadyBeforeLobby();
      if (!locationReady || !mounted) {
        if (mounted) setState(() => _finalizingLobby = false);
        return;
      }

      // Step 2: Enter the game.
      Navigator.pushReplacement(
        context,
        MaterialPageRoute(
          builder: (context) => GameLobbyScreen(
            sessionId: sessionId,
            gameCode: session.joinCode,
            gameName: session.sessionName,
            isLeader: true,
          ),
        ),
      );
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Could not create game: $error')));
    } finally {
      if (mounted) {
        setState(() => _creating = false);
      }
    }
  }

  Future<bool> _ensureLocationReadyBeforeLobby() async {
    while (mounted) {
      final position = await LocationService.instance.getCurrentPosition(
        timeLimit: const Duration(seconds: 10),
      );
      if (!mounted) return false;
      if (position != null) {
        return true;
      }

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

      if (retry != true) {
        return false;
      }
    }

    return false;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: XActColors.bg,
      resizeToAvoidBottomInset: true,
      body: SafeArea(
        child: _finalizingLobby
            ? _buildPreparingLobbyView()
            : Column(
                children: [
                  XActBranding.buildTopBar(
                    context: context,
                    eyebrow: 'New session',
                    title: 'Create a Game',
                  ),
                  Expanded(
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
                                  'Name your game',
                                  style: XActText.displaySm,
                                ),
                                const SizedBox(height: 6),
                                Text(
                                  'Pick something memorable for your crew',
                                  style: XActText.caption.copyWith(
                                    fontSize: 14,
                                    color: XActColors.text3,
                                  ),
                                ),
                              ],
                            ),
                          ),
                          const SizedBox(height: XActSpace.s7),
                          XActBranding.buildTextField(
                            label: 'Game name',
                            hintText: "e.g. Sam's Game",
                            controller: _gameNameController,
                            textCapitalization: TextCapitalization.words,
                          ),
                          const SizedBox(height: XActSpace.s4),
                          _InfoBanner(
                            icon: Icons.info_outline_rounded,
                            text:
                                'Next, you\'ll define the play area on a map. Players join with a code or QR.',
                          ),
                        ],
                      ),
                    ),
                  ),
                  Padding(
                    padding: const EdgeInsets.fromLTRB(24, 0, 24, 28),
                    child: XActBranding.buildPrimaryButton(
                      text: _creating ? 'Creating…' : 'Create Game',
                      onPressed: _creating ? null : _onCreate,
                    ),
                  ),
                ],
              ),
      ),
    );
  }

  Widget _buildPreparingLobbyView() {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const CircularProgressIndicator(color: XActColors.secondary),
          const SizedBox(height: 20),
          Text(
            'Preparing your lobby…',
            style: XActText.body.copyWith(color: XActColors.text2),
          ),
        ],
      ),
    );
  }
}

class _InfoBanner extends StatelessWidget {
  final IconData icon;
  final String text;
  const _InfoBanner({required this.icon, required this.text});

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
          Icon(icon, size: 18, color: XActColors.secondary),
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
