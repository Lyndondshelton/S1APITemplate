using System;
using System.Reflection;
using UnityEngine.Events;

namespace S1API_CalculatorPhoneApp.Utils;

/// <summary>Converts managed callbacks using the UnityAction type supplied by the running game.</summary>
internal static class UnityCallback
{
    private static readonly Func<Action, UnityAction> Convert = CreateConverter();

    public static UnityAction Create(Action callback) => Convert(callback);

    private static Func<Action, UnityAction> CreateConverter()
    {
        // CrossCompat is compiled against Mono, but UnityAction resolves to an IL2CPP
        // wrapper in an IL2CPP game. Never emit a direct UnityAction constructor call.
        if (typeof(Delegate).IsAssignableFrom(typeof(UnityAction)))
        {
            return callback => (UnityAction)(object)Delegate.CreateDelegate(
                typeof(UnityAction), callback.Target, callback.Method);
        }

        // Resolve interop only on IL2CPP so the shared DLL has no hard dependency
        // on Il2CppInterop.Runtime when loaded in a Mono game.
        var support = Type.GetType("Il2CppInterop.Runtime.DelegateSupport, Il2CppInterop.Runtime", true)!;
        foreach (var method in support.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (method.Name != "ConvertDelegate" || !method.IsGenericMethodDefinition ||
                method.GetGenericArguments().Length != 1)
                continue;

            var parameters = method.GetParameters();
            if (parameters.Length != 1 || parameters[0].ParameterType != typeof(Delegate))
                continue;

            var converter = method.MakeGenericMethod(typeof(UnityAction));
            return callback => (UnityAction)converter.Invoke(null, new object[] { callback })!;
        }

        throw new MissingMethodException(support.FullName, "ConvertDelegate<T>(Delegate)");
    }
}
