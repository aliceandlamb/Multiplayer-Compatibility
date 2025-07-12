using HarmonyLib;
using Multiplayer.API;
using Verse;

namespace Multiplayer.Compat
{
    /// <summary>MoeLotl Race by HenTaiLoliTeam </summary>
    /// <see href="https://steamcommunity.com/workshop/filedetails/?id=3292351432"/>
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
