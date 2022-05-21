using HarmonyLib;
using UnityEngine;

namespace BackgroundBlocks.Patches
{
    [HarmonyPatch(typeof(GameState), nameof(GameState.Update))]
    static class GameStateUpdatePatch
    {
        static void Prefix(GameState __instance)
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                GameControl gameControl = Object.FindObjectOfType<GameControl>();
                if (GameSettings.GetInstance().GameMode == GameState.GameMode.FREEPLAY
                    && gameControl && gameControl.Phase == GameControl.GamePhase.PLACE)
                {
                    ToggleBackgroundMode();
                }
            }
        }

        static void ToggleBackgroundMode()
        {
            BackgroundBlocksMod.enableBackgroundMode = !BackgroundBlocksMod.enableBackgroundMode;

            NotifyChanged("BackgroundMode", BackgroundBlocksMod.enableBackgroundMode);

            foreach (PiecePlacementCursor cur in UnityEngine.Object.FindObjectsOfType<PiecePlacementCursor>())
            {
                if (cur.Piece)
                {
                    if (BackgroundBlocksMod.enableBackgroundMode)
                    {
                        BackgroundBlocksMod.EnableBackground(cur.Piece.gameObject, false, true);
                    }
                    else
                    {
                        BackgroundBlocksMod.DisableBackground(cur.Piece.gameObject);
                    }
                }
            }
        }

        static void NotifyChanged(string name, bool value)
        {
            UserMessageManager.Instance.UserMessage($"{name} {(value ? "Enabled" : "Disabled")}");
        }
    }
}