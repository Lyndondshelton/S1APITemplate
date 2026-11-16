/*---------------------------------------------------------
 * Update the namespace to match what is defined in Core.cs.
 * The ModName should also match the namespace.
 * --------------------------------------------------------
 */

namespace S1API_NetWorthGUI.Utils
{
    public static class Constants
    {
        public const string ModName = BuildInfo.ModName;
        // Generated from <Version> in the project file during each build.
        public const string ModVersion = BuildInfo.Version;
        public const string ModAuthor = "Lyndon Shelton";
        public const string ModDescription = "This mod adds a Net Worth HUD to the users screen for tracking total net worth and gameplay earnings.";

        public const string PreferencesCategory = ModName;

        public static class Defaults
        {
            public const bool DebugLogsEnabled = false;

            /// <summary>Refresh the HUD twice per second using Unity's unscaled time.</summary>
            public const float HudRefreshSeconds = 0.5f;
        }

        public static class Constraints
        {
            public const float MinConstraint = 0f;
            public const float MaxConstraint = 100f;
        }

        public static class Game
        {
            public const string Studio = "TVGS";
            public const string Name = "Schedule I";
        }
    }
}
