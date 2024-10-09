using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace Hikawa.Objects.Critters
{
	/// <summary>
	/// Mostly a very mangled version of the StardewValley.BellsAndWhistles.Crow object.
	/// Not so impressive, but it's a dancing crow i guess?
	/// </summary>
	public class Crow : Critter
	{
		private enum State
		{
			Idle,
			Animating,
			Sleeping,
			Looking
		}
		private State _state;
		private readonly int _hopRange;
		private readonly int _crowBaseFrame;
		private readonly bool _isDeimos;

		private enum Frame
		{
			IdlePhobos = 0,
			Sleep = 3,
			IdleDeimos = 4,
			HopLow = 8,
			HopHigh = 9
		}

		private string Name => this._isDeimos ? "Deimos" : "Phobos";

		public Crow(bool isDeimos, Vector2 position, int hopRange = 0)
		{
			this.sprite = new AnimatedSprite(
				textureName: AssetManager.CrowSpritesAssetName,
				currentFrame: 0,
				spriteWidth: 32,
				spriteHeight: 32);

			this._isDeimos = isDeimos;
			this._hopRange = hopRange;
			this._state = State.Idle;

			this.startingPosition = this.position = (position * Game1.tileSize) + (new Vector2(x: 0.5f, y: 0.5f) * Game1.tileSize);
			this.baseFrame = this._crowBaseFrame = this._isDeimos ? (int)Frame.IdleDeimos : (int)Frame.IdlePhobos;
			this.flip = this._isDeimos;

			Log.D($"Perched crow {this.Name} generated at {this.startingPosition}");
		}

		public void Hop(Farmer who)
		{
			this.gravityAffectedDY = -this._hopRange;
		}

		private void DoneAnimating(Farmer who)
		{
			this._state = Game1.random.NextDouble() < 0.5d ? State.Idle : State.Animating;
		}

		private void LookAtPlayer(Farmer who, GameLocation environment)
		{
			if (this._state == State.Looking && this.IsFarmerInRange(environment: environment, range: 16) is Farmer farmer)
			{
				double angle = Utils.Vector.RadiansBetween(va: this.position, vb: farmer.Position);
				Log.D($"LookAt angle between crow ({this.position}) and {farmer.Name} ({farmer.Position}) == {angle}f");

				// TODO: METHOD: Select current frame based on angle, consider 'flip'
				this.sprite.currentFrame = (int)Frame.IdlePhobos;
			}
		}
		
		private Farmer IsFarmerInRange(GameLocation environment, int range)
		{
			return Utility.isThereAFarmerWithinDistance(this.position / Game1.tileSize, range, environment);
		}

		public override bool update(GameTime time, GameLocation environment)
		{
			bool isColliding = environment.isCollidingPosition(
				position: this.getBoundingBox(xOffset: -2, yOffset: 0),
				viewport: Game1.viewport,
				isFarmer: false,
				damagesFarmer: 0,
				glider: false,
				character: null,
				pathfinding: false,
				projectile: false,
				ignoreCharacterRequirement: true);

			// Hopping motion - don't hop through buildings, only hop onto AlwaysFront tiles
			if (this.yJumpOffset < 0f && !isColliding)
			{
				Point nextTileOver = new Point(
					x: (int)Math.Floor((this.position.X + (this.flip ? 1f : -1f)) / Game1.tileSize),
					y: (int)Math.Floor(this.position.Y / Game1.tileSize));

				if (environment.Map.GetLayer("AlwaysFront").Tiles[nextTileOver.X, nextTileOver.Y] != null)
				{
					this.position.X += 2f * (this.flip ? 1f : -1f);
				}

				this.sprite.CurrentFrame = this.yJumpOffset > -1f ? (int)Frame.HopLow : (int)Frame.HopHigh;
				return base.update(time: time, environment: environment);
			}
			
			// State picker
			switch (this._state)
			{
				case State.Idle:
					if (this.sprite.CurrentAnimation is null && this.yJumpOffset >= 0f && Game1.random.NextDouble() < 0.002d)
					{
						Log.D($"{this.Name}: Idle reroll");
						switch (Game1.random.Next(4))
						{
							case 0:
								Log.D($"{this.Name}: Picking Sleeping from Idle");
								this._state = State.Sleeping;
								break;
							case 1:
								Log.D($"{this.Name}: Picking Animating from Idle");
								this._state = State.Animating;
								break;
							case 2:
							case 3:
								if (this._hopRange > 0)
								{
									Log.D($"{this.Name}: Picking Hop from Idle");
									this.Hop(null);
								}
								break;
							case 4:
								Log.D($"{this.Name}: Picking Looking from Idle");
								if (this.IsFarmerInRange(environment: environment, range: 16) != null)
								{
									Log.D("Success.");
									this._state = State.Looking;
								}
								else
								{
									Log.D("Missed.");
								}
								break;
						}
					}
					else if (this.sprite.CurrentAnimation is null)
					{
						this.sprite.currentFrame = this._crowBaseFrame;
					}
					break;

				case State.Animating:
					if (this.sprite.CurrentAnimation is null)
					{
						Log.D($"Animating {this.Name}");
						List<FarmerSprite.AnimationFrame> animFrames = new();
						int frame = this._crowBaseFrame;
						if (this._isDeimos)
						{
							// Preening
							int loops = Game1.random.Next(2, 4);
							animFrames.Add(new (frame, 960, false, this.flip));
							for (int i = 0; i < loops; ++i)
							{
								animFrames.Add(new (frame + 1, 1200, false, this.flip));
								int subloops = Game1.random.Next(1, 3);
								for (int j = 0; j < subloops; ++j)
								{
									animFrames.Add(new (frame + 2, 560, false, this.flip));
									animFrames.Add(new (frame + 3, 360, false, this.flip));
								}
								animFrames.Add(new (frame + 2, Game1.random.Next(200, 600) * 8, false, this.flip));
							}
							animFrames.Add(new (frame + 1, 360, false, this.flip, this.DoneAnimating));
							Log.D($"Looping for {loops}, with {loops * (loops / 2)} subloops");
						}
						else
						{
							// Peeking
							bool shuteye = Game1.random.NextDouble() < 0.25;
							animFrames.Add(new (frame, 1200, false, this.flip));
							animFrames.Add(new (frame + 1, 440, false, this.flip));
							animFrames.Add(new (frame + 2, 1960, false, this.flip));
							animFrames.Add(new (frame + (shuteye ? 2 : 3), shuteye ? 12200 : 6600, false, this.flip));
							animFrames.Add(new (frame + 2, 160, false, this.flip));
							animFrames.Add(new (frame + 1, 320, false, this.flip));
							animFrames.Add(new (frame, 3600, false, this.flip, this.DoneAnimating));
							Log.D($"Shuteye: {shuteye}");
						}
						this.sprite.setCurrentAnimation(animFrames);
						this.sprite.loop = false;
					}
					break;

				case State.Sleeping:
					if (this.sprite.CurrentAnimation is null)
					{
						this.sprite.currentFrame = (int)Frame.Sleep;
					}
					if (Game1.random.NextDouble() < 0.002 && this.sprite.CurrentAnimation is null)
					{
						Log.D($"{this.Name}: Picking Idle from Sleeping");
						this._state = State.Idle;
					}
					break;

				case State.Looking:
					if (this.sprite.CurrentAnimation is null)
					{
						this.LookAtPlayer(null, environment);
					}
					break;
			}

			return base.update(time, environment);
		}

		public override void drawAboveFrontLayer(SpriteBatch b)
		{
			if (this.sprite is null)
				return;

			b.Draw(
				texture: Game1.shadowTexture,
				position: Game1.GlobalToLocal(Game1.viewport, this.position + new Vector2(0f, -4f)),
				sourceRectangle: Game1.shadowTexture.Bounds,
				color: Color.White,
				rotation: 0f,
				origin: new Vector2(x: Game1.shadowTexture.Bounds.Center.X, y: Game1.shadowTexture.Bounds.Center.Y),
				scale: Game1.pixelZoom - 1 + Math.Max(-3f, (this.yJumpOffset + this.yOffset) / Game1.tileSize),
				effects: SpriteEffects.None,
				layerDepth: 1f - (this._isDeimos ? 1f / 10000f : 0f));

			this.sprite.draw(
				b: b,
				screenPosition: Game1.GlobalToLocal(
					viewport: Game1.viewport,
					globalPosition: this.position
						+ (new Vector2(x: -1, y: -2) * Game1.tileSize)
						+ new Vector2(x: 0, y: this.yJumpOffset + this.yOffset)),
				layerDepth: 1f - (this._isDeimos ? 1f / 10000f : 0f),
				xOffset: 0,
				yOffset: 0,
				c: Color.White,
				flip: this.flip,
				scale: Game1.pixelZoom);
		}
	}
}
