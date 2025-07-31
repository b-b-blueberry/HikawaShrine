using System.Collections.Generic;

namespace Hikawa.Data;

public record class BowsDataAsset
{
    public string Identifier;
    public int Category;
    public string Type;
    /// <summary>
    /// Keyed by unqualified item ID.
    /// </summary>
    public Dictionary<string, BowsDataEntry> Bows;
}

public record class BowsDataEntry
{
    public string DisplayName;
    public string Description;
    public string Texture;
    public Rectangle SourceRect;
    public BowFrame[] HeldFrames;
    public int Damage;
    public int Speed;
    public int Pierces;
    public int Precision;
    public float CriticalChance;
    public float CriticalMultiplier;
    public float KnockbackMultiplier;
    public float? DrawTime;
    public string FireObject;
    public string FireSound;
    public float FireRate;
    public string BowstringColour;
    public bool IsMagical;
}

public record class BowFrame
{
    public Rectangle SourceRect;
    public Vector2[] Bowstrings;
}
