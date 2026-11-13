using System.Globalization;

namespace S1APITemplate.UI
{
    /// <summary>Formats HUD dollars without requiring installed culture data in the game host.</summary>
    internal static class MoneyDisplayFormatter
    {
        // IL2CPP's managed host can enable globalization-invariant mode. Named cultures
        // such as en-US throw there; invariant N2 keeps the same separators safely.
        public static string Format(float amount) => "$" + amount.ToString("N2", CultureInfo.InvariantCulture);
        public static string Format(decimal amount) => "$" + amount.ToString("N2", CultureInfo.InvariantCulture);
    }
}
