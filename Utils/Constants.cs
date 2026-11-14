/*---------------------------------------------------------
 * Update the namespace to match what is defined in Core.cs.
 * The ModName should also match the namespace.
 * --------------------------------------------------------
 */
namespace S1API_CalculatorPhoneApp.Utils
{
    public static class Constants
    {
        public const string ModName = "S1API_CalculatorPhoneApp";
        public const string ModVersion = "0.1.0";
        public const string ModAuthor = "Lyndon Shelton";
        public const string ModDescription = "Adds a calculator app to the in game phone.";

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
