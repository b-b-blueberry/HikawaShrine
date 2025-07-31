using Hikawa.Data;
using Hikawa.Objects.Items;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Monsters;
using StardewValley.Projectiles;
using System;

namespace Hikawa.Objects.Projectiles;

public class BowProjectile : BasicProjectile
{
    public readonly string BowId;
    public readonly int Charge;
    public readonly bool IsMagical;

    public static BowProjectile Create(Farmer who, GameLocation location, Bow bow, BowsDataEntry bowData, int charge, onCollisionBehavior onCollision = null)
    {
        Vector2 origin = bow.GetShootOrigin(who);
        Vector2 target = bow.AdjustForHeight(bow.aimPos.Value.ToVector2());
        Vector2 velocity = Utility.getVelocityTowardPoint(
            startingPoint: origin,
            endingPoint: target,
            speed: (bowData.Speed + Game1.random.Next(4, 6)) * (1f + who.buffs.WeaponSpeedMultiplier));

        if (!Game1.options.useLegacySlingshotFiring)
        {
            velocity.X *= -1f;
            velocity.Y *= -1f;
        }

        return new BowProjectile(
            who: who,
            location: location,
            bow: bow,
            bowData: bowData,
            charge: charge,
            velocity: velocity,
            origin: origin,
            target: target,
            onCollision: onCollision);
    }

    /// <summary>
    /// Use <see cref="Create(Farmer, GameLocation, Bow, BowsDataEntry, int, onCollisionBehavior)"/> instead.
    /// </summary>
    public BowProjectile(Farmer who, GameLocation location, Bow bow, BowsDataEntry bowData, int charge, Vector2 velocity, Vector2 origin, Vector2 target, onCollisionBehavior onCollision) : base(
        damageToFarmer: bowData.Damage,
        spriteIndex: 0,
        bouncesTillDestruct: 0,
        tailLength: bowData.IsMagical ? 3 : 0,
        rotationVelocity: 0,
        xVelocity: -velocity.X,
        yVelocity: -velocity.Y,
        startingPosition: origin - new Vector2(Game1.tileSize / 2),
        collisionSound: null,
        bounceSound: null,
        firingSound: null,
        explode: false,
        damagesMonsters: true,
        location: location,
        firer: who,
        collisionBehavior: onCollision,
        shotItemId: bowData.FireObject)
    {
        // BasicProjectile
        this.IgnoreLocationCollision = Game1.currentLocation.currentEvent is not null || Game1.currentMinigame is not null;
        this.startingRotation.Value = (float)Utils.Vector.RadiansBetween(origin, target);
        this.piercesLeft.Value = bowData.Pierces;

        // BowProjectile
        this.BowId = bow.ItemId;
        this.Charge = charge;
        this.IsMagical = bowData.IsMagical;
    }

    public override void behaviorOnCollisionWithOther(GameLocation location)
    {
        // prevent multiple arrow debris on wall collisions
        this.piercesLeft.Value = 0;

        base.behaviorOnCollisionWithOther(location);
    }

    public override void behaviorOnCollisionWithMonster(NPC n, GameLocation location)
    {
        Farmer player = this.GetPlayerWhoFiredMe(location);
        if (n is Monster monster)
        {
            // apply custom damage values
            if (ItemRegistry.GetData(this.BowId) is ParsedItemData itemData && itemData.RawData is BowsDataEntry bowData)
            {
                float damageMod = this.Charge / 2f;
                int damage = this.damageToFarmer.Value;
                damage = (int)(damageMod * (damage + Game1.random.Next(-(damage / 2), damage + 2)) * (1f + player.buffs.AttackMultiplier));
                location.damageMonster(
                    areaOfEffect: n.GetBoundingBox(),
                    minDamage: damage,
                    maxDamage: damage,
                    isBomb: false,
                    knockBackModifier: bowData.KnockbackMultiplier,
                    addedPrecision: bowData.Precision,
                    critChance: bowData.CriticalChance,
                    critMultiplier: bowData.CriticalMultiplier,
                    triggerMonsterInvincibleTimer: true,
                    who: player,
                    isProjectile: true);
                if (!monster.IsInvisible)
                {
                    --this.piercesLeft.Value;
                }
            }
        }
        else
        {
            // prevent global chat message and piercing arrows when firing at characters
            this.piercesLeft.Value = 0;
            n.getHitByPlayer(player, location);
            this.explosionAnimation(location);
        }
    }

    protected override void explosionAnimation(GameLocation location)
    {
        Farmer player = this.GetPlayerWhoFiredMe(location);
        this.collisionBehavior?.Invoke(location, this.getBoundingBox().Center.X, this.getBoundingBox().Center.Y, player);
        this.destroyMe = true;

        if (this.IsMagical)
        {
            for (int i = 0; i < 12; i++)
            {
                Vector2 motion = new Vector2(0f, -1.5f + Game1.random.Next(-10, 11) / 12f);
                motion = Vector2.Transform(motion, Matrix.CreateRotationZ((float)(MathF.PI / 6f + (float)(Game1.random.Next(-10, 11) / 50f)) * i));
                location.temporarySprites.Add(new TemporaryAnimatedSprite(
                    textureName: "LooseSprites/Cursors_1_6",
                    sourceRect: new Rectangle(144, 249, 7, 7),
                    animationInterval: 80,
                    animationLength: 6,
                    numberOfLoops: 1,
                    position: this.position.Value,
                    flicker: false,
                    flipped: false,
                    layerDepth: 1,
                    alphaFade: 0,
                    color: Utility.Get2PhaseColor(Color.Gold, Color.OrangeRed, 0, 1f, Game1.random.Next(1000)),
                    scale: Game1.pixelZoom,
                    scaleChange: 0,
                    rotation: 0,
                    rotationChange: 0)
                {
                    drawAboveAlwaysFront = true,
                    motion = motion
                });
            }
        }
        else
        {
            Texture2D texture = this.GetTexture();
            Rectangle sourceRect = this.GetSourceRect();

            Vector2 motion = new Vector2(this.xVelocity.Value, this.yVelocity.Value) / 10f;
            bool[] directions = Utility.horizontalOrVerticalCollisionDirections(boundingBox: this.getBoundingBox(), c: player, projectile: true);
            if (directions[0]) motion.X = -motion.X;
            if (directions[1]) motion.Y = -motion.Y;

            location.temporarySprites.Add(new TemporaryAnimatedSprite(
                textureName: texture.Name,
                sourceRect: sourceRect,
                animationInterval: 1000,
                animationLength: 0,
                numberOfLoops: 1,
                position: this.position.Value,
                flicker: false,
                flipped: false,
                layerDepth: 1,
                alphaFade: 0.025f,
                color: Color.White,
                scale: Game1.pixelZoom,
                scaleChange: -0.02f,
                rotation: this.rotation,
                rotationChange: 0.2f * (Game1.random.NextDouble() > 0.5f ? -1 : 1))
            {
                drawAboveAlwaysFront = false,
                motion = motion
            });
        }
    }
}
