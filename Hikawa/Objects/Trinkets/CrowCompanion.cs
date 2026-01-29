using StardewValley.Companions;
using StardewValley.Extensions;
using StardewValley.Locations;
using System;

namespace Hikawa.Objects.Trinkets
{
    public class CrowCompanion : Companion
    {
        const float rotationIncrement = (float)Math.PI / 64f;
        Rectangle DefaultSourceArea => new Rectangle(0, 160, 16, 16);

        float idleTimer;
        float peckTimer;
        bool perching;
        bool falling;
        bool flying;

        Vector2 velocity;

        // sensible
        Vector2 target;

        // fuck
        float extraVelocity;
        float maxSpeed;
        float rotation;
        float targetRotation;
        bool turningRight;


        public CrowCompanion()
        {
            // caw caw
        }

        public void ResetIdleTimer()
        {
            this.idleTimer = 0;
        }

        public bool CanFlyHere(GameLocation location)
        {
            return location is not null && (location.IsOutdoors || location is MineShaft mine && !mine.isSideBranch());
        }

        public void StartFlying(GameLocation location)
        {
            if (Game1.random.NextSingle() < 0.25f)
                location?.localSound("crow");

            this.perching = false;
            this.falling = false;
            this.flying = true;
            this.gravity = 0f;

            this.target = this.OwnerPosition;
            this.velocity = Utils.Vector.MotionTo(this.Position, this.target) * 3;

            this.ResetIdleTimer();
        }

        public void StopFlying(GameLocation location)
        {
            this.falling = true;
            this.flying = false;
            this.perching = false;

            if (this.IsLocal)
                this.lerp = -1;

            this.ResetIdleTimer();
        }

        public void Peck(GameLocation location)
        {
            location?.localSound("tinyWhip");

            this.peckTimer = 300;
        }

        public override void InitializeCompanion(Farmer farmer)
        {
            base.InitializeCompanion(farmer);
        }

        public override void CleanupCompanion()
        {
            base.CleanupCompanion();
        }

        public override void OnOwnerWarp()
        {
            base.OnOwnerWarp();

            var location = this.Owner.currentLocation;
            if (this.flying)
            {
                if (!this.CanFlyHere(location))
                    this.StopFlying(location);
                else
                    this.target = default;
            }
        }

        public override void Hop(float amount)
        {
            // hopping resets height, skip if in the air
            if (this.height <= 0)
            {
                base.Hop(amount);

                this.ResetIdleTimer();
                this.peckTimer = 0;
            }
        }

        public override void Update(GameTime time, GameLocation location)
        {
            var ms = (float)time.ElapsedGameTime.TotalMilliseconds;
            if (!this.flying)
            {
                // behave as a regular companion (hopping)
                base.Update(time, location);

                this.idleTimer += ms;

                if (this.IsLocal)
                {
                    this.peckTimer = MathF.Max(0, this.peckTimer - ms);

                    // idle pecking
                    if (this.idleTimer > 5000 && this.peckTimer <= 0 && Game1.random.NextSingle() < 0.005f)
                        this.Peck(location);

                    // start flying when midair after hopping repeatedly
                    if (this.CanFlyHere(location) && this.height > 0 && this.gravity > 0.5f && Game1.random.NextSingle() < 0.05f)
                        this.StartFlying(location);

                    // stop falling after hitting the ground
                    if (this.height <= 0)
                        this.falling = false;

                    // do the actual critter behaviour (pilfer items as forage)
                }
            }
            else if (this.flying)
            {
                this.idleTimer += ms;

                if (this.IsLocal)
                {
                    if (Game1.random.NextSingle() < 0.0001f)
                        location.localSound("crow");
                }

                // flying movement
                this.UpdateSensibleMovement();

                // do the actual critter behaviour (pilfer items as loot)

                // update fields
                this.hopEvent.Poll();
            }
            else if (this.perching)
            {
                // perching (no movement)

                // TODO: perching behaviour

                // update fields
                this.hopEvent.Poll();
            }
        }

        public void UpdateFlyingMovement()
        {
            Vector2 monsterPixel = this.Position;
            Vector2 playerPixel = this.Owner.Position;

            Vector2 slope = new Vector2(-(playerPixel.X - monsterPixel.X), playerPixel.Y - monsterPixel.Y);
            float t = Math.Max(1, Math.Abs(slope.X) + Math.Abs(slope.Y));
            if (t < (extraVelocity > 0 ? Game1.tileSize * 3 : Game1.tileSize))
            {
                this.velocity = new Vector2(
                    Math.Max(-(maxSpeed), Math.Min(maxSpeed, this.velocity.X * (1.05f))),
                    Math.Max(-(maxSpeed), Math.Min(maxSpeed, this.velocity.Y * (1.05f))));
            }

            slope /= t;

            {
                targetRotation = (float)Math.Atan2(-slope.Y, slope.X) - (float)Math.PI / 2;

                if (Math.Abs(targetRotation) - Math.Abs(rotation) > Math.PI * 7 / 8 && Game1.random.NextBool())
                    turningRight = true;
                else if (Math.Abs(targetRotation) - Math.Abs(rotation) < Math.PI / 8)
                    turningRight = false;

                if (turningRight)
                    rotation -= Math.Sign(targetRotation - rotation) * rotationIncrement;
                else
                    rotation += Math.Sign(targetRotation - rotation) * rotationIncrement;

                rotation %= (float)Math.PI * 2;
            }

            float maxAccel = Math.Min(5f, Math.Max(1f, 5f - t / Game1.tileSize / 2f)) + extraVelocity;

            slope.X = (float)Math.Cos(rotation + Math.PI / 2);
            slope.Y = -(float)Math.Sin(rotation + Math.PI / 2);

            this.velocity.X += -slope.X * maxAccel / 6f + Game1.random.Next(-10, 10) / 100f;
            this.velocity.Y += -slope.Y * maxAccel / 6f + Game1.random.Next(-10, 10) / 100f;

            if (Math.Abs(this.velocity.X) > Math.Abs(-slope.X * maxSpeed))
                this.velocity.X -= -slope.X * maxAccel / 6f;
            if (Math.Abs(this.velocity.Y) > Math.Abs(-slope.Y * maxSpeed))
                this.velocity.Y -= -slope.Y * maxAccel / 6f;

            if (velocity != Vector2.Zero)
            {
                this.Position = new Vector2(this.Position.X + this.velocity.X, this.Position.Y - this.velocity.Y);
                if (Math.Abs(this.velocity.X) <= .05f)
                    this.velocity.X = 0;
                if (Math.Abs(this.velocity.Y) <= .05f)
                    this.velocity.Y = 0;
            }
        }

        public void UpdateSensibleMovement()
        {
            var ms = (float)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
            if (this.target == default || Vector2.Distance(this.Position, this.target) < Game1.tileSize)
            {                
                if (this.perching)
                {
                    // stop flying
                    this.StopFlying(this.Owner.currentLocation);
                }
                else
                {
                    // set flying target
                    this.target = this.OwnerPosition
                        + new Vector2(-1) + new Vector2(Game1.random.Next(3), Game1.random.Next(3)) * Game1.tileSize;
                }

                if (!this.perching && this.idleTimer > 3000 && Game1.random.NextBool() && Vector2.Distance(this.Position, this.OwnerPosition) < Game1.tileSize * 3)
                {
                    // bird goes down
                    this.perching = true;
                }
            }
            else
            {
                {
                    var motion = Utils.Vector.MotionTo(this.Position, this.OwnerPosition);

                    // update facing direction
                    var direction = Game1.up;
                    if (motion.X > 0 && motion.X > motion.Y)
                        direction = Game1.right;
                    else if (motion.Y > 0 && motion.Y > motion.X)
                        direction = Game1.down;
                    else if (motion.X < 0 && motion.X < motion.Y)
                        direction = Game1.left;
                    this.direction.Value = direction;
                }
                {
                    var motion = Utils.Vector.MotionTo(this.Position, this.target);
                    var maxVelocity = Game1.tileSize / 12;
                    var maxHeight = Game1.tileSize / 2;

                    // update position
                    this.velocity += motion / ms * 3;
                    this.velocity = new Vector2(
                        MathF.CopySign(MathF.Min(MathF.Abs(maxVelocity), MathF.Abs(this.velocity.X)), this.velocity.X),
                        MathF.CopySign(MathF.Min(MathF.Abs(maxVelocity), MathF.Abs(this.velocity.Y)), this.velocity.Y));
                    this.Position = new Vector2(this.Position.X + this.velocity.X, this.Position.Y + this.velocity.Y);

                    // anti endless orbital motion
                    if (Math.Abs(this.velocity.X) <= .05f || (Math.Abs(this.velocity.Y) <= .05f))
                        this.target = default;

                    // bird goes up
                    var height = this.height + ms * 0.02f;
                    this.height = MathF.Min(maxHeight, height);
                }
            }
        }

        public override void Draw(SpriteBatch b)
        {
            if (this.Owner?.currentLocation is null || this.Owner.currentLocation.DisplayName == "Temp" && !Game1.isFestival())
                return;

            this.DrawCrow(b, false, Color.White, out Rectangle source, out Vector2 origin);

            var shadowPosition = Game1.GlobalToLocal(this.Position + this.Owner.drawOffset);
            var shadowLayerDepth = (this._position.Y - origin.Y) / 10000f - .000002f;

            b.Draw(
                texture: Game1.shadowTexture,
                position: shadowPosition,
                sourceRectangle: Game1.shadowTexture.Bounds,
                color: Color.White,
                rotation: 0,
                origin: Game1.shadowTexture.Bounds.Size.ToVector2() / 2,
                scale: Game1.pixelZoom * 0.75f * Utility.Lerp(1, 0.8f, Math.Min(this.height, 1)),
                effects: SpriteEffects.None,
                layerDepth: shadowLayerDepth);
        }

        public void DrawAboveAlwaysFront(SpriteBatch b)
        {
            this.DrawCrow(b, true, Color.White, out var _, out var _);
        }

        public void DrawCrow(SpriteBatch b, bool isDrawAboveAlwaysFront, Color color, out Rectangle source, out Vector2 origin)
        {
            var direction = this.direction.Value switch
            {
                Game1.up => 2,
                Game1.down => 1,
                _ => 0
            };
            var ms = (float)Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
            var texture = Game1.content.Load<Texture2D>("LooseSprites/birds");
            var effect = this.direction.Value == Game1.right ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            var layerDepth = this._position.Y / 10000f;
            var frames = 2;

            source = this.flying
                ? new Rectangle(
                    x: this.DefaultSourceArea.X + this.DefaultSourceArea.Width * (2 + (direction * frames) + (int)(ms / 150 % frames)),
                    y: this.DefaultSourceArea.Y,
                    width: this.DefaultSourceArea.Width,
                    height: this.DefaultSourceArea.Height)
                : this.falling
                    ? new Rectangle(
                        x: this.DefaultSourceArea.X + this.DefaultSourceArea.Width * (2 + (direction * frames)),
                        y: this.DefaultSourceArea.Y,
                        width: this.DefaultSourceArea.Width,
                        height: this.DefaultSourceArea.Height)
                    : this.DefaultSourceArea;
            if (this.peckTimer > 0)
                source.X += source.Width;

            origin = source.Size.ToVector2() / 2;

            if (isDrawAboveAlwaysFront != this.flying)
                return;

            var position = Game1.GlobalToLocal(this.Position
                + this.Owner.drawOffset
                + (this.flying ? new Vector2(MathF.Sin(ms / 300) * 1 * Game1.pixelZoom) : Vector2.Zero) // flying wobble
                + new Vector2(0, -source.Height * Game1.pixelZoom / 2) // align sprite with ground
                + new Vector2(0, -this.height * Game1.pixelZoom)); // hopping or flying offset

            b.Draw(
                texture: texture,
                position: position,
                sourceRectangle: source,
                color: color,
                rotation: this.rotation,
                origin: origin,
                scale: Game1.pixelZoom,
                effects: effect,
                layerDepth: layerDepth);
        }
    }
}
