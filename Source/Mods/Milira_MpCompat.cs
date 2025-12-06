// Milira_MpCompat.cs
// Broad, conservative Multiplayer compatibility patch for Milira.dll
// Place in: Multiplayer-Compatibility/Source/Mods/
// Adjust the [MpCompatFor] attribute if your MPCompat uses a different registration style.

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
        // Attempt to locate the multiplayer client-cancel prefix used across MPCompat patches.
        var cancelPrefixes = FindCancelPrefixes();

        // If none found, we'll still try to patch but no-op — better than crashing.
        if (cancelPrefixes.Count == 0)
        {
            // keep going: many MP forks have different names. The patcher will attempt to find types/methods regardless.
        }

        // List of Milira namespace/type name hints gathered from the assembly scan.
        // We will target types that contain "Milira" or "Milian" or the 'Milira' namespace prefix.
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

        // Iterate all loaded assemblies, find matching types, patch common methods.
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var asm in assemblies)
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch { continue; }

            foreach (var t in types)
            {
                if (t == null || t.FullName == null) continue;
                // only target types that match the mod's namespace / name hints
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

                // Patch nested compiler-generated iterator types for GenerateThings/CompGetGizmosExtra etc.
                PatchCompilerGeneratedIterators(harmony, asm, t, cancelPrefixes);

                // Additionally scan the type for fields of type Action/Func that are likely used as gizmo callbacks.
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

            // try to find a Prefix method (common signatures: Prefix(), Prefix(object __instance))
            var prefix = AccessTools.Method(t, "Prefix");
            if (prefix != null) result.Add(prefix);

            var prefixWithObj = AccessTools.Method(t, "Prefix", new Type[] { typeof(object) });
            if (prefixWithObj != null && !result.Contains(prefixWithObj)) result.Add(prefixWithObj);
        }

        // fallback: search all assemblies for a type named CancelSyncDuringPawnGeneration (less strict)
        if (result.Count == 0)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch { continue; }
                foreach (var tt in types)
                {
                    if (tt == null) continue;
                    if (!string.Equals(tt.Name, "CancelSyncDuringPawnGeneration", StringComparison.OrdinalIgnoreCase) &&
                        !tt.Name.ToLower().Contains("cancelsync")) continue;

                    var p = tt.GetMethod("Prefix", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (p != null) result.Add(p);
                }
            }
        }

        return result;
    }

    static void TryPatchMethodByName(Harmony harmony, Type t, string methodName, List<MethodInfo> cancelPrefixes)
    {
        var methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        foreach (var m in methods)
        {
            if (m.Name != methodName) continue;
            PatchWithAnyPrefix(harmony, m, cancelPrefixes);
        }
    }

    static void PatchWithAnyPrefix(Harmony harmony, MethodBase target, List<MethodInfo> cancelPrefixes)
    {
        if (target == null) return;

        // If we found one or more cancel prefix methods, try to use them (prefer the parameterless one).
        if (cancelPrefixes != null && cancelPrefixes.Count > 0)
        {
            MethodInfo chosen = cancelPrefixes.FirstOrDefault(mi => mi.GetParameters().Length == 0) ?? cancelPrefixes[0];
            try
            {
                harmony.Patch(target, prefix: new HarmonyMethod(chosen));
                return;
            }
            catch
            {
                // swallow and try other prefixes
                foreach (var p in cancelPrefixes)
                {
                    try
                    {
                        harmony.Patch(target, prefix: new HarmonyMethod(p));
                        return;
                    }
                    catch { }
                }
            }
        }

        // If no cancel prefix available, try to at least patch with a very conservative prefix found in Multiplayer namespace:
        var fallbackType = AccessTools.TypeByName("Multiplayer.Client");
        if (fallbackType != null)
        {
            var fallbackPrefix = AccessTools.Method(fallbackType, "CancelPrefix");
            if (fallbackPrefix != null)
            {
                try { harmony.Patch(target, prefix: new HarmonyMethod(fallbackPrefix)); return; }
                catch { }
            }
        }

        // If nothing found, do not throw — compatibility file should be resilient.
    }

    static void PatchCompilerGeneratedIterators(Harmony harmony, Assembly asm, Type parentType, List<MethodInfo> cancelPrefixes)
    {
        // Compiler generated iterator types have names like: ParentType+<CompGetGizmosExtra>d__10
        // We'll search all types in the same assembly for nested iterator types referring to our parent.
        Type[] asmTypes;
        try { asmTypes = asm.GetTypes(); } catch { return; }

        foreach (var nt in asmTypes)
        {
            if (nt == null || nt.FullName == null) continue;
            if (!nt.FullName.StartsWith(parentType.FullName + "+<", StringComparison.Ordinal)) continue;

            // find MoveNext, which executes the iterator body
            var moveNext = AccessTools.Method(nt, "MoveNext");
            if (moveNext != null)
                PatchWithAnyPrefix(harmony, moveNext, cancelPrefixes);

            // Many gizmo lambdas are compiled into methods on the iterator type (e.g., <CompGetGizmosExtra>b__10_0)
            var nestedMethods = nt.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var m in nestedMethods)
            {
                // lambda methods usually have 'b__' in name
                if (m.Name.Contains("b__") || m.Name.Contains("<") || m.Name.Contains("Invoke"))
                    PatchWithAnyPrefix(harmony, m, cancelPrefixes);
            }
        }
    }

    static void PatchClosureMethods(Harmony harmony, Type t, List<MethodInfo> cancelPrefixes)
    {
        // Patch any method on the type returning Action or containing "action" fields that are likely used as gizmo callbacks.
        var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        foreach (var f in fields)
        {
            var fType = f.FieldType;
            if (fType == null) continue;
            if (fType == typeof(Action) || (fType.IsGenericType && fType.GetGenericTypeDefinition() == typeof(Action<>)))
            {
                // Try to find a method on the declaring type that may be the target of that delegate.
                var methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var m in methods)
                {
                    // Heuristic: method referenced by delegate will often be non-generic and not a property accessor
                    if (m.IsSpecialName) continue;
                    if (m.Name.IndexOf("On", StringComparison.OrdinalIgnoreCase) >= 0
                        || m.Name.IndexOf("Do", StringComparison.OrdinalIgnoreCase) >= 0
                        || m.Name.IndexOf("Try", StringComparison.OrdinalIgnoreCase) >= 0
                        || m.Name.IndexOf("Start", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        PatchWithAnyPrefix(harmony, m, cancelPrefixes);
                    }
                }
            }
        }
    }
}
