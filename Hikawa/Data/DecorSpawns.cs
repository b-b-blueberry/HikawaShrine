using Hikawa.Objects.Decor;
using System.Collections.Generic;

namespace Hikawa.Data;

public record class DecorSpawnsDataAsset
{
    public string HearthLightBaseId;
    public Point HearthLightSize;
    public Vector2 HouseChimneyTile;
    /// <summary>
    /// Keyed by location name, value keyed by spawn tile location, value of bug ID
    /// </summary>
    public Dictionary<string, Dictionary<Vector2, string>> Bugs;
    public Vector2 CrowSpawnRadius;
    public Rectangle CrowSpawnArea;
    /// <summary>
    /// Keyed by chance to appear, chance is measured by whether key is higher than the random roll
    /// </summary>
    public Dictionary<float, CrowSpawnData> CrowPerches;
    public Point[] BabyCrowPerches;
    public Point[] BabyCrowRoosts;
    public Dictionary<string, List<ShrineTreeSpawnData>> ShrineTrees;
    public Dictionary<string, List<ShrubSpawnData>> Shrubs;
    public Dictionary<string, List<HangingSpriteData>> HangingSprites;
    public Dictionary<string, List<LightTileData>> LightTiles;
    public Dictionary<string, List<LightData>> Lights;
}

public record class BugSpawnData
{
    /// <summary>
    /// List of possible tile coordinates when adding to world.
    /// </summary>
    public Vector2[] Tiles;
    /// <summary>
    /// Relative weight when adding to world.
    /// </summary>
    public int Weight;
}

public record class CrowSpawnData
{
	// Paired by spawn positions for Phobos and Deimos
	// R value is hop range/radius
	public float X1, X2, Y1, Y2, R;
	/// <summary>
	/// Phobos
	/// </summary>
	public Vector2 V1 => new Vector2(X1, Y1);
	/// <summary>
	/// Deimos
	/// </summary>
	public Vector2 V2 => new Vector2(X2, Y2);
}

public record class ShrineTreeSpawnData
{
    /// <summary>
    /// Tile position of base of tree.
    /// </summary>
    public Vector2 Tile;
    /// <summary>
    /// Key of entry in base game WildTrees data model.
    /// </summary>
    public string Id;
    public bool Flip;
    public bool Leaves;
    public bool Shadow;
}

public record class ShrubSpawnData
{
    /// <summary>
    /// Tile position of base of shrub.
    /// </summary>
    public Vector2 Tile;
    public string Id;
    public int GrowthStage;
    public bool PreventGrowth = true;
    public bool PreventInteractions = true;
}

public record class HangingSpriteData
{
	public Vector2 Tile;
	public string TextureId;
	public Rectangle TextureRegion;
	public Vector2 TextureOrigin;
	public float Resistance;
	public float Limit;
	public bool DrawBehind = false;

	public int AnimationFrames;
	public float AnimationSpeed;
}

public record class LightTileData
{
	public Vector2 Tile = Vector2.Zero;
	public int TileId = 0;
	public string TileSheetId = null;
	public Point Size = new(1);
	public bool DrawAbove = false;
}

public record class LightData
{
	// Base
	public Vector2 Tile;
	public float Scale = 1f;
	public Color Color = Color.Black;
	public int TextureIndex;

	// Custom
	/// <summary>
	/// Asset key used for light texture.
	/// Values of null create a default <see cref="LightSource"/>.
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
