#pragma warning disable CS1591
namespace NVorbis.Contracts
{
    interface IFloorData
    {
        bool ExecuteChannel { get; }
        bool ForceEnergy { get; set; }
        bool ForceNoEnergy { get; set; }
    }
}
