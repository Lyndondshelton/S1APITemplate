# Calculator phone app

Adds a portrait Calculator app to the Schedule I phone through S1API.Forked 3.1.4.
S1API discovers the public `CalculatorApp : PhoneApp` class automatically and calls
`OnCreatedUI` when the phone home screen is created. Do not manually register or
instantiate a second copy in Core.cs.

## Controls

Use the on-screen buttons: 0–9, +, -, *, /, parentheses, decimal point, and =.
C clears the calculation; DEL removes the last character. There is no editable
text field, keyboard expression input, or clipboard paste.

Normal precedence applies: `35 * 8` is 280; `(120 * 10) + (40 * 7) - (3 * (10 + 7))`
is 1429. Negative numbers and decimal fractions are supported. Results use a dot
as the decimal separator regardless of system culture.

After =, a digit starts a new calculation; an operator continues from the result.
Repeated = leaves the result unchanged. Errors retain the expression for correction
with DEL or C. Input is limited to 256 characters. Results use double precision,
so decimal arithmetic may contain normal floating-point rounding.

The app owns only its own UI and calculation state. It does not change game money,
other phone apps, or save data. State survives closing/reopening the phone within
the same scene and resets when the phone UI is recreated on a new load.

## Implementation

- `CalculatorApp.cs`: phone screen, button callbacks, and icon lifecycle.
- `CalculatorEngine.cs`: arithmetic tokenization, RPN conversion, and evaluation.
- `CalculatorSession.cs`: button input, errors, clear/delete, and result chaining.

`Assets/CalcAppLogo.png` is embedded in the DLL. S1API TextureUtils loads it using
the fixed resource name `CalculatorPhoneApp.CalcAppLogo.png`; no separate icon file
is required in Mods. Native app closing and home navigation are handled by S1API.

## Validation

Run the standalone engine and button-state checks without game assemblies:

```sh
dotnet run --project Tests/CalculatorChecks
```

To build the mod, configure local Unity/game references in `local.build.props`
using the repository setup instructions. Build with deployment disabled first:

```sh
dotnet build S1APITemplate.csproj -c CrossCompat -p:AutomateLocalDeployment=false
```

Both Mono and IL2CPP are intended targets. Validate each runtime in game: load a
save, confirm exactly one calculator icon, open the app, test the examples and
error correction, close/reopen the phone, use home/back, open another app, then
return to menu and reload a save. Confirm the layout fits the phone, callbacks
still work, no duplicate app appears, and the log contains no app errors.

No game assemblies, decompiled game sources, or game assets are included.
