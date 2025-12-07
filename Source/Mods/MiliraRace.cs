using HarmonyLib;
using Multiplayer.API;
using Verse;

namespace Multiplayer.Compat
{
    /// <summary>
    /// Milira Race / AncotLibrary compatibility.
    /// Fixes desync caused by alternate-weapon switching (scatter / rapid / alt).
    ///
    /// Gizmo: AncotLibrary.Gizmo_SwitchWeapon_Hediff
    /// Action target: AncotLibrary.HediffComp_AlternateWeapon.EquipeFromStorage
    /// </summary>
    [MpCompatFor("Ancot.MiliraRace")]
    public class MiliraRace
    {
        public MiliraRace(ModContentPack mod)
        {
            // --- Register sync method ---
            // The underlying method that swaps weapons (and therefore weapon modes)
            // MUST be fully deterministic and executed on all clients.
            //
            // Equivalent to:
            // MP.RegisterSyncMethod(typeof(HediffComp_AlternateWeapon), "EquipeFromStorage");
            //
            MP.RegisterSyncMethod(
                AccessTools.DeclaredMethod("AncotLibrary.HediffComp_AlternateWeapon:EquipeFromStorage")
            );

            // --- Patch gizmo click handler ---
            //
            // EXACTLY like AlphaMechs patches a Command_Action via ProcessInput.
            //
            // When the user clicks the fire-mode toggle gizmo (Gizmo_SwitchWeapon_Hediff),
            // instead of executing its local effect we call the synced method.
            //
            Harmony harmony = new Harmony("Multiplayer.Compat.MiliraRace");
            harmony.Patch(
                AccessTools.Method("AncotLibrary.Gizmo_SwitchWeapon_Hediff:ProcessInput"),
                prefix: new HarmonyMethod(typeof(MiliraRace), nameof(Pre_GizmoInput))
            );
        }

        /// <summary>
        /// Re-route the gizmo click to the synced method.
        /// EXACT PATTERN used in AlphaMechs (ProcessInput prefix returning false).
        /// </summary>
        private static bool Pre_GizmoInput(object __instance)
        {
            // Get the HediffComp_AlternateWeapon reference out of the Gizmo.
            var compField = AccessTools.Field(__instance.GetType(), "comp");
            var comp = compField?.GetValue(__instance);

            if (comp != null)
            {
                // Call the synced weapon-swap operation.
                // This reproduces the AlphaMechs pattern: MP.CallSyncMethod(...)
                MP.CallSyncMethod(comp, "EquipeFromStorage");
                return false; // skip original (non-deterministic) ProcessInput
            }

            // Fallback: allow vanilla behavior (though this should never hit)
            return true;
        }
    }
}
