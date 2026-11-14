# Net Worth GUI

Displays the game's `S1API.Money.Money.GetNetWorth()` result in a translucent,
non-interactive panel anchored 20 reference pixels from the top-left corner.
The mod reads the game value; it does not change money or add asset values.
The expanded panel also shows revenue, expenses, and profit since the save loaded.

## Session accounting

### Earnings diagnostics

The **Log Earnings Diagnostics** MelonLoader preference defaults to `true`.
Its saved key remains `FinanceDiagnostics` for compatibility with existing settings. Logs appear in
the MelonLoader console and the game's `MelonLoader/Latest.log`, tagged with a
session number and event sequence. Set the preference to `false` to disable them.
This is separate from the older `DebugLogs` preference.

- `CASH`/`BANK`: requested amount, actual before/after balances, delta, and transfer
  exclusion. Bank entries include the transaction name and quantity.
- `STORAGE_PAYMENT`: actual cash consumed for wages/supplier payments.
- `TOTALS`: revenue, expenses, and profit after the transaction.
- `SNAPSHOT`: the first HUD sample and changed samples only, with game net worth,
  cash, bank, net-worth delta, and session totals. `otherReportedValue` is simply
  net worth minus cash minus bank; it is a diagnostic remainder, not a measured
  inventory/property valuation. A change here can distinguish game asset
  revaluation from a cash receipt.
- Session start/stop markers document resets. Managed game callers are included
  where available; IL2CPP may not expose native callers in managed stack traces.

Logging uses the same net-worth result as the HUD and never calls the game's
net-worth calculation a second time. To investigate a sale, retain the surrounding
`CASH`, `BANK`, `TOTALS`, and `SNAPSHOT` lines from `Latest.log`.

- Revenue accumulates actual cash receipts and non-transfer bank credits.
  Inspected cash paths include customer sales, dealer collections, recycling,
  pawn-shop payouts, and cash pickups. Bank expenses include purchases and deliveries.
- Expenses accumulate actual cash/bank payments and cash consumed from employee
  homes or supplier stashes. Moving cash into storage is not itself an expense.
- Profit is revenue minus expenses: session cash flow, with purchases expensed when
  paid. It is not inventory-cost accounting or the change in total asset value.
- ATM deposits/withdrawals and laundering principal are excluded. Cash suppression
  covers each ATM coroutine step and laundering confirmation; bank transfer labels
  are filtered at the received balance mutation, including delayed receipts.
- A new save load resets all session totals. Loading existing money does not count.
  Bank receipts arriving after the session starts belong to that session, except
  known transfers. The totals are not saved to disk.

`Earnings/SessionEarningsTracker.cs` owns accounting; `Earnings/TransactionHooks.cs`
resolves game methods by runtime name and installs Harmony prefix/postfix hooks.
It hooks the received bank transaction logic once, avoiding counting both the
request and receipt. Transfer context is restored by finalizers even on exceptions.
No game types appear in the shared mod's compile-time transaction signatures.
If a required hook cannot be installed, session rows show `Unavailable` and the
MelonLoader log explains the failure; the mod does not substitute balance estimates.

Coverage is based on locally inspected Mono methods and IL2CPP wrapper signatures.
Custom mods that bypass these methods are not automatically covered. Cash pickups
are treated as receipts; the game path does not identify whether a pickup was
previously owned. Multiplayer combines local cash events with observed shared bank
events; storage payments are observed on the process executing them. Do not treat
this as a synchronized team ledger. Runtime testing on both backends remains required.

Run the standalone accounting checks with a .NET 9 SDK:

```powershell
dotnet run --project Tests/SessionEarningsChecks
```

To reproduce IL2CPP host formatting restrictions, run:

```powershell
dotnet run --project Tests/SessionEarningsChecks -p:InvariantGlobalization=true -- --invariant
```

HUD formatting uses invariant culture, so it does not require `en-US` culture data.
Game type lookup requests exact names from loaded assemblies instead of enumerating
unrelated Unity types, avoiding the type-loading warnings seen during broad searches.

In-game validation: load a save (session totals zero), earn money from a sale and
recycling, spend cash, make an online purchase, deposit/withdraw at an ATM, move
cash to/from storage, collect dealer proceeds, pay wages, launder cash, and reload.
Transfers must leave revenue/expenses unchanged; profit must equal their difference.

## HUD architecture

- `UI/NetWorthHud.cs` owns a screen-space canvas, panel, and formatted dollar rows.
  Its `Show`, `SetValue`, and `Dispose` methods contain presentation logic only.
- `Core.cs` creates the HUD on `GameLifecycle.OnLoadComplete`, supplies the initial
  value, and refreshes it once per second from `OnUpdate` using unscaled time.
  It removes the HUD before scene changes, new loads, and application exit.
- `Utils/Constants.cs` owns mod metadata and the refresh interval.

The display has no input handlers and does not capture mouse clicks. Values are
reconstructed from each loaded save; the mod does not persist additional data.
HUD failures are reported to MelonLoader and disable the current HUD to avoid
repeating the error every frame.

## Building this installation

Use `CrossCompat` for the intended shared Mono/IL2CPP release DLL. The local
`MonoAssembliesPath` in the ignored `local.build.props` points at a preserved copy
of the alternate branch's `Schedule I_Data/Managed` folder under
`.local-references/Mono/Managed`. This lets compilation continue when Steam is
switched back to IL2CPP. S1API comes from the existing NuGet reference for this
configuration. A fresh checkout must supply its own managed reference files.

```powershell
dotnet build .\S1APITemplate.sln -c CrossCompat -p:AutomateLocalDeployment=false
```

Use a recent .NET SDK (this machine also has one at
`$env:USERPROFILE\.dotnet\dotnet.exe`). With the game closed, copy
`bin/CrossCompat/netstandard2.1/S1API_NetWorthGUI_v1.0.5.dll` into the game's `Mods` folder.
Remove any previous `S1APITemplate_Il2cpp.dll` from `Mods` when switching to this build,
so MelonLoader does not load both versions. Launch the game normally, load a save,
and check the panel, a money change, and returning to the menu/reloading a save.

Both runtimes are intended targets; a successful compilation does not establish
in-game compatibility. Test this DLL on both Mono and IL2CPP before release.
Players need MelonLoader and their runtime's S1API installation, not your local
reference folder. References have `Private=false`; the local folder is excluded
from Git, SDK items, and template generation. Keep game assemblies and generated
wrappers out of commits and release archives. Refresh the local copy when game
updates require newer references.

## Original template reference

A beginner-friendly Schedule I mod template for [S1API.Forked](https://www.nuget.org/packages/S1API.Forked), MelonLoader, and Harmony. The project currently pins S1API.Forked 3.1.4.

The default path is `CrossCompat`: one mod assembly that stays on S1API/MelonLoader abstractions and avoids direct `ScheduleOne` or `Il2CppScheduleOne` game types. Use `Mono` or `Il2cpp` only when your mod intentionally needs runtime-specific game assemblies.

## Quick Start

1. Clone this repository, or install it as a local .NET template and create a project:

```powershell
dotnet new install .
dotnet new s1api -n MyScheduleOneMod
Set-Location .\MyScheduleOneMod
```

2. Run the setup script once from the generated project:

```powershell
.\setup.ps1
```

3. Build the recommended starter configuration:

```powershell
dotnet build .\S1APITemplate.sln -c CrossCompat
```

The script writes `local.build.props` with your local Schedule I install paths. That file is ignored by git.

If detection picks the wrong installs, pass paths explicitly:

```powershell
.\setup.ps1 -MonoPath "<path-to-mono-install>" -Il2CppPath "<path-to-il2cpp-install>" -Force
```

To copy successful builds into the target `Mods` folder, generate props with deployment enabled:

```powershell
.\setup.ps1 -EnableDeployment -Force
```

## Build Configurations

`CrossCompat` is the recommended default for S1API mods.

```powershell
dotnet build .\S1APITemplate.sln -c CrossCompat
```

`Mono` targets the Mono/alternate branch and includes direct Mono game assembly references.

```powershell
dotnet build .\S1APITemplate.sln -c Mono
```

`Il2cpp` targets the IL2CPP/default branch and includes generated Il2Cpp assembly references.

```powershell
dotnet build .\S1APITemplate.sln -c Il2cpp
```

Players still need MelonLoader and S1API installed in the game at runtime.

## Developing Against a Local S1API Checkout

The default package reference is recommended for released mods. To compile against a sibling S1API checkout, build the matching S1API target and enable the local reference in `local.build.props`:

```powershell
dotnet build ..\S1API\S1API.sln -c MonoMelon -p:AutomateLocalDeployment=false
dotnet build ..\S1API\S1API.sln -c Il2CppMelon -p:AutomateLocalDeployment=false
```

```xml
<UseLocalS1APIForked>true</UseLocalS1APIForked>
<LocalS1APIRoot>..\S1API</LocalS1APIRoot>
```

`CrossCompat` and `Mono` consume the local `MonoMelon` build; `Il2cpp` consumes the local `Il2CppMelon` build. Override `LocalS1APIForkedPath` only for a non-standard output layout.

## Included References

The project includes the common references that S1API mods usually need so new modders do not have to add Unity assemblies manually:

- `S1API.Forked`, `LavaGang.MelonLoader`, and `HarmonyX` from NuGet for compile-time access.
- `Newtonsoft.Json`, TextMeshPro, and common `UnityEngine.*` modules from the selected game install when `local.build.props` is configured.
- `Assembly-CSharp` and `Assembly-CSharp-firstpass` for `Mono` and `Il2cpp` only.
- `Il2CppInterop.Runtime` for `Il2cpp`.

If your mod needs a less common Unity, FishNet, Steamworks, or Schedule One assembly, add it near the matching reference group in `S1APITemplate.csproj`.

## Project Layout

```text
S1APITemplate/
|-- .agents/
|   `-- skills/
|       |-- schedule-one-modding/
|       `-- schedule-one-custom-npcs/
|-- Core.cs
|-- Utils/
|   `-- Constants.cs
|-- example.build.props
|-- setup.ps1
|-- S1APITemplate.csproj
`-- S1APITemplate.sln
```

## Included Agent Skills

This template includes repo-local Codex/agent skills under `.agents/skills/`:

- `schedule-one-modding`: general Schedule I modding guidance for Mono, IL2CPP, CrossCompat, Harmony, S1API, MAPI, SteamNetworkLib, local game inspection, and packaging safety.
- `schedule-one-custom-npcs`: focused S1API custom NPC guidance for prefab configuration, appearance, schedules, dialogue, customer/dealer behavior, and save/load lifecycle.

They are development guidance only. They are not compiled into the mod DLL and should not include game assemblies, generated IL2CPP wrappers, decompiled dumps, AssetRipper exports, private logs, or local machine paths.

## Where To Put Code

- Use `GameLifecycle.OnPreLoad` for stable content definitions that save deserialization must resolve.
- Use `GameLifecycle.OnLoadComplete` for systems that need the loaded world, managers, or player state.
- Put IDs, version strings, config names, and log tags in `Utils/Constants.cs`.
- Keep CrossCompat code on S1API wrappers and public abstractions. If a file needs direct game types, guard it with `#if MONO` / `#if IL2CPP` or keep it out of `CrossCompat`.

## Useful Links

- [S1API GitHub](https://github.com/ifBars/S1API)
- [S1API.Forked on NuGet](https://www.nuget.org/packages/S1API.Forked)
- [S1API Documentation](https://ifbars.github.io/S1API-docs/)
