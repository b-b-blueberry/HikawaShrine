using System;
using StardewValley;

namespace Hikawa.Objects.Locations
{
	public class HearthLight : LightSource
	{
		/// <summary>
		/// Variable for current light intensity based on luminosity and variance.
		/// </summary>
		public float Intensity;

		/// <summary>
		/// Entry in Lights map in ModData.
		/// </summary>
		public readonly LightEntry Data;
		/// <summary>
		/// Source rectangle in texture sheet.
		/// </summary>
		public readonly Rectangle TextureRegion;

		public HearthLight(string id, LightEntry data, Vector2 position)
			: base(
				  id: id,
				  textureIndex: LightSource.cauldronLight,
				  position: position,
				  radius: Math.Max(0, data.Scale),
				  color: data.Color)
		{
			// Variables
			this.Intensity = 0f;

			// Properties
			this.Data = data;
			this.lightTexture = Game1.content.Load<Texture2D>(AssetManager.LightSpritesAssetName);
			this.TextureRegion = Game1.getSquareSourceRectForNonStandardTileSheet(
				tileSheet: this.lightTexture,
				tileWidth: ModEntry.ModData.HearthLightSize.X,
				tileHeight: ModEntry.ModData.HearthLightSize.Y,
				tilePosition: data.TextureIndex);
		}

		public override void Draw(SpriteBatch spriteBatch, GameLocation location, float lightMultiplier)
		{
			if (!Utility.isOnScreen(
				positionNonTile: this.position.Value,
				acceptableDistanceFromScreen: (int)(this.radius.Value * Game1.tileSize * 4))
				|| (!Game1.isStartingToGetDarkOut(Game1.currentLocation) && !Game1.isRaining))
				return;

			GameLocation here = Game1.currentLocation;
			Texture2D texture = this.lightTexture;
			int lightingQuality = Game1.options.lightingQuality;

			// Intensity is added to scale
			float scale = (this.radius.Value / (lightingQuality / 2)) + this.Intensity;
			// Intensity is multiplied with difference in luminosity
			Color color = this.color.Value * (this.Data.Luminosity + (1f - this.Data.Luminosity) * this.Intensity * 0.75f);
			color.R += (byte)Math.Floor(color.R * this.Intensity * 255f / 3f);

			if (Game1.currentGameTime.TotalGameTime.TotalMilliseconds % this.Data.Rate < 1)
			{
				// Update light intensity
				this.Intensity = (-0.5f + (float)Game1.random.NextDouble()) * this.Data.Variance * 3f;
			}

			// Fade outdoor lights as night deepens
			if (this.Data.ExtinguishAtTime > 0)
				color.A = (byte)(color.A * Math.Clamp(
					value: (this.Data.ExtinguishAtTime - Utils.GetPreciseTimeOfDay(Game1.timeOfDay)) / 100f * this.Data.ExtinguishRate,
					min: this.Data.ExtinguishedAlpha,
					max: 1f));

			spriteBatch.Draw(
				texture: texture,
				position: Game1.GlobalToLocal(Game1.viewport, this.position.Value) / (lightingQuality / 2),
				sourceRectangle: this.TextureRegion,
				color: color,
				rotation: 0f,
				origin: this.TextureRegion.Size.ToVector2() / 2,
				scale: scale,
				effects: SpriteEffects.None,
				layerDepth: 0.9f);
		}

		public static string GetId(GameLocation where, int which)
		{
			return $"{ModEntry.ModData.HearthLightBaseId}_{where.NameOrUniqueName}_{which}";
		}
	}
}
