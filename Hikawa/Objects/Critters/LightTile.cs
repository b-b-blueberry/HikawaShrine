using System;
using Hikawa.Objects.Locations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace Hikawa.Objects.Critters
{
	public class LightTile : Critter
    {
		public float Alpha = 0f;

		public readonly LightTileEntry Data;

		public LightTile(LightTileEntry data)
		{
			this.Data = data;
			this.sprite = new(
				textureName: data.TileSheetId,
				currentFrame: this.Data.TileId,
				spriteWidth: Game1.smallestTileSize,
				spriteHeight: Game1.smallestTileSize);
			this.sprite.ignoreSourceRectUpdates = true;
			this.sprite.SpriteWidth *= this.Data.Size.X;
			this.sprite.SpriteHeight *= this.Data.Size.Y;
			this.sprite.SourceRect = new(this.sprite.SourceRect.X, this.sprite.SourceRect.Y, this.sprite.SpriteWidth, this.sprite.SpriteHeight);
			this.startingPosition = this.position = this.Data.Tile * Game1.tileSize;
		}

		public override bool update(GameTime time, GameLocation environment)
		{
			this.Alpha = Utils.RatioFromPreciseTime(startTime: Game1.getStartingToGetDarkTime(Game1.currentLocation), endTime: 2630, isCircular: true);

			return base.update(time, environment);
		}

		public void DrawLightTile(SpriteBatch b)
		{
			if (this.Alpha <= 0.00001f
				|| !Utility.isOnScreen(
					positionNonTile: this.position,
					acceptableDistanceFromScreen: Math.Max(this.Data.Size.X, this.Data.Size.Y) * Game1.tileSize)
				|| !(Game1.isStartingToGetDarkOut(Game1.currentLocation) || Game1.isRaining))
				return;

			b.Draw(
				texture: this.sprite.Texture,
				position: Game1.GlobalToLocal(Game1.viewport, this.position),
				sourceRectangle: this.sprite.SourceRect,
				color: Color.White * this.Alpha,
				rotation: 0,
				origin: Vector2.Zero,
				scale: Game1.pixelZoom,
				effects: SpriteEffects.None,
				layerDepth: 1);
		}

		public override void draw(SpriteBatch b)
		{
			if (!this.Data.DrawAbove)
				this.DrawLightTile(b);
		}

		public override void drawAboveFrontLayer(SpriteBatch b)
		{
			if (this.Data.DrawAbove)
				this.DrawLightTile(b);
		}

		public override Rectangle getBoundingBox(int xOffset, int yOffset)
		{
			return Rectangle.Empty;
		}
	}
}
