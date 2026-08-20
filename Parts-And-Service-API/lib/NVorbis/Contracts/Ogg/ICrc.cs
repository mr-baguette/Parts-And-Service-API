#pragma warning disable CS1591
namespace NVorbis.Contracts.Ogg
{
    interface ICrc
    {
        void Reset();
        void Update(int nextVal);
        bool Test(uint checkCrc);
    }
}
