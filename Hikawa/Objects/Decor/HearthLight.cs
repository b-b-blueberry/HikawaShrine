using Hikawa.Data;
using System;

namespace Hikawa.Objects.Decor
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
        public readonly LightData Data;
        /// <summary>
        /// Source rectangle in texture sheet.
        /// </summary>
        public readonly Rectangle TextureRegion;

        public HearthLight(string id, LightData data, Vector2 position)
            : base(
                  id: id,
                  textureIndex: cauldronLight,
                  position: position,
                  radius: Math.Max(0, data.Scale),
                  color: data.Color)
        {
            // Variables
            Intensity = 0f;

            // Properties
            Data = data;
            lightTexture = Game1.content.Load<Texture2D>(AssetManager.LightSpritesAssetName);
            TextureRegion = Game1.getSquareSourceRectForNonStandardTileSheet(
                tileSheet: lightTexture,
                tileWidth: ModEntry.DecorSpawnsData.Value.HearthLightSize.X,
                tileHeight: ModEntry.DecorSpawnsData.Value.HearthLightSize.Y,
                tilePosition: data.TextureIndex);
        }

        public override void Draw(SpriteBatch spriteBatch, GameLocation location, float lightMultiplier)
        {
            if (!Utility.isOnScreen(
                positionNonTile: position.Value,
                acceptableDistanceFromScreen: (int)(radius.Value * Game1.tileSize * 4))
                || !Game1.isStartingToGetDarkOut(Game1.currentLocation) && !Game1.isRaining)
                return;

            GameLocation here = Game1.currentLocation;
            Texture2D texture = lightTexture;
            int lightingQuality = Game1.options.lightingQuality;

            // Intensity is added to scale
            float scale = radius.Value / (lightingQuality / 2) + Intensity;
            // Intensity is multiplied with difference in luminosity
            Color color = this.color.Value * (Data.Luminosity + (1f - Data.Luminosity) * Intensity * 0.75f);
            color.R += (byte)Math.Floor(color.R * Intensity * 255f / 3f);

            if (Game1.currentGameTime.TotalGameTime.TotalMilliseconds % Data.Rate < 1)
            {
                // Update light intensity
                Intensity = (-0.5f + (float)Game1.random.NextDouble()) * Data.Variance * 3f;
            }

            // Fade outdoor lights as night deepens
            if (Data.ExtinguishAtTime > 0)
                color.A = (byte)(color.A * Math.Clamp(
                    value: (Data.ExtinguishAtTime - Utils.GetPreciseTimeOfDay(Game1.timeOfDay)) / 100f * Data.ExtinguishRate,
                    min: Data.ExtinguishedAlpha,
                    max: 1f));

            spriteBatch.Draw(
                texture: texture,
                position: Game1.GlobalToLocal(Game1.viewport, position.Value) / (lightingQuality / 2),
                sourceRectangle: TextureRegion,
                color: color,
                rotation: 0f,
                origin: TextureRegion.Size.ToVector2() / 2,
                scale: scale,
                effects: SpriteEffects.None,
                layerDepth: 0.9f);
        }

        public static string GetId(GameLocation where, int which)
        {
            return $"{ModEntry.DecorSpawnsData.Value.HearthLightBaseId}_{where.NameOrUniqueName}_{which}";
        }
    }
}
