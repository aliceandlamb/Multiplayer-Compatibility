using HarmonyLib;
using Multiplayer.API;
using Verse;

namespace Multiplayer.Compat
{
    /// <summary>Alpha Mechs by Sarg Bjornson</summary>
    /// <see href="https://github.com/juanosarg/AlphaMechs"/>
    /// <see href="https://steamcommunity.com/sharedfiles/filedetails/?id=2973169158"/>
    [MpCompatFor("HenTaiLoliTeam.axolotl")]
    public class MoeLotlRace
    {
        public MoeLotlRace(ModContentPack mod)
        {
            // Gizmos
            MP.RegisterSyncMethod(AccessTools.DeclaredMethod("Axolotl.CompProperties_LotiQiRangedWeapon_ChangeProjectile"));
        }
    }
}
