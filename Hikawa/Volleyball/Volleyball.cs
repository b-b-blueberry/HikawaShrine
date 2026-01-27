using Netcode;
using StardewModdingAPI;
using StardewValley.Network;
using System;
using System.Linq;

namespace Hikawa.Volleyball
{
	public class Volleyball : INetObject<NetFields>
	{
        public NetString BallType;
        public VolleyballBallData BallData;

        public Vector2 Tile => Vector2.Floor(this.Position.Value / Game1.tileSize);
		public Vector2 PositionDecay => new(x: -0.0005f, y: -0.0005f);
		public float zPositionDecay => -0.005f;

        public Texture2D Texture;
		public float Scale;
        public int CollisionSize;
        public int TravelTimeBeforeHit;

		public NetFields NetFields { get; } = new(nameof(Volleyball));

		public NetVector2 Position;
		public NetVector2 Velocity;

		public NetFloat zPosition;
		public NetFloat zVelocity;

        public NetFloat Rotation;
        public NetFloat RotationVelocity;

		public NetBool IsInPlay;
        public NetString LastHitBy;
        public NetBool LastHitNet;
        public NetInt TeamHitCount;
        public NetInt HitCount;
        public NetLocationRef Location;
        public NetEvent1Field<string, NetString> TouchPlayerEvent;
        public NetEvent1Field<Vector2, NetVector2> TouchGroundEvent;
		public NetInt TravelTime;
		public NetInt BouncesAllowed;
        public NetInt BouncesLeft;

        public VolleyballLocation VolleyballLocation => (VolleyballLocation)this.Location.Value;

        private Vector2 _velocityAccumulator = Vector2.Zero;
        private float _zVelocityAccumulator = 0;

		public Volleyball(VolleyballLocation location, VolleyballRules rules, Vector2? position = null)
        {
            this.BallType = new();

			this.Scale = Game1.pixelZoom;
			this.TravelTimeBeforeHit = 250;

			this.Position = new(position ?? Vector2.Zero);
			this.zPosition = new(0);

			this.Velocity = new(Vector2.Zero);
			this.zVelocity = new(0);

			this.Rotation = new(0);
			this.RotationVelocity = new(0);

			this.IsInPlay = new(false);
            this.LastHitBy = new();
            this.LastHitNet = new();
            this.Location = new(location);
			this.TouchPlayerEvent = new();
			this.TouchGroundEvent = new();
			this.TravelTime = new(0);
			this.TeamHitCount = new(0);
			this.HitCount = new(0);
			this.BouncesAllowed = new(3);
			this.BouncesLeft = new(this.BouncesAllowed.Value);

			this.NetFields.SetOwner(this)
				.AddField(this.Position, nameof(this.Position))
				.AddField(this.zPosition, nameof(this.zPosition))

				.AddField(this.Velocity, nameof(this.Velocity))
				.AddField(this.zVelocity, nameof(this.zVelocity))

				.AddField(this.Rotation, nameof(this.Rotation))
				.AddField(this.RotationVelocity, nameof(this.RotationVelocity))

				.AddField(this.IsInPlay, nameof(this.IsInPlay))
				.AddField(this.LastHitBy, nameof(this.LastHitBy))
				.AddField(this.LastHitNet, nameof(this.LastHitNet))
				.AddField(this.TravelTime, nameof(this.TravelTime))
                .AddField(this.TeamHitCount, nameof(this.TeamHitCount))
				.AddField(this.HitCount, nameof(this.HitCount))
                .AddField(this.TouchPlayerEvent, nameof(this.TouchPlayerEvent))
				.AddField(this.TouchGroundEvent, nameof(this.TouchGroundEvent))
				.AddField(this.BouncesAllowed, nameof(this.BouncesAllowed))
				.AddField(this.BouncesLeft, nameof(this.BouncesAllowed));

            this.BallType.fieldChangeEvent += (field, oldValue, newValue) =>
            {
                this.BallData = ModEntry.VolleyballData.Value.Balls[newValue];
                this.Texture = Game1.content.Load<Texture2D>(this.BallData.TextureId);
                this.CollisionSize = (int)((this.BallData.SourceArea.Width + this.BallData.SourceArea.Height) / 2 * this.Scale);
            };
            this.BallType.Value = rules.BallType;
		}

        public void Start(Vector3 position, Character character, bool isLeftSidePlayerStarting)
        {
            Log.D($"{nameof(Volleyball)} Start (chara: '{character?.Name}' left: {isLeftSidePlayerStarting}) {position}");

            // Starting position
            this.Position.Set(new Vector2(x: position.X, y: position.Y));
			this.zPosition.Set(position.Z);

            // Starting velocity
            Vector3 velocity = new(
                x: 0f,
                y: 0f,
                z: 4f);
			this.Hit(character: character, setVelocity: velocity, sound: this.BallData.HeavyHitSound);

			// Play
			this.IsInPlay.Set(true);
            this.HitCount.Set(0);
            this.TeamHitCount.Set(0);
            this.BouncesLeft.Set(this.BouncesAllowed.Value);
        }

        public void Hit(Character character, Vector3 setVelocity, string sound)
		{
			Log.D($"{nameof(Volleyball)} Hit (chara: '{character?.Name}' sound: '{sound}')");

			this.Velocity.Set(x: setVelocity.X, y: setVelocity.Y);
			this.zVelocity.Set(setVelocity.Z);

            ++this.HitCount.Value;
            ++this.TeamHitCount.Value;
			this.TravelTime.Set(0);
			this.LastHitBy.Set(character.Name);
            this.LastHitNet.Set(false);

			this.Location.Value.playSound(sound);
		}

        public void OnCollisionWithCharacter(Character character)
        {
            if (this.IsInPlay.Value)
			{
				Log.D($"{nameof(Volleyball)} CollisionWithCharacter (character: {character.Name})");

                // Scale power relative to distance of cursor from player
                Vector2 aimpoint = character == Game1.player
                    ? Utility.PointToVector2(new Point(Game1.viewport.Location.X, Game1.viewport.Location.Y) + Game1.getMousePosition(ui_scale: false))
                    : ((VolleyballNPC)character).Aimpoint.Value;
                // Vector2 velocity = Utils.Vector.MotionTo(character.Position, target: aimpoint) * 3f; // Set power
                Vector2 velocity = Utils.Vector.PointAt(character.Position, aimpoint) * 0.01f; // Variable power

                // TODO: FIX LIMIT BREAK
                // Limit power to a reasonable amount
				Vector2 limitedVelocity = character == Game1.player ? Vector2.Clamp(value1: velocity, min: new Vector2(-3f), max: new Vector2(3f)) : velocity;

                // Add bonus power for farmer spikes or character specials
				float addedPower = this.GetCharacterHitPower(character: character);
				Vector2 addedVelocity = Vector2.Normalize(limitedVelocity) * addedPower;

				this.Velocity.Set(limitedVelocity + addedVelocity);

				this.zVelocity.Set(Math.Abs(this.zVelocity.Value)
                    // players will add upwards velocity when first jumping
                    + character.yJumpVelocity * 0.1f
                    // players will add downwards velocity when at the peak of their jump
                    + (addedPower * character.yJumpOffset * 0.01f));

                // aim upwards when very close to net
                float range = Game1.tileSize * 2f;
                float dist = MathF.Abs(character.Position.X - VolleyballLocation.PlayAreaCentre.X);
                float up = (range - MathF.Min(range, dist)) / range * 1f;
                this.zVelocity.Value -= up;

                // Rotation
                this.RotationVelocity.Set(this.RotationVelocity.Value * -1 + 5f + 3f * addedPower);

                // Set travel time for checks to prevent instant rebound
                this.HitCount.Value++;
                if (this.LastHitBy is null || this.VolleyballLocation.Players.IndexOf(character) == this.VolleyballLocation.Players.IndexOf(this.VolleyballLocation.GetPlayer(this.LastHitBy.Value)) / 2)
                    this.TeamHitCount.Value++;
                else
                    this.TeamHitCount.Set(0);
				this.TravelTime.Set(0);
                this.LastHitNet.Set(false);
                this.TouchPlayerEvent.Fire(character.Name);
                this.LastHitBy.Set(character.Name);

				this.Location.Value.playSound(addedPower > 3 ? this.BallData.HitSound : this.BallData.SmallHitSound);

				Log.D($"_velocityAccumulator: start at {this.Velocity.Value} Z: {this.zVelocity.Value}");
				this._velocityAccumulator = this.Velocity.Value;
				this._zVelocityAccumulator = this.zVelocity.Value;
			}
        }

        public void OnCollisionWithNet()
        {
			Log.D($"{nameof(Volleyball)} CollisionWithNet()");

			// Bounce back towards player with no change in Z-axis velocity
            this.Velocity.Set(Utils.Vector.Abs(this.Velocity.Value) * (this.VolleyballLocation.GetPlayer(this.LastHitBy.Value).Position.X < VolleyballLocation.PlayAreaCentre.X ? -1 : 1));
            this.Velocity.X *= 0.5f; // prevent wild rebounds
			this.Position.Value += this.Velocity.Value;

			this.RotationVelocity.Value *= -0.5f;

            // Reset travel time for checks to prevent instant rebound
			this.TravelTime.Set(0);
            this.LastHitNet.Set(true);

			this.Location.Value.playSound(this.BallData.SmallHitSound);

			Log.D($"_velocityAccumulator: {this._velocityAccumulator} Z:{this._zVelocityAccumulator}");
			this._velocityAccumulator = Vector2.Zero;
			this._zVelocityAccumulator = 0;
		}

		public void OnCollisionWithGround()
        {
            if (this.BouncesLeft.Value > 0)
            {
                // Bounce ball
				this.BouncesLeft.Set(this.BouncesLeft.Value - 1);
				this.zPosition.Set(0);

                float bounceScale = (float)this.BouncesLeft.Value / this.BouncesAllowed.Value;

				this.Velocity.Set(x: this.Velocity.X * bounceScale, y: this.Velocity.Y * bounceScale);
				this.zVelocity.Set(Math.Abs(this.zVelocity.Value) * bounceScale);
                this.RotationVelocity.Set(this.RotationVelocity.Value * bounceScale);

                if (this.IsInPlay.Value)
                {
					this.TouchGroundEvent.Fire(this.Position.Value);
				}

				this.Location.Value.playSound(this.BallData.SmallHitSound);

				Log.D($"_velocityAccumulator: {this._velocityAccumulator} Z:{this._zVelocityAccumulator}");
				this._velocityAccumulator = Vector2.Zero;
                this._zVelocityAccumulator = 0;
			}
        }

        public float GetCharacterHitPower(Character character)
        {
            float power = 0;
            if (character is Farmer farmer)
            {
                // Farmers have added power when swinging or spiking, increasing with current height of jump
                if (farmer.UsingTool)
                    power = 3 - farmer.yJumpOffset * 0.1f;
            }
            else if (character is VolleyballNPC other)
            {
                // Characters have added power when jumping, scaling with their attributes
                other.OnVolleyballHit(ref power);
            }
            return power;
        }

        public Rectangle GetCharacterCollisionArea(Character character)
        {
            var source = character.GetBoundingBox();
            var area = source;
            area.Y -= source.Height / 2;
            area.Height += source.Height / 2;
            area.Inflate(source.Width / 4, source.Height / 4);
            if (character is Farmer farmer)
            {
                if (farmer.UsingTool)
                {
                    // increase general size by flat value when swinging tool
                    area.Inflate(source.Width / 4, source.Height / 4);

                    // area covers additional space in facing direction
                    if (farmer.FacingDirection % 2 == 0)
                    {
                        area.Y += (-1 + 2 * (farmer.FacingDirection / 2)) * source.Height / 2;
                    }
                    else
                    {
                        area.X -= (-1 + 2 * (farmer.FacingDirection / 2)) * source.Width / 2;
                    }
                }
                else if (farmer.yJumpOffset < 0)
                {
                    // increase general size scaled to jump height
                    // area is largest at peak of jump
                    area.Inflate(source.Width / 4 * -farmer.yJumpOffset / Game1.tileSize, source.Height / 4 * -farmer.yJumpOffset / Game1.tileSize);
                }
            }
            else if (character is VolleyballNPC other)
            {
                area.Inflate(source.Width / 2, source.Height / 2);
            }
            return area;
        }

        public Character IsCollidingWithCharacter()
        {
            return this.VolleyballLocation.Players.FirstOrDefault((Character c) =>
			{
                // hit cooldown for this character
                if (this.LastHitBy.Value == c.Name && this.TravelTime.Value < this.TravelTimeBeforeHit)
                    return false;

                Rectangle hitbox = this.GetCharacterCollisionArea(character: c);
                var size = this.CollisionSize / 2;
                var direction = Utils.Vector.MotionTo(this.Position.Value, hitbox.Center.ToVector2());


				bool isInReachXY = hitbox.Contains((int)(this.Position.X + direction.X * size), (int)(this.Position.Y + direction.Y * size));
				bool isInReachZ = Math.Abs(this.zPosition.Value + c.yJumpOffset * 2) < size;

                if (isInReachXY && isInReachZ)
				{
					Log.D($"hit {c.Name}" +
						$"\nbox (x: {hitbox.X} y: {hitbox.Y} width: {hitbox.Width} height: {hitbox.Height})" +
						$"\ndir (x: {direction.X:0} y: {direction.Y:0} size: {this.CollisionSize})" +
						$"\njump (this: {this.zPosition.Value:0.000} chara: {(c.yJumpOffset * 2):0.000} dist: {Math.Abs(this.zPosition.Value + c.yJumpOffset * 2)})");
				}

				return isInReachXY && isInReachZ;
			});
        }

        public bool IsCollidingWithNet()
        {
            // Ball must be lower than net height
			return this.zPosition.Value <= VolleyballLocation.NetSize.Y
                // Prevent instant rebound
                && this.TravelTime.Value > 100
				&& !this.LastHitNet.Value
                // Check difference in position between ball and centre based on combined ball and net size
                && Math.Abs(this.Position.X - VolleyballLocation.PlayAreaCentre.X) < /*VolleyballLocation.NetSize.X +*/ this.CollisionSize
                // Ignore collision if on opposite side of net to player who hit it
                && Math.Sign(this.VolleyballLocation.GetPlayer(this.LastHitBy.Value).Position.X - VolleyballLocation.PlayAreaCentre.X) == Math.Sign(this.Position.X - VolleyballLocation.PlayAreaCentre.X);
        }

        public bool Update(GameTime time)
        {
            // TODO: FARMHAND COLLIDES WITH BALL FOR CURSOR POSITION
            if (!Context.IsMainPlayer)
                return false;

			// Position behaviours
			this.UpdatePosition(time);

            // Play behaviours
            if (this.IsInPlay.Value)
            {
				this.TravelTime.Value += time.ElapsedGameTime.Milliseconds;
            }

			// Collision behaviours
			if (this.IsInPlay.Value && this.IsCollidingWithCharacter() is Character character)
			{
				this.OnCollisionWithCharacter(character: character);
			}
			else if (this.IsCollidingWithNet())
			{
				this.OnCollisionWithNet();
			}
            else if (this.zPosition.Value <= 0 && this.zVelocity.Value < 0.1)
            {
				this.OnCollisionWithGround();
            }

            return false;
        }

        public void UpdatePosition(GameTime time)
        {
            // Vector3 oldPosition = new(x: this.Position.X, y: this.Position.Y, z: this.zPosition.Value);

            float rotationDecay = this.IsInPlay.Value ? -(float)Math.CopySign(0.005f, this.Rotation.Value) : 0;

			this.Velocity.Value += this.Velocity.Value * this.PositionDecay * time.ElapsedGameTime.Milliseconds;
			this.zVelocity.Value = this.BouncesLeft.Value <= 0 && this.zPosition.Value <= 0 ? 0 : this.zVelocity.Value + this.zPositionDecay * time.ElapsedGameTime.Milliseconds;

			this.Position.Value += this.Velocity.Value;
			this.zPosition.Value += this.zVelocity.Value;
			this.RotationVelocity.Value = this.BallData.IsFloaty ? this.zVelocity.Value : this.RotationVelocity.Value - rotationDecay * time.ElapsedGameTime.Milliseconds;
			this.Rotation.Value += this.RotationVelocity.Value / 360;
            /*
            if (oldPosition != new Vector3(x: this.Position.X, y: this.Position.Y, z: this.zPosition.Value))
            {
                Log.D(
                    $" P (X: {this.Position.X:0} Y: {this.Position.Y:0} Z: {this.zPosition.Value:0})" +
                    $" V (X: {this.Velocity.X:0.000} Y: {this.Velocity.Y:0.000} Z: {this.zVelocity.Value:0.000})");
            }
            */

            if (this._velocityAccumulator != Vector2.Zero)
            {
				this._velocityAccumulator += this.Velocity.Value;
                this._zVelocityAccumulator += this.zVelocity.Value;
			}
        }

        public void Draw(SpriteBatch b)
        {
            // Sprite
            b.Draw(
                texture: this.Texture,
                position: Game1.GlobalToLocal(
                    viewport: Game1.viewport,
                    globalPosition: this.Position.Value
                        + new Vector2(x: 0, y: -this.zPosition.Value)
                        + new Vector2(x: 0, y: -this.BallData.SourceArea.Height) / 2 * Game1.pixelZoom
                        ),
                sourceRectangle: this.BallData.SourceArea,
                color: Color.White,
                rotation: this.Rotation.Value,
                origin: this.BallData.SourceArea.Size.ToVector2() / 2,
                scale: this.Scale,
                effects: SpriteEffects.None,
                layerDepth: (this.Position.Value.Y + 96f) / 10000f);
        }
    }
}
