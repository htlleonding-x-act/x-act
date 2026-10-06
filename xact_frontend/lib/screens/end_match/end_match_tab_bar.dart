import 'package:flutter/material.dart';

import '../../widgets/xact_branding.dart';

final class EndMatchTab {
  const EndMatchTab(this.label, this.icon);

  final String label;
  final IconData icon;
}

class EndMatchTabBar extends StatelessWidget {
  const EndMatchTabBar({
    super.key,
    required this.controller,
    required this.tabs,
  });

  final TabController controller;
  final List<EndMatchTab> tabs;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 44,
      padding: const EdgeInsets.all(XActSpace.s1),
      decoration: BoxDecoration(
        color: XActColors.surface,
        borderRadius: XActRadius.pill,
        border: Border.all(color: XActColors.hairlineSoft),
      ),
      child: TabBar(
        controller: controller,
        indicatorSize: TabBarIndicatorSize.tab,
        dividerColor: Colors.transparent,
        indicator: BoxDecoration(
          color: XActColors.surface3,
          borderRadius: XActRadius.pill,
          border: Border.all(color: XActColors.hairlineHi),
        ),
        labelColor: XActColors.text1,
        unselectedLabelColor: XActColors.text3,
        labelStyle: XActText.bodySm.copyWith(fontWeight: FontWeight.w700),
        unselectedLabelStyle: XActText.bodySm,
        splashBorderRadius: XActRadius.pill,
        // the default padding cuts "Overview" off on phones
        labelPadding: const EdgeInsets.symmetric(horizontal: XActSpace.s1),
        tabs: [
          for (final tab in tabs)
            Tab(
              height: 36,
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(tab.icon, size: 16),
                  const SizedBox(width: 6),
                  Flexible(
                    child: Text(tab.label, overflow: TextOverflow.ellipsis),
                  ),
                ],
              ),
            ),
        ],
      ),
    );
  }
}
