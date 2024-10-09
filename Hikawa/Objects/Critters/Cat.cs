using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace Hikawa.Objects.Critters
{
	public class Cat : Critter
	{
		// Animation keys
		private enum State
		{
			// States from standing (idle):
			Idle,
			StartRunning,
			Running,
			StopRunning,
			StartSitting,
			// States from sitting:
			Sitting,
			StopSitting,
			StartGrooming,
			Grooming,
			StopGrooming,
		}
		private State _state;

		// Distance to player influencing behaviour changes
		private readonly int _scareRange;

		// Animation frame spans
		private enum Frame
		{
			Idle = 0,
			Run = 4,
			SitDown = 12,
			Groom = 19
		}
		private const int SitDownFrameCount = 4;

		// Starting idle frames
		internal static readonly int StandingBaseFrame = 0;
		internal static readonly int SittingBaseFrame = 16;

		public Cat(Vector2 position, int baseFrame, int scareRange, bool flip)
		{
			this.sprite = new AnimatedSprite(AssetManager.CatSpritesAssetName, baseFrame, 32, 32);

			this._state = State.Idle;
			this._scareRange = scareRange;

			this.startingPosition = this.position = position * Game1.tileSize + new Vector2(Game1.tileSize / 2);
			this.baseFrame = baseFrame;
			this.flip = flip;

			Log.W($"Cat generated at (X:{this.startingPosition.X / Game1.tileSize:.00} Y:{this.startingPosition.Y / Game1.tileSize:.00})");
		}
		
		/// <summary>
		/// After a non-looping animation ends, reroute behaviours.
		/// Lead-in to the first frame of the next animation to
		/// avoid flashing a frame of the null animation base frame.
		/// </summary>
		private void DoneAnimating(Farmer who)
		{
			string message = "other";
			switch (this._state)
			{
				case State.StartRunning:
					message = "StartRunning";
					this._state = State.Running;
					break;
				case State.Running:
					message = "Running";
					break;
				case State.StartSitting:
					message = "StartSitting";
					this._state = State.Sitting;
					break;
				case State.StartGrooming:
					message = "StartGrooming";
					this._state = State.Grooming;
					break;
				case State.Grooming:
					message = "Grooming";
					this._state = State.StopGrooming;
					break;
				case State.StopGrooming:
					message = "StopGrooming";
					this._state = State.Sitting;
					break;
				default:
					this._state = State.Idle;
					break; 
			}

			this.sprite.CurrentAnimation = null;
			Log.W($"Cat: Done {message}");
		}
		
		private void PlayMeow(Farmer who)
		{
			if (Utility.isOnScreen(positionNonTile: this.position, acceptableDistanceFromScreen: Game1.tileSize))
			{
				Game1.playSound("cat");
			}
		}

		/// <summary>
		/// Fetch any farmer or farmhand within scareRange of this critter.
		/// </summary>
		/// <returns></returns>
		private Farmer IsFarmerInRange(GameLocation environment, int range)
		{
			return Utility.isThereAFarmerWithinDistance(
				tileLocation: this.position / Game1.tileSize,
				tilesAway: range,
				location: environment);
		}
		
		public override bool update(GameTime time, GameLocation environment)
		{
			//if (_state != State.Running && _scareRange > 0 && IsFarmerInRange(environment, _scareRange) != null)
			//_state = State.StartRunning;
			//if (_state != State.Grooming && _scareRange > 0 && IsFarmerInRange(environment, _scareRange) != null)
			//_state = State.StartGrooming;

			// Pick behaviour for this frame, starting animations on the first frame of their state
			int frame = 0;
			bool loop = false;
			(int f, int ms)[] frames = null;
			switch (this._state)
			{
				case State.Idle:
					if (this.sprite.CurrentAnimation is null)
						this.sprite.CurrentFrame = this.baseFrame;

					// Testing animations: Sitting
					if (this._scareRange > 0 && this.IsFarmerInRange(environment, this._scareRange) is not null)
					{
						Log.W("Cat: Triggered StartSitting");
						this._state = State.StartSitting;
					}

					// TODO: ASSETS: Standing animations
					break;

				case State.StartRunning:
					if (this.sprite.CurrentAnimation is null)
					{
						Log.W("Cat: Into StartRunning");
						frame = (int)Frame.Idle;
						frames = new[] { (frame + 1, 60), (frame + 2, 70), (frame + 3, 80) };
						loop = false;
					}
					break;

				case State.Running:
					if (this.sprite.CurrentAnimation is null || this.sprite.CurrentFrame == (int)Frame.Run - 1)
					{
						Log.W("Cat: Into Running");
						frame = (int)Frame.Run;
						frames = new[] {
							(frame + 1, 70), (frame + 2, 70), (frame + 3, 80), (frame + 4, 90),
							(frame + 5, 80), (frame + 6, 70), (frame + 7, 65), (frame + 8, 60)
						};
						loop = true;
					}
					break;

				case State.StopRunning:
					if (this.sprite.CurrentAnimation is null || this.sprite.CurrentFrame == (int)Frame.Run + 8)
					{
						Log.W("Cat: Into StopRunning");
						frame = (int)Frame.Run;
						frames = new[] { (frame - 2, 70), (frame - 3, 90) };
						loop = false;
					}
					break;

				case State.StartSitting:
					if (this.sprite.CurrentAnimation is null)
					{
						Log.W("Cat: Into StartSitting");
						frame = (int)Frame.SitDown;
						frames = new[] { (frame, 120), (frame + 1, 120), (frame + 2, 120), (frame + 3, 120) };
						loop = false;
					}
					break;
					
				case State.Sitting:
					if (this.sprite.CurrentAnimation is null)
						this.sprite.CurrentFrame = Cat.SittingBaseFrame;

					if (Game1.random.NextDouble() < 0.008d)
					{
						int roll = Game1.random.Next(1, 3);
						Log.W($"Cat: Sitting rolled {roll}");
						switch (roll)
						{
							case 1:
								Log.W("Cat: Picked StartGrooming from Sitting");
								this._state = State.StartGrooming;
								break;
							case 2:
								Log.W("Cat: Picked SKIP from Sitting");
								// TODO: ASSETS: Sitting animations
								break;
						}
					}
					break;

				case State.StopSitting:
					if (this.sprite.CurrentAnimation is null)
					{
						Log.W("Cat: Into StopSitting");
						int num = Cat.SitDownFrameCount;
						frame = (int)Frame.SitDown;
						frames = new[] { (frame + num - 1, 120), (frame + num - 2, 120), (frame + num - 3, 120), (frame + num - 4, 120) }; 
						loop = false;
					}
					break;

				case State.StartGrooming:
					if (this.sprite.CurrentAnimation is null)
					{
						Log.W("Cat: Into StartGrooming");
						frame = (int)Frame.Groom;
						frames = new[] { (frame - 3, 80), (frame - 2, 80), (frame - 1, 80) }; 
						loop = false;
					}
					break;
					
				case State.Grooming:
					if (this.sprite.CurrentAnimation is null || this.sprite.CurrentFrame == (int)Frame.Groom - 1)
					{
						Log.W("Cat: Into Grooming");
						List<(int, int)> frameList = new ();

						frame = (int)Frame.Groom;
						int loops = Game1.random.Next(1, 3);
						int subloops = Game1.random.Next(2, 6);
						for (int i = 0; i < loops; ++i)
						{
							for (int j = 0; j < subloops; ++j)
							{
								frameList.AddRange(new (int, int) [] {
									(frame, 120), (frame + 1, 120), (frame + 2, 120),
									(frame + 3, 120), (frame + 4, 120), (frame + 1, 120)
								});
							}
							frameList.Add((frame, (int)(Game1.random.NextDouble() * 60 * 10 + 300)));
						}
						frameList.Add((frame, 1400));
						frames = frameList.ToArray();
						loop = false;
					}
					break;

				case State.StopGrooming:
					if (this.sprite.CurrentAnimation is null || this.sprite.CurrentFrame == (int)Frame.Groom)
					{
						Log.W("Cat: Into StopGrooming");
						frame = (int)Frame.Groom;
						frames = new[] { (frame - 1, 120), (frame - 2, 120), (frame - 3, 120) }; 
						loop = false;
					}
					break;
			}

			if (this._state is State.Running or State.StopRunning) {
				/*
				// Bounce offset while running
				var jump =  RunFrameCount % (Math.Abs(sprite.CurrentFrame - RunFrame) / 2f) * (sprite.CurrentFrame < RunFrameCount / 2 ? 1f : -1f) * 2f;
				jump = !float.IsNaN(jump) ? jump : 0f;
				Log.D($"Jump: {yJumpOffset} + {jump:.00} = {yJumpOffset + jump}");
				yJumpOffset += jump;
				*/

				// Running velocity
				this.position.X += 6f * (this.flip ? -1f : 1f);

				// Testing: Run back and forth across 15 tiles horizontally
				if (this.position.X < this.startingPosition.X - 10f * Game1.tileSize)
				{
					this.flip = false;
					this.sprite.CurrentAnimation = null;
					this._state = State.Idle;
				}
				else if (this.position.X > this.startingPosition.X + 10f * Game1.tileSize)
				{
					this.flip = true;
					this.sprite.CurrentAnimation = null;
					this._state = State.Idle;
				}
			}

			if (frames is not null)
			{
				this.sprite.setCurrentAnimation(frames
					.Select((pair) => new FarmerSprite.AnimationFrame(
						frame: pair.f,
						milliseconds: pair.ms,
						secondaryArm: false,
						flip: this.flip))
					.ToList());
				this.sprite.endOfAnimationFunction = this.DoneAnimating;
				this.sprite.loop = loop;
			}

			return base.update(time, environment);
		}

		public override void draw(SpriteBatch b)
		{
			if (this.sprite is null)
				return;

			b.Draw(Game1.shadowTexture,
				position: Game1.GlobalToLocal(
					viewport: Game1.viewport,
					globalPosition: this.position + new Vector2(x: 0f, y: -4f)),
				sourceRectangle: Game1.shadowTexture.Bounds,
				color: Color.White,
				rotation: 0f,
				origin: Utility.PointToVector2(Game1.shadowTexture.Bounds.Center),
				scale: Game1.pixelZoom - 1 + Math.Max(-3f, (this.yJumpOffset + this.yOffset) / Game1.tileSize),
				effects: SpriteEffects.None,
				layerDepth: (this.position.Y - 1f) / 10000f);

			this.sprite.draw(b,
				screenPosition: Game1.GlobalToLocal(
					viewport: Game1.viewport,
					globalPosition: this.position
						+ new Vector2(Game1.tileSize) * -0.5f
						+ new Vector2(x: 0, y: this.yJumpOffset + this.yOffset)),
				layerDepth: this.position.Y / 10000f + this.position.X / 100000f,
				xOffset: 0,
				yOffset: 0,
				c: Color.White,
				flip: this.flip,
				scale: Game1.pixelZoom);
		}
	}
}
