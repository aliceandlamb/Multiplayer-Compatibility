using System;
using System.Reflection;
using HarmonyLib;
using Multiplayer.API;
using RimWorld;
using Verse;

namespace Multiplayer.Compat;

/// <summary>Adaptive Storage Framework by Soul, Phaneron, Bradson</summary>
/// <see href="https://github.com/bbradson/Adaptive-Storage-Framework"/>
/// <see href="https://steamcommunity.com/workshop/filedetails/?id=3033901359"/>
[MpCompatFor("adaptive.storage.framework")]
public class AdaptiveStorageFramework
{
    #region Fields

    private static FastInvokeHandler thingClassAnyFreeSlotsMethod;

    #endregion

    #region Main patch

    public AdaptiveStorageFramework(ModContentPack mod)
    {
        LongEventHandler.ExecuteWhenFinished(LatePatch);
    }

    private static void LatePatch()
    {
        MpCompatPatchLoader.LoadPatch<AdaptiveStorageFramework>();

        // Sync dropping items from the ASF contents ITab
        MP.RegisterSyncMethod(
                AccessTools.DeclaredMethod("AdaptiveStorage.ContentsITab:OnDropThing"))
            .SetContext(SyncContext.MapSelected)
            .CancelIfAnyArgNull()
            .CancelIfNoSelectedMapObjects();

        var type = AccessTools.TypeByName("AdaptiveStorage.ThingClass");
        if (type == null)
        {
            Log.Warning("[MPCompat] ASF: Could not find AdaptiveStorage.ThingClass, skipping extra patches.");
            return;
        }

        // Cache ThingClass.AnyFreeSlots property getter for CancelExecutionIfFull
        var anyFreeSlotsGetter = AccessTools.DeclaredPropertyGetter(type, "AnyFreeSlots");
        if (anyFreeSlotsGetter != null)
        {
            thingClassAnyFreeSlotsMethod = MethodInvoker.GetHandler(anyFreeSlotsGetter);
        }
        else
        {
            Log.Warning("[MPCompat] ASF: Could not find AnyFreeSlots property on ThingClass.");
        }

        // Try to patch the dev-mode GodMode gizmo that adds random stacks
        var inner = AccessTools.Inner(type, "GodModeGizmos");
        if (inner == null)
        {
            Log.Warning("[MPCompat] ASF: Inner type ThingClass.GodModeGizmos not found, skipping dev gizmo sync.");
            return;
        }

        MethodInfo method = null;

        try
        {
            // The constructor most likely has a single ThingClass argument.
            // If ASF changes, this may no longer be true; we guard with try/catch.
            method = MpMethodUtil.GetLambda(inner, null, MethodType.Constructor, new[] { type }, 0);
        }
        catch (Exception e)
        {
            Log.Warning("[MPCompat] ASF: Failed to get GodModeGizmos ctor lambda for dev gizmo sync. " +
                        "Skipping this dev-only patch.\n" + e);
        }

        if (method == null)
        {
            // Everything else (OnDropThing etc.) is still synced even if we skip this.
            return;
        }

        MP.RegisterSyncDelegate(inner, method.DeclaringType!.Name, method.Name, null)
            .SetDebugOnly();

   
