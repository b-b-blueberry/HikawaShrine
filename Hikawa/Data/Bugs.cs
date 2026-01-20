using Hikawa.Objects.Items;
using System.Collections.Generic;

namespace Hikawa.Data;

/// <summary>
/// this is the model loaded from file. what do i even name these anymore
/// </summary>
public class BugsDataAsset
{
    /// <summary>
    /// Item data definition fields.
    /// </summary>
    public BugFurnitureData BugFurnitureData;
    /// <summary>
    /// Keyed by local ID exclusive of mod ID
    /// </summary>
    public Dictionary<string, BugFurnitureDataEntry> BugFurniture;
    /// <summary>
    /// Item data definition fields.
    /// </summary>
    public BugToolData BugToolData;
    /// <summary>
    /// Keyed by bug ID
    /// </summary>
    public Dictionary<string, BugData> BugData;
}

/// <summary>Generic data applied to all <see cref="BugFurniture"/> items.</summary>
public record class BugFurnitureData
{
    public string Identifier;
    public int Category;
    public string Type;
    public string FurnitureType;
}

/// <summary>Specific data applied to a given <see cref="BugFurniture"/> item.</summary>
public record class BugFurnitureDataEntry
{
    public string DisplayName;
    public string Description;
    public string TextureId;
    public int SpriteIndex;
    public Point SpriteSize;
    public Point CollisionSize;
    public BugSlot[] BugSlots;
}

public record class BugToolData
{
    public string Identifier;
    public string Name;
    public string DisplayName;
    public string Description;
    public string TextureName;
    public Point SpriteSize;
    public int SpriteIndex;
    public int Category;
    public string Type;
}

/// <summary>Placement data for bugs in <see cref="BugFurniture"/>, where each item has limited slots for one bug each.</summary>
public record class BugSlot
{
    public string Type;
    public Vector2 Position;
    public bool Flip;
}

/// <summary>Flags determining slot availability for bugs in <see cref="BugFurniture"/>. Matches some given <see cref="BugData.BugSlots"/> values by name.</summary>
public enum BugSlotType
{
    Air,
    Ground,
    Perch
}

public record class BugData
{
    /// <summary>
    /// Asset key of bug texture.
    /// </summary>
    public string TextureId;
    /// <summary>
    /// Source area in bug texture when drawn in world.
    /// </summary>
    public Rectangle WorldTextureRegion;
    /// <summary>
    /// Source area in bug texture when drawn in menu.
    /// </summary>
    public Rectangle MenuTextureRegion;
    /// <summary>
    /// Source area in tool texture applied to tool when holding this bug.
    /// </summary>
    public Rectangle ToolTextureRegion;
    /// <summary>
    /// Translated display name.
    /// </summary>
    public string DisplayName;
    /// <summary>
    /// Translated scientific binomial.
    /// </summary>
    public string ScientificName;
    /// <summary>
    /// Translated description.
    /// </summary>
    public string Description;
    /// <summary>
    /// Value to match <see cref="StardewValley.Season"/> to add to world.
    /// </summary>
    public string Season;

    /// <summary>
    /// Number of animation frames in world.
    /// </summary>
    public int AnimationFrames;
    /// <summary>
    /// Milliseconds per frame in world.
    /// </summary>
    public int AnimationSpeed;

    /// <summary>
    /// Whether to play flying animations in world.
    /// </summary>
    public bool IsFlying;
    /// <summary>
    /// Whether to play gold effects in world.
    /// </summary>
    public bool IsGold;

    /// <summary>
    /// Slot used in furniture.
    /// </summary>
    public string[] BugSlots;

    /// <summary>
    /// Context tag applied to bug tool when caught.
    /// </summary>
    public string ContextTag;
}
