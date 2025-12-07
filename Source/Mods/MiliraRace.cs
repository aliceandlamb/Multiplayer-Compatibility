using HarmonyLib;
using Multiplayer.API;
using Verse;

namespace Multiplayer.Compat
{
    /// <summary>
    /// Milira Race / AncotLibrary alternate-weapon switch (scatter / rapid fire)
    /// Prevents desync by syncing the weapon swap operation.
    /// </summary>
    [MpCompatFor("Ancot.MiliraRace")]
    public class MiliraRace
    {
        public MiliraRace(ModContentPack mod)
        {
            // Sync the underlying weapon-switch method
            MP.RegisterSyncMethod(
                AccessTools.DeclaredMethod("AncotLibrary.HediffComp_AlternateWeapon:EquipeFromStorage")
            );

            // Patch the gizmo so input triggers the synced call
            var harmony = new Harmony("Multiplayer.Compat.MiliraRace");
            harmony.Patch(
                AccessTools.Method("AncotLibrary.Gizmo_SwitchWeapon_Hediff:ProcessInput"),
                prefix: new HarmonyMethod(typeof(MiliraRace), nameof(PreProcessInput))
            );
        }

        // Replaces the gizmo's local behavior with a synced method call
        private static bool PreProcessInput(object __instance)
        {
            var compField = AccessTools.Field(__instance.GetType(), "comp");
            var comp = compField?.GetValue(__instance);

            if (MP.InInterface && comp != null)
            {
                MP.CallSyncMethod(comp, "EquipeFromStorage");
                return false; // Skip original ProcessInput execution
            }

            return true; // Singleplayer: allow vanilla behavior
        }
    }
}
