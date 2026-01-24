using Netcode;
using StardewModdingAPI;
using StardewValley.GameData.Characters;
using StardewValley.TokenizableStrings;
using System;

namespace Hikawa.Volleyball
{
	public class VolleyballNPC : Character
	{
		public Volleyball Volleyball;

		public NetVector2 Aimpoint;
		public NetVector2 TargetPosition;

		public CharacterData CharacterData;
		public VolleyballCharacterData VolleyballData;

		public float LungeSpeed;
		public float LungeTimer;
		public float LungeCooldown;

        public VolleyballNPC(string name, CharacterData characterData, VolleyballCharacterData volleyballData, Vector2? position = null) : base(
			sprite: new AnimatedSprite(volleyballData.TextureId),
			position: position ?? Vector2.Zero,
			speed: 1,
			name: name)
		{
			this.CharacterData = characterData;
			this.VolleyballData = volleyballData;

			this.displayName = TokenParser.ParseText(characterData.DisplayName);

            this.Sprite.SpriteWidth = characterData.Size.X;
            this.Sprite.SpriteHeight = characterData.Size.Y;
			this.Sprite.UpdateSourceRect();
        }

        public static VolleyballNPC MakeFor(string baseName)
        {
            VolleyballNPC character = null;
            if (Game1.characterData.TryGetValue(baseName, out var characterData) && ModEntry.VolleyballData.Value.Characters.TryGetValue(baseName, out var volleyballData))
            {
                character = new(baseName + ModEntry.ModData.NpcVolleyballSuffix, characterData, volleyballData);
            }
            return character;
        }

        protected override void initNetFields()
		{
			base.initNetFields();

			this.Aimpoint = new(Vector2.Zero);
			this.TargetPosition = new(Vector2.Zero);

			this.NetFields
				.AddField(this.Aimpoint, nameof(this.Aimpoint))
				.AddField(this.TargetPosition, nameof(this.TargetPosition));
		}

		public void ResetVolleyballValues()
		{
			this.Aimpoint.Set(Vector2.Zero);
			this.TargetPosition.Set(Vector2.Zero);
		}

		public void OnVolleyballHit(ref float power)
        {
            power = (1 + this.yJumpOffset * 0.1f) * this.VolleyballData.Power;

            var centre = VolleyballLocation.PlayAreaCentre;
            var origin = this.StandingPixel.ToVector2();

            if (Math.Abs(centre.X - origin.X) > VolleyballLocation.PlayArea.Width * Game1.tileSize / 4) // far from net
                power += 1 + Game1.random.NextSingle();
			else if (Game1.random.NextSingle() < this.VolleyballData.SpikePreference && this.yJumpOffset < Game1.tileSize / 2) // jumping spike
				power += 3;
        }

		public void TryLunge()
		{
			// TODO: FIX LUNGE RUBBER BANDING

			// cannot lunge while jumping or lunging
			if (this.yJumpOffset != 0 || this.LungeCooldown > 0)
				return;

			var centre = VolleyballLocation.PlayAreaCentre;
            var origin = this.StandingPixel.ToVector2();

            // must be on same side of net
            if (Math.Sign(this.Volleyball.Position.X - centre.X) != Math.Sign(origin.X - centre.X))
                return;

            var dist = Math.Abs(Vector2.Distance(origin, this.Volleyball.Position.Value));
            bool isBallFarXY = dist > Game1.tileSize * 2;
            bool isBallVeryFarXY = dist > Game1.tileSize * 5;
			bool wannaLunge = isBallFarXY && !isBallVeryFarXY // within close-enough range but not hopeless
                && this.Volleyball.zVelocity.Value < 0.1; // ball is falling

            if (wannaLunge && Game1.random.NextSingle() < this.VolleyballData.SpikePreference / 2)
            {
                Game1.playSound("throwDownITem"); // [sic]

                this.jump(this.VolleyballData.Jump / 2);

                var sum = this.VolleyballData.Speed - this.VolleyballData.Jump - this.VolleyballData.Responsiveness;

                this.LungeSpeed = this.VolleyballData.Speed;
                this.LungeTimer = 250 * (16 - sum);
                this.LungeCooldown = 7500 * (16 - sum);
            }
		}

		public void TryJump()
        {
			// cannot jump while jumping or lunging
            if (this.yJumpOffset != 0 || this.LungeTimer > 0)
                return;

            var centre = VolleyballLocation.PlayAreaCentre;
            var origin = this.StandingPixel.ToVector2();
            var bounds = this.Volleyball.GetCharacterCollisionArea(this);

            bool isBallNearXY = Math.Abs(Vector2.Distance(origin, this.Volleyball.Position.Value)) < this.Volleyball.CollisionSize + this.Volleyball.GetCharacterCollisionArea(this).Width / 2;
            bool isBallNearZ = this.Volleyball.zPosition.Value > bounds.Height
				&& this.Volleyball.zPosition.Value < bounds.Height * 1.5f;
            bool wannaJump = isBallNearXY && isBallNearZ
				&& this.Volleyball.TravelTime.Value > 500
                && (Math.Abs(centre.X - origin.X) < Game1.tileSize * 2 // very close to net, or
                || (Math.Abs(centre.X - origin.X) < VolleyballLocation.PlayArea.Width * Game1.tileSize / 4 // close to net
                    && this.Volleyball.zVelocity.Value < 0.1) // ball is falling
                || this.Volleyball.HitCount.Value == 0); // first hit serve

            if (wannaJump)
            {
                // TODO: DEBUG: HIT BEHAVIOUR
                this.jump(jumpVelocity: 4 * this.VolleyballData.Jump - (1 - this.VolleyballData.Weight));
                this.yJumpGravity = -0.25f * this.VolleyballData.Weight;
            }
        }

		public void UpdateVolleyballAimpoint()
		{
			Vector2 oldPosition = this.TargetPosition.Value;
			Vector2 newPosition;

            var players = ((VolleyballLocation)this.Volleyball.Location.Value).Players;
            var other = players[(players.IndexOf(this) + players.Count / 2) % players.Count];

            var centre = VolleyballLocation.PlayAreaCentre;
            var origin = this.StandingPixel.ToVector2();
            var otherOrigin = other.StandingPixel.ToVector2();

			if (this.Volleyball.LastHitBy.Value != this.Name // responding to spike, try to bounce in place and return
				&& Math.Abs(centre.X - origin.X) < Game1.tileSize * 2)
			{
				// near position
				newPosition = origin;// + Utils.Vector.MotionTo(origin, centre) * Game1.tileSize;
            }
            else if (this.Volleyball.LastHitBy.Value == this.Name // stop juggling until you lose
                && Math.Abs(centre.X - origin.X) < Game1.tileSize * 1)
            {
                // somewhere far away
                int sign = Math.Sign(origin.X - centre.X);
                var area = VolleyballLocation.PlayArea;
                area = new Rectangle(area.X * Game1.tileSize, area.Y * Game1.tileSize, area.Width * Game1.tileSize / 2, area.Height * Game1.tileSize / 2);
                area.X += (int)(area.Width * (sign * 0.5f - 0.5f)) // half of width depending on side, plus another half to be on outside
                    + (int)(area.Width * sign * 0.5f); // plus another half to be on outside half
                newPosition = Utility.getRandomPositionInThisRectangle(area, Game1.random);
            }
            else if (Game1.random.NextSingle() < 0.5f && Math.Abs(Vector2.Distance(origin, otherOrigin)) < Game1.tileSize * 3)
            {
                // aim over nearby enemy player
                newPosition = origin + Utils.Vector.MotionTo(origin, otherOrigin) * Game1.tileSize * 5;
            }
			else if (Game1.random.NextSingle() < 0.5f)
			{
				// far from enemy player
				newPosition = new Vector2(otherOrigin.X, centre.Y) + new Vector2(0, otherOrigin.Y - centre.Y) / 2;
            }
			else if (Game1.random.NextSingle() < 0.5f)
			{
				// random position on other side
                var sign = Math.Sign(origin.X - centre.X);
				var area = VolleyballLocation.PlayArea;
				area = new Rectangle(area.X * Game1.tileSize, area.Y * Game1.tileSize, area.Width * Game1.tileSize / 2, area.Height * Game1.tileSize / 2);
				area.X += (int)(area.Width * (sign * 0.5f - 0.5f)); // half of width depending on side
				newPosition = Utility.getRandomPositionInThisRectangle(area, Game1.random);
            }
            else
			{
				newPosition = Game1.player.Position;
            }

            this.Aimpoint.Set(newPosition);

			Log.D($"Aimpoint(from: {oldPosition}, to: {this.Aimpoint.Value})");
		}

		public void UpdateVolleyballTargetPosition()
		{
			// don't change target position while lunging
			if (this.TargetPosition.Value != Vector2.Zero && this.LungeSpeed > 0)
				return;

            var centre = VolleyballLocation.PlayAreaCentre;
            var origin = this.StandingPixel.ToVector2();
            var sign = Math.Sign(origin.X - centre.X);

			Vector2 oldPosition = this.TargetPosition.Value;
			Vector2 newPosition;

            if (this.Volleyball.LastHitBy.Value == this.Name && this.Volleyball.LastHitNet.Value) // stop hitting the net
            {
				// panic
				newPosition = this.Volleyball.Position.Value;
            }
            else if (sign * this.Volleyball.Position.X < sign * centre.X)
			{
				// volleyball on opposite side of net, flying towards us, predict ball
				if (sign == Math.Sign(this.Volleyball.Velocity.Value.X) // moving towards this side
					&& Math.Abs(centre.X - this.Volleyball.Position.X) < Game1.tileSize * 2) // near to net
				{
					newPosition = this.Volleyball.Position.Value + this.Volleyball.Velocity.Value * Game1.tileSize * 0.75f; // move to a reasonable possible landing position
				}
				else
			{
				// volleyball on opposite side of net, move to dummy pos
                Log.D($"TargetPosition(from: {oldPosition}, to: {newPosition}) OPP");
                    newPosition = new Vector2(centre.X + sign * VolleyballLocation.PlayArea.Width * Game1.tileSize / 4, centre.Y);
                }
            }
			else
			{
                // volleyball on this side of net, move to ball
                newPosition =
                    // Position outward from centre
                    // new Vector2(x: centre.X + Math.Sign(centre.X - origin.X) * origin.X, y: 0) +
                    // Move to the predicted path of the ball
                    //(this.Volleyball.Velocity * );

                    this.Volleyball.Position.Value;
                Log.D($"TargetPosition(from: {oldPosition}, to: {newPosition}) THIS");
            }
            this.TargetPosition.Value = newPosition;

		}

		public override void update(GameTime time, GameLocation location, long id, bool move)
		{
			base.update(time, location, id, move);

			// Update strategy
			int rate = (int)(30 / this.VolleyballData.Responsiveness);
			if (Context.IsMainPlayer && this.Volleyball?.IsInPlay.Value is true && (time.TotalGameTime.TotalMilliseconds / rate) % 16 < 1)
			{
				Log.D($"ThinkAt(rate: {this.VolleyballData.Responsiveness}, time: {time.TotalGameTime.TotalMilliseconds}, at: {this.Position})");
				this.TryJump();
				this.TryLunge();
				this.UpdateVolleyballAimpoint();
				this.UpdateVolleyballTargetPosition();
			}
		}

		public override void updateMovement(GameLocation location, GameTime time)
		{
            // lunge behaviours
            if (this.LungeSpeed > 0)
                this.LungeSpeed = Math.Max(0, this.LungeSpeed - (float)time.ElapsedGameTime.TotalMilliseconds * 0.005f);
            if (this.LungeTimer > 0)
                this.LungeTimer = Math.Max(0, this.LungeTimer - (float)time.ElapsedGameTime.TotalMilliseconds);
            if (this.LungeCooldown > 0)
                this.LungeCooldown = Math.Max(0, this.LungeCooldown - (float)time.ElapsedGameTime.TotalMilliseconds);

            // do not move while recovering from a lunge
            if (this.LungeSpeed <= 0 && this.LungeTimer > 0)
                return;

			// do not move after round ends unless still lunging
			if (this.Volleyball?.IsInPlay.Value is not true || this.LungeSpeed > 0)
				return;

			var target = this.TargetPosition.Value;
			var origin = this.StandingPixel.ToVector2();

			this.addedSpeed = (this.Volleyball.LastHitBy.Value == this.Name && this.Volleyball.LastHitNet.Value) ? 0.5f : 0;
            var velocity = Utils.Vector.MotionTo(origin, target);
			var distance = velocity * (this.Speed + this.addedSpeed + this.LungeSpeed) * this.VolleyballData.Speed;
            var distanceToStop = distance.Length();

            // do not stop while lunging
            if (target != Vector2.Zero && (this.LungeTimer > 0 || Math.Abs(Vector2.Distance(origin, target)) > distanceToStop))
			{
				this.Position += distance;
			}
		}

		public override void draw(SpriteBatch b, float alpha = 1)
		{
            b.Draw(this.Sprite.Texture, this.getLocalPosition(Game1.viewport) + new Vector2(Game1.tileSize / 2, Game1.tileSize + Game1.tileSize / 4 + yJumpOffset * 2), new Rectangle(Sprite.SourceRect.X, Sprite.SourceRect.Y, Sprite.SourceRect.Width, Sprite.SourceRect.Height / 2), Color.White, 0, new Vector2(Game1.tileSize / 2, Game1.tileSize * 3 / 2) / 4f, Math.Max(0.2f, scale.Value) * Game1.pixelZoom, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, this.StandingPixel.Y / 10000f);

            this.DrawShadow(b);

			// npc targetposition
			Vector2 from = this.getLocalPosition(Game1.viewport);
            Vector2 target = Game1.GlobalToLocal(Game1.viewport, this.TargetPosition.Value);
            Rectangle source = AssetManager.ExtraSpritesVolleyballAimpointArea;
            source.X += Math.Clamp(((VolleyballLocation)this.Volleyball.Location.Value).Players.IndexOf(this), min: 0, max: 4) * source.Width;
			Utility.drawLineWithScreenCoordinates((int)from.X, (int)from.Y, (int)target.X, (int)target.Y, b, Color.White);
            b.Draw(
                texture: ModEntry.Sprites,
                sourceRectangle: source,
                position: target,
                color: Color.White,
                rotation: MathF.PI / 2,
                origin: Utility.PointToVector2(source.Size) / 2,
                scale: Game1.pixelZoom,
                effects: SpriteEffects.None,
                layerDepth: 1f);

            // npc aimpoint
            target = Game1.GlobalToLocal(Game1.viewport, this.Aimpoint.Value);
            source = AssetManager.ExtraSpritesVolleyballAimpointArea;
            source.X += Math.Clamp(((VolleyballLocation)this.Volleyball.Location.Value).Players.IndexOf(this), min: 0, max: 4) * source.Width;
            Utility.drawLineWithScreenCoordinates((int)from.X, (int)from.Y, (int)target.X, (int)target.Y, b, Color.White);
            b.Draw(
                texture: ModEntry.Sprites,
                sourceRectangle: source,
                position: target,
                color: Color.White,
                rotation: MathF.PI + MathF.PI / 2,
                origin: Utility.PointToVector2(source.Size) / 2,
                scale: Game1.pixelZoom,
                effects: SpriteEffects.None,
                layerDepth: 1f);
        }

		public override void draw(SpriteBatch b, int ySourceRectOffset, float alpha = 1)
		{
			this.draw(b, alpha);
		}

		public override void drawAboveAlwaysFrontLayer(SpriteBatch b)
		{
			base.drawAboveAlwaysFrontLayer(b);
		}

		public override void DrawShadow(SpriteBatch b)
		{
			base.DrawShadow(b);
		}
	}
}
