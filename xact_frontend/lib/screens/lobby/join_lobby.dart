import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:xact_frontend/api/api_service.dart';
import 'package:xact_frontend/api/models.dart';
import 'package:xact_frontend/screens/team/team_lobby.dart';
import 'package:xact_frontend/services/app_session.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class JoinGameScreen extends StatefulWidget {
  const JoinGameScreen({super.key});

  @override
  State<JoinGameScreen> createState() => _JoinGameScreenState();
}

class _JoinGameScreenState extends State<JoinGameScreen> {
  final _gameCodeController = TextEditingController();
  final _usernameController = TextEditingController();
  bool _joining = false;

  @override
  void dispose() {
    _gameCodeController.dispose();
    _usernameController.dispose();
    super.dispose();
  }

  void _onJoin() async {
    final gameCode = _gameCodeController.text.trim().toUpperCase();
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
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Could not join game: $error')));
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
            'Join Friend\'s Game',
            style: TextStyle(
              color: Colors.white,
              fontSize: 24,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 20),
          XActBranding.buildTextField(
            label: 'Game Code',
            hintText: 'Enter 6-character code...',
            controller: _gameCodeController,
            keyboardType: TextInputType.text,
            maxLength: 6,
            textCapitalization: TextCapitalization.characters,
            inputFormatters: [
              FilteringTextInputFormatter.allow(RegExp(r'[A-Za-z0-9]')),
              UpperCaseTextFormatter(),
            ],
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

class UpperCaseTextFormatter extends TextInputFormatter {
  @override
  TextEditingValue formatEditUpdate(
    TextEditingValue oldValue,
    TextEditingValue newValue,
  ) {
    return newValue.copyWith(text: newValue.text.toUpperCase());
  }
}
