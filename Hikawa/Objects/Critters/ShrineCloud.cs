using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Hikawa.Objects.Critters
{
	public class ShrineCloud : StardewValley.BellsAndWhistles.Cloud
    {
		public float alpha = 1f;
		public float scale = 1f;

		public readonly SpriteEffects effects;

		public ShrineCloud(Vector2 position) : base(position)
		{
			// demetriums
			this.effects = (SpriteEffects)(0x1 << (Game1.random.Next(Enum.GetValues(typeof(SpriteEffects)).Length)));
		}

		public override bool update(GameTime time, GameLocation environment)
		{
			var backLayer = Game1.currentLocation?.Map?.GetLayer("Back");
			Point tile = new((int)(this.position.X / Game1.tileSize), (int)(this.position.Y / Game1.tileSize));
			if (backLayer is not null && backLayer.Tiles[tile.X, tile.Y] is null)
			{
				this.alpha -= 0.0025f;
				this.scale -= 0.001f;
			}
			return this.alpha < 0f || this.scale < 0f || base.update(time, environment);
		}

		public override void drawAboveFrontLayer(SpriteBatch b)
		{
			Rectangle source = new Rectangle(128, 0, 146, 99);
			b.Draw(
				texture: Game1.mouseCursors,
				position: Game1.GlobalToLocal(this.position),
				sourceRectangle: source,
				color: Color.White * this.alpha,
				rotation: this.effects is (SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically) ? (float)Math.PI : 0f,
				origin: source.Size.ToVector2() / 2,
				scale: this.zoom * this.scale,
				effects: effects,
				layerDepth: 1f);
		}
	}
}
