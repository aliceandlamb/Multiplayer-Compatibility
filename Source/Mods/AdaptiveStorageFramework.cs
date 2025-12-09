using System;
using HarmonyLib;
using Multiplayer.API;
using RimWorld;
using Verse;

namespace Multiplayer.Compat;

[MpCompatFor("adaptive.storage.framework")]
public class AdaptiveStorageFramework
{
    private static FastInvokeHandler thingClassAnyFreeSlotsMethod;

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

        // Resolve ThingClass
        var type = AccessTools.TypeByName("AdaptiveStorage.ThingClass");
        if (type == null)
        {
            Log.Warning("[MPCompat] ASF: ThingClass not found, skipping slot checks.");
            return;
        }

        var getter = AccessTools.DeclaredPropertyGetter(type, "AnyFreeSlots");
        if (getter != null)
            thingClassAnyFreeSlotsMethod = MethodInvoker.GetHandler(getter);
        else
            Log.Warning("[MPCompat] ASF: AnyFreeSlots getter missing.");
    }

    [MpCompatPrefix("AdaptiveStorage.ContentsITab", "OnDropThing")]
    private static bool CancelExecutionIfNotContained(ITab_ContentsBase tab, Thing thing, ref int count)
    {
        if (!tab.container.Contains(thing))
            return false;

        if (thing.stackCount < count)
            count = thing.stackCount;

        return true;
    }

    private static bool CancelExecutionIfFull(Building_Storage parent)
    {
        if (thingClassAnyFreeSlotsMethod == null)
            return true;

        return (bool)thingClassAnyFreeSlotsMethod(parent);
    }

    [MpCompatSyncWorker("AdaptiveStorage.ContentsITab", shouldConstruct = true)]
    private static void NoSync(SyncWorker sync, ref object obj)
    {
        // Don't sync, only construct
    }
}

