using StardewValley;
using StardewValley.Projectiles;
using StardewValley.TerrainFeatures;

namespace Hikawa.Objects.Projectiles
{
	class WandProjectile : BasicProjectile
    {
        public enum Type_
        {
            Stars = 512 / 16 * 144 / 16,
        }
        public readonly Type_ Type;

        public WandProjectile(Type_ type, Character owner, GameLocation location, Vector2 origin, Vector2 target)
            : base()
        {
            Type = type;
            projectileSheet = ModEntry.Instance.Helper.GameContent.Load<Texture2D>(AssetManager.ExtraSpritesAssetName);
        }

        private void explosionAnimation(GameLocation where)
        {
            /*var sourceRect = new Rectangle(0, 144, 16, 16);
            const int spriteFrames = 1;
			Game1.Multiplayer.broadcastSprites(
                where,
                new TemporaryAnimatedSprite(
                    AssetManager.ExtraSpritesAssetName,
                    new Rectangle(
                        sourceRect.X + sourceRect.Width * spriteFrames, sourceRect.Y, 16, 16),
                    100, 8, 0,
                    position,
                    false,
                    false)
                {
                    scale = Game1.pixelZoom
                });*/
        }

        public override void behaviorOnCollisionWithPlayer(GameLocation location, Farmer player)
        {
        }

        public override void behaviorOnCollisionWithTerrainFeature(TerrainFeature t, Vector2 tileLocation, GameLocation location)
        {
            explosionAnimation(location);
        }

        public override void behaviorOnCollisionWithOther(GameLocation location)
        {
            explosionAnimation(location);
        }

        public override void behaviorOnCollisionWithMonster(NPC n, GameLocation location)
        {
            explosionAnimation(location);
        }

        public override void updatePosition(GameTime time)
        {
           /* position.X += xVelocity;
            position.Y += yVelocity;*/
        }
        public override Rectangle getBoundingBox()
        {
            return base.getBoundingBox();
        }

        public override void draw(SpriteBatch b)
        {
            base.draw(b);
        }
    }
}
