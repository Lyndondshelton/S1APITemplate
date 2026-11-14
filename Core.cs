using MelonLoader;
using S1API.Lifecycle;
using S1API.Money;
using S1APITemplate.Utils;
using S1APITemplate.UI;
using UnityEngine;
using S1APITemplate.Earnings;

[assembly: MelonInfo(typeof(S1APITemplate.Core), Constants.ModName, Constants.ModVersion, Constants.ModAuthor)]
[assembly: MelonGame(Constants.Game.Studio, Constants.Game.Name)]

namespace S1APITemplate
{
    public sealed class Core : MelonMod
    {
        public static Core? Instance { get; private set; }

        private static MelonPreferences_Category? _preferencesCategory;
        private static MelonPreferences_Entry<bool>? _debugLogsEntry;
        private static MelonPreferences_Entry<bool>? _earningsLogsEntry;
        private NetWorthHud? _netWorthHud;
        private float _nextRefreshTime;
        private readonly SessionEarningsTracker _sessionEarnings = new SessionEarningsTracker();
        private bool _transactionHooksReady;

        public static bool DebugLogsEnabled => _debugLogsEntry?.Value ?? Constants.Defaults.DebugLogsEnabled;

        public override void OnInitializeMelon()
        {
            Instance = this;
            InitializePreferences();
            EarningsDiagnostics.Configure(message => LoggerInstance.Msg(message), () => _earningsLogsEntry?.Value == true);
            try
            {
                TransactionHooks.Install(_sessionEarnings);
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

        /**
         * Called when a save game is loaded successfully.
         */
        private void OnLoadComplete() {
            RemoveHud();
            try {
                _netWorthHud = new NetWorthHud();
                _netWorthHud.Show();
                if (_transactionHooksReady) _sessionEarnings.Start();
                EarningsDiagnostics.BeginSession();
                RefreshNetWorth();
            } catch (System.Exception exception) {
                RemoveHud();
                LoggerInstance.Error($"Could not create net-worth HUD: {exception}");
            }
        }

        /**
         * <summary>Polls on Unity's main thread, only while a loaded save owns a HUD.</summary>
         */
        public override void OnUpdate() {
            if (_netWorthHud == null || Time.unscaledTime < _nextRefreshTime)
                return;

            try {
                RefreshNetWorth();
            } catch (System.Exception exception) {
                // Stop after a failure instead of flooding the log every frame.
                RemoveHud();
                LoggerInstance.Error($"Could not refresh net-worth HUD: {exception}");
            }
        }

        /**
         * Refreshes the Net Worth HUD keeping the info current and up-to-date.
         */
        private void RefreshNetWorth() {
            // Observe the exact same game result displayed on the HUD; do not invoke
            // GetNetWorth again for logging because its calculation runs game callbacks.
            float worth = Money.GetNetWorth();
            EarningsDiagnostics.Observe(worth, Money.GetCashBalance(), Money.GetOnlineBalance(), _sessionEarnings);
            _netWorthHud?.SetValue(worth, _sessionEarnings.Revenue, _sessionEarnings.Expenses,
                _sessionEarnings.Profit, _transactionHooksReady);
            _nextRefreshTime = Time.unscaledTime + Constants.Defaults.HudRefreshSeconds;
        }

        /**
         * Stops and disposes of the Net Worth HUD.
         */
        private void RemoveHud() {
            if (_sessionEarnings.IsActive)
                EarningsDiagnostics.Write($"SESSION STOP revenue={_sessionEarnings.Revenue:F2} expenses={_sessionEarnings.Expenses:F2} profit={_sessionEarnings.Profit:F2}");
            _sessionEarnings.Stop();
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
            // Preserve the saved key so existing diagnostic preferences still apply.
            _earningsLogsEntry = _preferencesCategory.CreateEntry(
                "FinanceDiagnostics", true, "Log Earnings Diagnostics",
                "Log transactions, exclusions, and changed net-worth snapshots to MelonLoader Latest.log.");
            _preferencesCategory.SaveToFile(false);
        }
    }
}
