import 'package:flutter/material.dart';
import 'package:xact_frontend/api/api_service.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

/// returns true once the new name is saved
Future<bool> showRenameDialog(BuildContext context, String currentName) async {
  final renamed = await showDialog<bool>(
    context: context,
    builder: (_) => _RenameDialog(currentName: currentName),
  );
  return renamed ?? false;
}

/// returns true once the account is deleted and the player is signed out
Future<bool> showDeleteAccountDialog(
  BuildContext context,
  String username,
) async {
  final deleted = await showDialog<bool>(
    context: context,
    builder: (_) => _DeleteAccountDialog(username: username),
  );
  return deleted ?? false;
}

class _RenameDialog extends StatefulWidget {
  const _RenameDialog({required this.currentName});

  final String currentName;

  @override
  State<_RenameDialog> createState() => _RenameDialogState();
}

class _RenameDialogState extends State<_RenameDialog> {
  // same limit as the backend validator
  static const _maxLength = 50;

  late final TextEditingController _controller = TextEditingController(
    text: widget.currentName,
  );
  bool _saving = false;
  String? _error;

  bool get _canSave {
    final name = _controller.text.trim();
    return !_saving && name.isNotEmpty && name != widget.currentName;
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    setState(() {
      _saving = true;
      _error = null;
    });

    try {
      await ApiService.instance.renameMe(_controller.text);
      if (mounted) Navigator.of(context).pop(true);
    } catch (e) {
      if (mounted) {
        setState(() {
          _saving = false;
          _error = describeApiError(e);
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text('Change username', style: XActText.heading),
      content: TextField(
        controller: _controller,
        autofocus: true,
        maxLength: _maxLength,
        enabled: !_saving,
        style: XActText.body,
        cursorColor: XActColors.secondary,
        textInputAction: TextInputAction.done,
        onChanged: (_) => setState(() => _error = null),
        onSubmitted: (_) => _canSave ? _save() : null,
        decoration: InputDecoration(
          hintText: 'Your name in games',
          errorText: _error,
          errorMaxLines: 3,
        ),
      ),
      actions: [
        TextButton(
          onPressed: _saving ? null : () => Navigator.of(context).pop(false),
          child: Text(
            'Cancel',
            style: XActText.bodySm.copyWith(color: XActColors.text3),
          ),
        ),
        TextButton(
          onPressed: _canSave ? _save : null,
          child: Text(
            _saving ? 'Saving…' : 'Save',
            style: XActText.bodySm.copyWith(
              color: _canSave ? XActColors.secondary : XActColors.text4,
              fontWeight: FontWeight.w700,
            ),
          ),
        ),
      ],
    );
  }
}

class _DeleteAccountDialog extends StatefulWidget {
  const _DeleteAccountDialog({required this.username});

  final String username;

  @override
  State<_DeleteAccountDialog> createState() => _DeleteAccountDialogState();
}

class _DeleteAccountDialogState extends State<_DeleteAccountDialog> {
  final TextEditingController _controller = TextEditingController();
  bool _deleting = false;
  String? _error;

  // typing the name keeps a stray tap from deleting the account
  bool get _confirmed => _controller.text.trim() == widget.username;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _delete() async {
    setState(() {
      _deleting = true;
      _error = null;
    });

    try {
      await ApiService.instance.deleteMyAccount();
      if (mounted) Navigator.of(context).pop(true);
    } catch (e) {
      if (mounted) {
        setState(() {
          _deleting = false;
          _error = describeApiError(e);
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final canDelete = _confirmed && !_deleting;

    return AlertDialog(
      title: Text('Delete account?', style: XActText.heading),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'This removes your name and email from X-ACT, leaves your '
            'current game and signs you out. Signing in again with the same '
            'login brings the account back.',
            style: XActText.body.copyWith(
              color: XActColors.text3,
              fontSize: 14,
            ),
          ),
          const SizedBox(height: XActSpace.s4),
          Text(
            'Type ${widget.username} to confirm',
            style: XActText.bodySm.copyWith(color: XActColors.text2),
          ),
          TextField(
            controller: _controller,
            enabled: !_deleting,
            style: XActText.body,
            cursorColor: XActColors.primary,
            onChanged: (_) => setState(() {}),
            decoration: InputDecoration(
              hintText: widget.username,
              errorText: _error,
              errorMaxLines: 3,
            ),
          ),
        ],
      ),
      actions: [
        TextButton(
          onPressed: _deleting ? null : () => Navigator.of(context).pop(false),
          child: Text(
            'Cancel',
            style: XActText.bodySm.copyWith(color: XActColors.text3),
          ),
        ),
        TextButton(
          onPressed: canDelete ? _delete : null,
          child: Text(
            _deleting ? 'Deleting…' : 'Delete',
            style: XActText.bodySm.copyWith(
              color: canDelete ? XActColors.primary : XActColors.text4,
              fontWeight: FontWeight.w700,
            ),
          ),
        ),
      ],
    );
  }
}
