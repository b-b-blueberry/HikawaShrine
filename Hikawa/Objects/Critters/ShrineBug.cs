using System;
using System.Collections.Generic;
using Hikawa.Data;
using StardewValley.BellsAndWhistles;

namespace Hikawa.Objects.Critters
{
	public class ShrineBug : Critter
	{
		public string BugId;
		public Vector2 BugOffset;
		public BugData Definition;

		public ShrineBug() : base() {}

		public ShrineBug(Vector2 tile, string bugId, BugData definition)
			: base()
		{
			this.Init(bugId, definition);

			this.startingPosition = this.position = tile * Game1.tileSize;
		}

		public void Init(string bugId, BugData definition)
		{
			this.BugId = bugId;
			this.Definition = definition;

			this.BugOffset = default;

			// Animate bug sprite
			Rectangle r = this.Definition.WorldTextureRegion;
			this.sprite = new(textureName: this.Definition.TextureId)
			{
				SpriteWidth = r.Width,
				SpriteHeight = r.Height,
				SourceRect = r
			};
			int x = r.X / r.Width;
			int y = r.Y / r.Height;
			int frame = y * this.sprite.Texture.Width / r.Width + x;
			List<FarmerSprite.AnimationFrame> frames = [];
			for (int i = 0; i < definition.AnimationFrames; ++i)
				frames.Add(new(frame + i, definition.AnimationSpeed));
			this.sprite.setCurrentAnimation(frames);
			this.sprite.loop = true;
		}

		public void DrawAt(SpriteBatch b, Vector2 position, float layerDepth = 1f)
        {
            if (this.sprite is null)
                return;

            this.sprite.draw(
				b: b,
				screenPosition: position
					+ this.BugOffset,
				layerDepth: layerDepth,
				xOffset: 0,
				yOffset: 0,
				c: Color.White,
				flip: this.flip,
				scale: Game1.pixelZoom);
		}

		public override Rectangle getBoundingBox(int xOffset, int yOffset)
        {
            if (this.sprite is null)
                return Rectangle.Empty;

            Vector2 size = new Vector2(this.sprite.SpriteWidth, this.sprite.SpriteHeight) * Game1.pixelZoom;
			Vector2 global = this.position
				+ size * -0.5f
				+ new Vector2(0, this.yJumpOffset + this.yOffset)
				+ new Vector2(xOffset, yOffset)
				;
			return new(location: global.ToPoint(), size: size.ToPoint());
		}

		public override bool update(GameTime time, GameLocation environment)
		{
			if (this.Definition is null)
				return true;

			if (this.Definition.IsFlying)
			{
				this.BugOffset.Y = MathF.Sin(Game1.currentGameTime.TotalGameTime.Milliseconds / 1000f * MathF.PI * 1f) * 8f;
			}

			if (this.Definition.IsGold
				&& this.position != default
				&& time.TotalGameTime.TotalMilliseconds % 60 == 0
				&& Utility.isOnScreen(positionNonTile: this.position, acceptableDistanceFromScreen: Game1.tileSize))
			{
				Color[] colours = [Color.White, Color.OrangeRed, Color.Gold, Color.Yellow];
				int size = Game1.tileSize / 2;
				var sprite = TemporaryAnimatedSprite.GetTemporaryAnimatedSprite(
					textureName: null,
					sourceRect: new Rectangle(0, 0, 1, 1),
					position: this.position
						+ new Vector2(x: -1, y: 0) * size / 2f
						+ new Vector2(x: (float)Game1.random.NextDouble() * size, y: (float)Game1.random.NextDouble() * size / 4f),
					flipped: false,
					alphaFade: 0.0025f,
					color: colours[Game1.random.Next(colours.Length)]);
				sprite.alpha = 0.4f + (float)Game1.random.NextDouble() * 0.4f;
				sprite.motion = new Vector2(0f, -0.25f);
				sprite.acceleration = new Vector2(0.0015f, 0f);
				sprite.interval = 99999f;
				sprite.layerDepth = 1f;
				sprite.scale = Game1.pixelZoom;
				sprite.scaleChange = -0.02f;
				sprite.texture = Game1.staminaRect;
				environment.TemporarySprites.Add(sprite);
			}

			return base.update(time, environment);
		}

		public override void draw(SpriteBatch b)
        {
            Rectangle bounds = this.getBoundingBox(0, 0);
			Vector2 global = bounds.Location.ToVector2()
				+ bounds.Size.ToVector2() / 2;
			this.DrawAt(
                b: b,
                position: Game1.GlobalToLocal(Game1.viewport, global),
                layerDepth: this.position.Y / 10000f + this.position.X / 1000000f);
		}

		public override void drawAboveFrontLayer(SpriteBatch b)
		{
			base.drawAboveFrontLayer(b);
		}
	}
}
