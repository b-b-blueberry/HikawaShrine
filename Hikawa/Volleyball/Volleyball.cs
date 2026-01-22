using Netcode;
using StardewModdingAPI;
using StardewValley.Network;
using System;
using System.Linq;

namespace Hikawa.Volleyball
{
	public class Volleyball : INetObject<NetFields>
	{
        public enum Style
        {
            Beachball,
            Volleyball
        }

        public Rectangle SourceArea => AssetManager.ExtraSpritesVolleyballArea;
        public Vector2 Tile => Vector2.Floor(this.Position.Value / Game1.tileSize);
		public Vector2 PositionDecay => new(x: -0.0005f, y: -0.0005f);
		public float zPositionDecay => -0.005f;

		public float Scale;
        public int CollisionSize;
        public int TravelTimeBeforeHit;
		public string SmallHitSound;
		public string HitSound;
		public string HeavyHitSound;
        public Style VisualStyle;

		public NetFields NetFields { get; } = new(nameof(Volleyball));

		public NetVector2 Position;
		public NetVector2 Velocity;

		public NetFloat zPosition;
		public NetFloat zVelocity;

        public NetFloat Rotation;
        public NetFloat RotationVelocity;

		public NetBool IsInPlay;
        public NetString LastHitBy;
        public NetLocationRef Location;
        public NetEvent0 TouchPlayerEvent;
        public NetEvent1Field<Vector2, NetVector2> TouchGroundEvent;
		public NetInt TravelTime;
		public NetInt BouncesAllowed;
        public NetInt BouncesLeft;

        private Vector2 _velocityAccumulator = Vector2.Zero;
        private float _zVelocityAccumulator = 0;

		public Volleyball(VolleyballLocation location, Vector2? position = null, Style style = Style.Beachball)
        {
			this.LastHitBy = new();

			this.Scale = Game1.pixelZoom;
            this.CollisionSize = (int)(16 * this.Scale);
			this.TravelTimeBeforeHit = 500;
			this.SmallHitSound = "bob";
			this.HitSound = "pickUpItem";
			this.HeavyHitSound = "throwDownITem"; // [sic]
            this.VisualStyle = style;

			this.Position = new(position ?? Vector2.Zero);
			this.zPosition = new(0);

			this.Velocity = new(Vector2.Zero);
			this.zVelocity = new(0);

			this.Rotation = new(0);
			this.RotationVelocity = new(0);

			this.IsInPlay = new(false);
            this.LastHitBy = new();
            this.Location = new(location);
			this.TouchPlayerEvent = new();
			this.TouchGroundEvent = new();
			this.TravelTime = new(0);
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
				.AddField(this.TouchPlayerEvent, nameof(this.TouchPlayerEvent))
				.AddField(this.TouchGroundEvent, nameof(this.TouchGroundEvent))
				.AddField(this.TravelTime, nameof(this.TravelTime))
				.AddField(this.BouncesAllowed, nameof(this.BouncesAllowed))
				.AddField(this.BouncesLeft, nameof(this.BouncesAllowed));
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
			this.Hit(character: character, setVelocity: velocity, sound: this.HeavyHitSound);

			// Play
			this.IsInPlay.Set(true);
            this.BouncesLeft.Set(this.BouncesAllowed.Value);
        }

        public void Hit(Character character, Vector3 setVelocity, string sound)
		{
			Log.D($"{nameof(Volleyball)} Hit (chara: '{character?.Name}' sound: '{sound}')");

			this.Velocity.Set(x: setVelocity.X, y: setVelocity.Y);
			this.zVelocity.Set(setVelocity.Z);

			this.TravelTime.Set(0);
			this.LastHitBy.Set(character.Name);

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

                // Set Z-axis velocity to move upwards, where jumping players using specials will add downwards velocity
				this.zVelocity.Set(Math.Abs(this.zVelocity.Value) + (addedPower * character.yJumpOffset * 0.01f));

                // Rotation
                this.RotationVelocity.Set(this.RotationVelocity.Value * -1 + 5f + 3f * addedPower);

                // Set travel time for checks to prevent instant rebound
				this.TravelTime.Set(0);
				this.LastHitBy.Set(character.Name);
                this.TouchPlayerEvent.Fire();

				this.Location.Value.playSound(this.SmallHitSound);

				Log.D($"_velocityAccumulator: start at {this.Velocity.Value} Z: {this.zVelocity.Value}");
				this._velocityAccumulator = this.Velocity.Value;
				this._zVelocityAccumulator = this.zVelocity.Value;
			}
        }

        public void OnCollisionWithNet()
        {
			Log.D($"{nameof(Volleyball)} CollisionWithNet()");

			// Bounce back towards player with no change in Z-axis velocity
			this.Velocity.Set(Utils.Vector.Abs(this.Velocity.Value) * (((VolleyballLocation)this.Location.Value).GetPlayer(this.LastHitBy.Value).Position.X < VolleyballLocation.PlayAreaCentre.X ? -1 : 1));
			this.Position.Value += this.Velocity.Value;

			this.RotationVelocity.Value *= -0.5f;

            // Reset travel time for checks to prevent instant rebound
			this.TravelTime.Set(0);

			this.Location.Value.playSound(this.SmallHitSound);

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

                // End round on touch ground
                if (this.IsInPlay.Value)
                {
					this.IsInPlay.Set(false);
					this.TouchGroundEvent.Fire(this.Position.Value);
				}

				this.Location.Value.playSound(this.SmallHitSound);

				Log.D($"_velocityAccumulator: {this._velocityAccumulator} Z:{this._zVelocityAccumulator}");
				this._velocityAccumulator = Vector2.Zero;
                this._zVelocityAccumulator = 0;
			}
        }

        public float GetCharacterHitPower(Character character)
        {
            if (character is Farmer farmer)
            {
                // Farmers have added power when swinging or spiking, increasing with current height of jump
                return farmer.UsingTool ? 3 - farmer.yJumpOffset * 0.1f : 0;
            }
            else if (character is VolleyballNPC other)
            {
                // Characters have added power when jumping, scaling with their attributes
                return (1 + other.yJumpOffset * 0.1f) * other.VolleyballData.Power;
            }
            return 0;
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
            return ((VolleyballLocation)this.Location.Value).Players.FirstOrDefault((Character c) =>
			{
				// TODO: FIX: collision with player on hit to compass NE/ENE
				Rectangle bounds = this.GetCharacterCollisionArea(character: c);
                Vector2 distance = Utility.PointToVector2(bounds.Center) - this.Position.Value;

				bool isInReachXY = (Math.Abs(distance.X) + Math.Abs(distance.Y)) / 2 < this.CollisionSize + (bounds.Width + bounds.Height) / 2;
				bool isInReachZ = Math.Abs(this.zPosition.Value + c.yJumpOffset * 2) < this.CollisionSize / 2;
                bool isCollisionOnCooldown = this.LastHitBy.Value == c.Name && this.TravelTime.Value < this.TravelTimeBeforeHit;

                Vector2 motion = Utils.Vector.MotionTo(origin: this.Position.Value, target: Utility.PointToVector2(bounds.Center));
                Vector2 scaledMotion = motion * this.CollisionSize / 2;
                Vector2 positionAfterScaledMotion = this.Position.Value + scaledMotion;

				isInReachXY = bounds.Contains(positionAfterScaledMotion);

                if (isInReachXY && isInReachZ && !isCollisionOnCooldown)
				{
					Log.D(
						$"\nrect (x: {bounds.X} y: {bounds.Y} width: {bounds.Width} height: {bounds.Height})" +
						$"\ndist (x: {distance.X:0} y: {distance.Y:0} size: {this.CollisionSize})" +
						$"\njump (this: {this.zPosition.Value:0.000} chara: {(c.yJumpOffset * 2):0.000} dist: {Math.Abs(this.zPosition.Value + c.yJumpOffset * 2)})");
				}

				return isInReachXY && isInReachZ && !isCollisionOnCooldown;
			});
        }

        public bool IsCollidingWithNet()
        {
            // Ball must be lower than net height
			return this.zPosition.Value <= VolleyballLocation.NetSize.Y
                // Prevent instant rebound with value larger than an average tick duration
				&& this.TravelTime.Value > 100
                // Check difference in position between ball and centre based on combined ball and net size
                && Math.Abs(this.Position.X - VolleyballLocation.PlayAreaCentre.X) < /*VolleyballLocation.NetSize.X +*/ this.CollisionSize
                // Ignore collision if on opposite side of net to player who hit it
                && Math.Sign(((VolleyballLocation)this.Location.Value).GetPlayer(this.LastHitBy.Value).Position.X - VolleyballLocation.PlayAreaCentre.X) == Math.Sign(this.Position.X - VolleyballLocation.PlayAreaCentre.X);
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
			this.RotationVelocity.Value = this.VisualStyle is Style.Beachball ? this.zVelocity.Value : this.RotationVelocity.Value - rotationDecay * time.ElapsedGameTime.Milliseconds;
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
                texture: ModEntry.Sprites,
                position: Game1.GlobalToLocal(
                    viewport: Game1.viewport,
                    globalPosition: this.Position.Value
                        + new Vector2(x: 0, y: -this.zPosition.Value)
                        - new Vector2(x: 0, y: this.SourceArea.Y) / 2 * Game1.pixelZoom
                        ),
                sourceRectangle: this.SourceArea,
                color: Color.White,
                rotation: this.Rotation.Value,
                origin: Utility.PointToVector2(this.SourceArea.Size) / 2,
                scale: this.Scale,
                effects: SpriteEffects.None,
                layerDepth: (this.Position.Value.Y + 96f) / 10000f);
        }
    }
}
