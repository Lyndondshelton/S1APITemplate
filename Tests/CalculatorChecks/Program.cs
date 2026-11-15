using System.Globalization;
using S1API_CalculatorPhoneApp.PhoneApps.Calculator;

var calculator = new CalculatorEngine();
int checks = 0;
foreach (string culture in new[] { "en-US", "fr-FR" })
{
    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
    foreach (var (expression, expected) in new (string, double)[]
    {
        ("2 + 3 * 4", 14), ("(2 + 3) * 4", 20), ("10 - 3 - 2", 5),
        ("8 / 2", 4), ("8 / 2 / 2", 2), ("-2 * 3", -6), ("2 * -3", -6),
        ("-(2 + 3)", -5), ("--2", 2), ("2--3", 5), ("+(-.5 + 1.25)", .75),
        ("  12.5 +  .5  ", 13), ("0", 0), ("2 / (3 - 1)", 1), ("1 + 2 * (3 + 4 * 5)", 47)
    })
    {
        if (!calculator.TryEvaluate(expression, out double result, out string error)
            || Math.Abs(result - expected) > 1e-10 || error.Length != 0)
            throw new Exception($"{expression}: expected {expected}, got {result}; {error}");
        checks++;
    }
    foreach (string? expression in new string?[]
    {
        null, "", " ", "2 +", "2 3", "2(3)", "()", "(2 + 3", "2 + 3)",
        "2 + * 3", "abc", "1..2", ".", "1,5", "1 / 0", "1 / -0", "2 / (1 - 1)",
        "1e3", new string('9', 400), new string('9', 200) + "*" + new string('9', 200)
    })
    {
        if (calculator.TryEvaluate(expression, out double result, out string error)
            || result != 0 || string.IsNullOrWhiteSpace(error))
            throw new Exception($"Expected readable error for '{expression}'");
        checks++;
    }
}
Console.WriteLine($"Passed {checks} calculator checks across two cultures.");

var session = new CalculatorSession();
void Press(string expression)
{
    foreach (char key in expression) session.Press(key.ToString());
}
void Expect(string display, bool error = false)
{
    if (session.Display != display || (session.Error.Length > 0) != error)
        throw new Exception($"Button state: expected {display}, got {session.Display}: {session.Error}");
    checks++;
}
Expect("0");
Press("2+3*4="); Expect("14");
session.Press("="); Expect("14");
Press("/2="); Expect("7");
Press("9"); Expect("9"); // Digit after equals starts a new calculation.
session.Press("DEL"); Expect("0");
Press("(120*10)+(40*7)-(3*(10+7))="); Expect("1429");
session.Press("C"); Expect("0");
Press("2+="); Expect("2+", true);
Press("3="); Expect("5");
session.Press("C"); Press("1/0="); Expect("1/0", true);
session.Press("DEL"); Press("2="); Expect("0.5");
session.Press("keyboard input"); Expect("0.5");
session.Press("C"); Press(new string('1', 256));
session.Press("1"); Expect(new string('1', 256), true);
session.Press("DEL"); Expect(new string('1', 255));
session.Press("C"); Press(".5+.25="); Expect("0.75");
session.Press("C"); Press("1/100000000000000000000=");
Press("*100000000000000000000="); Expect("1");
Console.WriteLine($"Passed {checks} total arithmetic and button-state checks.");
