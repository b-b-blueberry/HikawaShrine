using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace Hikawa.Objects.Critters
{
	public class ShrineCrowTradeCrow : Birdie
	{
		private int characterCheckTimer = 200;

		private readonly IReflectedField<int> _state;

		private const int FlyingState = 1;
		private const int BaseFrame = 108;

		public ShrineCrowTradeCrow(Vector2 position)
			: base(position: position, yOffset: Game1.tileSize * 0.75f, startingIndex: ShrineCrowTradeCrow.BaseFrame, stationary: true)
        {
			this._state = ModEntry.Instance.Helper.Reflection.GetField<int>(this, "state");

			this.sprite = new(Game1.birdsSpriteSheet.Name, ShrineCrowTradeCrow.BaseFrame, 16, 16);
			this.sprite.setCurrentAnimation(
			[
				new((short)this.baseFrame, 1300),
				new((short)(this.baseFrame + 1), 1300)
			]);
			this.sprite.loop = true;
			this.flip = true;
		}

		public override bool update(GameTime time, GameLocation environment)
		{
			this.sprite.animateOnce(time);

			int ms = time.ElapsedGameTime.Milliseconds;
			this.characterCheckTimer -= ms;
			if (this.characterCheckTimer < 0)
			{
				Character c = Utility.isThereAFarmerOrCharacterWithinDistance(this.position / Game1.tileSize, 3, environment);
				this.characterCheckTimer = 200;
				if (c is Farmer f && this._state.GetValue() != ShrineCrowTradeCrow.FlyingState)
				{
					if (Game1.random.NextDouble() < 0.5f)
						Game1.playSound("crow");

					this._state.SetValue(ShrineCrowTradeCrow.FlyingState);
					this.sprite.setCurrentAnimation(
					[
						new((short)(this.baseFrame - 6), 70),
						new((short)(this.baseFrame - 5), 60, secondaryArm: false, this.flip, this.playFlap),
						new((short)(this.baseFrame - 6), 70),
						new((short)(this.baseFrame - 5), 60)
					]);
					this.sprite.loop = true;
				}
			}
			if (this._state.GetValue() == ShrineCrowTradeCrow.FlyingState)
			{
				this.position.X += Game1.tileSize * 1.25f / ms;
				this.yOffset -= Game1.tileSize * 0.5f / ms;
			}

			return this.position.X < -Game1.tileSize * 2 || this.position.Y < -Game1.tileSize * 2 || this.position.X > environment.map.DisplayWidth || this.position.Y > environment.map.DisplayHeight;
		}

		private void playFlap(Farmer who)
		{
			if (Utility.isOnScreen(this.position, Game1.tileSize))
			{
				Game1.playSound("batFlap");
			}
		}
	}
}
