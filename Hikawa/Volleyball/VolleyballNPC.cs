using Netcode;
using StardewModdingAPI;
using StardewValley.TokenizableStrings;
using System;

namespace Hikawa.Volleyball
{
	public class VolleyballNPC : Character
	{
		public Volleyball Volleyball;

		public NetInt ThinkRate;
		public NetVector2 Aimpoint;
		public NetVector2 TargetPosition;

		public VolleyballNPC(string name, string displayName, Vector2? position = null) : base(
			sprite: new AnimatedSprite(textureName: $"Characters/{name}"),
			position: position ?? Vector2.Zero,
			speed: 1,
			name: name)
		{
			this.displayName = displayName;
		}

		public static VolleyballNPC MakeFor(string baseName)
		{
			return new VolleyballNPC(
				name: baseName + ModEntry.ModData.NpcVolleyballSuffix,
				displayName: Game1.characterData.TryGetValue(baseName, out var data) ? TokenParser.ParseText(data.DisplayName) : baseName);
		}

		protected override void initNetFields()
		{
			base.initNetFields();

			this.ThinkRate = new(32);
			this.Aimpoint = new(Vector2.Zero);
			this.TargetPosition = new(Vector2.Zero);

			this.NetFields
				.AddField(this.ThinkRate, nameof(this.ThinkRate))
				.AddField(this.Aimpoint, nameof(this.Aimpoint))
				.AddField(this.TargetPosition, nameof(this.TargetPosition));
		}

		public void ResetVolleyballValues()
		{
			this.Aimpoint.Set(Vector2.Zero);
			this.TargetPosition.Set(Vector2.Zero);
		}

		public void UpdateVolleyballAimpoint()
		{
			Vector2 oldPosition = this.TargetPosition.Value;

			this.Aimpoint.Set(Game1.player.Position);

			Log.D($"Aimpoint(from: {oldPosition}, to: {this.Aimpoint.Value})");
		}

		public void UpdateVolleyballTargetPosition()
		{
			int sign = Math.Sign(this.Position.X - VolleyballLocation.PlayAreaCentre.X);
			Vector2 oldPosition = this.TargetPosition.Value;
			Vector2 newPosition;

			if (sign * this.Volleyball.Position.X < sign * VolleyballLocation.PlayAreaCentre.X)
			{
				// volleyball on opposite side of net, move to dummy pos
				newPosition = new Vector2(VolleyballLocation.PlayAreaCentre.X + sign * VolleyballLocation.PlayArea.Width * Game1.tileSize / 4, VolleyballLocation.PlayAreaCentre.Y);
                Log.D($"TargetPosition(from: {oldPosition}, to: {newPosition}) OPP");
            }
			else
			{
                // volleyball on this side of net, move to ball
                newPosition =
                    // Position outward from centre
                    // new Vector2(x: VolleyballLocation.PlayAreaCentre.X + Math.Sign(VolleyballLocation.PlayAreaCentre.X - this.Position.X) * this.Position.X, y: 0) +
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
			if (Context.IsMainPlayer && this.Volleyball?.IsInPlay.Value is true && (time.TotalGameTime.TotalMilliseconds / this.ThinkRate.Value) % 16 < 1)
			{
				Log.D($"ThinkAt(rate: {this.ThinkRate.Value}, time: {time.TotalGameTime.TotalMilliseconds}, at: {this.Position})");

				bool wannaJump = this.yJumpOffset == 0 // not currently jumping
					&& Math.Abs(Vector2.Distance(this.Position, this.Volleyball.Position.Value)) < this.Volleyball.CollisionSize // ball is within reach
					&& this.Volleyball.zPosition.Value < this.Volleyball.GetCharacterCollisionArea(this).Height && this.Volleyball.zVelocity.Value < 0.1; // ball is overhead and falling

                if (wannaJump)
				{
					this.jump(jumpVelocity: 4f); // TODO: DEBUG: HIT BEHAVIOUR
					this.yJumpGravity = -0.25f;
				}

				this.UpdateVolleyballAimpoint();
				this.UpdateVolleyballTargetPosition();
			}
		}

		public override void updateMovement(GameLocation location, GameTime time)
		{
			const float distanceToStop = Game1.tileSize / 4f;
			if (this.TargetPosition.Value != Vector2.Zero && Math.Abs(Vector2.Distance(this.Position, this.TargetPosition.Value)) > distanceToStop)
			{
				Vector2 velocity = Utils.Vector.MotionTo(origin: this.Position, target: this.TargetPosition.Value);
				this.Position += velocity * (this.Speed + this.addedSpeed) * 3.5f;
			}
		}

		public override void draw(SpriteBatch b, float alpha = 1)
		{
			base.draw(b, alpha);

			// npc targetposition
			Vector2 from = this.getLocalPosition(Game1.viewport);
            Vector2 target = Game1.GlobalToLocal(Game1.viewport, this.TargetPosition.Value);
            Rectangle source = AssetManager.ExtraSpritesVolleyballAimpointArea;
            source.X += Math.Clamp(((VolleyballLocation)this.Volleyball.Location.Value).Players.IndexOf(Game1.player) * source.Width, min: 0, max: 4);
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
