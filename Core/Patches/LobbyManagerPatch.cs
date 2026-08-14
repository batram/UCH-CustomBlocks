using HarmonyLib;

namespace CustomBlocks.Core.Patches
{
    // Connect() is where the game registers every message handler (server
    // relay + client receive); the mod's channel joins right behind them.
    [HarmonyPatch(typeof(LobbyManager), nameof(LobbyManager.Connect))]
    static class LobbyManagerConnectPatch
    {
        static void Postfix(LobbyManager __instance)
        {
            CustomBlockNet.RegisterHandlers(__instance.client);
        }
    }
}
