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
    public int Damage;
    public int Speed;
    public string FireObject;
    public string FireSound;
    public string BowstringColour;
    public bool IsMagical;
}
