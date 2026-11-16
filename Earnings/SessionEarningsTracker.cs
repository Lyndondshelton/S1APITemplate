using System;

namespace S1API_NetWorthGUI.Earnings
{
    /// <summary>Session-only cash-flow totals. Transfers never enter either total.</summary>
    internal sealed class SessionEarningsTracker
    {
        public bool IsActive { get; private set; }
        public decimal Revenue { get; private set; }
        public decimal Expenses { get; private set; }
        public decimal Profit => Revenue - Expenses;

        public void Start()
        {
            Revenue = Expenses = 0m;
            IsActive = true;
        }

        public void Stop() => IsActive = false;

        /// <summary>Accepts an actual transaction delta, never a periodically sampled net change.</summary>
        public void Record(float delta)
        {
            if (!IsActive || float.IsNaN(delta) || float.IsInfinity(delta) ||
                delta >= (float)decimal.MaxValue || delta <= (float)decimal.MinValue)
                return;

            decimal amount = (decimal)delta;
            if (amount > 0m) Revenue += amount;
            else if (amount < 0m) Expenses -= amount;
        }

        /// <summary>These stable ledger names were verified in the local game assembly.</summary>
        public static bool IsBankTransfer(string? name) =>
            string.Equals(name, "Cash Deposit", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "Cash Withdrawal", StringComparison.OrdinalIgnoreCase) ||
            (name?.StartsWith("Money laundering (", StringComparison.OrdinalIgnoreCase) ?? false);

        public void RecordBank(string? name, float delta)
        {
            if (!IsBankTransfer(name)) Record(delta);
        }
    }
}
