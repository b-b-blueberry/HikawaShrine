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
			int rate = (int)(30 / this.VolleyballData.Responsiveness);
			if (Context.IsMainPlayer && this.Volleyball?.IsInPlay.Value is true && (time.TotalGameTime.TotalMilliseconds / rate) % 16 < 1)
			{
				Log.D($"ThinkAt(rate: {this.VolleyballData.Responsiveness}, time: {time.TotalGameTime.TotalMilliseconds}, at: {this.Position})");

				bool wannaJump = this.yJumpOffset == 0 // not currently jumping
					&& Math.Abs(VolleyballLocation.PlayAreaCentre.X - this.Position.X) < VolleyballLocation.PlayArea.Width * Game1.tileSize / 4 // close to net
					&& Math.Abs(Vector2.Distance(this.Position, this.Volleyball.Position.Value)) < this.Volleyball.CollisionSize * 1.5f // ball is within reach
					&& this.Volleyball.zPosition.Value < this.Volleyball.GetCharacterCollisionArea(this).Height && this.Volleyball.zVelocity.Value < 0.1; // ball is overhead and falling

                if (wannaJump)
				{
                    // TODO: DEBUG: HIT BEHAVIOUR
                    this.jump(jumpVelocity: 4 * this.VolleyballData.Jump - (1 - this.VolleyballData.Weight));
					this.yJumpGravity = -0.25f * this.VolleyballData.Weight;
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
				this.Position += velocity * (this.Speed + this.addedSpeed) * this.VolleyballData.Speed;
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
