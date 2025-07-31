using Hikawa.Data;
using Hikawa.Objects.Items;
using StardewValley.Monsters;
using StardewValley.Projectiles;
using System;

namespace Hikawa.Objects.Projectiles;

public class BowProjectile : BasicProjectile
{
    public bool IsMagical;

    public static BowProjectile Create(Farmer who, GameLocation location, Bow bow, BowsDataEntry bowData, int charge, onCollisionBehavior onCollision = null)
    {
        Vector2 origin = bow.GetShootOrigin(who);
        Vector2 target = bow.AdjustForHeight(bow.aimPos.Value.ToVector2());
        Vector2 velocity = Utility.getVelocityTowardPoint(
            startingPoint: origin,
            endingPoint: target,
            speed: (bowData.Speed + Game1.random.Next(4, 6)) * (1f + who.buffs.WeaponSpeedMultiplier));

        float damageMod = charge / 2f;
        int damage = bowData.Damage;

        if (!Game1.options.useLegacySlingshotFiring)
        {
            velocity.X *= -1f;
            velocity.Y *= -1f;
        }

        return new BowProjectile(
            who: who,
            location: location,
            bowData: bowData,
            velocity: velocity,
            origin: origin,
            target: target,
            damage: (int)(damageMod * (damage + Game1.random.Next(-(damage / 2), damage + 2)) * (1f + who.buffs.AttackMultiplier)),
            onCollision: onCollision);
    }

    /// <summary>
    /// Use <see cref="Create(Farmer, GameLocation, Bow, BowsDataEntry, int, onCollisionBehavior)"/> instead.
    /// </summary>
    public BowProjectile(Farmer who, GameLocation location, BowsDataEntry bowData, Vector2 velocity, Vector2 origin, Vector2 target, int damage, onCollisionBehavior onCollision) : base(
        damageToFarmer: damage,
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
        if (n is not Monster)
        {
            // prevent global chat message and piercing arrows when firing at characters
            this.piercesLeft.Value = 0;
            n.getHitByPlayer(this.GetPlayerWhoFiredMe(location), location);
            this.explosionAnimation(location);
        }
        else
        {
            base.behaviorOnCollisionWithMonster(n, location);
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
