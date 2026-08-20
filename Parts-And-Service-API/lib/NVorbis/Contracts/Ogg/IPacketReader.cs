#pragma warning disable CS1591
using System;

namespace NVorbis.Contracts.Ogg
{
    interface IPacketReader
    {
        Memory<byte> GetPacketData(int pagePacketIndex);

        void InvalidatePacketCache(IPacket packet);
    }
}
