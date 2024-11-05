using System;
using Netcode;
using StardewModdingAPI;
using StardewValley;

namespace Hikawa.Volleyball
{
	public class VolleyballNPC : Character
	{
		public Volleyball Volleyball;

		public NetInt ThinkRate;
		public NetVector2 Aimpoint;
		public NetVector2 TargetPosition;

		public VolleyballNPC(string name, string displayName, Vector2? position = null) : base(
			sprite: new AnimatedSprite(textureName: "TileSheets/Craftables"),
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
				displayName: Game1.characterData.TryGetValue(baseName, out var data) ? data.DisplayName : baseName);
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
			Vector2 oldPosition = this.TargetPosition.Value;

			this.TargetPosition.Value = // Position outward from centre
				// new Vector2(x: VolleyballLocation.PlayAreaCentre.X + Math.Sign(VolleyballLocation.PlayAreaCentre.X - this.Position.X) * this.Position.X, y: 0) +
				// Move to the predicted path of the ball
				//(this.Volleyball.Velocity * );
			this.Position;

			Log.D($"TargetPosition(from: {oldPosition}, to: {this.TargetPosition.Value})");
		}

		public override void update(GameTime time, GameLocation location, long id, bool move)
		{
			base.update(time, location, id, move);

			// Update strategy
			if (Context.IsMainPlayer && this.Volleyball?.IsInPlay.Value is not null and true && (time.TotalGameTime.TotalMilliseconds / this.ThinkRate.Value) % 16 < 1)
			{
				Log.D($"ThinkAt(rate: {this.ThinkRate.Value}, time: {time.TotalGameTime.TotalMilliseconds}, at: {this.Position})");

				if (this.yJumpOffset > -0.1f && Math.Abs(Vector2.Distance(this.Position, this.Volleyball.Position.Value)) < this.Volleyball.CollisionSize)
				{
					this.jump(jumpVelocity: 2f); // TODO: DEBUG: HIT BEHAVIOUR
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
				this.Position += velocity * (this.Speed + this.addedSpeed) * 0.005f;
			}
		}

		public override void draw(SpriteBatch b, float alpha = 1)
		{
			base.draw(b, alpha);


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
