using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace Hikawa.GameObjects.Critters
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
		private const int RunFrame = 4;
		private const int SitDownFrame = 12;
		private const int SitDownFrameCount = 4;
		private const int GroomFrame = 19;

		// Starting idle frames
		internal static readonly int StandingBaseFrame = 0;
		internal static readonly int SittingBaseFrame = 16;

		public Cat(Vector2 position, int baseFrame, int scareRange, bool flip)
		{
			var asset = ModEntry.Instance.Helper.Content.GetActualAssetKey(Path.Combine(
				ModConsts.SpritesPath, ModConsts.CatSpritesFile + ".png"));
			sprite = new AnimatedSprite(asset, baseFrame, 32, 32);

			_state = State.Idle;
			_scareRange = scareRange;
			
			startingPosition = this.position = position * 64f + new Vector2(32f);
			this.baseFrame = baseFrame;
			this.flip = flip;

			Log.W($"Cat generated at {startingPosition.ToString()}");
		}
		
		/// <summary>
		/// After a non-looping animation ends, reroute behaviours.
		/// Lead-in to the first frame of the next animation to
		/// avoid flashing a frame of the null animation base frame.
		/// </summary>
		private void DoneAnimating(Farmer who)
		{
			var message = "other";
			switch (_state)
			{
				case State.StartRunning:
					message = "StartRunning";
					_state = State.Running;
					break;
				case State.Running:
					message = "Running";
					break;
				case State.StartSitting:
					message = "StartSitting";
					_state = State.Sitting;
					break;
				case State.StartGrooming:
					message = "StartGrooming";
					_state = State.Grooming;
					break;
				case State.Grooming:
					message = "Grooming";
					_state = State.StopGrooming;
					break;
				case State.StopGrooming:
					message = "StopGrooming";
					_state = State.Sitting;
					break;
				default:
					_state = State.Idle;
					break; 
			}

			sprite.CurrentAnimation = null;
			Log.W($"Cat: Done {message}");
		}
		
		private void PlayMeow(Farmer who)
		{
			if (Utility.isOnScreen(position, 64))
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
			return Utility.isThereAFarmerWithinDistance(position / 64f, range, environment);
		}
		
		public override bool update(GameTime time, GameLocation environment)
		{
			//if (_state != State.Running && _scareRange > 0 && IsFarmerInRange(environment, _scareRange) != null)
				//_state = State.StartRunning;
			//if (_state != State.Grooming && _scareRange > 0 && IsFarmerInRange(environment, _scareRange) != null)
				//_state = State.StartGrooming;

			// Pick behaviour for this frame, starting animations on the first frame of their state
			switch (_state)
			{
				case State.Idle:
					if (sprite.CurrentAnimation == null)
						sprite.CurrentFrame = baseFrame;

					// Testing animations: Sitting
					if (_scareRange > 0 && IsFarmerInRange(environment, _scareRange) != null)
					{
						Log.W("Cat: Triggered StartSitting");
						_state = State.StartSitting;
					}

					// todo: standing animations
					break;

				case State.StartRunning:
					if (sprite.CurrentAnimation == null)
					{
						Log.W("Cat: Into StartRunning");
						var animFrames = new List<FarmerSprite.AnimationFrame>
						{
							new FarmerSprite.AnimationFrame(1, 60, false, flip),
							new FarmerSprite.AnimationFrame(2, 70, false, flip),
							new FarmerSprite.AnimationFrame(3, 80, false, flip,
								DoneAnimating)
						};
						sprite.setCurrentAnimation(animFrames);
						sprite.loop = false;
					}
					break;

				case State.Running:
					if (sprite.CurrentAnimation == null || sprite.CurrentFrame == RunFrame - 1)
					{
						Log.W("Cat: Into Running");
						var animFrames = new List<FarmerSprite.AnimationFrame>();
						for (var i = 0; i < 8; ++i)
						{
							var ms = i switch
							{
								1 => 70,
								2 => 70,
								3 => 80,
								4 => 90,
								5 => 80,
								6 => 70,
								7 => 65,
								8 => 60,
								_ => -1
							};
							animFrames.Add(new FarmerSprite.AnimationFrame(RunFrame + i, ms, false, flip));
						}
						sprite.setCurrentAnimation(animFrames);
						sprite.loop = true;
					}
					break;

				case State.StopRunning:
					if (sprite.CurrentAnimation == null || sprite.CurrentFrame == RunFrame + 8)
					{
						Log.W("Cat: Into StopRunning");
						var animFrames = new List<FarmerSprite.AnimationFrame>
						{
							new FarmerSprite.AnimationFrame(RunFrame - 2, 70, false, flip),
							new FarmerSprite.AnimationFrame(RunFrame - 3, 90, false, flip,
								DoneAnimating)
						};

						sprite.setCurrentAnimation(animFrames);
						sprite.loop = false;
					}
					break;

				case State.StartSitting:
					if (sprite.CurrentAnimation == null)
					{
						Log.W("Cat: Into StartSitting");
						var animFrames = new List<FarmerSprite.AnimationFrame>
						{
							new FarmerSprite.AnimationFrame(SitDownFrame, 120, false, flip),
							new FarmerSprite.AnimationFrame(SitDownFrame + 1, 120, false, flip),
							new FarmerSprite.AnimationFrame(SitDownFrame + 2, 120, false, flip),
							new FarmerSprite.AnimationFrame(SitDownFrame + 3, 120, false, flip,
								DoneAnimating)
						};

						sprite.setCurrentAnimation(animFrames);
						sprite.loop = false;
					}
					break;
					
				case State.Sitting:
					if (sprite.CurrentAnimation == null)
					{
						sprite.CurrentFrame = SittingBaseFrame;
					}

					if (Game1.random.NextDouble() < 0.008d)
					{
						var roll = Game1.random.Next(1, 3);
						Log.W($"Cat: Sitting rolled {roll}");
						switch (roll)
						{
							case 1:
								Log.W("Cat: Picked StartGrooming from Sitting");
								_state = State.StartGrooming;
								break;
							case 2:
								Log.W("Cat: Picked SKIP from Sitting");
								// todo: sitting animations
								break;
						}
					}
					break;

				case State.StopSitting:
					if (sprite.CurrentAnimation == null)
					{
						Log.W("Cat: Into StopSitting");
						var animFrames = new List<FarmerSprite.AnimationFrame>
						{
							new FarmerSprite.AnimationFrame(SitDownFrame + SitDownFrameCount - 1, 120, false, flip),
							new FarmerSprite.AnimationFrame(SitDownFrame + SitDownFrameCount - 2, 120, false, flip),
							new FarmerSprite.AnimationFrame(SitDownFrame + SitDownFrameCount - 3, 120, false, flip),
							new FarmerSprite.AnimationFrame(SitDownFrame + SitDownFrameCount - 4, 120, false, flip,
								DoneAnimating)
						};

						sprite.setCurrentAnimation(animFrames);
						sprite.loop = false;
					}
					break;

				case State.StartGrooming:
					if (sprite.CurrentAnimation == null)
					{
						Log.W("Cat: Into StartGrooming");
						var animFrames = new List<FarmerSprite.AnimationFrame>
						{
							new FarmerSprite.AnimationFrame(GroomFrame - 3, 80, false, flip),
							new FarmerSprite.AnimationFrame(GroomFrame - 2, 80, false, flip),
							new FarmerSprite.AnimationFrame(GroomFrame - 1, 80, false, flip,
								DoneAnimating)
						};

						sprite.setCurrentAnimation(animFrames);
						sprite.loop = false;
					}
					break;
					
				case State.Grooming:
					if (sprite.CurrentAnimation == null || sprite.CurrentFrame == GroomFrame -1)
					{
						Log.W("Cat: Into Grooming");
						var animFrames = new List<FarmerSprite.AnimationFrame>();

						var loops = Game1.random.Next(1, 3);
						var subloops = Game1.random.Next(2, 6);
						for (var i = 0; i < loops; ++i)
						{
							for (var j = 0; j < subloops; ++j)
							{
								animFrames.Add(new FarmerSprite.AnimationFrame(GroomFrame, 120, false, flip));
								animFrames.Add(new FarmerSprite.AnimationFrame(GroomFrame + 1, 120, false, flip));
								animFrames.Add(new FarmerSprite.AnimationFrame(GroomFrame + 2, 120, false, flip));
								animFrames.Add(new FarmerSprite.AnimationFrame(GroomFrame + 3, 120, false, flip));
								animFrames.Add(new FarmerSprite.AnimationFrame(GroomFrame + 4, 120, false, flip));
								animFrames.Add(new FarmerSprite.AnimationFrame(GroomFrame + 1, 120, false, flip));
							}
							animFrames.Add(new FarmerSprite.AnimationFrame(
								GroomFrame, (int)(Game1.random.NextDouble() * 60 * 10 + 300), false, flip));
						}
						animFrames.Add(new FarmerSprite.AnimationFrame(
							GroomFrame, 1400, false, flip, DoneAnimating));

						sprite.setCurrentAnimation(animFrames);
						sprite.loop = false;
					}
					break;

				case State.StopGrooming:
					if (sprite.CurrentAnimation == null || sprite.CurrentFrame == GroomFrame)
					{
						Log.W("Cat: Into StopGrooming");
						var animFrames = new List<FarmerSprite.AnimationFrame>
						{
							new FarmerSprite.AnimationFrame(GroomFrame - 1, 120, false, flip),
							new FarmerSprite.AnimationFrame(GroomFrame - 2, 120, false, flip),
							new FarmerSprite.AnimationFrame(GroomFrame - 3, 120, false, flip,
								DoneAnimating)
						};

						sprite.setCurrentAnimation(animFrames);
						sprite.loop = false;
					}
					break;
			}

			if (_state == State.Running || _state == State.StopRunning) {
				/*
				// Bounce offset while running
				var jump =  RunFrameCount % (Math.Abs(sprite.CurrentFrame - RunFrame) / 2f) * (sprite.CurrentFrame < RunFrameCount / 2 ? 1f : -1f) * 2f;
				jump = !float.IsNaN(jump) ? jump : 0f;
				Log.D($"Jump: {yJumpOffset} + {jump:.00} = {yJumpOffset + jump}");
				yJumpOffset += jump;
				*/

				// Running velocity
				position.X += 6f * (flip ? -1f : 1f);

				// Testing: Run back and forth across 15 tiles horizontally
				if (position.X < startingPosition.X - 10f * 64f)
				{
					flip = false;
					sprite.CurrentAnimation = null;
					_state = State.Idle;
				}
				else if (position.X > startingPosition.X + 10f * 64f)
				{
					flip = true;
					sprite.CurrentAnimation = null;
					_state = State.Idle;
				}
			}

			return base.update(time, environment);
		}

		public override void draw(SpriteBatch b)
		{
			if (sprite == null)
				return;
			b.Draw(Game1.shadowTexture,
				Game1.GlobalToLocal(Game1.viewport, position + new Vector2(0f, -4f)),
				Game1.shadowTexture.Bounds, Color.White,
				0f,
				new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y),
				3f + Math.Max(-3f, (yJumpOffset + yOffset) / 64f),
				SpriteEffects.None,
				(position.Y - 1f) / 10000f);
			sprite.draw(b,
				Game1.GlobalToLocal(Game1.viewport, position + new Vector2(-64f, -128f + yJumpOffset + yOffset)),
				position.Y / 10000f + position.X / 100000f,
				0,
				0,
				Color.White,
				flip,
				4f);
		}
	}
}
