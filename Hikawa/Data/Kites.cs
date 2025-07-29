using System.Collections.Generic;

namespace Hikawa.Data;

public record class KitesDataAsset
{
    public string Identifier;
    public string ItemId;
    public int Category;
    public string Type;
    public int Fragility;
    /// <summary>
    /// Keyed by local ID exclusive of mod ID
    /// </summary>
    public Dictionary<string, KiteDataEntry> Kites;
}

public record class KiteDataEntry
{
    public int Price;
    public bool Passable;
    public string DisplayName;
    public string Description;
    public string Texture;
    public int SpriteIndex;
    public string StakeTexture;
    /// <remarks>mmmm steak sauce........</remarks>
    public Rectangle StakeSource;
    public Vector2 StakeOrigin;
    public List<KiteDataOnTheKiteItsASmallerKiteNotTheMainDataOrTheDataEntryClassesThisIsDifferent> Kites;
}

/// <remarks>
/// this is also not the instance class that's different too
/// </remarks>
public record class KiteDataOnTheKiteItsASmallerKiteNotTheMainDataOrTheDataEntryClassesThisIsDifferent
{
    public string KiteTexture;
    public Rectangle KiteSource;
    public Vector2 KiteOrigin;
    public Color StringColor;
    /// <summary> hite of the keight </summary>
    public int Height;
    public int Length;
    public float Gravity;
    public int Wind;
    public Vector2 Scaling;
    public bool Vertical;
}
