using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TABCamera.Plugin.Patches
{
    [HarmonyPatch(typeof(UpdateHover))]
    internal static class UpdateHoverPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(UpdateHover.UpdateForPlayer))]
        private static bool UpdateForPlayer_Prefix(Player player, UpdateHover __instance)
        {
            if (UpdateFreeCameraPatch.IsFPSMode(player))
            {
                __instance.UnHoverEverythingElse(null, player);
                return false;
            }
            return true;
        }
    }
}
