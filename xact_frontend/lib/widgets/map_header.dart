import 'package:flutter/material.dart';

import '../api/api_service.dart';

class MapHeader extends StatefulWidget {
  const MapHeader({super.key});

  @override
  State<MapHeader> createState() => _MapHeaderState();
}

class _MapHeaderState extends State<MapHeader> {
  late final Future<MapHeaderData> _load;

  @override
  void initState() {
    super.initState();
    _load = ApiService.instance.loadMapHeader();
  }

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
          borderRadius: const BorderRadius.only(
            bottomLeft: Radius.circular(12),
            bottomRight: Radius.circular(12),
          ),
        ),
        child: SafeArea(
          bottom: false,
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text(
                'X-ACT',
                style: TextStyle(
                  fontSize: 28,
                  fontWeight: FontWeight.bold,
                  color: Colors.white,
                ),
              ),
              FutureBuilder<MapHeaderData>(
                future: _load,
                builder: (context, snapshot) {
                  final text = snapshot.hasError
                      ? 'Next ping: unavailable'
                      : (snapshot.data?.nextPingText ?? 'Next ping: ...');
                  return Row(
                    children: [
                      Icon(
                        Icons.access_time,
                        color: Colors.orange.shade400,
                        size: 20,
                      ),
                      const SizedBox(width: 6),
                      Text(
                        text,
                        style: TextStyle(
                          color: Colors.orange.shade400,
                          fontSize: 16,
                        ),
                      ),
                    ],
                  );
                },
              ),
            ],
          ),
        ),
      ),
    );
  }
}
