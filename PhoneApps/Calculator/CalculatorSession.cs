using System.Globalization;

namespace S1API_CalculatorPhoneApp.PhoneApps.Calculator;

/// <summary>Button-only input state, independent of Unity for testing.</summary>
public sealed class CalculatorSession
{
    private const int MaxLength = 256;
    private readonly CalculatorEngine _engine = new CalculatorEngine();
    private string _expression = string.Empty;
    private bool _hasResult;
    public string Display { get; private set; } = "0";
    public string Error { get; private set; } = string.Empty;

    public void Press(string key)
    {
        if (key == "C")
        {
            _expression = string.Empty;
            Display = "0";
            Error = string.Empty;
            _hasResult = false;
            return;
        }
        if (key == "DEL")
        {
            if (_expression.Length > 0) _expression = _expression.Substring(0, _expression.Length - 1);
            _hasResult = false;
        }
        else if (key == "=")
        {
            if (_hasResult) return;
            if (!_engine.TryEvaluate(_expression, out double value, out string error))
            { Error = error; return; }
            Display = value.ToString("G15", CultureInfo.InvariantCulture);
            // Expand exponent notation for subsequent operations; the parser accepts decimal literals.
            _expression = value.ToString("0." + new string('#', 340), CultureInfo.InvariantCulture);
            _hasResult = true;
            Error = string.Empty;
            return;
        }
        else
        {
            if (key.Length != 1 || "0123456789.+-*/()".IndexOf(key[0]) < 0) return;
            if (_hasResult && "0123456789.(".IndexOf(key[0]) >= 0) _expression = string.Empty;
            if (_expression.Length >= MaxLength)
            { Error = "Expression is too long. Clear or delete to continue."; return; }
            _expression += key;
            _hasResult = false;
        }
        Display = _expression.Length == 0 ? "0" : _expression;
        Error = string.Empty;
    }
}
