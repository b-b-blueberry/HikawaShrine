using Netcode;
using StardewValley.Companions;
using StardewValley.Extensions;
using StardewValley.Locations;
using System;

namespace Hikawa.Objects.Trinkets
{
    public class CrowCompanion : Companion
    {
        Rectangle DefaultSourceArea => new Rectangle(0, 160, 16, 16);

        Vector2 velocity;
        Vector2 target;

        float idleTimer;
        float peckTimer;
        bool foraging;
        bool falling;
        bool flying;
        int pecks;
        int hops;

        public NetRef<Item> item = new();

        public CrowCompanion()
        {
            // caw caw
        }

        public override void InitNetFields()
        {
            base.InitNetFields();

            this.NetFields
                .AddField(item);
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
            if (Game1.random.NextBool(0.25))
                location?.localSound("crow");

            Game1.createMultipleItemDebris(
                item: ItemRegistry.Create(ModEntry.ModData.ItemFeather, Game1.random.Next(0, 5)),
                pixelOrigin: this.Position + new Vector2(0, -this.height),
                direction: -1,
                location: location,
                groundLevel: (int)this.Position.Y);

            this.foraging = false;
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
            this.foraging = false;

            if (this.IsLocal)
                this.lerp = -1;

            this.ResetIdleTimer();
        }

        public void Peck(GameLocation location)
        {
            location?.localSound("tinyWhip");

            ++this.pecks;
            this.peckTimer = 300;
        }

        public void StartForaging(GameLocation location)
        {
            this.foraging = true;
            this.pecks = 0;

            this.ResetIdleTimer();
        }

        public void StopForaging(GameLocation location)
        {
            this.pecks = 0;
            this.foraging = false;

            // set held item
            this.item.Value = this.GetForageItem(location);

            // fly to player to avoid lerp snap
            this.StartFlying(location);
        }

        public Item GetForageItem(GameLocation location)
        {
            if (Game1.random.NextBool(0.1) && location.tryGetRandomArtifactFromThisLocation(this.Owner, Game1.random, 0.5) is Item artefact)
                return artefact;
            return ItemRegistry.Create(StardewValley.Object.woodQID, Game1.random.Next(2, 10));
        }

        public void DropForageItem(GameLocation location)
        {
            Game1.createItemDebris(
                item: this.item.Value,
                pixelOrigin: this.Position + new Vector2(0, -this.height),
                direction: -1,
                location: location,
                groundLevel: (int)this.Position.Y);
            this.item.Value = null;
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

                ++this.hops;
                this.hops %= 4;

                // start flying on repeat hops
                var location = this.Owner.currentLocation;
                if (this.hops == 0 && Game1.random.NextBool(0.2) && this.CanFlyHere(location))
                    this.StartFlying(location);

                this.peckTimer = 0;

                this.ResetIdleTimer();
            }
        }

        public override void Update(GameTime time, GameLocation location)
        {
            var ms = (float)time.ElapsedGameTime.TotalMilliseconds;

            if (this.item.Value is not null && this.idleTimer > 2000 && Vector2.Distance(this.Position, this.OwnerPosition) < Game1.tileSize * 2)
                this.DropForageItem(location);

            if (this.flying)
            {
                this.idleTimer += ms;

                if (this.IsLocal)
                {
                    // caw caw
                    if (Game1.random.NextBool(0.0001))
                        location.localSound("crow");
                }

                // flying movement
                this.UpdateFlyingMovement();

                // update fields
                this.hopEvent.Poll();
            }
            else if (this.foraging)
            {
                this.idleTimer += ms;

                // active pecking
                if (this.IsLocal)
                {
                    this.peckTimer = MathF.Max(0, this.peckTimer - ms);

                    if (this.idleTimer > 600)
                    {
                        this.ResetIdleTimer();
                        if (this.pecks >= 3 && Game1.random.NextBool(0.5))
                        {
                            this.StopForaging(location);
                        }
                        else
                        {
                            this.Peck(location);
                        }
                    }
                }

                // update fields
                this.hopEvent.Poll();
            }
            else
            {
                // behave as a regular companion (hopping)
                base.Update(time, location);

                this.idleTimer += ms;

                if (this.IsLocal)
                {
                    this.peckTimer = MathF.Max(0, this.peckTimer - ms);

                    // idle pecking
                    if (this.idleTimer > 5000 && this.peckTimer <= 0 && Game1.random.NextBool(0.005))
                        this.Peck(location);

                    // forage if idle on ground
                    if (this.idleTimer > 5000 && this.peckTimer <= 0 && Game1.random.NextBool(0.05) && this.CanFlyHere(location))
                        this.StartForaging(location);

                    // stop falling after hitting the ground
                    if (this.height <= 0)
                        this.falling = false;
                }
            }
        }

        public void UpdateFlyingMovement()
        {
            var ms = (float)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
            if (this.target == default || Vector2.Distance(this.Position, this.target) < Game1.tileSize)
            {
                if (this.foraging)
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

                if (!this.foraging && this.idleTimer > 3000 && Game1.random.NextBool() && Vector2.Distance(this.Position, this.OwnerPosition) < Game1.tileSize * 3)
                {
                    // bird goes down
                    this.foraging = true;
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
            var scale = Game1.pixelZoom;
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

            source = this.flying || this.falling
                ? new Rectangle(
                    x: this.DefaultSourceArea.X + this.DefaultSourceArea.Width * (2 + (direction * frames) + (int)(ms / 150 % frames)),
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
                + (this.flying ? new Vector2(MathF.Sin(ms / 300) * 1 * scale) : Vector2.Zero) // flying wobble
                + new Vector2(0, -source.Height * scale / 2) // align sprite with ground
                + new Vector2(0, -this.height * scale)); // hopping or flying offset

            b.Draw(
                texture: texture,
                position: position,
                sourceRectangle: source,
                color: color,
                rotation: 0,
                origin: origin,
                scale: scale,
                effects: effect,
                layerDepth: layerDepth);

            if (this.item.Value is Item item)
            {
                var data = ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId);
                item.drawInMenu(
                    spriteBatch: b,
                    location: position
                        + new Vector2(-data.GetSourceRect().Width * scale / 2, 0),
                    scaleSize: 1,
                    transparency: 1,
                    layerDepth: layerDepth + .0001f,
                    drawStackNumber: StackDrawType.Hide,
                    color: Color.White,
                    drawShadow: false);
            }
        }
    }
}
