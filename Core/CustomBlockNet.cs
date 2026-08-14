using UnityEngine;
using UnityEngine.Networking;

namespace CustomBlocks.Core
{
    // Mod-owned network message for custom-block effects. Vanilla has no
    // channel that carries "transmitter X fired at receiver Y", and its
    // relay (LobbyManager.readMessage) only knows vanilla message ids, so
    // this channel registers its own server relay and client dispatch.
    public class MsgCustomBlockEvent : MessageBase
    {
        public const short ProtocolVersion = 1;

        public short Version = ProtocolVersion;
        public int SourceID = -1;   // Placeable.ID of the acting block
        public int TargetID = -1;   // Placeable.ID of the affected block
        public short Action;        // block-defined action code
        public Vector3 Payload;     // small free-form payload (e.g. a color)

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(Version);
            writer.Write(SourceID);
            writer.Write(TargetID);
            writer.Write(Action);
            writer.Write(Payload);
        }

        public override void Deserialize(NetworkReader reader)
        {
            Version = reader.ReadInt16();
            SourceID = reader.ReadInt32();
            TargetID = reader.ReadInt32();
            Action = reader.ReadInt16();
            Payload = reader.ReadVector3();
        }
    }

    public static class CustomBlockNet
    {
        // Vanilla ids stop well below 200 (47 + a running count); stay far
        // above them. Overridable from config before the first Connect.
        public static short MessageId = 1300;

        public static void RegisterHandlers(NetworkClient client)
        {
            if (NetworkServer.active)
            {
                NetworkServer.RegisterHandler(MessageId, RelayOnServer);
            }
            if (client != null)
            {
                client.RegisterHandler(MessageId, DispatchOnClient);
            }
        }

        // The channel is host-authoritative: senders are expected to gate on
        // NetworkServer.active, and every peer (host included) applies the
        // effect when the relayed message comes back around.
        public static void Send(short action, int sourceID, int targetID, Vector3 payload)
        {
            MsgCustomBlockEvent msg = new MsgCustomBlockEvent();
            msg.Action = action;
            msg.SourceID = sourceID;
            msg.TargetID = targetID;
            msg.Payload = payload;

            NetworkManager manager = NetworkManager.singleton;
            if (manager != null && manager.client != null && manager.client.isConnected)
            {
                manager.client.Send(MessageId, msg);
            }
            else
            {
                // no network (shouldn't happen in UCH, which always runs a
                // local host) - apply locally so the effect still lands
                Dispatch(msg);
            }
        }

        static void RelayOnServer(NetworkMessage msg)
        {
            NetworkServer.SendToAll(MessageId, msg.ReadMessage<MsgCustomBlockEvent>());
        }

        static void DispatchOnClient(NetworkMessage msg)
        {
            Dispatch(msg.ReadMessage<MsgCustomBlockEvent>());
        }

        // Channel-wide handlers for actions that do not belong to a single
        // CustomBlock type (e.g. background-ness on a vanilla block). Global
        // action codes are NEGATIVE; positive codes go to the target block's
        // OnNetworkEvent.
        static readonly System.Collections.Generic.Dictionary<short, System.Action<MsgCustomBlockEvent>> globalHandlers
            = new System.Collections.Generic.Dictionary<short, System.Action<MsgCustomBlockEvent>>();

        public static void RegisterGlobalAction(short action, System.Action<MsgCustomBlockEvent> handler)
        {
            globalHandlers[action] = handler;
        }

        static void Dispatch(MsgCustomBlockEvent e)
        {
            if (e == null)
            {
                return;
            }
            if (e.Version != MsgCustomBlockEvent.ProtocolVersion)
            {
                Debug.LogWarning("CustomBlockNet: dropping message with protocol version "
                    + e.Version + " (mine is " + MsgCustomBlockEvent.ProtocolVersion + ")");
                return;
            }

            System.Action<MsgCustomBlockEvent> handler;
            if (e.Action < 0 && globalHandlers.TryGetValue(e.Action, out handler))
            {
                handler(e);
                return;
            }

            // the affected block gets the event; fall back to the source
            // block for self-targeted actions
            CustomBlock cb = FindBlock(e.TargetID) ?? FindBlock(e.SourceID);
            if (cb != null)
            {
                cb.OnNetworkEvent(e);
            }
        }

        // Any placeable by networked ID, preferring the placed copy (reload
        // ghosts share IDs with live blocks). For global actions that also
        // target vanilla blocks.
        public static Placeable FindPlaceable(int placeableID)
        {
            if (placeableID == -1)
            {
                return null;
            }
            Placeable fallback = null;
            foreach (Placeable p in Placeable.AllPlaceables)
            {
                if (p != null && p.ID == placeableID)
                {
                    if (p.placed)
                    {
                        return p;
                    }
                    if (fallback == null)
                    {
                        fallback = p;
                    }
                }
            }
            return fallback;
        }

        static CustomBlock FindBlock(int placeableID)
        {
            if (placeableID == -1)
            {
                return null;
            }
            // reload leaves unplaced ghost copies sharing the ID of the live
            // block — the placed one is the real addressee
            CustomBlock fallback = null;
            foreach (Placeable p in Placeable.AllPlaceables)
            {
                if (p != null && p.ID == placeableID)
                {
                    CustomBlock cb = p.GetComponent<CustomBlock>();
                    if (cb != null)
                    {
                        if (p.placed)
                        {
                            return cb;
                        }
                        fallback = cb;
                    }
                }
            }
            return fallback;
        }
    }
}
