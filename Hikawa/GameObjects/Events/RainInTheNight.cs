using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Events;
using xTile.Dimensions;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Hikawa.GameObjects.Events
{
	public class RainInTheNight : FarmEvent
	{
		private bool _isBlack;
		private bool _awaitingInput;
		private float _crystalGlareOpacity;
		private float _crystalBallOpacity;
		private bool _crystalGlareFadeIn;
		private bool _crystalBallFadeIn;
		private bool _monologue1;
		private bool _monologue1plus;
		private bool _afterMonologue1;
		private bool _panning;
		private bool _monologue2;
		private bool _afterMonologue2;
		private int _fire;
		private int _timer;
		private float _cameraSpeedMult;
		private bool _terminate;

		private readonly Vector2 _targetLocation;
		private readonly Farm _farm;

		private readonly Texture2D _texture;
		private static readonly int X = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Center.X;
		private static readonly int Y = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Center.Y;
		private static float _yOffset;

		private static readonly Rectangle SourceRectGlare = new Rectangle(
			208, 16, 112, 32);
		private static readonly List<Rectangle> SourceRects = new List<Rectangle>
		{
			// 0 4 3 1 2
			new Rectangle(64, 32, 128, 38),
			new Rectangle(160, 74, 48, 64),
			new Rectangle(112, 74, 48, 64),
			new Rectangle(0, 80, 64, 64),
			new Rectangle(64, 80, 48, 64),
		};
		private static readonly Rectangle DestRectGlare = new Rectangle(
			X, Y - Y / 3 * 2, SourceRectGlare.Width * 4, SourceRectGlare.Height * 4);
		private static readonly List<Rectangle> DestRects = new List<Rectangle>
		{
			// 0 4 3 1 2
			new Rectangle(X - 16 * 4, Y + Y / 4 - 16 * 4, 128 * 4, 38 * 4),
			new Rectangle(X + 32 * 4, Y + Y / 4, 48 * 4, 64 * 4),
			new Rectangle(X + 16 * 4, Y + Y / 4, 48 * 4, 64 * 4),
			new Rectangle(X - 24 * 4, Y + Y / 4, 64 * 4, 64 * 4),
			new Rectangle(X + 00 * 4, Y + Y / 4, 48 * 4, 64 * 4),
		};

		private IModHelper Helper => ModEntry.Instance.Helper;
		private ITranslationHelper i18n => Helper.Translation;

		public NetFields NetFields { get; } = new NetFields();

		public RainInTheNight()
		{
			Log.W("RainInTheNight");
			_farm = Game1.getLocationFromName("Farm") as Farm;
			_farm.updateMap();

			try
			{
				var textureName = Path.Combine(ModConsts.SpritesPath, $"{ModConsts.ExtraSpritesFile}.png");
				_texture = Helper.Content.Load<Texture2D>(textureName);
			}
			catch (Exception e)
			{
				Log.E($"Failed to load the sprite we need -- skipping rest of the event. Report this pls\n{e}");
				_terminate = true;
				return;
			}
			
			// Find a decent point on the map to stick the banana, starting with our preferred coordinates
			var r = new Random((int)Game1.uniqueIDForThisGame + (int)Game1.stats.DaysPlayed);
			var whereabouts = ModConsts.StoryPlantPositionsForFarmTypes[Game1.whichFarm];
			const int acceptableRadius = 2;
			var surroundingTilesToTry = new List<Vector2>();
			var attempts = 50;
			while (attempts --> 0)
			{
				// Grab all surrounding tiles
				for (var y = 0; y < acceptableRadius * 2; ++y)
				{
					for (var x = 0; x < acceptableRadius * 2; ++x)
					{
						surroundingTilesToTry.Add(new Vector2(
							(int)whereabouts.X - acceptableRadius + x, 
							(int)whereabouts.Y - acceptableRadius + y));
					}
				}

				// Shuffle them
				var n = surroundingTilesToTry.Count;
				while (n > 1)
				{
					--n;
					var k = Game1.random.Next(n);
					var temp = surroundingTilesToTry[k];
					surroundingTilesToTry[k] = surroundingTilesToTry[n];
					surroundingTilesToTry[n] = temp;
				}
				
				// Check for an open space to stick the banana
				foreach (var tile in surroundingTilesToTry)
				{
					_targetLocation = tile;
						if (!_farm.isTileLocationTotallyClearAndPlaceable(_targetLocation)
						    || !_farm.isTileLocationOpen(new Location((int)_targetLocation.X, (int)_targetLocation.Y)))
							continue;

						Log.W($"Targeting point at {_targetLocation.ToString()}");
						return;
				}

				// Look again if we're still holding a banana
				whereabouts = new Vector2(
					r.Next(5, _farm.map.GetLayer("Back").TileWidth - 4), 
					r.Next(5, _farm.map.GetLayer("Back").TileHeight - 4));
				Log.W($"Trying again for some point around {whereabouts.ToString()}");
			}
		}

		public bool setUp() {
			_isBlack = true;
			_monologue1 = true;

			// Set location
			Game1.currentLocation = _farm;
			_farm.resetForPlayerEntry();
			Game1.changeMusicTrack("nightTime");

			// Set viewport
			Game1.viewport.X = (int)Math.Max(0, Math.Min(_farm.Map.DisplayWidth,
				_targetLocation.X * 64f + 32f - Game1.viewport.Width / 2f));
			Game1.viewport.Y = (int)Math.Max(0, Math.Min(_farm.Map.DisplayHeight,
				_targetLocation.Y * 64f + 640f - Game1.viewport.Height / 2f));

			Log.D($"Viewport : {Game1.viewport.X}, {Game1.viewport.Y} ({Game1.viewport.Width} x {Game1.viewport.Height})");

			Game1.timeOfDay = 2400;
			Game1.displayHUD = false;
			Game1.viewportFreeze = true;
			Game1.displayFarmer = false;
			Game1.freezeControls = true;
			return false;
		}

		public bool tickUpdate(GameTime time)
		{
			Game1.UpdateGameClock(time);
			_farm.UpdateWhenCurrentLocation(time);
			_farm.updateEvenIfFarmerIsntHere(time);
			Game1.UpdateOther(time);

			if (_terminate)
				return true;

			_timer += time.ElapsedGameTime.Milliseconds;
			
			_yOffset = 6f * (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / (Math.PI * 384f));

			// PHASE 1 -- INPUT
			if (_timer > 3333 && _monologue1 && !_monologue1plus)
			{
				if (_awaitingInput && !Game1.dialogueUp)
				{
					Log.D("Phase 1 end");

					_awaitingInput = false;
					_monologue1 = false;
					_monologue1plus = true;
					_timer = 3333;
				}
				else if (!Game1.dialogueUp)
				{
					Log.W("Phase 1: Monologue 1");
					
					Game1.drawObjectDialogue(i18n.Get("talk.story.plant.mono1"));
					_awaitingInput = true;
				}
			}

			// PHASE 1 AND A BIT -- INPUT
			if (_timer > 5000 && _monologue1plus && !_afterMonologue1)
			{
				if (_awaitingInput && !Game1.dialogueUp)
				{
					Log.D("Phase 1 end");

					_awaitingInput = false;
					_monologue1plus = false;
					_afterMonologue1 = true;
					_timer = 5000;
				}
				else if (!Game1.dialogueUp)
				{
					Log.W("Phase 1 and a bit: Monologue 1 plus");
					
					Game1.drawObjectDialogue(i18n.Get("talk.story.plant.mono1plus"));
					_awaitingInput = true;
				}
			}

			// PHASE 2 -- TIMER
			// After first monologue, fade out of black, then start panning upward from our start point
			if (_timer > 6250 && _afterMonologue1 && !_awaitingInput)
			{
				Log.W("Phase 2: Wait and pan");

				_isBlack = false;
				Game1.nonWarpFade = true;
				Game1.fadeClear();
				if (Game1.musicPlayerVolume > 0f)
				{
					Game1.changeMusicTrack("none");
					Game1.playSound(ModConsts.ContentPrefix + "dark_despair");
				}

				_timer = 6250;
				_afterMonologue1 = false;
				_panning = true;
			}

			// Pan viewport upward to our target
			if (_panning)
			{
				if (_cameraSpeedMult < 0.175f)
					_cameraSpeedMult += 0.0006f;
				Game1.viewport.Y -= (int)Math.Ceiling(Math.Max(0.01f, time.ElapsedGameTime.Milliseconds * (0.175f - _cameraSpeedMult)));
				if (Game1.viewport.Y <= (_targetLocation.Y + 2) * 64f - Game1.viewport.Height / 2f)
				{
					Log.D($"Reached target at {_targetLocation.ToString()}");

					_panning = false;
					_monologue2 = true;
					_timer = 9001;
				}
			}

			// Fade in the crystal ball
			if (_timer > 8400 && _panning && !_crystalGlareFadeIn && _fire == 0)
				_crystalGlareFadeIn = true;
			else if (_crystalGlareFadeIn && _crystalGlareOpacity >= 1f)
				_crystalGlareFadeIn = false;
			else if (_crystalGlareFadeIn)
				_crystalGlareOpacity += (_crystalGlareOpacity < 0.3f 
					? 0.03f 
					: (_crystalGlareOpacity < 0.6f 
						? 0.045f 
						: 0.06f)) / time.ElapsedGameTime.Milliseconds;

			if (_timer > 9500 && _panning && !_crystalBallFadeIn && _fire == 0)
				_crystalBallFadeIn = true;
			else if (_crystalBallFadeIn && _crystalBallOpacity >= 1f)
				_crystalBallFadeIn = false;
			else if (_crystalBallFadeIn)
				_crystalBallOpacity += (_crystalBallOpacity < 0.3f 
					? 0.035f 
					: (_crystalBallOpacity < 0.6f 
						? 0.05f 
						: 0.065f)) / time.ElapsedGameTime.Milliseconds;

			// PHASE 3 -- INPUT
			// After panning to the target, start the fireworks
			if (_timer > 10500 && !_panning && _monologue2)
			{
				if (_awaitingInput && !Game1.dialogueUp)
				{
					Log.D("Phase 3 end");

					_awaitingInput = false;
					_monologue2 = false;
					_afterMonologue2 = true;
				}
				else if (!Game1.dialogueUp)
				{
					Log.W("Phase 3: Monologue 2");
					
					Game1.drawObjectDialogue(i18n.Get("talk.story.plant.mono2"));
					_timer = 10500;
					_awaitingInput = true;
				}
			}
			
			// PHASE 3 AND A BIT -- TIMER
			if (_timer > 11250 && _afterMonologue2)
			{
				_afterMonologue2 = false;
				_fire = 1;
			}

			// Fade out the crystal ball
			if (_fire >= 1 && _crystalBallOpacity > 0f)
				_crystalBallOpacity -= (_crystalBallOpacity < 0.3f
					? 0.03f
					: (_crystalBallOpacity < 0.6f
						? 0.045f
						: 0.06f)) / time.ElapsedGameTime.Milliseconds;

			// PHASE 4 -- TIMER
			// Start the fireworks after dialogue is closed
			if (_crystalBallOpacity < 0.7f && _fire == 1)
			{
				Log.W("Phase 4: Fireworks");
				Log.W("Zap 1");

				_fire = 2;
				var where = new Vector2(_targetLocation.X - 3, _targetLocation.Y - 1);
				var lightningStrike = new Farm.LightningStrikeEvent
				{
					createBolt = true,
					boltPosition = where * 64f
				};
				_farm.lightningStrikeEvent.Fire(lightningStrike);
				Game1.playSound("thunder");
			}
			if (_crystalBallOpacity < 0.55f && _fire == 2)
			{
				Log.W("Zap 2");

				_fire = 3;
				var where = new Vector2(_targetLocation.X + 1, _targetLocation.Y + 2);
				var lightningStrike = new Farm.LightningStrikeEvent
				{
					createBolt = true,
					boltPosition = where * 64f
				};
				_farm.lightningStrikeEvent.Fire(lightningStrike);
				Game1.playSound("thunder");
			}
			if (_crystalBallOpacity < 0.15f && _fire == 3)
			{
				Log.W("Zap 3");

				_fire = 4;
				_timer = 11250;
				var where = new Vector2(_targetLocation.X + 1, _targetLocation.Y + 2);
				var lightningStrike = new Farm.LightningStrikeEvent
				{
					createBolt = true,
					boltPosition = where * 64f,
					bigFlash = true
				};
				_farm.lightningStrikeEvent.Fire(lightningStrike);

				Game1.currentLightSources.Add(new LightSource(
					1, 
					_targetLocation, 
					1f, 
					Color.Black, 
					942069));
			}

			// Wait a moment before sticking the banana
			if (!(_crystalBallOpacity > 0) && _fire == 4 && _timer > 11250)
			{
				_fire = 5;
			}

			// PHASE 5 -- TIMER
			// Throw down that banana
			if (!(_crystalBallOpacity > 0f) && _fire == 5)
			{
				Log.W("Phase 5: Plant");

				_fire = 6;
				
				var assetKey = Helper.Content.GetActualAssetKey(
					Path.Combine(ModConsts.SpritesPath, $"{ModConsts.ExtraSpritesFile}.png"));
				ModEntry.Multiplayer.broadcastSprites(_farm,
					new TemporaryAnimatedSprite(
						assetKey,
						new Rectangle(
							0,
							48,
							16, 32), 
						900f, 
						4, 
						1, 
						_targetLocation * 64f, 
						false, 
						false)
					{
						scale = 4f,
						pulse = true,
						pulseAmount = 1.5f,
						pulseTime = 512f,
						holdLastFrame = true
					});
			}
			
			// Fade out the crystal glare
			if (_fire >= 4)
				_crystalGlareOpacity -= (_crystalGlareOpacity < 0.3f
					? 0.04f
					: (_crystalGlareOpacity < 0.6f
						? 0.05f
						: 0.06f)) / time.ElapsedGameTime.Milliseconds;

			// PHASE 0 -- TIMER
			// Fade to black outro
			if (_timer > 18000 && !Game1.fadeToBlack && _fire == 6)
			{
				if (!Game1.dialogueUp && _awaitingInput)
				{
					_timer = 22000;
				}

				Log.W("Phase 0: Fade out");
				
				Game1.globalFadeToBlack(AfterLastFade);
				Game1.changeMusicTrack("none");
				Game1.freezeControls = false;
				_fire = 7;
				_awaitingInput = true;
			}
			
			// End the rain
			if (_timer > 25000 && !Game1.dialogueUp && !_terminate && _fire == 7)
			{
				Log.D("End of RainInTheNight");

				// Mark for no repeat views once we've sat through all this
				ModEntry.Instance.SaveData.Story[ModData.Chapter.Plant] = ModData.Progress.Stage1;
				_terminate = true;
			}

			return false;
		}
		
		public void AfterLastFade()
		{
			_isBlack = true;
			Game1.globalFadeToClear();
			Game1.drawObjectDialogue(i18n.Get("talk.story.plant.mono3"));
		}

		public void draw(SpriteBatch b)
		{
			// Blackout
			if (_isBlack)
				b.Draw(
					Game1.staminaRect,
					new Rectangle(
						0, 
						0, 
						Game1.graphics.GraphicsDevice.Viewport.Width, 
						Game1.graphics.GraphicsDevice.Viewport.Height), 
					Color.Black);
			
			// Crystal glare
			if (_crystalGlareOpacity > 0f)
				b.Draw(_texture,
					new Rectangle(DestRectGlare.X,
						DestRectGlare.Y + (int)Math.Ceiling(_yOffset),
						DestRectGlare.Width,
						DestRectGlare.Height),
					SourceRectGlare,
					Color.White * _crystalGlareOpacity,
					0f,
					new Vector2(SourceRectGlare.Width / 2, SourceRectGlare.Height / 2), 
					SpriteEffects.None,
					1f);

			// Crystal ball
			if (_crystalBallOpacity > 0f)
				for (var i = 0; i < SourceRects.Count; ++i)
					b.Draw(_texture,
						new Rectangle(
							DestRects[i].X,
							DestRects[i].Y + (int)Math.Ceiling(_yOffset + _yOffset * Math.Abs(DestRects.Count / 2f - i) / 2f),
							DestRects[i].Width,
							DestRects[i].Height),
						SourceRects[i],
						Color.White * _crystalBallOpacity,
						0f,
						new Vector2(SourceRects[i].Width / 2, SourceRects[i].Height / 2),
						SpriteEffects.None,
						0.9f - i / 10000f);
		}

		public void makeChangesToLocation()
		{
			if (!Game1.IsMasterGame)
				return;

			// Drop that banana
			if (_farm.terrainFeatures.ContainsKey(_targetLocation))
			{
				_farm.terrainFeatures.Remove(_targetLocation);
			}
			//_farm.terrainFeatures.Add(_targetLocation, new HikawaBanana());
		}

		public void drawAboveEverything(SpriteBatch b) {}
	}
}
