#pragma warning disable CS1591
namespace NVorbis.Contracts
{
    interface IMdct
    {
        void Reverse(float[] samples, int sampleCount);
    }
}
