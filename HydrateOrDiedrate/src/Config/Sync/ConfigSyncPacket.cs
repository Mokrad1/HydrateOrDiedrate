﻿using System.Collections.Generic;
using ProtoBuf;

namespace HydrateOrDiedrate.Config.Sync
{
    [ProtoContract(SkipConstructor = true)]
    public class ConfigSyncPacket
    {
        [ProtoMember(1)]
        public Config ServerConfig { get; set; }

        [ProtoMember(2)]
        public List<string> HydrationPatches { get; set; }

        [ProtoMember(3)]
        public List<string> BlockHydrationPatches { get; set; }

        [ProtoMember(4)]
        public List<string> CoolingPatches { get; set; }
    }
}