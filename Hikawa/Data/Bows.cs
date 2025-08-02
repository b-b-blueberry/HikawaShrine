using Hikawa.Objects.Items;
using System.Collections.Generic;

namespace Hikawa.Data;

public record class BowsDataAsset
{
    public string Identifier;
    public int Category;
    public string Type;
    /// <summary>
    /// List of all <see cref="Bow"/> items.
    /// Keyed by unqualified item ID.
    /// </summary>
    public Dictionary<string, BowsDataEntry> Bows;
}

public record class BowsDataEntry
{
    /** APPEARANCE **/

    /// <summary>Translated string used for item name.</summary>
    public string DisplayName;
    /// <summary>Translated string used for item description.</summary>
    public string Description;
    /// <summary>Sprite asset used for inventory and held appearances.</summary>
    public string Texture;
    /// <summary>Region in <see cref="Texture"/> used for inventory appearance.</summary>
    public Rectangle SourceRect;
    /// <summary>Values used for held appearances, in order of facing direction.</summary>
    public BowFrame[] HeldFrames;

    /** PROJECTILE **/

    /// <summary>Damage value. Base damage dealt on collision.</summary>
    public int Damage;
    /// <summary>Damage value. Projectile rate of motion.</summary>
    public int Speed;
    /// <summary>Damage value. Number of entities projectile can collide with before being destroyed. Projectile cannot pierce map geometry (i.e. pass through walls).</summary>
    public int Pierces;
    /// <summary>Damage value. Counteracts evasion.</summary>
    public int Precision;
    /// <summary>Damage value. Value given as ratio from 0 (never) to 1 (always) of applying <see cref="CriticalMultiplier"/>.</summary>
    public float CriticalChance;
    /// <summary>Damage value. Value given as a ratio of 0 (no damage), 1 (normal damage), or higher (additional damage).</summary>
    public float CriticalMultiplier;
    /// <summary>Damage value. Value given as a ratio of 0 (no knockback), 1 (normal knockback), or higher (additional knockback).</summary>
    public float KnockbackMultiplier;

    /** BOW **/

    /// <summary>Duration in seconds the <see cref="Bow"/> must be charged before it can be released. Defaults to <see cref="DrawTime"/>.</summary>
    public float? MinimumDrawTime;
    /// <summary>Duration in seconds the <see cref="Bow"/> takes to reach full charge.</summary>
    public float? DrawTime;
    /// <summary>Cue ID for sound played when starting to charge. Does not play for continuous use with a defined <see cref="FireRate"/>.</summary>
    public string DrawSound;
    /// <summary>Item ID for projectile fired.</summary>
    public string FireObject;
    /// <summary>Cue ID for sound played when released. Does not play when released before <see cref="MinimumDrawTime"/>.</summary>
    public string FireSound;
    /// <summary></summary>
    public float FireRate;
    /// <summary>Multiplier affecting aim responsiveness. Value given as a ratio from 0 (aim does not move) to 1 (aim follows cursor exactly). Values less than 0.2 are noticeable.</summary>
    public float TurnRate;
    /// <summary>Colour of dynamic bowstrings drawn when held.</summary>
    public string BowstringColour;
    /// <summary>Whether to play magic effects on use. Arrows will not be consumed.</summary>
    public bool IsMagical;

    /** ITEM **/

    /// <summary>Whether item may be lost on death.</summary>
    public bool CanBeLostOnDeath;
}

public record class BowFrame
{
    /// <summary>Region in <see cref="BowsDataEntry.Texture"/> used for held appearance.</summary>
    public Rectangle SourceRect;
    /// <summary>List of unscaled pixel coordinates for lines drawn from the pulling hand to the bow sprite when held, relative to the <see cref="SourceRect"/> origin.</summary>
    public Vector2[] Bowstrings;
}
