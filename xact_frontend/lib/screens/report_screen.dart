import 'package:flutter/material.dart';

import '../widgets/xact_branding.dart';

class ReportScreen extends StatelessWidget {
  const ReportScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Container(
      color: XActColors.bg,
      child: SafeArea(
        child: Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Text(
              'Report voting is waiting for backend endpoint integration.',
              style: XActText.bodySm.copyWith(color: XActColors.text3),
              textAlign: TextAlign.center,
            ),
          ),
        ),
      ),
    );
  }
}
