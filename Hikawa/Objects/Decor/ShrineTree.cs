using Hikawa.Data;
using Hikawa.Objects.Locations;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Xml.Serialization;

namespace Hikawa.Objects.Decor
{
    [XmlType($"{ModConsts.SpaceCoreXmlPrefix}{nameof(ShrineTree)}")] // SpaceCore serialisation signature
    public class ShrineTree : Tree
    {
        public readonly ShrineTreeSpawnData SpawnData;
        public readonly ShrineTreeData TreeData;

        private readonly int _leafOffset;
        //private readonly IReflectedField<List<Leaf>> _leaves;

        private const int LeafInterval = 300;

        public ShrineTree() { }

        public ShrineTree(ShrineTreeData treeData, ShrineTreeSpawnData spawnData)
            : base(id: spawnData.Id)
        {
            this.TreeData = treeData;
            this.SpawnData = spawnData;

            this.growthStage.Set(treeStage);
            this.flipped.Set(this.SpawnData.Flip);

            this._leafOffset = Game1.random.Next(ShrineTree.LeafInterval);
            //this._leaves = ModEntry.Instance.Helper.Reflection.GetField<List<Leaf>>(this, "leaves");
        }

        public void SpawnLeaf()
        {
            Rectangle area = this.TreeData.LeafRegion.Value;
            var sprite = TemporaryAnimatedSprite.GetTemporaryAnimatedSprite(
                textureName: AssetManager.ExtraSpritesAssetName,
                sourceRect: new Rectangle(400, 208, 16, 16),
                position: this.Tile * Game1.tileSize
                    - this.TreeData.TextureRegion.Size.ToVector2() / 2f * Game1.pixelZoom
                    + area.Size.ToVector2() / 2f * Game1.pixelZoom
                    + new Vector2(
                        x: (float)(-area.Width / 2f + area.Width * Game1.random.NextDouble()),
                        y: (float)(-area.Height / 2f + area.Height * Game1.random.NextDouble())) * Game1.pixelZoom,
                flipped: Game1.random.NextDouble() < 0.5d,
                alphaFade: 0f,
                color: Color.White);
            sprite.motion = new Vector2(
                x: WeatherDebris.globalWind * 0.25f,
                y: 0.5f);
            sprite.acceleration = new Vector2(
                x: sprite.motion.X / 250f,
                y: 0f);
            sprite.alphaFadeFade = -0.00001f;
            sprite.animationLength = 11;
            sprite.totalNumberOfLoops = 8;
            sprite.interval = (float)(100f + 100f * Game1.random.NextDouble());
            sprite.layerDepth = 1f;
            sprite.scale = Game1.pixelZoom;
            sprite.scaleChange = -0.0025f;
            this.Location.TemporarySprites.Add(sprite);
        }

        public override Rectangle getRenderBounds()
        {
            // Unused method :(
            Vector2 size = this.TreeData.TextureRegion.Size.ToVector2() * Game1.pixelZoom;
            Vector2 global = this.Tile * Game1.tileSize + this.TreeData.TextureOrigin * Game1.pixelZoom - size;
            return new((int)global.X, (int)global.Y, (int)size.X, (int)size.Y);
        }

        public override bool tickUpdate(GameTime time)
        {
            if (// Poll rate
                time.TotalGameTime.Ticks % LeafInterval == this._leafOffset
                // Instance state
                && this.SpawnData.Leaves && this.TreeData.LeafRegion.HasValue && this.Location is not null
                && this.getRenderBounds().Intersects(new(Game1.viewport.X, Game1.viewport.Y, Game1.viewport.Width, Game1.viewport.Height))
                // World state
                // && Game1.dayOfMonth > WorldDate.DaysPerMonth / 2
                )
            {
                this.SpawnLeaf();
            }

            return base.tickUpdate(time);
        }

        public override bool performUseAction(Vector2 tileLocation)
        {
            if (!this.TreeData.CanShake)
                return false;

            return base.performUseAction(tileLocation);
        }

        public override bool performToolAction(Tool t, int explosion, Vector2 tileLocation)
        {
            GameLocation location = this.Location ?? Game1.currentLocation;

            if (t is Axe)
            {
                location.playSound("axchop", tileLocation);
                this.lastPlayerToHit.Value = t.getLastFarmerToUse().UniqueMultiplayerID;
                location.debris.Add(new Debris(
                    debrisType: Debris.woodDebris,
                    numberOfChunks: Game1.random.Next(1, 3),
                    debrisOrigin: t.getLastFarmerToUse().GetToolLocation() + new Vector2(16f, 0f),
                    playerPosition: t.getLastFarmerToUse().Position,
                    groundLevel: 0,
                    color: this.GetChopDebrisColor()));
                if (location is Shrine)
                {
                    this.shake(tileLocation, doEvenIfStillShaking: true);
                    Game1.drawObjectDialogue(ModEntry.I18n.Get("world.shrine.tree.warning"));
                    return false;
                }
            }

            return base.performToolAction(t, explosion, tileLocation);
        }

        public override void draw(SpriteBatch spriteBatch)
        {
            if (this.isTemporarilyInvisible)
                return;

            float baseSortPosition = this.getBoundingBox().Bottom;

            TryGetData(this.treeType.Value, out var data);

            // Error data
            if (this.texture.Value is null || data is null)
            {
                IItemDataDefinition itemType = ItemRegistry.RequireTypeDefinition("(O)");
                spriteBatch.Draw(
                    texture: itemType.GetErrorTexture(),
                    position: Game1.GlobalToLocal(Game1.viewport, new Vector2(
                        this.Tile.X * 64f + (this.shakeTimer > 0f ? MathF.Sin(MathF.PI * 2f / this.shakeTimer) * 3f : 0f),
                        this.Tile.Y * 64f)),
                    sourceRectangle: itemType.GetErrorSourceRect(),
                    color: Color.White * this.alpha,
                    rotation: 0,
                    origin: Vector2.Zero,
                    scale: Game1.pixelZoom,
                    effects: this.flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    layerDepth: (baseSortPosition + 1f) / 10000f);
                return;
            }

            // Alpha
            {
                Vector2 v1 = Game1.player.GetBoundingBox().Center.ToVector2();
                Vector2 v2 = this.Tile * Game1.tileSize + new Vector2(this.TreeData.TextureRegion.Width * 0.25f, this.TreeData.TextureRegion.Height * -0.25f) * Game1.pixelZoom;
                float r = (this.TreeData.TextureRegion.Width + this.TreeData.TextureRegion.Height) / 2 * Game1.pixelZoom / 2;
                this.alpha = Math.Clamp(Vector2.Distance(v1, v2) / r, min: 0.4f, max: 1f);
            }

            // Shadow
            if (this.TreeData.HasShadow)
            {
                spriteBatch.Draw(
                    texture: this.IsLeafy() ? Game1.mouseCursors : Game1.mouseCursors_1_6,
                    position: Game1.GlobalToLocal(Game1.viewport, new Vector2(x: this.Tile.X - 0.8f, y: this.Tile.Y - 0.25f) * Game1.tileSize),
                    sourceRectangle: this.IsLeafy() ? shadowSourceRect : new Rectangle(469, 298, 42, 31),
                    color: Color.White * (MathF.PI / 2f - MathF.Abs(this.shakeRotation)),
                    rotation: 0,
                    origin: Vector2.Zero,
                    scale: Game1.pixelZoom,
                    effects: this.flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    layerDepth: 1E-06f);
            }

            Vector2 origin = this.TreeData.TextureOrigin;
            if (this.flipped.Value)
                origin.X = this.TreeData.TextureRegion.Width - origin.X;

            // Tree
            spriteBatch.Draw(
                texture: this.texture.Value,
                position: Game1.GlobalToLocal(Game1.viewport, new Vector2(x: this.Tile.X + 0.5f, y: this.Tile.Y + 1f) * Game1.tileSize),
                sourceRectangle: this.TreeData.TextureRegion,
                color: Color.White * this.alpha,
                rotation: this.shakeRotation,
                origin: origin,
                scale: Game1.pixelZoom,
                effects: this.flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                layerDepth: (baseSortPosition + 2f) / 10000f - this.Tile.X / 1000000f);
            /*
			foreach (Leaf leaf in this._leaves.GetValue())
			{
				spriteBatch.Draw(
					texture: this.texture.Value,
					position: Game1.GlobalToLocal(Game1.viewport, leaf.position),
					sourceRectangle: new Rectangle(16 + leaf.type % 2 * 8, 112 + leaf.type / 2 * 8, 8, 8),
					color: Color.White,
					rotation: leaf.rotation,
					origin: Vector2.Zero,
					scale: Game1.pixelZoom,
					effects: SpriteEffects.None,
					layerDepth: baseSortPosition / 10000f + 0.01f);
			}
			*/
        }
    }
}
