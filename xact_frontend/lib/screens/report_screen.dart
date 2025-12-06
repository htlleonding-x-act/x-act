import 'package:flutter/material.dart';

class ReportScreen extends StatelessWidget {
  const ReportScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Container(
      color: const Color(0xFF1E293B),
      child: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.warning, size: 64, color: Colors.white54),
            const SizedBox(height: 16),
            Text('Report Screen', style: Theme.of(context).textTheme.headlineSmall?.copyWith(color: Colors.white54)),
          ],
        ),
      ),
    );
  }
}
