import 'package:flutter/material.dart';

import '../services/game_start_transition_service.dart';
import 'xact_branding.dart';

class GameStartOverlay extends StatelessWidget {
  const GameStartOverlay({super.key});

  @override
  Widget build(BuildContext context) {
    return ValueListenableBuilder<GameStartTransitionState>(
      valueListenable: GameStartTransitionService.instance.state,
      builder: (context, transition, _) {
        if (!transition.isVisible) {
          return const SizedBox.shrink();
        }

        final text = transition.isGoPhase
            ? 'GO!'
            : (transition.secondsRemaining?.toString() ?? '');

        final accentColor = transition.isGoPhase
            ? const Color(0xFF22C55E)
            : XActBranding.primaryRed;

        return IgnorePointer(
          ignoring: true,
          child: AnimatedOpacity(
            duration: const Duration(milliseconds: 180),
            opacity: 1,
            child: Stack(
              children: [
                Positioned.fill(
                  child: DecoratedBox(
                    decoration: BoxDecoration(
                      gradient: LinearGradient(
                        begin: Alignment.topLeft,
                        end: Alignment.bottomRight,
                        colors: [
                          XActBranding.backgroundColor.withValues(alpha: 0.94),
                          const Color(0xFF0F1320).withValues(alpha: 0.96),
                        ],
                      ),
                    ),
                  ),
                ),
                Positioned(
                  left: -120,
                  top: -80,
                  child: _GlowBlob(
                    size: 320,
                    color: XActBranding.primaryBlue.withValues(alpha: 0.28),
                  ),
                ),
                Positioned(
                  right: -90,
                  bottom: -90,
                  child: _GlowBlob(
                    size: 280,
                    color: XActBranding.primaryRed.withValues(alpha: 0.26),
                  ),
                ),
                Center(
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 24),
                    child: ConstrainedBox(
                      constraints: const BoxConstraints(maxWidth: 680),
                      child: Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 28,
                          vertical: 30,
                        ),
                        decoration: BoxDecoration(
                          color: Colors.white.withValues(alpha: 0.06),
                          borderRadius: BorderRadius.circular(24),
                          border: Border.all(
                            color: Colors.white.withValues(alpha: 0.18),
                          ),
                          boxShadow: [
                            BoxShadow(
                              color: accentColor.withValues(alpha: 0.22),
                              blurRadius: 42,
                              spreadRadius: 6,
                            ),
                          ],
                        ),
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Text(
                              transition.isGoPhase
                                  ? 'HUNT IS LIVE'
                                  : 'GAME STARTS IN',
                              textAlign: TextAlign.center,
                              style: const TextStyle(
                                color: Colors.white,
                                fontSize: 28,
                                letterSpacing: 3.5,
                                fontWeight: FontWeight.w800,
                              ),
                            ),
                            const SizedBox(height: 10),
                            Text(
                              transition.isGoPhase
                                  ? 'Good luck. Catch Mister X.'
                                  : 'Get ready for the chase',
                              textAlign: TextAlign.center,
                              style: TextStyle(
                                color: Colors.white.withValues(alpha: 0.72),
                                fontSize: 16,
                                letterSpacing: 0.8,
                                fontWeight: FontWeight.w500,
                              ),
                            ),
                            const SizedBox(height: 24),
                            AnimatedSwitcher(
                              duration: const Duration(milliseconds: 260),
                              switchInCurve: Curves.easeOutBack,
                              switchOutCurve: Curves.easeIn,
                              transitionBuilder: (child, animation) {
                                final scale = Tween<double>(
                                  begin: 0.75,
                                  end: 1,
                                ).animate(animation);

                                return FadeTransition(
                                  opacity: animation,
                                  child: ScaleTransition(
                                    scale: scale,
                                    child: child,
                                  ),
                                );
                              },
                              child: Text(
                                text,
                                key: ValueKey<String>(text),
                                textAlign: TextAlign.center,
                                style: TextStyle(
                                  color: accentColor,
                                  fontSize: transition.isGoPhase ? 124 : 136,
                                  height: 0.95,
                                  fontWeight: FontWeight.w900,
                                  shadows: [
                                    Shadow(
                                      color: accentColor.withValues(alpha: 0.42),
                                      blurRadius: 28,
                                    ),
                                    const Shadow(
                                      color: Colors.black54,
                                      blurRadius: 10,
                                      offset: Offset(0, 3),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}

class _GlowBlob extends StatelessWidget {
  final double size;
  final Color color;

  const _GlowBlob({required this.size, required this.color});

  @override
  Widget build(BuildContext context) {
    return IgnorePointer(
      child: SizedBox(
        width: size,
        height: size,
        child: DecoratedBox(
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            gradient: RadialGradient(
              colors: [
                color,
                color.withValues(alpha: 0.0),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
