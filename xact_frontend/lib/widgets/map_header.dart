import 'package:flutter/material.dart';

class MapHeader extends StatelessWidget {
  const MapHeader({super.key});

  @override
  Widget build(BuildContext context) {
    return Positioned(
      top: 0,
      left: 0,
      right: 0,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
        decoration: BoxDecoration(
          color: const Color(0xFF0F172A),
          borderRadius: const BorderRadius.only(bottomLeft: Radius.circular(12), bottomRight: Radius.circular(12)),
        ),
        child: SafeArea(
          bottom: false,
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text(
                'X-ACT',
                style: TextStyle(fontSize: 28, fontWeight: FontWeight.bold, color: Colors.white),
              ),
              Row(
                children: [
                  Icon(Icons.access_time, color: Colors.orange.shade400, size: 20),
                  const SizedBox(width: 6),
                  Text('Next ping: 2m', style: TextStyle(color: Colors.orange.shade400, fontSize: 16)),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
