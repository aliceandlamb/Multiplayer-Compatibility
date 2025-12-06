// Milira_MpCompat.cs
// Broad, conservative Multiplayer compatibility patch for Milira.dll
// Place in: Multiplayer-Compatibility/Source/Mods/
// If you get a compile error about 'MpCompatFor', add the correct 'using' for your MPCompat loader (commonly "Multiplayer" or "Multiplayer.Compat")


using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;


// Keep the attribute so the MPCompat loader can pick this up. If you get a compile error, see the note above.
[MpCompatFor("Milira")]
public static class Milira_MpCompat
{
public static void Patch(Harmony harmony)
{
if (harmony == null) return;


// Attempt to locate the multiplayer client-cancel prefixes used across MPCompat patches.
var cancelPrefixes = FindCancelPrefixes();


// List of Milira namespace/type name hints gathered from the assembly scan.
var typeNameHints = new[]
{
"Milira",
"Milian",
"Milira.Comp",
"Milira.WorldObject",
"Milira.CompSwitchResonate",
"Milira.CompThingContainer_Milian",
"Milira.CompFlightControl",
"Milira.WorldObjectCompMiliraSettlement",
"Milira.CompMilianGestateInfo",
"Milira.CompSpawner_MiliraFeather",
"Milira.CompMilianHairSwitch",
"Milira.CompDelayedPawnSpawnOnWakeup"
};


var assemblies = AppDomain.CurrentDomain.GetAssemblies();
foreach (var asm in assemblies)
{
Type[] types;
try { types = asm.GetTypes(); }
catch { continue; }


foreach (var t in types)
{
if (t == null || t.FullName == null) continue;
if (!typeNameHints.Any(h => t.FullName.IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0))
continue;


// Common UI / gizmo methods to patch
TryPatchMethodByName(harmony, t, "CompGetGizmosExtra", cancelPrefixes);
TryPatchMethodByName(harmony, t, "GetGizmos", cancelPrefixes);
TryPatchMethodByName(harmony, t, "GetCaravanGizmos", cancelPrefixes);
TryPatchMethodByName(harmony, t, "GetWornGizmosExtra", cancelPrefixes);
TryPatchMethodByName(harmony, t, "GetGizmosExtra", cancelPrefixes);
TryPatchMethodByName(harmony, t, "ProcessInput", cancelPrefixes);
TryPatchMethodByName(harmony, t, "OnClick", cancelPrefixes);
TryPatchMethodByName(harmony, t, "DoWindowContents", cancelPrefixes);
TryPatchMethodByName(harmony, t, "Tick", cancelPrefixes);
TryPatchMethodByName(harmony, t, "CompTick", cancelPrefixes);
TryPatchMethodByName(harmony, t, "PostExposeData", cancelPrefixes);
TryPatchMethodByName(harmony, t, "GeneratePawn", cancelPrefixes);
TryPatchMethodByName(harmony, t, "TrySpawn", cancelPrefixes);


// Patch compiler-generated iterators and closure methods
PatchCompilerGeneratedIterators(harmony, asm, t, cancelPrefixes);
PatchClosureMethods(harmony, t, cancelPrefixes);
}
}
}


static List<MethodInfo> FindCancelPrefixes()
{
var result = new List<MethodInfo>();


string[] candidateTypeNames =
{
"Multiplayer.Client.CancelSyncDuringPawnGeneration",
"Multiplayer.Client.CancelSyncDuringGUICall",
"Multiplayer.Client.CancelOnClient",
"Multiplayer.Client.CancelSync",
"Multiplayer.Client.CancelSyncDuringFrame",
"Multiplayer.Client.CancelSyncDuringMethod"
};


foreach (var typeName in candidateTypeNames)
{
var t = AccessTools.TypeByName(typeName);
if (t == null) continue;


var prefix = AccessTools.Method(t, "Prefix");
if (prefix != null && !result.Contains(prefix)) result.Add(prefix);


var prefixWithObj = AccessTools.Method(t, "Prefix", new Type[] { typeof(object) });
if (prefixWithObj != null && !result.Contains(prefixWithObj)) result.Add(prefixWithObj);
}


// fallback: search for any type that looks like a CancelSync* helper
if (result.Count == 0)
{
foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
{
Type[] types;
try { types = asm.GetTypes(); } catch { continue; }
foreach (var tt in types)
{
if (tt == null) continue;
