using System;
using S1APITemplate.Finance;
using S1APITemplate.UI;
var t = new SessionFinanceTracker();
// Receipts and spending must remain separate even inside one HUD update interval.
void Check(bool ok, string name) { if(!ok) throw new Exception(name); Console.WriteLine("PASS: " + name); }
t.Record(99); Check(t.Revenue == 0, "Loading/inactive money ignored");
t.Start(); t.Record(1000); t.Record(-600); Check(t.Revenue == 1000 && t.Expenses == 600 && t.Profit == 400, "Earnings and expenses between refreshes retained separately");
t.RecordBank("Cash Deposit", 1000); t.RecordBank("Cash Withdrawal", -500); t.RecordBank("Money laundering (Laundromat)", 800); Check(t.Revenue == 1000 && t.Expenses == 600, "ATM and laundering bank principal excluded");
t.RecordBank("Property purchase", -2000); Check(t.Expenses == 2600 && t.Profit == -1600, "Bank purchases count; losses remain negative");
t.Record(float.NaN); t.Record(float.PositiveInfinity); t.Record(float.MaxValue); Check(t.Expenses == 2600, "Invalid amounts ignored");
t.Stop(); t.Record(999); Check(t.Revenue == 1000, "Menu transactions ignored");
t.Start(); Check(t.Revenue == 0 && t.Expenses == 0 && t.Profit == 0, "New save resets session");
for(int i=0;i<10;i++) t.Record(0.1f); Check(t.Revenue == 1m, "Decimal accumulation avoids float drift");

// Run with -p:InvariantGlobalization=true and -- --invariant to reproduce the game host.
if (Array.IndexOf(args, "--invariant") >= 0)
{
    bool namedCultureUnavailable = false;
    try { System.Globalization.CultureInfo.GetCultureInfo("en-US"); }
    catch (System.Globalization.CultureNotFoundException) { namedCultureUnavailable = true; }
    Check(namedCultureUnavailable, "Regression process is globalization-invariant");
}
Check(MoneyDisplayFormatter.Format(12345.67f) == "$12,345.67", "Net worth formats without named cultures");
Check(MoneyDisplayFormatter.Format(9876.54m) == "$9,876.54", "Session amounts format without named cultures");
Check(MoneyDisplayFormatter.Format(0m) == "$0.00", "Zero displays cents");
Check(GameTypeLookup.Find("MonoLookupProbe") == typeof(ScheduleOne.MonoLookupProbe), "Mono type resolution");
Check(GameTypeLookup.Find("SharedLookupProbe") == typeof(Il2CppScheduleOne.SharedLookupProbe), "IL2CPP wrapper takes precedence");
bool missingTypeRejected = false;
try { GameTypeLookup.Find("MissingLookupProbe"); }
catch (MissingMemberException) { missingTypeRejected = true; }
Check(missingTypeRejected, "Missing game types fail explicitly");

var diagnosticLines = new System.Collections.Generic.List<string>();
FinanceDiagnostics.Configure(diagnosticLines.Add, () => true);
FinanceDiagnostics.BeginSession();
FinanceDiagnostics.Observe(2880f, 100f, 500f, t);
FinanceDiagnostics.Observe(2880f, 100f, 500f, t);
Check(diagnosticLines.Count == 2, "Unchanged snapshots are not spammed");
FinanceDiagnostics.Observe(3598f, 205f, 500f, t);
Check(diagnosticLines[2].Contains("netWorthDelta=718.00") && diagnosticLines[2].Contains("cashDelta=105.00") && diagnosticLines[2].Contains("otherReportedDelta=613.00"), "Diagnostics distinguish net-worth jumps from cash changes");
FinanceDiagnostics.Configure(diagnosticLines.Add, () => false);
FinanceDiagnostics.Observe(9999f, 0f, 0f, t);
Check(diagnosticLines.Count == 3, "Diagnostic preference disables output");

namespace ScheduleOne { internal sealed class MonoLookupProbe { } internal sealed class SharedLookupProbe { } }
namespace Il2CppScheduleOne { internal sealed class SharedLookupProbe { } }
