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

		public Vector2 Aimpoint;
		public Vector2 TargetPosition;
        public NetEvent1Field<Vector2, NetVector2> LungeEvent;

        public CharacterData CharacterData;
		public VolleyballCharacterData VolleyballData;

        public float HitCooldown;

		public Vector2 LungeVelocity;
		public float LungeTimer;
		public float LungeCooldown;

        public float VolleyballSpeed => (this.Speed + this.addedSpeed) * this.VolleyballData.Speed;

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

			this.LungeEvent = new();

            this.NetFields
				.AddField(this.LungeEvent, nameof(this.LungeEvent));
            this.LungeEvent.onEvent += this.Lunge;
		}

		public void ResetVolleyballValues()
		{
			this.Aimpoint = Vector2.Zero;
			this.TargetPosition = Vector2.Zero;
            this.HitCooldown = 0;
            this.LungeVelocity = Vector2.Zero;
            this.LungeTimer = this.LungeCooldown = 0;
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

            this.HitCooldown = 1000;
        }

		public void TryLunge()
		{
            // don't lunge while jumping or lunging
            if (this.yJumpOffset < 0 || this.LungeCooldown > 0)
				return;

			var centre = VolleyballLocation.PlayAreaCentre;
            var origin = this.StandingPixel.ToVector2();
            var target = this.TargetPosition;

            // don't lunge at opposite side
            if (Math.Sign(this.Volleyball.Position.X - centre.X) != Math.Sign(origin.X - centre.X))
                return;

            // don't lunge into the net
            if (Math.Abs(target.X - centre.X) < Game1.tileSize * 2)
                return;

            var distance = Math.Abs(Vector2.Distance(origin, this.Volleyball.Position.Value));
            var isBallFarXY = distance > Game1.tileSize * this.Volleyball.Velocity.Value.Length() / this.VolleyballSpeed * 2;
            var isBallVeryFarXY = distance > Game1.tileSize * this.Volleyball.Velocity.Value.Length() / this.VolleyballSpeed * 4;
            var wannaLunge = isBallFarXY && !isBallVeryFarXY // within close-enough range but not hopeless
                && this.Volleyball.zVelocity.Value < 0.1; // ball is falling

            if (wannaLunge && (Game1.random.NextSingle() < this.VolleyballData.SpikePreference || this.Volleyball.Velocity.Value.Length() > 9f))
            {
                this.LungeEvent.Fire(Utils.Vector.MotionTo(origin, target) * (1 + this.VolleyballSpeed + this.VolleyballData.Speed));
            }
		}

        public void Lunge(Vector2 velocity)
        {
            Game1.playSound("throwDownITem"); // [sic]

            this.jump(this.VolleyballData.Jump / 2);

            var sum = this.VolleyballData.Speed - this.VolleyballData.Jump - this.VolleyballData.Responsiveness;

            this.LungeVelocity = velocity;
            this.LungeVelocity.Y *= -1;
            this.LungeTimer = 150 * (16 - sum);
            this.LungeCooldown = 750 * (16 - sum);
        }

        public void TryJump()
        {
            // don't jump while jumping or lunging
            if (this.yJumpOffset != 0 || this.LungeTimer > 0)
                return;

            var centre = VolleyballLocation.PlayAreaCentre;
            var origin = this.StandingPixel.ToVector2();
            var bounds = this.Volleyball.GetCharacterCollisionArea(this);

            var isBallNearXY = Math.Abs(Vector2.Distance(origin, this.Volleyball.Position.Value)) < this.Volleyball.CollisionSize + this.Volleyball.GetCharacterCollisionArea(this).Width / 2;
            var isBallNearZ = this.Volleyball.zPosition.Value > bounds.Height
				&& this.Volleyball.zPosition.Value < bounds.Height * 1.5f;
            var isFirstHit = this.Volleyball.HitCount.Value == 0;
            var isLastHit = this.Volleyball.TeamHitCount.Value == 2 || this.Volleyball.HitCount.Value == 1;
            var wannaJump = isBallNearXY && isBallNearZ
				&& this.Volleyball.TravelTime.Value > 500
                && (Math.Abs(centre.X - origin.X) < Game1.tileSize * 2 // very close to net, or
                || (Math.Abs(centre.X - origin.X) < VolleyballLocation.PlayArea.Width * Game1.tileSize / 4 // close to net
                    && this.Volleyball.zVelocity.Value < 0.1) // ball is falling
                || isFirstHit); // serving

            if (wannaJump)
            {
                this.jump(jumpVelocity: 4 * this.VolleyballData.Jump - (1 - this.VolleyballData.Weight));
                this.yJumpGravity = -0.25f * this.VolleyballData.Weight;
            }
        }

		public void UpdateVolleyballAimpoint()
		{
			Vector2 oldPosition = this.TargetPosition;
			Vector2 newPosition;

            var players = ((VolleyballLocation)this.Volleyball.Location.Value).Players;
            var other = players[(players.IndexOf(this) + players.Count / 2) % players.Count];
            var isLastHit = this.Volleyball.TeamHitCount.Value == 2 || this.Volleyball.HitCount.Value == 1;

            var centre = VolleyballLocation.PlayAreaCentre;
            var origin = this.StandingPixel.ToVector2();
            var otherOrigin = other.StandingPixel.ToVector2();

			if (!isLastHit // responding to spike, try to bounce in place and return
                && this.Volleyball.LastHitBy.Value != this.Name
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

            this.Aimpoint = newPosition;
		}

		public void UpdateVolleyballTargetPosition()
		{
			// don't change target position while lunging
			if (this.TargetPosition != Vector2.Zero && this.LungeTimer > 0)
				return;

            var centre = VolleyballLocation.PlayAreaCentre;
            var origin = this.StandingPixel.ToVector2();
            var sign = Math.Sign(origin.X - centre.X);

			Vector2 oldPosition = this.TargetPosition;
			Vector2 newPosition;

            if (this.Volleyball.LastHitBy.Value == this.Name && this.Volleyball.LastHitNet.Value)
            {
				// rebound panic
				newPosition = this.Volleyball.Position.Value // current position
                    + this.Volleyball.Velocity.Value * Game1.tileSize / this.VolleyballSpeed * 2; // predicted position
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
                    newPosition = new Vector2(centre.X + sign * VolleyballLocation.PlayArea.Width * Game1.tileSize / 4, centre.Y);
                }
            }
			else
			{
                // volleyball on this side of net, move to ball
				newPosition = this.Volleyball.Position.Value // current position
					+ this.Volleyball.Velocity.Value * Game1.tileSize / this.VolleyballSpeed * 2; // predicted position
            }
            this.TargetPosition = newPosition;
		}

		public override void update(GameTime time, GameLocation location, long id, bool move)
		{
			base.update(time, location, id, move);

			// update strategy
			int rate = (int)(30 / this.VolleyballData.Responsiveness);
			if (Context.IsMainPlayer && this.Volleyball?.IsInPlay.Value is true && (time.TotalGameTime.TotalMilliseconds / rate) % 16 < 1)
			{
				this.TryJump();
				this.TryLunge();
				this.UpdateVolleyballAimpoint();
				this.UpdateVolleyballTargetPosition();
            }

            // update fields
            this.LungeEvent.Poll();
        }

		public override void updateMovement(GameLocation location, GameTime time)
        {
            if (this.HitCooldown > 0)
                this.HitCooldown = Math.Max(0, this.HitCooldown - (float)time.ElapsedGameTime.TotalMilliseconds);

            // lunge behaviours
            if (Math.Abs(this.LungeVelocity.X) > 0.1f || Math.Abs(this.LungeVelocity.Y) > 0.1f)
                this.LungeVelocity -= this.LungeVelocity / (float)time.ElapsedGameTime.TotalMilliseconds * 0.5f;
            if (this.LungeTimer > 0)
                this.LungeTimer = Math.Max(0, this.LungeTimer - (float)time.ElapsedGameTime.TotalMilliseconds);
            if (this.LungeCooldown > 0)
                this.LungeCooldown = Math.Max(0, this.LungeCooldown - (float)time.ElapsedGameTime.TotalMilliseconds);

            if (this.LungeTimer > 0)
            {
                // lunging movement

                // don't move while recovering from a lunge
                if (Math.Abs(this.LungeVelocity.X) <= 0.1f && Math.Abs(this.LungeVelocity.Y) <= 0.1f)
                return;

                this.setTrajectory(this.LungeVelocity);

                this.MovePosition(time, Game1.viewport, location);
            }
            else
            {
                // regular movement

                // don't move after round ends
                if (this.Volleyball?.IsInPlay.Value is not true)
				return;

			    var target = this.TargetPosition;
			    var origin = this.StandingPixel.ToVector2();
                var velocity = Utils.Vector.MotionTo(origin, target) * this.VolleyballSpeed;
                var distanceToStop = velocity.Length();

                // don't move past target
                if (origin == target || target == Vector2.Zero || Math.Abs(Vector2.Distance(origin, target)) <= distanceToStop)
                    return;

                velocity.Y *= -1;
                this.setTrajectory(velocity);

                this.MovePosition(time, Game1.viewport, location);
			}
		}

		public override void draw(SpriteBatch b, float alpha = 1)
		{
            b.Draw(this.Sprite.Texture, this.getLocalPosition(Game1.viewport) + new Vector2(Game1.tileSize / 2, Game1.tileSize + Game1.tileSize / 4 + yJumpOffset * 2), new Rectangle(Sprite.SourceRect.X, Sprite.SourceRect.Y, Sprite.SourceRect.Width, Sprite.SourceRect.Height / 2), Color.White, 0, new Vector2(Game1.tileSize / 2, Game1.tileSize * 3 / 2) / 4f, Math.Max(0.2f, scale.Value) * Game1.pixelZoom, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, this.StandingPixel.Y / 10000f);

            this.DrawShadow(b);
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
