using System;
using System.Collections.Generic;
using System.Globalization;

namespace S1API_CalculatorPhoneApp.PhoneApps.Calculator;

/// <summary>Arithmetic evaluation without Unity or phone dependencies.</summary>
public sealed class CalculatorEngine
{
    public bool TryEvaluate(string? expression, out double result, out string error)
    {
        result = 0;
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(expression))
        {
            error = "Enter an expression.";
            return false;
        }

        var output = new List<string>();
        var operators = new Stack<string>();
        bool expectingOperand = true;

        for (int i = 0; i < expression!.Length;)
        {
            char c = expression[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }

            if ((c >= '0' && c <= '9') || c == '.')
            {
                if (!expectingOperand) { error = "Expected an operator between values."; return false; }
                int start = i++;
                while (i < expression.Length &&
                       ((expression[i] >= '0' && expression[i] <= '9') || expression[i] == '.')) i++;
                string number = expression.Substring(start, i - start);
                if (!double.TryParse(number, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double value)
                    || !IsFinite(value))
                { error = "Invalid or excessively large number."; return false; }
                output.Add(number);
                expectingOperand = false;
                continue;
            }

            i++;
            if (c == '(')
            {
                if (!expectingOperand) { error = "Expected an operator before '('."; return false; }
                operators.Push("(");
            }
            else if (c == ')')
            {
                if (expectingOperand) { error = "Expected a value before ')'."; return false; }
                while (operators.Count > 0 && operators.Peek() != "(") output.Add(operators.Pop());
                if (operators.Count == 0) { error = "Unmatched closing parenthesis."; return false; }
                operators.Pop();
                expectingOperand = false;
            }
            else if (c == '+' || c == '-' || c == '*' || c == '/')
            {
                bool unary = expectingOperand;
                if (unary && c != '+' && c != '-')
                { error = "Expected a value before the operator."; return false; }
                string op = unary ? "u" + c : c.ToString();
                // Equal-precedence binary operators evaluate left to right. Unary signs
                // associate right to left, allowing expressions such as --2 and 2 * -3.
                while (operators.Count > 0 && operators.Peek() != "(" &&
                       (Precedence(operators.Peek()) > Precedence(op) ||
                        (!unary && Precedence(operators.Peek()) == Precedence(op))))
                    output.Add(operators.Pop());
                operators.Push(op);
                expectingOperand = true;
            }
            else
            { error = $"Unsupported character '{c}'."; return false; }
        }

        if (expectingOperand) { error = "Expression must end with a value."; return false; }
        while (operators.Count > 0)
        {
            string op = operators.Pop();
            if (op == "(") { error = "Unmatched opening parenthesis."; return false; }
            output.Add(op);
        }

        // RPN stores each operator after its operands, so intermediate results
        // can be evaluated with a single value stack.
        var values = new Stack<double>();
        foreach (string token in output)
        {
            if (double.TryParse(token, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double number))
            { values.Push(number); continue; }
            bool unary = token == "u+" || token == "u-";
            if (values.Count < (unary ? 1 : 2))
            { error = "Operator is missing a value."; return false; }
            // Pop the right operand first: subtraction and division depend on order.
            double right = values.Pop();
            double value;
            if (unary) value = token == "u-" ? -right : right;
            else
            {
                double left = values.Pop();
                if (token == "/" && right == 0)
                { error = "Cannot divide by zero."; return false; }
                switch (token)
                {
                    case "+": value = left + right; break;
                    case "-": value = left - right; break;
                    case "*": value = left * right; break;
                    default: value = left / right; break;
                }
            }
            if (!IsFinite(value)) { error = "Result is too large."; return false; }
            values.Push(value);
        }
        if (values.Count != 1) { error = "Invalid expression."; return false; }
        result = values.Pop();
        return true;
    }

    private static int Precedence(string op) =>
        op == "u+" || op == "u-" ? 3 : op == "*" || op == "/" ? 2 : 1;

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
