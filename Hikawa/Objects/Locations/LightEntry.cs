using Microsoft.Xna.Framework;

namespace Hikawa.Objects.Locations
{
	public class LightTileEntry
	{
		public Vector2 Tile = Vector2.Zero;
		public int TileId = 0;
		public string TileSheetId = null;
		public Point Size = new(1);
		public bool DrawAbove = false;
	}

	public class LightEntry
	{
		// Base
		public Vector2 Tile;
		public float Scale = 1f;
		public Color Color = Color.Black;
		public int TextureIndex;

		// Custom
		/// <summary>
		/// Asset key used for light texture.
		/// Values of null create a default <see cref="StardewValley.LightSource"/>.
		/// Values other than null create a custom <see cref="HearthLight"/>.
		/// </summary>
		public string TextureName = null;
		/// <summary>
		/// Percentage visibility of light before variance is applied.
		/// Values range from 0 to 1.
		/// Higher values are more clearly visible.
		/// </summary>
		public float Luminosity = 1f;
		/// <summary>
		/// Percentage of light intensity.
		/// Values range from 0 to 1.
		/// Higher values are more clearly visible.
		/// </summary>
		public float Variance = 0.002f;
		/// <summary>
		/// Frames elapsed between changes of light intensity.
		/// Values greater than 0.
		/// Higher values are less frequent.
		/// </summary>
		public int Rate = 4;
		/// <summary>
		/// Time of day when opacity is reduced to minimum value.
		/// Formatted to match WorldDate integer times.
		/// Values range from 0 to 2600.
		/// Value of 0 does not extinguish.
		/// </summary>
		public int ExtinguishAtTime = 0;
		/// <summary>
		/// Rate of change of opacity when extinguishing.
		/// Affects duration between extinguish time and start time.
		/// Values above 0.
		/// </summary>
		public float ExtinguishRate = 1f;
		/// <summary>
		/// Minimum opacity ratio when extinguished.
		/// May reach values greater than 0 before ExtinguishAtTime.
		/// Values range from 0 to 1.
		/// </summary>
		public float ExtinguishedAlpha = 0f;
	}
}
