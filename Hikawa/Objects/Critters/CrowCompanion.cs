using StardewValley.Companions;
using System;

namespace Hikawa.Objects.Critters
{
    public class CrowCompanion : Companion
    {
        Rectangle DefaultSourceArea => new Rectangle(0, 160, 16, 16);
        float cawTimer;
        bool perching;

        public CrowCompanion()
        {
        }

        public override void InitializeCompanion(Farmer farmer)
        {
            base.InitializeCompanion(farmer);
        }

        public override void CleanupCompanion()
        {
            base.CleanupCompanion();
        }

        public override void OnOwnerWarp()
        {
            base.OnOwnerWarp();
        }

        public override void Hop(float amount)
        {
            base.Hop(amount);
        }

        public override void Update(GameTime time, GameLocation location)
        {
            base.Update(time, location);

        }

        public override void Draw(SpriteBatch b)
        {
            if (this.Owner?.currentLocation is null || (this.Owner.currentLocation.DisplayName == "Temp" && !Game1.isFestival()))
                return;

            var texture = Game1.content.Load<Texture2D>("LooseSprites/birds");
            var position = Game1.GlobalToLocal(this.Position + this.Owner.drawOffset + new Vector2(0, -this.height * Game1.pixelZoom));
            var shadowPosition = Game1.GlobalToLocal(this.Position + this.Owner.drawOffset);
            var source = this.DefaultSourceArea;
            var color = Color.White;
            var origin = source.Size.ToVector2() / 2;
            var effect = this.direction.Value == Game1.right ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            var layerDepth = this._position.Y / 10000f;
            var shadowLayerDepth = (this._position.Y - origin.Y) / 10000f - .000002f;

            b.Draw(
                texture: texture,
                position: position,
                sourceRectangle: source,
                color: color,
                rotation: 0,
                origin: origin,
                scale: Game1.pixelZoom,
                effects: effect,
                layerDepth: layerDepth);
            b.Draw(
                texture: Game1.shadowTexture,
                position: shadowPosition,
                sourceRectangle: Game1.shadowTexture.Bounds,
                color: Color.White,
                rotation: 0f,
                origin: new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y),
                scale: Game1.pixelZoom * 0.75f * Utility.Lerp(1, 0.8f, Math.Min(this.height, 1)),
                effects: SpriteEffects.None,
                layerDepth: shadowLayerDepth);
        }
    }
}
