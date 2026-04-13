import 'package:flutter/material.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

/// Result returned when a new team is created via the dialog.
class AddTeamResult {
  final String name;
  final Color color;
  final int maxPlayers;

  const AddTeamResult({
    required this.name,
    required this.color,
    this.maxPlayers = 3,
  });
}

/// Dialog that lets the lobby leader create a new team.
///
/// Usage:
/// ```dart
/// final result = await showDialog<AddTeamResult>(
///   context: context,
///   builder: (_) => const AddTeamDialog(),
/// );
/// ```
class AddTeamDialog extends StatefulWidget {
  final String title;
  final String submitLabel;
  final String initialName;
  final int initialMaxPlayers;
  final Color initialColor;

  const AddTeamDialog.edit({
    super.key,
    required this.initialName,
    required this.initialMaxPlayers,
    required this.initialColor,
  }) : title = 'Edit Team',
       submitLabel = 'Save';

  const AddTeamDialog.create({super.key})
    : title = 'Add New Team',
      submitLabel = 'Create',
      initialName = '',
      initialMaxPlayers = 3,
      initialColor = Colors.teal;

  @override
  State<AddTeamDialog> createState() => _AddTeamDialogState();
}

class _AddTeamDialogState extends State<AddTeamDialog> {
  late final TextEditingController _nameController;
  late int _maxPlayers;
  late Color _selectedColor;

  // ── Available team colors ───────────────────────────────────────────────
  static const List<Color> _colorOptions = [
    Colors.teal,
    Colors.orange,
    Colors.pink,
    Colors.cyan,
    Colors.amber,
    Colors.indigo,
    Colors.lime,
    Colors.deepOrange,
  ];

  @override
  void initState() {
    super.initState();
    _nameController = TextEditingController(text: widget.initialName);
    _maxPlayers = widget.initialMaxPlayers;
    _selectedColor = widget.initialColor;
  }

  @override
  void dispose() {
    _nameController.dispose();
    super.dispose();
  }

  void _submit() {
    final name = _nameController.text.trim();
    if (name.isEmpty) {
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('Please enter a team name')));
      return;
    }
    // TODO: Call backend POST /api/teams to persist the new team
    Navigator.of(context).pop(
      AddTeamResult(name: name, color: _selectedColor, maxPlayers: _maxPlayers),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Dialog(
      backgroundColor: XActBranding.cardColor,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // ── Title ────────────────────────────────────────────────────
            Text(
              widget.title,
              style: TextStyle(
                color: Colors.white,
                fontSize: 20,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 20),

            // ── Team name ────────────────────────────────────────────────
            XActBranding.buildTextField(
              label: 'Team Name',
              hintText: 'e.g. Detectives Delta',
              controller: _nameController,
            ),
            const SizedBox(height: 16),

            // ── Max players ──────────────────────────────────────────────
            const Text(
              'Max Players',
              style: TextStyle(color: Colors.white70, fontSize: 14),
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                IconButton(
                  onPressed: _maxPlayers > 1
                      ? () => setState(() => _maxPlayers--)
                      : null,
                  icon: const Icon(Icons.remove_circle_outline),
                  color: Colors.white54,
                ),
                Text(
                  '$_maxPlayers',
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 20,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                IconButton(
                  onPressed: _maxPlayers < 10
                      ? () => setState(() => _maxPlayers++)
                      : null,
                  icon: const Icon(Icons.add_circle_outline),
                  color: Colors.white54,
                ),
              ],
            ),
            const SizedBox(height: 16),

            // ── Color picker ─────────────────────────────────────────────
            const Text(
              'Team Color',
              style: TextStyle(color: Colors.white70, fontSize: 14),
            ),
            const SizedBox(height: 8),
            Wrap(
              spacing: 10,
              runSpacing: 10,
              children: _colorOptions.map((c) {
                final isSelected = c == _selectedColor;
                return GestureDetector(
                  onTap: () => setState(() => _selectedColor = c),
                  child: Container(
                    width: 36,
                    height: 36,
                    decoration: BoxDecoration(
                      color: c,
                      shape: BoxShape.circle,
                      border: isSelected
                          ? Border.all(color: Colors.white, width: 3)
                          : null,
                    ),
                  ),
                );
              }).toList(),
            ),
            const SizedBox(height: 24),

            // ── Action buttons ───────────────────────────────────────────
            Row(
              children: [
                Expanded(
                  child: TextButton(
                    onPressed: () => Navigator.of(context).pop(),
                    child: const Text(
                      'Cancel',
                      style: TextStyle(color: Colors.white54),
                    ),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: ElevatedButton(
                    onPressed: _submit,
                    style: ElevatedButton.styleFrom(
                      backgroundColor: XActBranding.primaryBlue,
                      foregroundColor: Colors.white,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(10),
                      ),
                      padding: const EdgeInsets.symmetric(vertical: 12),
                    ),
                    child: Text(widget.submitLabel),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
