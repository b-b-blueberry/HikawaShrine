using StardewValley;
using StardewValley.BellsAndWhistles;

namespace Hikawa.Objects.Critters
{
	public class ShrineBabyCrow : Bird
    {
		public ShrineBabyCrow(PerchingBirds context, Point point)
			: base(point: point, context: context, bird_type: 10, flap_frames: 2)
		{
			// caw caw
		}

		public override void Draw(SpriteBatch b)
		{
			if (this.context is ShrineBabyCrowController c && c.IsDrawingAboveAlwaysFront == this.birdState is BirdState.Flying)
			{
				Vector2 local = Game1.GlobalToLocal(Game1.viewport, this.position);
				Vector2 birdSize = new Vector2(x: context.GetBirdWidth(), y: context.GetBirdHeight());
				Vector2 shadowSize = new Vector2(x: Game1.shadowTexture.Width, y: Game1.shadowTexture.Height);
				b.Draw(
					texture: Game1.shadowTexture,
					sourceRectangle: Game1.shadowTexture.Bounds,
					position: local
						+ new Vector2(x: -birdSize.X * Game1.pixelZoom / 16, y: birdSize.Y * Game1.pixelZoom / 8)
						,
					color: Color.White,
					rotation: 0f,
					origin: shadowSize / 2,
					scale: 3f,
					effects: SpriteEffects.None,
					layerDepth: (this.position.Y - 1) / 10000f);

				base.Draw(b);
			}
		}

		public override void Update(GameTime time)
		{
			base.Update(time);
		}

		public override void FlyToNewPoint()
		{
			base.FlyToNewPoint();
		}
	}
}
