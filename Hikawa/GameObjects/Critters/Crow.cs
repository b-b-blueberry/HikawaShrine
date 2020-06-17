using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using xTile.Dimensions;

namespace Hikawa.GameObjects.Critters
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

		public Crow(bool isDeimos, Vector2 position, int hopRange)
		{
			var asset = ModEntry.Instance.Helper.Content.GetActualAssetKey(
				Path.Combine(ModConsts.SpritesPath, ModConsts.CrowSpritesFile + ".png"));
			sprite = new AnimatedSprite(asset, 0, 32, 32);
			
			_isDeimos = isDeimos;
			_hopRange = hopRange;
			_state = State.Idle;
			
			startingPosition = this.position = position * 64f + new Vector2(32f);
			baseFrame = _crowBaseFrame = _isDeimos ? 4 : 0;
			flip = _isDeimos;

			Log.W($"Perched crow {WhichCrow()} generated at {startingPosition.ToString()}");
		}

		public void Hop(Farmer who)
		{
			gravityAffectedDY = -_hopRange;
		}

		private void DoneAnimating(Farmer who)
		{
			_state = Game1.random.NextDouble() < 0.5d ? State.Idle : State.Animating;
		}

		private void LookAtPlayer(Farmer who, GameLocation environment)
		{
			var farmer = IsFarmerInRange(environment, 16);
			if (farmer == null || _state != State.Looking)
				return;

			var angle = ModEntry.Vector.RadiansBetween(position, farmer.Position);
			Log.W($"LookAt angle between crow ({position}) and {farmer.Name} ({farmer.Position}) == {angle}f");

			// todo: select current frame based on angle, consider 'flip'
			sprite.currentFrame = 0;
		}
		
		private Farmer IsFarmerInRange(GameLocation environment, int range)
		{
			return Utility.isThereAFarmerWithinDistance(position / 64f, range, environment);
		}

		public override bool update(GameTime time, GameLocation environment)
		{
			// Hopping motion - don't hop through buildings, only hop onto AlwaysFront tiles
			if (yJumpOffset < 0f && !environment.isCollidingPosition(getBoundingBox(-2, 0), Game1.viewport,
				false, 0, false, null, false, false, true))
			{
				var nextTileOver = new Location(
					(int)Math.Floor((position.X + (flip ? 1f : -1f)) / 64f),
					(int)Math.Floor(position.Y / 64f));
				if (environment.Map.GetLayer("AlwaysFront").Tiles[nextTileOver.X, nextTileOver.Y] != null)
					position.X += 2f * (flip ? 1f : -1f);

				sprite.CurrentFrame = yJumpOffset > -1f ? 8 : 9;
				return base.update(time, environment);
			}
			
			// State picker
			switch (_state)
			{
				case State.Idle:
					if (sprite.CurrentAnimation == null && yJumpOffset >= 0f && Game1.random.NextDouble() < 0.002d)
					{
						Log.D($"{WhichCrow()}: Idle reroll");
						switch (Game1.random.Next(4))
						{
							case 0:
								Log.W($"{WhichCrow()}: Picking Sleeping from Idle");
								_state = State.Sleeping;
								break;
							case 1:
								Log.W($"{WhichCrow()}: Picking Animating from Idle");
								_state = State.Animating;
								break;
							case 2:
							case 3:
								if (_hopRange == 0)
									break;

								Log.W($"{WhichCrow()}: Picking Hop from Idle");
								Hop(null);
								break;
							case 4:
								Log.W($"{WhichCrow()}: Picking Looking from Idle");
								if (IsFarmerInRange(environment, 16) != null)
								{
									Log.D("Success.");
									_state = State.Looking;
								}
								else
								{
									Log.D("Missed.");
								}
								break;
						}
					}
					else if (sprite.CurrentAnimation == null)
					{
						sprite.currentFrame = _crowBaseFrame;
					}
					break;

				case State.Animating:
					if (sprite.CurrentAnimation == null)
					{
						Log.D($"Animating {WhichCrow()}");
						var animFrames = new List<FarmerSprite.AnimationFrame>();
						if (_isDeimos)
						{
							// Preening
							var loops = Game1.random.Next(2, 4);
							animFrames.Add(new FarmerSprite.AnimationFrame((short)_crowBaseFrame, 960, false, flip));
							for (var i = 0; i < loops; ++i)
							{
								animFrames.Add(new FarmerSprite.AnimationFrame((short)_crowBaseFrame + 1, 1200, false, flip));
								var subloops = Game1.random.Next(1, 3);
								for (var j = 0; j < subloops; ++j)
								{
									animFrames.Add(new FarmerSprite.AnimationFrame((short)_crowBaseFrame + 2, 560, false, flip));
									animFrames.Add(new FarmerSprite.AnimationFrame((short)_crowBaseFrame + 3, 360, false, flip));
								}
								animFrames.Add(new FarmerSprite.AnimationFrame(
									(short)_crowBaseFrame + 2, Game1.random.Next(200, 600) * 8, false, flip));
							}
							animFrames.Add(new FarmerSprite.AnimationFrame((short)_crowBaseFrame + 1, 360, false, flip, DoneAnimating));
							Log.D($"Looping for {loops}, with {loops * (loops / 2)} subloops");
						}
						else
						{
							// Peeking
							var shuteye = Game1.random.NextDouble() < 0.25;
							animFrames.Add(new FarmerSprite.AnimationFrame((short)_crowBaseFrame, 1200, false, flip));
							animFrames.Add(new FarmerSprite.AnimationFrame((short)_crowBaseFrame + 1, 440, false, flip));
							animFrames.Add(new FarmerSprite.AnimationFrame((short)_crowBaseFrame + 2, 1960, false, flip));
							animFrames.Add(new FarmerSprite.AnimationFrame(
								(short)_crowBaseFrame + (shuteye ? 2 : 3), shuteye ? 12200 : 6600, false, flip));
							animFrames.Add(new FarmerSprite.AnimationFrame((short)_crowBaseFrame + 2, 160, false, flip));
							animFrames.Add(new FarmerSprite.AnimationFrame((short)_crowBaseFrame + 1, 320, false, flip));
							animFrames.Add(new FarmerSprite.AnimationFrame((short)_crowBaseFrame, 3600, false, flip, DoneAnimating));
							Log.D($"Shuteye: {shuteye}");
						}
						sprite.setCurrentAnimation(animFrames);
						sprite.loop = false;
					}
					break;

				case State.Sleeping:
					if (sprite.CurrentAnimation == null)
					{
						sprite.currentFrame = 3;
					}
					if (Game1.random.NextDouble() < 0.002 && sprite.CurrentAnimation == null)
					{
						Log.W($"{WhichCrow()}: Picking Idle from Sleeping");
						_state = State.Idle;
					}
					break;

				case State.Looking:
					if (sprite.CurrentAnimation == null)
					{
						LookAtPlayer(null, environment);
					}
					break;
			}

			return base.update(time, environment);
		}

		private string WhichCrow()
		{
			return _isDeimos ? "Deimos" : "Phobos";
		}

		public override void drawAboveFrontLayer(SpriteBatch b)
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
				1f - (_isDeimos? 1f / 10000f : 0f));

			sprite.draw(b,
				Game1.GlobalToLocal(Game1.viewport, position + new Vector2(-64f, -128f + yJumpOffset + yOffset)),
				1f - (_isDeimos? 1f / 10000f : 0f),
				0,
				0,
				Color.White,
				flip,
				4f);
		}
	}
}
