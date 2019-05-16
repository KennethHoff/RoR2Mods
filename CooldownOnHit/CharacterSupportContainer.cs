using System;
using UnityEngine.Networking;

namespace CooldownOnHit
{
    public class CharacterSupportContainer : MessageBase
    {

        public int index;

        public bool supported;


        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(index);
            writer.Write(supported);
        }
        public override void Deserialize(NetworkReader reader)
        {
            index = reader.ReadInt32();
            supported = reader.ReadBoolean();
        }
    }
}
