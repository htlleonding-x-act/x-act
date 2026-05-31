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

/// Dialog that lets the lobby leader create or edit a team.
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
  })  : title = 'Edit team',
        submitLabel = 'Save';

  const AddTeamDialog.create({
    super.key,
    this.initialName = 'Team 3',
  })  : title = 'Add new team',
        submitLabel = 'Create',
        initialMaxPlayers = 3,
        initialColor = const Color(0xFF5B7CFA);

  @override
  State<AddTeamDialog> createState() => _AddTeamDialogState();
}

class _AddTeamDialogState extends State<AddTeamDialog> {
  late final TextEditingController _nameController;
  late int _maxPlayers;
  late Color _selectedColor;

  static const List<Color> _colorOptions = [
    Color(0xFF5B7CFA), // detective blue
    Color(0xFF34D399), // success green
    Color(0xFFF6B05B), // warning amber
    Color(0xFFFF4D5E), // primary red
    Color(0xFFA78BFA), // violet
    Color(0xFF06B6D4), // cyan
    Color(0xFFF472B6), // pink
    Color(0xFF94A3B8), // slate
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
    Navigator.of(context).pop(
      AddTeamResult(name: name, color: _selectedColor, maxPlayers: _maxPlayers),
    );
  }

  @override
  Widget build(BuildContext context) {
    final screenHeight = MediaQuery.of(context).size.height;
    final keyboardInset = MediaQuery.of(context).viewInsets.bottom;

    return Dialog(
      backgroundColor: XActColors.surface,
      surfaceTintColor: Colors.transparent,
      insetPadding: const EdgeInsets.symmetric(horizontal: 24, vertical: 24),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(24),
        side: BorderSide(color: XActColors.hairlineSoft),
      ),
      child: ConstrainedBox(
        constraints: BoxConstraints(
          maxWidth: 420,
          maxHeight: screenHeight * 0.85 - keyboardInset,
        ),
        child: SingleChildScrollView(
          keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
          padding: const EdgeInsets.all(20),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        XActBranding.buildEyebrow('Lobby'),
                        const SizedBox(height: 2),
                        Text(widget.title, style: XActText.heading),
                      ],
                    ),
                  ),
                  XActBranding.circleIconButton(
                    icon: Icons.close_rounded,
                    onPressed: () => Navigator.of(context).pop(),
                  ),
                ],
              ),
              const SizedBox(height: 18),
              XActBranding.buildTextField(
                label: 'Team name',
                hintText: 'e.g. Team 3',
                controller: _nameController,
                textCapitalization: TextCapitalization.words,
              ),
              const SizedBox(height: 18),
              Text(
                'Max players',
                style: XActText.bodySm.copyWith(
                  color: XActColors.text3,
                  fontWeight: FontWeight.w500,
                  letterSpacing: .2,
                ),
              ),
              const SizedBox(height: 8),
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 6,
                ),
                decoration: BoxDecoration(
                  color: Colors.white.withValues(alpha: .03),
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(color: XActColors.hairlineSoft),
                ),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    IconButton(
                      onPressed: _maxPlayers > 1
                          ? () => setState(() => _maxPlayers--)
                          : null,
                      icon: const Icon(Icons.remove_rounded),
                      color: XActColors.text2,
                    ),
                    Text(
                      '$_maxPlayers',
                      style: XActText.heading.copyWith(fontSize: 20),
                    ),
                    IconButton(
                      onPressed: _maxPlayers < 10
                          ? () => setState(() => _maxPlayers++)
                          : null,
                      icon: const Icon(Icons.add_rounded),
                      color: XActColors.text2,
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 18),
              Text(
                'Team color',
                style: XActText.bodySm.copyWith(
                  color: XActColors.text3,
                  fontWeight: FontWeight.w500,
                  letterSpacing: .2,
                ),
              ),
              const SizedBox(height: 10),
              Wrap(
                spacing: 12,
                runSpacing: 12,
                children: _colorOptions.map((c) {
                  final isSelected = c.toARGB32() == _selectedColor.toARGB32();
                  return GestureDetector(
                    onTap: () => setState(() => _selectedColor = c),
                    child: Container(
                      width: 36,
                      height: 36,
                      decoration: BoxDecoration(
                        color: c,
                        shape: BoxShape.circle,
                        border: Border.all(
                          color: isSelected
                              ? Colors.white
                              : Colors.white.withValues(alpha: .06),
                          width: isSelected ? 3 : 1,
                        ),
                        boxShadow: isSelected
                            ? [
                                BoxShadow(
                                  color: c.withValues(alpha: .5),
                                  blurRadius: 12,
                                ),
                              ]
                            : null,
                      ),
                    ),
                  );
                }).toList(),
              ),
              const SizedBox(height: 22),
              Row(
                children: [
                  Expanded(
                    child: XActBranding.buildGhostButton(
                      text: 'Cancel',
                      height: 48,
                      onPressed: () => Navigator.of(context).pop(),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: XActBranding.buildSecondaryButton(
                      text: widget.submitLabel,
                      height: 48,
                      onPressed: _submit,
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
