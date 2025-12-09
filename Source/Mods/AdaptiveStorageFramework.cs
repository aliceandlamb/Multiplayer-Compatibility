using System;
using System.Reflection;
using HarmonyLib;
using Multiplayer.API;
using RimWorld;
using Verse;

namespace Multiplayer.Compat;

/// <summary>
/// Adaptive Storage Framework by Soul, Phaneron, Bradson
/// MP Compatibility patch
/// </summary>
[MpCompatFor("adaptive.storage.framework")]
public class AdaptiveStorageFramework
{
    #region Fields

    private static FastInvokeHandler thingClassAnyFreeSlotsMethod;

    #endregion

    #region Init

    public AdaptiveStorageFramework(ModContentPack mod)
    {
        LongEventHandler.ExecuteWhenFinished(LatePatch);
    }

    private static void LatePatch()
    {
        MpCompatPatchLoader.LoadPatch<AdaptiveStorageFramework>();

        // Sync dropping items from ASF tab
        MP.RegisterSyncMethod(
                AccessTools.DeclaredMethod("AdaptiveStorage.ContentsITab:OnDropThing"))
            .SetContext(SyncContext.MapSelected)
            .CancelIfAnyArgNull()
            .CancelIfNoSelectedMapObjects();

        // Locate ThingClass
        var type = AccessTools.TypeByName("AdaptiveStorage.ThingClass");
        if (type == null)
        {
            Log.Warning("[MPCompat] ASF: Could not find AdaptiveStorage.ThingClass, skipping patches.");
            return;
        }

        // Resolve AnyFreeSlots getter
        var getter = AccessTools.DeclaredPropertyGetter(type, "AnyFreeSlots");
        if (getter != null)
            thingClassAnyFreeSlotsMethod = MethodInvoker.GetHandler(getter);
        else
            Log.Warning("[MPCompat] ASF: Missing AnyFreeSlots property.");

        // Locate inner GodModeGizmos type
        var inner = AccessTools.Inner(type, "GodModeGizmos");
        if (inner == null)
        {
            Log.Warning("[MPCompat] ASF: Missing GodModeGizmos inner class.");
            return;
        }

        MethodInfo lambdaMethod = null;

        try
        {
            lambdaMethod = MpMethodUtil.GetLambda(
                inner,
                null,
                MethodType.Constructor,
                new[] { type },
                0);
        }
        catch (Exception e)
        {
            Log.Warning("[MPCompat] ASF: Failed to get GodModeGizmos constructor lambda. Skipping dev sync.\n" + e);
        }

        // If the lambda is missing, skip patching
        if (lambdaMethod == null)
            return;

        // Register dev-mode gizmo sync (debug only)
        MP.RegisterSyncDelegate(inner, lambdaMethod.DeclaringType!.Name, lambdaMethod.Name, null)
            .SetDebugOnly();

        // Patch to prevent errors when storage is full
        MpCompat.harmony.Patch(
            lambdaMethod,
            prefix: new HarmonyMethod(typeof(AdaptiveStorageFramework), nameof(CancelExecutionIfFull)));
    }

    #endregion

    #region Harmony patches

    [MpCompatPrefix("AdaptiveStorage.ContentsITab", "OnDropThing")]
    private static bool CancelExecutionIfNotContained(ITab_ContentsBase __instance, Thing __0, ref int __1)
    {
        if (!__instance.container.Contains(__0))
            return false;

        if (__0.stackCount < __1)
            __1 = __0.stackCount;

        return true;
    }

    private static bool CancelExecutionIfFull(Building_Storage ___Parent)
    {
        if (thingClassAnyFreeSlotsMethod == null)
            return true;

        return (bool)thingClassAnyFreeSlotsMethod(___Parent);
    }

    #endregion

    #region Sync workers

    [MpCompatSyncWorker("AdaptiveStorage.ContentsITab", shouldConstruct = true)]
    private static void NoSync(SyncWorker sync, ref object obj)
    {
        // Only construct, do not sync
    }

    #endregion
}

   
