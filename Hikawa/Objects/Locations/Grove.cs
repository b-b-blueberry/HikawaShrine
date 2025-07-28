using System;
using System.Xml.Serialization;

namespace Hikawa.Objects.Locations
{
    [XmlType($"{ModConsts.SpaceCoreXmlPrefix}{nameof(Grove)}")] // SpaceCore serialisation signature
    public class Grove : GameLocation
    {
        private Vector2 fogOffset;
        private Vector2 fogPosition;
        private float fogScale => Game1.pixelZoom + 0.001f;
        private Rectangle fogSource => new Rectangle(640, 0, 64, 64);

        public Grove() : base() {}

        public Grove(string filename, string locationName) : base(filename, locationName) {}

        public static GameLocation Get()
        {
            return Game1.getLocationFromName(ModEntry.ModData.MapGrove);
        }
        /*
        public override void checkForMusic(GameTime time)
        {
            // no sounds in daylight
            if (Game1.isStartingToGetDarkOut(this) || this.IsRainingHere())
                base.checkForMusic(time);
        }
        */
        public override void tryToAddCritters(bool onlyIfOnScreen = false)
        {
            if (Game1.CurrentEvent is not null)
                return;
            
            double mapArea = this.Map.DisplayWidth * this.Map.DisplayHeight / Game1.tileSize;
            double baseChance = Math.Max(0.15d, Math.Min(0.5d, mapArea / 15000d));
            double cloudChance = baseChance * 2;
            if (this.IsRainingHere() || this.critters is null)
                return;

            this.addClouds(cloudChance / (onlyIfOnScreen ? 2d : 1d), onlyIfOnScreen);
            if (Game1.isDarkOut(this) && Game1.random.NextDouble() < 0.01d)
                this.addOwl();
        }

        public override void spawnObjects()
        {
            base.spawnObjects();

            Utils.SpawnObjectsInArea(
                where: this,
                area: new Rectangle(8, 8, 16, 16),
                itemIds: [ModEntry.ModData.ItemCharcoal],
                attempts: 1,
                max: 5);
        }

        protected override void resetLocalState()
        {
            base.resetLocalState();
        }

        public override void UpdateWhenCurrentLocation(GameTime time)
        {
            base.UpdateWhenCurrentLocation(time);

            this.fogPosition = new Vector2(-Game1.viewport.X, -Game1.viewport.Y) - this.fogSource.Size.ToVector2() * Game1.pixelZoom;

            double ms = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
            float dt = Game1.currentGameTime.ElapsedGameTime.Milliseconds; // Elapsed time

            // fog
            this.fogOffset -= dt * (new Vector2(0.025f, -0.015f + (float)(Math.Sin(ms * Math.PI / 11000d) / 45d)));
            this.fogOffset.X %= -this.fogSource.Width * this.fogScale;
            this.fogOffset.Y %= -this.fogSource.Height * this.fogScale;
            //this.fogPosition -= Game1.getMostRecentViewportMotion();

            bool isWindy = this.IsDebrisWeatherHere();
            float spriteChance = MathF.Sqrt(Game1.viewport.Height) / (isWindy ? 350f : 450f);

            // embers
            if (Game1.random.NextDouble() < spriteChance)
            {
                Color[] colours = [Color.LightGray, Color.Gray, Color.DarkOrange, Color.OrangeRed, Color.IndianRed, Color.RosyBrown];
                var sprite = TemporaryAnimatedSprite.GetTemporaryAnimatedSprite(
                    textureName: null,
                    sourceRect: new Rectangle(0, 0, 1, 1),
                    position: new Vector2(Game1.viewport.X, Game1.viewport.Y)
                        + Utility.getRandomPositionOnScreen()
                        + new Vector2(x: -Game1.tileSize, y: 0f) / 2f
                        + new Vector2(x: (float)Game1.random.NextDouble() * Game1.tileSize, y: (float)Game1.random.NextDouble() * Game1.tileSize / 4f),
                    flipped: false,
                    alphaFade: 0f,
                    color: colours[Game1.random.Next(colours.Length)]);

                sprite.alpha = 0f;
                sprite.alphaFade = -0.02f;
                sprite.alphaFadeFade = -0.00025f;

                sprite.motion = new Vector2(
                    x: WeatherDebris.globalWind * -0.25f - 1f,
                    y: 0f);
                sprite.acceleration = new Vector2(
                    x: sprite.motion.X / 250f,
                    y: 0f);
                sprite.interval = 99999f;
                sprite.yPeriodic = true;
                sprite.yPeriodicLoopTime = (float)(1500f + Game1.random.NextDouble() * 1500f);
                sprite.yPeriodicRange = Game1.tileSize / 8 + (float)(Game1.random.NextDouble() * Game1.tileSize / 8);
                sprite.layerDepth = sprite.position.Y / 10000f + sprite.position.X / 1000000f;
                sprite.scale = (float)(Game1.pixelZoom + Game1.random.NextDouble() * 4f);
                sprite.scaleChange = -0.01f;
                sprite.texture = Game1.staminaRect;
                this.TemporarySprites.Add(sprite);
            }
        }

        public override void drawAboveAlwaysFrontLayer(SpriteBatch b)
        {
            base.drawAboveAlwaysFrontLayer(b);

            bool isThickFog = this.IsDebrisWeatherHere() || this.IsRainingHere() || this.IsGreenRainingHere();
            double ms = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
            float alpha = (isThickFog ? 0.75f : 0.55f) + 0.1f * (float)(Math.Sin(ms * Math.PI / 9000d));
            Color colour = this.IsGreenRainingHere() ? new Color(125, 235, 135) : new Color(235, 225, 215);
            for (float x = this.fogPosition.X + this.fogOffset.X; x < Game1.graphics.GraphicsDevice.Viewport.Width + this.fogSource.Width * this.fogScale; x += this.fogSource.Width * this.fogScale)
                for (float y = this.fogPosition.Y + this.fogOffset.Y; y < Game1.graphics.GraphicsDevice.Viewport.Height + this.fogSource.Height * this.fogScale; y += this.fogSource.Height * this.fogScale)
                    b.Draw(
                        texture: Game1.mouseCursors,
                        position: new Vector2(x, y),
                        sourceRectangle: this.fogSource,
                        color: colour * alpha,
                        rotation: 0,
                        origin: Vector2.Zero,
                        scale: this.fogScale,
                        effects: SpriteEffects.FlipHorizontally,
                        layerDepth: 1);
        }
    }
}
