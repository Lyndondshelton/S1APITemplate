using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using S1API.Money;

namespace S1API_NetWorthGUI.Earnings
{
    /// <summary>
    /// Runtime-resolved prefix/postfix hooks keep game types out of the shared DLL.
    /// The received bank RPC is patched at its balance mutation, avoiding duplicate
    /// counting of the server request and observer notification on a host.
    /// </summary>
    internal static class TransactionHooks
    {
        private static readonly HarmonyLib.Harmony Patches = new HarmonyLib.Harmony("NetWorthGui.SessionEarnings");
        internal static SessionEarningsTracker? Tracker { get; private set; }
        [ThreadStatic] private static int _transferDepth;
        [ThreadStatic] private static int _storageExpenseDepth;

        public static void Install(SessionEarningsTracker tracker)
        {
            try
            {
                Patch(Find("Money.MoneyManager", "ChangeCashBalance"), nameof(CashBefore), nameof(CashAfter));
                var manager = FindType("Money.MoneyManager");
                var receipt = manager.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Single(m => m.Name.StartsWith("RpcLogic___ReceiveOnlineTransaction_", StringComparison.Ordinal));
                Patch(receipt, nameof(BankBefore), nameof(BankAfter));

                // Suppress just each execution step, never the ATM coroutine's entire lifetime.
                var iterator = FindType("UI.ATMInterface").GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                    .Single(t => t.Name.Contains("ProcessTransaction"));
                PatchScope(AccessTools.Method(iterator, "MoveNext"), nameof(TransferBefore));
                PatchScope(Find("UI.LaunderingInterface", "ConfirmAmount"), nameof(TransferBefore));

                // Money in storage moves freely; only consumption by wages/suppliers is an expense.
                PatchScope(Find("Employees.EmployeeHome", "RemoveCash"), nameof(StorageBefore));
                PatchScope(Find("Economy.SupplierStash", "RemoveCash"), nameof(StorageBefore));
                Patch(Find("ItemFramework.CashInstance", "ChangeBalance"), nameof(StorageCashBefore), nameof(StorageCashAfter));
                Tracker = tracker;
            }
            catch
            {
                // Partial transfer coverage would silently produce misleading revenue.
                Patches.UnpatchSelf();
                throw;
            }
        }

        public static void Uninstall()
        {
            Tracker = null;
            Patches.UnpatchSelf();
        }

        private static Type FindType(string suffix) => GameTypeLookup.Find(suffix);

        private static MethodInfo Find(string type, string method) => AccessTools.Method(FindType(type), method)
            ?? throw new MissingMethodException(type, method);

        private static HarmonyMethod Hook(string name) => new HarmonyMethod(typeof(TransactionHooks), name);
        private static void Patch(MethodInfo target, string before, string after) =>
            Patches.Patch(target, prefix: Hook(before), postfix: Hook(after));

        private static void PatchScope(MethodInfo target, string before) =>
            Patches.Patch(target, prefix: Hook(before), finalizer: Hook(nameof(EndScope)));

        private static void TransferBefore(out bool __state) { __state = true; _transferDepth++; }
        private static void StorageBefore(out bool __state) { __state = false; _storageExpenseDepth++; }
        // Finalizers restore context even when a game transaction throws.
        private static void EndScope(bool __state)
        {
            if (__state) _transferDepth--;
            else _storageExpenseDepth--;
        }

        private static void CashBefore(out float __state) =>
            __state = Tracker?.IsActive == true ? Money.GetCashBalance() : float.NaN;
        private static void CashAfter(float __0, float __state)
        {
            if (float.IsNaN(__state)) return;
            float after = Money.GetCashBalance();
            bool excluded = _transferDepth > 0;
            EarningsDiagnostics.Write($"CASH requested={__0:F2} before={__state:F2} after={after:F2} actualDelta={after - __state:F2} excluded={excluded} reason={(excluded ? "ATM/laundering scope" : "receipt/payment")} caller={EarningsDiagnostics.Caller()}");
            if (!excluded) Tracker?.Record(after - __state);
            if (Tracker != null) EarningsDiagnostics.Totals(Tracker);
        }

        private static void BankBefore(out float __state) =>
            __state = Tracker?.IsActive == true ? Money.GetOnlineBalance() : float.NaN;
        private static void BankAfter(string __0, float __1, float __2, float __state)
        {
            if (float.IsNaN(__state)) return;
            float after = Money.GetOnlineBalance();
            EarningsDiagnostics.Write($"BANK name='{__0}' unitAmount={__1:F2} quantity={__2:F2} requestedTotal={__1 * __2:F2} before={__state:F2} after={after:F2} actualDelta={after - __state:F2} excluded={SessionEarningsTracker.IsBankTransfer(__0)}");
            Tracker?.RecordBank(__0, after - __state);
            if (Tracker != null) EarningsDiagnostics.Totals(Tracker);
        }

        private static float StoredBalance(object instance) => Traverse.Create(instance).Property("Balance").GetValue<float>();
        private static void StorageCashBefore(object __instance, out float __state) =>
            __state = _storageExpenseDepth > 0 && Tracker?.IsActive == true ? StoredBalance(__instance) : float.NaN;
        private static void StorageCashAfter(object __instance, float __state)
        {
            if (_storageExpenseDepth > 0 && _transferDepth == 0 && !float.IsNaN(__state))
            {
                float after = StoredBalance(__instance);
                EarningsDiagnostics.Write($"STORAGE_PAYMENT before={__state:F2} after={after:F2} actualDelta={after - __state:F2} caller={EarningsDiagnostics.Caller()}");
                Tracker?.Record(after - __state);
                if (Tracker != null) EarningsDiagnostics.Totals(Tracker);
            }
        }
    }
}
