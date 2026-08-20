#pragma warning disable CS1591
namespace StbImageSharp
{
#if !STBSHARP_INTERNAL
	public
#else
	internal
#endif
	enum ColorComponents
	{
		Default,
		Grey,
		GreyAlpha,
		RedGreenBlue,
		RedGreenBlueAlpha
	}
}