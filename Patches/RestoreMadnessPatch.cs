using HarmonyLib;
using UnityEngine;

namespace ModBlocks.Patches
{
    [HarmonyPatch(typeof(Steamworks.SteamFriends), "GetPersonaName")]
    static class RestoreMadnessPatch
    {
        static void Postfix(ref string __result)
        {
            float dice = Random.Range(0, 6);

            if (__result == "MAD MAN")
            {
                if (dice == 3f)
                {
                    Application.Quit();
                }
                else if (dice < 3)
                {
                    __result = "HAPPY MAN";
                }
            }
        }
    }
}