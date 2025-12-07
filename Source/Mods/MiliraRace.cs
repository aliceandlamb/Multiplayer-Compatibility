using HarmonyLib;
using Multiplayer.API;
using Verse;

namespace Multiplayer.Compat
{
    /// Milira Race by Ancot
    ///
    /// Syncs the alternate-weapon swap gizmo (scatter / rapid fire etc.)
    /// so it no longer causes desyncs.
    ///
    [MpCompatFor("Ancot.MiliraRace")]
    public class MiliraRace
    {
        public MiliraRace(ModContentPack mod)
        {
            // HediffComp_AlternateWeapon.EquipeFromStorage
            // This is what the Gizmo_SwitchWeapon_Hediff Command_Action calls.
            // Using string + AccessTools like AlphaMechs so we don't need a direct
            // reference to AncotLibrary.dll in the MPCompat project.
            MP.RegisterSyncMethod(
                AccessTools.DeclaredMethod("AncotLibrary.HediffComp_AlternateWeapon:EquipeFromStorage")
            );
        }
    }
}
