using System;
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
    public string TextureId;
    public int SpriteIndex;
    public string StakeTextureId;
    /// <remarks>mmmm steak sauce........</remarks>
    public Rectangle StakeSource;
    public Vector2 StakeOrigin;
    public List<KiteDataOnTheKiteItsASmallerKiteNotTheMainDataOrTheDataEntryClassesThisIsDifferent> Kites;

    public Lazy<Texture2D> Texture;
    public Lazy<Texture2D> StakeTexture;

    public KiteDataEntry()
    {
        this.Texture = new(() => Game1.content.Load<Texture2D>(this.TextureId));
        this.StakeTexture = new(() => Game1.content.Load<Texture2D>(this.StakeTextureId));
    }
}

/// <remarks>
/// this is also not the instance class that's different too
/// </remarks>
public record class KiteDataOnTheKiteItsASmallerKiteNotTheMainDataOrTheDataEntryClassesThisIsDifferent
{
    public string KiteTextureId;
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

    public Lazy<Texture2D> KiteTexture;

    public KiteDataOnTheKiteItsASmallerKiteNotTheMainDataOrTheDataEntryClassesThisIsDifferent()
    {
        this.KiteTexture = new(() => Game1.content.Load<Texture2D>(this.KiteTextureId));
    }
}
