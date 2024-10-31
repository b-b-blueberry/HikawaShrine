using StardewValley;
using StardewValley.BellsAndWhistles;

namespace Hikawa.Objects.Critters
{
	public class ShrineBabyCrowController : PerchingBirds
	{
		public bool IsDrawingAboveAlwaysFront;

		public ShrineBabyCrowController(int count, Point[] perches, Point[] roosts)
			: base(bird_texture: Game1.birdsSpriteSheet, flap_frames: 2, width: 16, height: 16, origin: new Vector2(8f, 14f), perch_locations: perches, roost_locations: roosts)
		{
			this.birdSpeed = 4;
			this.peckDuration = 10;
			for (int i = 0; i < count; ++i)
			{
				this.AddBird(-1);
			}
		}

		public override void AddBird(int bird_type)
		{
			ShrineBabyCrow bird = new(context: this, point: this.GetFreeBirdPoint());
			this._birds.Add(bird);
			this.ReserveBirdPoint(bird, bird.endPosition);
		}

		public override void Draw(SpriteBatch b)
		{
			foreach (var bird in this._birds)
				bird.Draw(b);
		}
	}
}
