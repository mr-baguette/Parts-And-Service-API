#pragma warning disable CS1591
namespace StbImageSharp
{
#if !STBSHARP_INTERNAL
	public
#else
	internal
#endif
	class AnimatedFrameResult : ImageResult
	{
		public int DelayInMs { get; set; }
	}
}