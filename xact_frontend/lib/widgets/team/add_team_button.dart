import 'package:flutter/material.dart';

import '../xact_branding.dart';

/// Ghost button for adding a new team in the lobby.
class AddTeamButton extends StatelessWidget {
  final VoidCallback onPressed;

  const AddTeamButton({super.key, required this.onPressed});

  @override
  Widget build(BuildContext context) {
    return XActBranding.buildGhostButton(
      text: 'Add another team',
      icon: Icons.add_rounded,
      height: 48,
      onPressed: onPressed,
    );
  }
}
