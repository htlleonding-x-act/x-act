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

        final accentColor =
            transition.isGoPhase ? XActColors.success : XActColors.primary;

        return IgnorePointer(
          ignoring: true,
          child: AnimatedOpacity(
            duration: const Duration(milliseconds: 180),
            opacity: 1,
            child: Material(
              type: MaterialType.transparency,
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
                            XActBranding.buildEyebrow(
                              transition.isGoPhase
                                  ? 'Hunt is live'
                                  : 'Game starts in',
                              color: accentColor,
                            ),
                            const SizedBox(height: 10),
                            Text(
                              transition.isGoPhase
                                  ? 'Good luck. Catch Mister X.'
                                  : 'Get ready for the chase',
                              textAlign: TextAlign.center,
                              style: XActText.heading.copyWith(
                                fontSize: 22,
                                color: XActColors.text1,
                              ),
                            ),
                            const SizedBox(height: 18),
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
                                style: XActText.display.copyWith(
                                  color: accentColor,
                                  fontSize: transition.isGoPhase ? 112 : 128,
                                  letterSpacing: -4,
                                  height: 0.95,
                                  fontWeight: FontWeight.w700,
                                  shadows: [
                                    Shadow(
                                      color: accentColor.withValues(alpha: 0.42),
                                      blurRadius: 32,
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
