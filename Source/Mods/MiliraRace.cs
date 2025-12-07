using HarmonyLib;
using Multiplayer.API;
using Verse;

namespace Multiplayer.Compat
{
    [MpCompatFor("Ancot.MiliraRace")]
    public class MiliraRace
    {
        public MiliraRace(ModContentPack mod)
        {
            // Sync the weapon-swap method invoked by the gizmo
            MP.RegisterSyncMethod(
                AccessTools.DeclaredMethod("AncotLibrary.HediffComp_AlternateWeapon:EquipeFromStorage")
            );

            // Patch the gizmo click so it triggers the synced method
            var harmony = new Harmony("Multiplayer.Compat.MiliraRace");
            harmony.Patch(
                AccessTools.Method("AncotLibrary.Gizmo_SwitchWeapon_Hediff:ProcessInput"),
                prefix: new HarmonyMethod(typeof(MiliraRace), nameof(PreProcessInput))
            );
        }

        private static bool PreProcessInput(object __instance)
        {
            var compField = AccessTools.Field(__instance.GetType(), "comp");
            var comp = compField?.GetValue(__instance);
            if (comp is null)
                return true;

            MP.CallSyncMethod(comp, "EquipeFromStorage");
            return false; // skip original local execution
        }
    }
}
