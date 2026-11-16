using System;
using System.Diagnostics;
using System.Linq;

namespace S1API_NetWorthGUI.Earnings
{
    /// <summary>Optional diagnostic observations only; never recalculates or modifies game money.</summary>
    internal static class EarningsDiagnostics
    {
        private static Action<string>? _log;
        private static Func<bool>? _enabled;
        private static int _session;
        private static long _event;
        private static string? _previousSnapshot;
        private static float? _previousWorth;
        private static float _previousCash;
        private static float _previousBank;

        public static bool Enabled => _enabled?.Invoke() == true;

        public static void Configure(Action<string> log, Func<bool> enabled)
        {
            _log = log;
            _enabled = enabled;
        }

        public static void BeginSession()
        {
            _session++;
            _event = 0;
            _previousSnapshot = null;
            _previousWorth = null;
            _previousCash = _previousBank = 0f;
            Write($"SESSION START: revenue/expenses reset; amounts below are observations, not money changes.");
        }

        public static void Write(FormattableString message)
        {
            if (!Enabled) return;
            // Named culture data is unavailable in some IL2CPP hosts.
            string line = FormattableString.Invariant(message).Replace('\r', ' ').Replace('\n', ' ');
            _log?.Invoke($"[Earnings session={_session} event={++_event}] {line}");
        }

        /// <summary>Managed callers help identify sales/recycling. Native IL2CPP callers may be absent.</summary>
        public static string Caller()
        {
            if (!Enabled) return string.Empty;
            var callers = new StackTrace().GetFrames()?.Select(f => f.GetMethod())
                .Where(m => m?.DeclaringType?.FullName?.StartsWith("ScheduleOne.", StringComparison.Ordinal) == true ||
                            m?.DeclaringType?.FullName?.StartsWith("Il2CppScheduleOne.", StringComparison.Ordinal) == true)
                .Select(m => m!.DeclaringType!.FullName + "." + m.Name).Take(4).ToArray();
            return callers == null || callers.Length == 0 ? "unavailable (native caller may not be exposed)" : string.Join(" <- ", callers);
        }

        public static void Totals(SessionEarningsTracker tracker) =>
            Write($"TOTALS revenue={tracker.Revenue:F2} expenses={tracker.Expenses:F2} profit={tracker.Profit:F2}");

        /// <summary>Log the initial sample and changes only, not every one-second refresh.</summary>
        public static void Observe(float worth, float cash, float bank, SessionEarningsTracker tracker)
        {
            if (!Enabled) return;
            string snapshot = FormattableString.Invariant($"netWorth={worth:F2} cash={cash:F2} bank={bank:F2} otherReportedValue={worth - cash - bank:F2} revenue={tracker.Revenue:F2} expenses={tracker.Expenses:F2} profit={tracker.Profit:F2}");
            if (snapshot == _previousSnapshot) return;
            float delta = _previousWorth.HasValue ? worth - _previousWorth.Value : 0f;
            float cashDelta = _previousWorth.HasValue ? cash - _previousCash : 0f;
            float bankDelta = _previousWorth.HasValue ? bank - _previousBank : 0f;
            Write($"SNAPSHOT {snapshot} netWorthDelta={delta:F2} cashDelta={cashDelta:F2} bankDelta={bankDelta:F2} otherReportedDelta={delta - cashDelta - bankDelta:F2} baseline={!_previousWorth.HasValue}");
            _previousSnapshot = snapshot;
            _previousWorth = worth;
            _previousCash = cash;
            _previousBank = bank;
        }
    }
}
