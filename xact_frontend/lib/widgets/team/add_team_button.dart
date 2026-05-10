import 'package:flutter/material.dart';

import '../xact_branding.dart';

/// Outlined button for adding a new team in the lobby.
class AddTeamButton extends StatelessWidget {
  final VoidCallback onPressed;

  const AddTeamButton({super.key, required this.onPressed});

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: double.infinity,
      child: OutlinedButton.icon(
        onPressed: onPressed,
        icon: Icon(Icons.add, color: XActColors.text3),
        label: Text(
          'Add New Team',
          style: XActText.body.copyWith(color: XActColors.text3),
        ),
        style: OutlinedButton.styleFrom(
          side: BorderSide(color: XActColors.text5),
          shape: const RoundedRectangleBorder(borderRadius: XActRadius.md),
          padding: const EdgeInsets.symmetric(vertical: XActSpace.s3),
        ),
      ),
    );
  }
}
