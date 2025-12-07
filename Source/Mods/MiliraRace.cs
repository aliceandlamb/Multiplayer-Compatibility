using HarmonyLib;
using Multiplayer.API;
using Verse;

namespace Multiplayer.Compat
{
    /// Milira Race by Ancot
    ///
    /// Syncs Milira ability effects and jump/fly verbs so they work properly in MP.
    /// Format modeled very closely on AlphaMechs.cs.
    ///
    [MpCompatFor("Ancot.MiliraRace")]
    public class MiliraRace
    {
        public MiliraRace(ModContentPack mod)
        {
            // --- Alternate weapon swap (best-effort, like your working version) ---
            // This syncs the click of the weapon-switch gizmo. It does NOT guarantee
            // full determinism internally (the method itself is invasive), but it's
            // the same style as AlphaMechs and what you already had compiling.
            MP.RegisterSyncMethod(
                AccessTools.DeclaredMethod("AncotLibrary.HediffComp_AlternateWeapon:EquipeFromStorage")
            );

            // --- Ability effect Apply() methods (these ARE safely syncable) ---

            // Sickle sweep ability
            MP.RegisterSyncMethod(
                AccessTools.DeclaredMethod("Milira.CompAbilityEffect_Sickle:Apply")
            );

            // Excalibur / sword slash ability
            MP.RegisterSyncMethod(
                AccessTools.DeclaredMethod("Milira.CompAbilityEffect_Excalibur:Apply")
            );

            // Spear thrust / line attack
            MP.RegisterSyncMethod(
                AccessTools.DeclaredMethod("Milira.CompAbilityEffect_Spear:Apply")
            );

            // Lance charge (if implemented via CompAbilityEffect)
            MP.RegisterSyncMethod(
                AccessTools.DeclaredMethod("Milira.CompAbilityEffect_LanceCharge:Apply")
            );

            // Broad shield launcher ability
            MP.RegisterSyncMethod(
                AccessTools.DeclaredMethod("Milira.CompAbilityEffect_LaunchBroadShieldUnit:Apply")
            );

            // --- Jump / fly / dash verbs (TryCastShot is a classic sync target) ---

            // Generic short fly ability
            MP.RegisterSyncMethod(
                AccessTools.DeclaredMethod("Milira.Verb_CastAbilityMiliraFly:TryCastShot")
            );

            // Lance fly/charge
            MP.RegisterSyncMethod(
                AccessTools.DeclaredMethod("Milira.Verb_CastAbilityMiliraFly_Lance:TryCastShot")
            );

            // Rook crash
            MP.RegisterSyncMethod(
                AccessTools.DeclaredMethod("Milira.Verb_CastAbilityMiliraFly_Rook:TryCastShot")
            );

            // Knight charge / guard jump
            MP.RegisterSyncMethod(
                AccessTools.DeclaredMethod("Milira.Verb_CastAbilityMiliraFly_KnightCharge:TryCastShot")
            );

            // Plain Milira jump / fly verbs, if present in the DLL
            MP.RegisterSyncMethod(
                AccessTools.DeclaredMethod("Milira.Verb_CastMiliraFly:TryCastShot")
            );
            MP.RegisterSyncMethod(
                AccessTools.DeclaredMethod("Milira.Verb_MiliraJump:TryCastShot")
            );
        }
    }
}
