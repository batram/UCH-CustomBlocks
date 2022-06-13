using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ModBlocks
{
    [System.Serializable]
    public class ModBlock : MonoBehaviour
    {
        public static string nameTag = " [ModBlock]";
        public static string startMarker = " mbi::";
        public static string endMarker = "::mbi";
        public string layer;
        public float alpha = 1f;

        public Placeable placeable
        {
            get { return this.gameObject.GetComponent<Placeable>(); }
        }

        public PlaceableMetadata meta
        {
            get { return gameObject.GetComponent<PlaceableMetadata>(); }
        }


        void Awake()
        {
            Debug.Log("ModBlockInfo Awake: " + this.gameObject.name);
            if (meta.blockSerializeIndex < ModBlocksMod.magicBackgroundBlockNumber)
            {
                meta.blockSerializeIndex += ModBlocksMod.magicBackgroundBlockNumber;
            }

            ParseNameData();
            TagName();
            if (ModBlocksMod.InFreePlace())
            {
                PlaceableHighlighter.HighlightAlpha(placeable);
            } else
            {
                RestoreLayer();
                SetCollide(false);
            }
        }

        void OnDestroy()
        {
            Debug.Log("ModBlockInfo destroyed: " + this.gameObject.name);

            DeTagName();

            if (meta.blockSerializeIndex >= ModBlocksMod.magicBackgroundBlockNumber)
            {
                meta.blockSerializeIndex -= ModBlocksMod.magicBackgroundBlockNumber;
            }

            this.SetLayer("Default", false);
            this.SetCollide(true);

            PlaceableHighlighter.ResetAlpha(this.placeable);
        }


        public void PersistInGOName()
        {
            if (!this.gameObject.name.Contains(startMarker))
            {
                this.gameObject.name += startMarker + this.ToJsonString() + endMarker;
            }
        }

        public string ToJsonString()
        {
            return JsonUtility.ToJson(this);
        }

        public void ParseNameData()
        {
            try
            {
                if (this.gameObject.name.Contains(startMarker))
                {
                    Regex rx = new Regex(startMarker + "(?<mbi>.*)" + endMarker);
                    MatchCollection matches = rx.Matches(this.gameObject.name);
                    if (matches.Count != 0)
                    {
                        var match = matches[0];
                        var json_data = match.Groups["mbi"].Value;
                        JsonUtility.FromJsonOverwrite(json_data, this);
                        ClearNameData();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("ModBlockInfo parse failed: " + e);
            }
        }
        public void ClearNameData()
        {
            this.gameObject.name = Regex.Replace(this.gameObject.name, startMarker + ".*" + endMarker, ""); 
        }

        public void TagName()
        {
            if (!this.name.Contains(nameTag))
            {
                this.name += nameTag;
            }
        }
        public void DeTagName()
        {
            if (this.name.Contains(nameTag))
            {
                this.name = this.name.Replace(nameTag, "");
            }
        }

        public void SetLayer(string layer, bool nochange = true)
        {
            Placeable pla = this.gameObject.GetComponent<Placeable>();
            pla.dontChangeArtLayers = nochange;

            foreach (SpriteRenderer spren in pla.gameObject.GetComponents<SpriteRenderer>())
            {
                spren.sortingLayerName = layer;
            }
            foreach (SpriteRenderer spren in pla.gameObject.GetComponentsInChildren<SpriteRenderer>())
            {
                spren.sortingLayerName = layer;
            }
        }

        public void RestoreLayer()
        {
            Debug.Log("RestoreLayer: " + this.layer);
            this.SetLayer(this.layer);
        }


        public void SetCollide(bool active)
        {
            this.gameObject.transform.Find("SolidCollider")?.transform.gameObject.SetActive(active);
            this.gameObject.transform.Find("InnerHazard")?.transform.gameObject.SetActive(active);

            // L Boards Collider misspelled, still evil for ᵉˡᵉᵇᵃⁿᵗ
            if(GameState.GetInstance().currentSnapshotInfo.authorDisplayName != "ᵉˡᵉᵇᵃⁿᵗ")
            {
                this.gameObject.transform.Find("InnerHarzard")?.transform.gameObject.SetActive(active);
            } 
            else
            {
                UserMessageManager.Instance.UserMessage("ᵉˡᵉᵇᵃⁿᵗ sends his regards!");
            }
        }


    }
}