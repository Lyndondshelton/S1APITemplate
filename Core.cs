using MelonLoader;
using S1API.Lifecycle;
using S1API.Money;
using S1APITemplate.Utils;
using S1APITemplate.UI;
using UnityEngine;
using S1APITemplate.Finance;

// Once you update the Core.cs namespace, you will have to update the typeof definition here. 
[assembly: MelonInfo(typeof(S1APITemplate.Core), Constants.ModName, Constants.ModVersion, Constants.ModAuthor)]
[assembly: MelonGame(Constants.Game.Studio, Constants.Game.Name)]

/*
 * ----------------------------------------------------
 * Update the namespace to match what is defined in the
 * S1APITemplate.csproj file.
 * ----------------------------------------------------
 */
namespace S1APITemplate
{
    public sealed class Core : MelonMod
    {
        public static Core? Instance { get; private set; }

        private static MelonPreferences_Category? _preferencesCategory;
        private static MelonPreferences_Entry<bool>? _debugLogsEntry;
        private static MelonPreferences_Entry<bool>? _financeLogsEntry;
        private NetWorthHud? _netWorthHud;
        private float _nextRefreshTime;
        private readonly SessionFinanceTracker _sessionFinance = new SessionFinanceTracker();
        private bool _transactionHooksReady;

        public static bool DebugLogsEnabled => _debugLogsEntry?.Value ?? Constants.Defaults.DebugLogsEnabled;

        public override void OnInitializeMelon()
        {
            Instance = this;
            InitializePreferences();
            FinanceDiagnostics.Configure(message => LoggerInstance.Msg(message), () => _financeLogsEntry?.Value == true);
            try
            {
                TransactionHooks.Install(_sessionFinance);
                _transactionHooksReady = true;
            }
            catch (System.Exception exception)
            {
                LoggerInstance.Error($"Session transaction hooks unavailable: {exception}");
            }
            GameLifecycle.OnPreLoad += OnPreLoad;
            GameLifecycle.OnLoadComplete += OnLoadComplete;
            GameLifecycle.OnPreSceneChange += RemoveHud;

            LoggerInstance.Msg($"{Constants.ModName} initialized.");
        }

        public override void OnApplicationQuit()
        {
            GameLifecycle.OnPreLoad -= OnPreLoad;
            GameLifecycle.OnLoadComplete -= OnLoadComplete;
            GameLifecycle.OnPreSceneChange -= RemoveHud;
            RemoveHud();
            TransactionHooks.Uninstall();
            Instance = null;
        }

        private void OnPreLoad()
        {
            // A fresh load must never retain UI or values from the previous save.
            RemoveHud();
        }

        private void OnLoadComplete()
        {
            RemoveHud();
            try
            {
                _netWorthHud = new NetWorthHud();
                _netWorthHud.Show();
                if (_transactionHooksReady) _sessionFinance.Start();
                FinanceDiagnostics.BeginSession();
                RefreshNetWorth();
            }
            catch (System.Exception exception)
            {
                RemoveHud();
                LoggerInstance.Error($"Could not create net-worth HUD: {exception}");
            }
        }

        /// <summary>Polls on Unity's main thread, only while a loaded save owns a HUD.</summary>
        public override void OnUpdate()
        {
            if (_netWorthHud == null || Time.unscaledTime < _nextRefreshTime)
                return;

            try
            {
                RefreshNetWorth();
            }
            catch (System.Exception exception)
            {
                // Stop after a failure instead of flooding the log every frame.
                RemoveHud();
                LoggerInstance.Error($"Could not refresh net-worth HUD: {exception}");
            }
        }

        private void RefreshNetWorth()
        {
            // Observe the exact same game result displayed on the HUD; do not invoke
            // GetNetWorth again for logging because its calculation runs game callbacks.
            float worth = Money.GetNetWorth();
            FinanceDiagnostics.Observe(worth, Money.GetCashBalance(), Money.GetOnlineBalance(), _sessionFinance);
            _netWorthHud?.SetValue(worth, _sessionFinance.Revenue,
                _sessionFinance.Expenses, _sessionFinance.Profit, _transactionHooksReady);
            _nextRefreshTime = Time.unscaledTime + Constants.Defaults.HudRefreshSeconds;
        }

        private void RemoveHud()
        {
            if (_sessionFinance.IsActive)
                FinanceDiagnostics.Write($"SESSION STOP revenue={_sessionFinance.Revenue:F2} expenses={_sessionFinance.Expenses:F2} profit={_sessionFinance.Profit:F2}");
            _sessionFinance.Stop();
            _netWorthHud?.Dispose();
            _netWorthHud = null;
            _nextRefreshTime = 0f;
        }

        private static void InitializePreferences()
        {
            _preferencesCategory = MelonPreferences.CreateCategory(Constants.PreferencesCategory);
            _debugLogsEntry = _preferencesCategory.CreateEntry(
                "DebugLogs",
                Constants.Defaults.DebugLogsEnabled,
                "Enable Debug Logs",
                "Show detailed debug messages in the MelonLoader console.");
            _financeLogsEntry = _preferencesCategory.CreateEntry(
                "FinanceDiagnostics", true, "Log Finance Diagnostics",
                "Log transactions, exclusions, and changed net-worth snapshots to MelonLoader Latest.log.");
            _preferencesCategory.SaveToFile(false);
        }
    }
}
