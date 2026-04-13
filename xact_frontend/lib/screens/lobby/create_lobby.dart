import 'package:flutter/material.dart';
import 'package:xact_frontend/api/api_service.dart';
import 'package:xact_frontend/api/models.dart';
import 'package:xact_frontend/screens/lobby/define_game_area_screen.dart';
import 'package:xact_frontend/screens/team/team_lobby.dart';
import 'package:xact_frontend/services/app_session.dart';
import 'package:xact_frontend/services/geofence_store.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class CreateLobbyScreen extends StatefulWidget {
  const CreateLobbyScreen({super.key});

  @override
  State<CreateLobbyScreen> createState() => _CreateLobbyScreenState();
}

class _CreateLobbyScreenState extends State<CreateLobbyScreen> {
  final _lobbyNameController = TextEditingController();
  bool _creating = false;

  @override
  void dispose() {
    _lobbyNameController.dispose();
    super.dispose();
  }

  void _onCreate() async {
    final lobbyName = _lobbyNameController.text.trim();

    if (lobbyName.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please enter a lobby name')),
      );
      return;
    }

    setState(() => _creating = true);

    try {
      final hostUserId = await ApiService.instance.ensureMvpUser(
        preferredName: 'Host',
      );

      final session = await ApiService.instance.createLobby(
        lobbyName: lobbyName,
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
          content: Text('Lobby created! Join code: ${session.joinCode}'),
        ),
      );

      // Step 1: Let the host define the game area.
      final areaSaved = await Navigator.push<bool>(
        context,
        MaterialPageRoute(
          builder: (_) => DefineGameAreaScreen(sessionId: sessionId),
        ),
      );

      if (areaSaved != true || !mounted) return;

      await ApiService.instance.saveGeofenceArea(
        sessionId: sessionId,
        points: GeofenceStore.instance.points,
      );

      if (!mounted) return;

      // Step 2: Enter the game.
      Navigator.push(
        context,
        MaterialPageRoute(
          builder: (context) => TeamLobbyScreen(
            sessionId: sessionId,
            lobbyCode: session.joinCode,
            isLeader: true,
          ),
        ),
      );
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Could not create lobby: $error')));
    } finally {
      if (mounted) {
        setState(() => _creating = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: XActBranding.backgroundColor,
      body: SafeArea(
        child: SingleChildScrollView(
          keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
          padding: EdgeInsets.fromLTRB(
            24,
            24,
            24,
            16 + MediaQuery.of(context).viewInsets.bottom,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              XActBranding.buildHeader(),
              const SizedBox(height: 32),
              _buildCreateForm(),
              const SizedBox(height: 16),
              XActBranding.buildFooter(),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildCreateForm() {
    return XActBranding.buildFormCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Create New Lobby',
            style: TextStyle(
              color: Colors.white,
              fontSize: 24,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 20),
          XActBranding.buildTextField(
            label: 'Lobby Name',
            hintText: 'Enter lobby name...',
            controller: _lobbyNameController,
          ),
          const SizedBox(height: 24),
          Row(
            children: [
              Expanded(
                child: XActBranding.buildSecondaryButton(
                  text: _creating ? 'Creating...' : 'Create',
                  onPressed: _creating ? null : _onCreate,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: XActBranding.buildCancelButton(
                  text: 'Cancel',
                  onPressed: () => Navigator.of(context).pop(),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
