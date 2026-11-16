/*---------------------------------------------------------
 * Update the namespace to match what is defined in Core.cs.
 * The ModName should also match the namespace.
 * --------------------------------------------------------
 */

namespace S1APITemplate.Utils
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
