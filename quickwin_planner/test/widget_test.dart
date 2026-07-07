import 'package:flutter_test/flutter_test.dart';
import 'package:quickwin_planner/main.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  testWidgets('QuickWin Planner opens with core UI', (tester) async {
    SharedPreferences.setMockInitialValues({});

    await tester.pumpWidget(const QuickWinApp());
    await tester.pumpAndSettle();

    expect(find.text('QuickWin Planner'), findsOneWidget);
    expect(find.text('Win the day in small steps'), findsOneWidget);
    expect(find.text('Add'), findsOneWidget);
  });
}
