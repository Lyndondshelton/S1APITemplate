namespace S1APITemplate.Utils
{
    public static class Constants
    {
        public const string ModName = "S1APITemplate";
        public const string ModVersion = "1.0.5";
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
