import 'package:flutter/material.dart';

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
        icon: const Icon(Icons.add, color: Colors.white54),
        label: const Text(
          'Add New Team',
          style: TextStyle(color: Colors.white54),
        ),
        style: OutlinedButton.styleFrom(
          side: const BorderSide(color: Colors.white24),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          padding: const EdgeInsets.symmetric(vertical: 14),
        ),
      ),
    );
  }
}
