using HarmonyLib;
using Multiplayer.API;
using Verse;

namespace Multiplayer.Compat
{
    /// <summary>
    /// Milira Race / AncotLibrary alternate weapon gizmo
    /// Syncs the "switch weapon" hediff gizmo so it no longer desyncs.
    /// </summary>
    [MpCompatFor("Ancot.MiliraRace")]
    public class MiliraRace
    {
        public MiliraRace(ModContentPack mod)
        {
            // HediffComp_AlternateWeapon.CompGetGizmos()
            // The switch-weapon button is implemented as a lambda inside this method.
            // We need to sync that lambda, not EquipeFromStorage directly.
            MP.RegisterLambdaMethod(
                "AncotLibrary.HediffComp_AlternateWeapon",
                "CompGetGizmos",
                0 // first (and only) gizmo lambda in this method
            );
        }
    }
}
