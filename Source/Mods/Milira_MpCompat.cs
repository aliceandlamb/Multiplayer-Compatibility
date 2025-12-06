// Milira_MpCompat.cs (repaired)
// Broad, conservative Multiplayer compatibility patch for Milira.dll


using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;


[MpCompatFor("Milira")]
public static class Milira_MpCompat
{
public static void Patch(Harmony harmony)
{
if (harmony == null)
return;


var cancelPrefixes = FindCancelPrefixes();


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


foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
{
Type[] types;
try { types = asm.GetTypes(); }
catch { continue; }


foreach (var t in types)
{
if (t?.FullName == null)
continue;


if (!typeNameHints.Any(h => t.FullName.IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0))
continue;


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


var p0 = AccessTools.Method(t, "Prefix");
if (p0 != null)
result.Add(p0);


var p1 = AccessTools.Method(t, "Prefix", new[] { typeof(object) });
