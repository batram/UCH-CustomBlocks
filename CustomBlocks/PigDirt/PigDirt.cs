using GameEvent;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace CustomBlocks.CustomBlocks
{
    class PigDirt : CustomBlock
    {
        public override int BasedId { get { return 30; } }
        public override string BasePlaceableName { get { return "Coin"; } }
        public override string BasePickableBlockName { get { return "Coin_Pick"; } }
        public override string Name { get { return GetType().Name; } }
        public new static int StaticId { get; set; }
        public override int CustomId
        {
            get { return StaticId; }
            set { StaticId = value; }
        }

        protected Sprite sp;
        new public Sprite sprite
        {
            get
            {
                if (sp == null)
                {
                    Texture2D texture = LoadTexture(Path.Combine(CustomBlock.ImageDir, this.Name + ".png"));
                    sp = Sprite.Create(texture, new Rect(0, 0, 54, 54), new Vector2(0, 0), 100f);
                }
                return sp;
            }
        }

        override public PickableBlock CreatePickableBlock()
        {
            PickableBlock pb = base.CreatePickableBlock();
            pb.transform.localPosition -= new Vector3(18f, 19.5f, 1);
            pb.transform.localScale = new Vector3(0.75f, 0.75f, 1);
            if (pb.GetComponentInChildren<Animator>())
            {
                pb.GetComponentInChildren<Animator>().enabled = false;
            }
            pb.GetComponentInChildren<SpriteRenderer>().sprite = sprite;

            return pb;
        }

        void OnTriggerEnter2D(Collider2D col)
        {
            if (placed)
            {
                Debug.Log(col.gameObject.name + " : " + gameObject.name + " : " + Time.time);

                Character c = col.GetComponentInParent<Character>();
                //TODO: smear coin to different players, with transfer cooldown
            }
        }

        override public Placeable CreatePlaceablePrefab()
        {
            Placeable placeable = base.CreatePlaceablePrefab();
            placeable.gameObject.AddComponent(GetType());

            //TODO: Custom animation
            placeable.GetComponentInChildren<Animator>().enabled = false;
            placeable.GetComponentInChildren<SpriteRenderer>().sprite = sprite;

            //TODO: spawn zombie flies

            return placeable;
        }

        override public void FixSprite(Transform sprite_holder)
        {
        }

        [HarmonyPatch(typeof(Character), nameof(Character.CmdFinishedWithCoin))]
        static class CmdFinishedWithCoinPatch
        {
            static bool Prefix(Character __instance, NetworkInstanceId netSurrogateId)
            {
                Coin c = Coin.GetCoinFromSurrogateID(netSurrogateId);
                Debug.Log("CmdFinishedWithCoinPatch Prefix" + c + " dirt? " + c?.GetComponent<PigDirt>());
                if (c != null && c?.GetComponent<PigDirt>() != null)
                {
                    Debug.Log("CmdFinishedWithCoinPatch Prefix Dirt indeed");

                    PointBlock pb = new PointBlock(PointBlock.pointBlockType.coin, __instance.networkNumber);
                    //ScoreKeeper.Instance.AwardPoint(pb, false);
                    //Hide coin => pigdirt type in suicide
                    MsgPointAwarded msgPointAwarded = new MsgPointAwarded();
                    msgPointAwarded.PlayerNumber = pb.playerNumber;
                    msgPointAwarded.PointType = PointBlock.pointBlockType.suicide;
                    msgPointAwarded.AlwaysAward = pb.AlwaysAward;
                    NetworkManager.singleton.client.Send(NetMsgTypes.PointAwarded, msgPointAwarded);

                    __instance.CallRpcFinishedWithCoin(netSurrogateId);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ScoreKeeper), nameof(ScoreKeeper.handleEvent))]
        static class ScoreKeeperhandleEventPatch
        {
            static bool Prefix(ScoreKeeper __instance, GameEvent.GameEvent e)
            {
                if (e.GetType() == typeof(NetworkMessageReceivedEvent))
                {
                    NetworkMessageReceivedEvent networkMessageReceivedEvent = e as NetworkMessageReceivedEvent;
                    if (networkMessageReceivedEvent.Message.msgType == NetMsgTypes.PointAwarded)
                    {
                        MsgPointAwarded msgPointAwarded = networkMessageReceivedEvent.ReadMessage as MsgPointAwarded;

                        if (msgPointAwarded.PointType == PointBlock.pointBlockType.suicide)
                        {
                            PointBlock pointBlock = new PointBlock(PointBlock.pointBlockType.coin, msgPointAwarded.PlayerNumber);
                            pointBlock.suicideValue = -1;
                            __instance.addPointBlock(pointBlock);
                            return false;
                        }
                    }
                }

                return true;
            }
        }


        [HarmonyPatch(typeof(GraphScoreBoard), nameof(GraphScoreBoard.GetPreinstantiatedPointBlock))]
        static class GraphScoreBoardGetPreinstantiatedPointBlockPatch
        {
            static void Postfix(GraphScoreBoard __instance, PointBlock pb, ref ScorePiece __result) { 
            
                if(pb.suicideValue == -1 && __result.pieceImage != null)
                {
                    __result.pieceImage.color = new Color(131/255f, 74/255f, 55/255f);
                    __result.text.color = __result.pieceImage.color;
                    __result.text.text = "Pig Dirt";
                }
            }
        }


        

        [HarmonyPatch(typeof(ScoreLine), "AddScorePointBlock")]
        static class ScoreLineAddScorePointBlockPatch
        {
            static void Postfix(ScoreLine __instance, ref PointBlock pb)
            {
                Debug.Log("AddScorePointBlock: " + pb.suicideValue);

                if (pb.suicideValue == -1)
                {
                    __instance.NextAttachPosition -= new Vector3((__instance.lastNewScorepiece.width * 2) * __instance.scoreBoardParent.SizeFactor, 0f, 0f);
                    __instance.lastNewScorepiece.transform.localPosition -= new Vector3((__instance.lastNewScorepiece.width) * __instance.scoreBoardParent.SizeFactor, 5f, 0f);
                }
            }
        }


        [HarmonyPatch(typeof(PointBlock), "get_pointValue")]
        static class PointBlockPatch
        {
            static void Postfix(PointBlock __instance, ref int __result)
            {
                if(__instance.suicideValue == -1)
                {
                    __result = __result * -1;
                }
                Debug.Log("get_pointValue: " + __result + " type " + __instance.type + " " + __instance.suicideValue);

            }
        }
    }
}