using System;

namespace S1APITemplate.Finance
{
    /// <summary>Resolves exact game type names without enumerating unrelated Unity types.</summary>
    internal static class GameTypeLookup
    {
        public static Type Find(string suffix)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            // Prefer the IL2CPP wrapper when available, just as the original resolver did.
            foreach (string prefix in new[] { "Il2CppScheduleOne.", "ScheduleOne." })
            {
                foreach (var assembly in assemblies)
                {
                    // Assembly.GetType looks up this name only. Harmony's TypeByName
                    // fallback enumerates all types and encounters invalid Unity wrappers.
                    var type = assembly.GetType(prefix + suffix, throwOnError: false, ignoreCase: false);
                    if (type != null) return type;
                }
            }

            throw new MissingMemberException("Cannot find game type: " + suffix);
        }
    }
}
