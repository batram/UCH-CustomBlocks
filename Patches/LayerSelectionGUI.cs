using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModBlocks.Patches
{
    [HarmonyPatch(typeof(FreeplayFullMessage), nameof(FreeplayFullMessage.Awake))]
    static class FreeplayFullMessagePatch
    {
        static void Prefix(FreeplayFullMessage __instance)
        {
            Debug.Log("FreeplayFullMessage 12 awoken: " + __instance.name);
            DefaultControls.Resources uiResources = new DefaultControls.Resources();

            List<string> m_DropOptions = new List<string> {};

            for (int i = 0; i < SortingLayer.layers.Length; i++)
            {
                var s = SortingLayer.layers[i];
                m_DropOptions.Add(s.name);
            }


            var canvas_go = new GameObject();
            canvas_go.name = "HighlightCanvas";
            Canvas can = canvas_go.AddComponent<Canvas>();
            canvas_go.transform.SetParent(__instance.transform, false);

            var mgo = GameObject.Find("Fullness Message/Container/Message");
            canvas_go.transform.position = mgo.transform.position - new Vector3(100f, 150f);


            can.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas_go.AddComponent<CanvasScaler>();
            canvas_go.AddComponent<GraphicRaycaster>();

            

            GameObject highlight_toggle_go = DefaultControls.CreateToggle(uiResources);
            highlight_toggle_go.AddComponent<LayoutElement>();
            highlight_toggle_go.transform.SetParent(canvas_go.transform, false);
            highlight_toggle_go.name = "HighlightToggle";
            Toggle highlight_toggle = highlight_toggle_go.GetComponent<Toggle>();
            highlight_toggle.transform.localPosition = new Vector2(180f, 0f);

            Text text = highlight_toggle.GetComponentInChildren<Text>();
            text.text = "Highlight";
            text.fontSize = 16;
            highlight_toggle.isOn = ModBlocksMod.highlightSelectedLayer;

            highlight_toggle.onValueChanged.AddListener((value) => {  
                text.text = "Highlight " + value;
                LayerSelectionGUI.NotifyChanged("Highlight Layer", ModBlocksMod.highlightSelectedLayer);
                ModBlocksMod.highlightSelectedLayer = highlight_toggle.isOn;
                PlaceableHighlighter.UpdateAll();
            });
     
            GameObject layer_dropdown_go = DefaultControls.CreateDropdown(uiResources);
            layer_dropdown_go.name = "LayerDropdown";
            layer_dropdown_go.transform.SetParent(canvas_go.transform, false);

            var layer_dropdown = layer_dropdown_go.GetComponent<Dropdown>();
            layer_dropdown.ClearOptions();
            layer_dropdown.AddOptions(m_DropOptions);
            layer_dropdown.Select();
            layer_dropdown.value = ModBlocksMod.selectedLayer;

            layer_dropdown.onValueChanged.AddListener((value) => {
                ModBlocksMod.selectedLayer = value;
                UserMessageManager.Instance.UserMessage("Layer selected: " + SortingLayer.layers[ModBlocksMod.selectedLayer].name.PadLeft(20, ' '));
                LayerSelectionGUI.UpdatePicked();
                PlaceableHighlighter.UpdateAll();
            });
        }
    }

    [HarmonyPatch(typeof(FreeplayFullMessage), nameof(FreeplayFullMessage.handleEvent))]
    static class FreeplayFullMessageHandleEventPatch
    {
        static void Prefix(GameEvent.GameEvent e)
        {
            var HighlightCanvas = GameObject.Find("HighlightCanvas");

            if (e.GetType() == typeof(GameEvent.StartPhaseEvent))
            {
                if((e as GameEvent.StartPhaseEvent).Phase == GameControl.GamePhase.PLACE){
                    Debug.Log("FreeplayFullMessage handleEvent PLACE!");
                    HighlightCanvas.transform.GetChild(0)?.gameObject.SetActive(true);
                    HighlightCanvas.transform.GetChild(1)?.gameObject.SetActive(true);
                } else
                {
                    HighlightCanvas.transform.GetChild(0)?.gameObject.SetActive(false);
                    HighlightCanvas.transform.GetChild(1)?.gameObject.SetActive(false);
                }
            }
        }
    }

    static class LayerSelectionGUI
    {
        public static void NotifyChanged(string name, bool value)
        {
            UserMessageManager.Instance.UserMessage($"{name} {(value ? "Enabled" : "Disabled")}");
        }

        public static void UpdatePicked()
        {
            foreach (PiecePlacementCursor cur in Object.FindObjectsOfType<PiecePlacementCursor>())
            {
                if (cur.Piece)
                {
                    if (ModBlocksMod.enableModBlockMode)
                    {
                        ModBlock mbi = ModBlocksMod.EnableModBlock(cur.Piece.gameObject);
                        mbi.layer = SortingLayer.layers[ModBlocksMod.selectedLayer].name;
                    }
                    else
                    {
                        ModBlocksMod.DisableModBlock(cur.Piece.gameObject);
                    }
                }
            }
        }
    }
}