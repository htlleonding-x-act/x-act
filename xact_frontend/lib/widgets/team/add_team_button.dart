import 'package:flutter/material.dart';

import '../xact_branding.dart';

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
