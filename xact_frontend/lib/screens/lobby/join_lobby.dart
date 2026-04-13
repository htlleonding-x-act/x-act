import 'package:flutter/material.dart';
import 'package:xact_frontend/api/api_service.dart';
import 'package:xact_frontend/api/models.dart';
import 'package:xact_frontend/screens/team/team_lobby.dart';
import 'package:xact_frontend/services/app_session.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class JoinLobbyScreen extends StatefulWidget {
  const JoinLobbyScreen({super.key});

  @override
  State<JoinLobbyScreen> createState() => _JoinLobbyScreenState();
}

class _JoinLobbyScreenState extends State<JoinLobbyScreen> {
  final _lobbyCodeController = TextEditingController();
  final _usernameController = TextEditingController();
  bool _joining = false;

  @override
  void dispose() {
    _lobbyCodeController.dispose();
    _usernameController.dispose();
    super.dispose();
  }

  void _onJoin() async {
    final lobbyCode = _lobbyCodeController.text.trim();
    final username = _usernameController.text.trim();

    if (lobbyCode.isEmpty || username.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please fill in all fields')),
      );
      return;
    }
    setState(() => _joining = true);

    try {
      final userId = await ApiService.instance.ensureMvpUser(
        preferredName: username,
      );
      AppSession.instance.setIdentity(userId: userId, username: username);

      final session = await ApiService.instance.joinLobbyByCode(lobbyCode);
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
        if (existingMembership != null) {
          break;
        }
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
        teamName: 'Spectators',
        role: TeamRole.spectator,
        colorCode: '#64748B',
      );

      final member =
          existingMembership ??
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

      if (!mounted) return;
      Navigator.push(
        context,
        MaterialPageRoute(
          builder: (context) => TeamLobbyScreen(
            sessionId: session.sessionId,
            lobbyCode: session.joinCode,
            isLeader: false,
          ),
        ),
      );
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Could not join lobby: $error')));
    } finally {
      if (mounted) {
        setState(() => _joining = false);
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
              _buildJoinForm(),
              const SizedBox(height: 16),
              XActBranding.buildFooter(),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildJoinForm() {
    return XActBranding.buildFormCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Join Lobby',
            style: TextStyle(
              color: Colors.white,
              fontSize: 24,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 20),
          XActBranding.buildTextField(
            label: 'Lobby Code',
            hintText: 'Enter 6-digit code...',
            controller: _lobbyCodeController,
            keyboardType: TextInputType.number,
            maxLength: 6,
          ),
          const SizedBox(height: 16),
          XActBranding.buildTextField(
            label: 'Username',
            hintText: 'Enter your username...',
            controller: _usernameController,
          ),
          const SizedBox(height: 24),
          Row(
            children: [
              Expanded(
                child: XActBranding.buildSecondaryButton(
                  text: _joining ? 'Joining...' : 'Join',
                  onPressed: _joining ? null : _onJoin,
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
