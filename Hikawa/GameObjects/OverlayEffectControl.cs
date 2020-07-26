using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;

namespace Hikawa.GameObjects
{
	// TODO: SYSTEM: Consider adding List<OverlayEffect> for stacking effects
	// eg. shadow under player + haze
	
	internal class OverlayEffectControl
	{
		internal enum Effect
		{
			None,
			Mist,
			Haze,
			Dark,
			Nighttime,
			StuffAbove,
			StuffBelow,
			Stars,
			Count
		}

		private const float TextureScale = 4f;

		// Variables on-set
		private float _fxXMotion;
		private float _fxYMotion;
		private float _fxOpacity;
		private float _fxRotationRad;
		private float _fxScale;
		private bool _isGluedToViewport;
		private Effect _previousEffect;
		private Effect _currentEffect;
		// TODO: SYSTEM: Have a stack for effects history?

		// Variables changing on-ticked
		private bool _shouldDrawEffects;
		private Texture2D _fxTexture;
		private float _fxXOffset;
		private float _fxYOffset;
		private Vector2 _fxPosition = Vector2.Zero;

		internal OverlayEffectControl()
		{
			Set(Effect.Mist);
		}

		internal bool Set(Effect whichEffect)
		{
			return Set(whichEffect, 1f);
		}

		internal bool Set(Effect whichEffect, float effectScale)
		{
			if (_currentEffect != Effect.None)
				_previousEffect = _currentEffect;

			Reset();
			_currentEffect = whichEffect;
			switch (whichEffect)
			{
				case Effect.Mist:
					_fxTexture = Game1.temporaryContent.Load<Texture2D>(
						"LooseSprites\\steamAnimation");
					_fxXMotion = 0.05f;
					_fxYMotion = 0.02f;
					_fxOpacity = 0.8f;
					_fxRotationRad = (float)(90d * Math.PI / 180d);
					break;

				case Effect.Haze:
					_fxTexture = Game1.temporaryContent.Load<Texture2D>(
						"LooseSprites\\steamAnimation");
					_fxXMotion = 0.005f;
					_fxYMotion = 0.035f;
					_fxOpacity = 0.5f;
					break;

				case Effect.Stars:
					// TODO: CONTENT: Effect.Stars

					Log.E("Did not enable Effect.Stars.");
					break;
					
					_fxYMotion = 0.5f;
					_fxOpacity = 0.8f;
					break;

				case Effect.Dark:
				case Effect.Nighttime:
					_fxTexture = ModEntry.Instance.Helper.Content.Load<Texture2D>(
						Path.Combine(ModConsts.SpritesPath, $"{ModConsts.ExtraSpritesFile}.png"));
					_fxScale = effectScale;
					_isGluedToViewport = true;
					break;

				case Effect.StuffAbove:
					if (!Game1.currentLocation.Name.StartsWith(ModConsts.VortexMapId))
					{
						Log.E("Did not enable Effect.StuffAbove: Not in Vortex.");
						break;
					}

					var str = "Tilesheets:";
					var sources = new List<string>();
					var names = new List<string>();
					foreach (var tilesheet in Game1.currentLocation.Map.TileSheets)
					{
						names.Add(tilesheet.Id);
						sources.Add(tilesheet.ImageSource);
					}
					for (var i = 0; i < sources.Count; ++i)
					{
						str = $"{str}\n{names[i]}: {sources[i]}";
					}
					Log.W(str);

					_fxTexture = ModEntry.Instance.Helper.Content.Load<Texture2D>(
						Game1.currentLocation.Map.GetTileSheet(ModConsts.BusSpritesFile).ImageSource);
					_isGluedToViewport = true;
					
					break;

				case Effect.None:
					Log.W("Did not enable overlay: None set.");
					break;
			}

			if (!_isGluedToViewport)
			{
				_fxPosition = new Vector2(
					Game1.viewport.X,
					Game1.viewport.Y);
			}
			
			return _fxTexture != null;
		}

		internal Effect CurrentEffect()
		{
			return _currentEffect;
		}

		internal bool IsEnabled()
		{
			return _shouldDrawEffects;
		}

		internal void Enable(Effect whichEffect)
		{
			Enable(whichEffect, 1f);
		}

		internal void Enable(Effect whichEffect, float effectScale)
		{
			if (Set(whichEffect, effectScale))
			{
				ModEntry.Instance.Helper.Events.Display.RenderedWorld += OnRenderedWorld;
				if (_currentEffect == Effect.Nighttime)
					ModEntry.Instance.Helper.Events.GameLoop.TimeChanged += OnTimeChanged;

				_shouldDrawEffects = true;

				Log.W($"Enabled {_currentEffect}");
			}
			else
			{
				Log.W($"Did not enable overlay: {_currentEffect}.");
				Disable();
			}
		}

		internal void Disable()
		{
			Log.W($"Disabled {_currentEffect}");
			
			ModEntry.Instance.Helper.Events.Display.RenderedWorld -= OnRenderedWorld;
			if (_currentEffect == Effect.Nighttime)
				ModEntry.Instance.Helper.Events.GameLoop.TimeChanged -= OnTimeChanged;

			Reset();
			_shouldDrawEffects = false;
		}

		internal void Reset()
		{
			_currentEffect = Effect.None;
			_fxTexture = null;
			_fxScale = _fxXMotion = _fxYMotion = _fxOpacity = _fxRotationRad = 0f;
			_fxPosition = Vector2.Zero;
			_isGluedToViewport = false;
		}

		internal void Previous()
		{
			Set(_previousEffect);
		}

		internal void Toggle()
		{ 
			if (_shouldDrawEffects)
				Disable();
			else
				Enable(_currentEffect);
		}

		private void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
		{
			Update(Game1.currentGameTime);
			DrawMist(e.SpriteBatch);
		}
		
		private void OnTimeChanged(object sender, TimeChangedEventArgs e)
		{
			if (_currentEffect != Effect.Nighttime && Game1.timeOfDay >= Game1.getStartingToGetDarkTime())
			{
				_fxOpacity = ModEntry.GetProgressFromEveningIntoNighttime();
			}
		}

		internal void Update(GameTime time)
		{
			if (!_isGluedToViewport)
				_fxPosition -= Game1.getMostRecentViewportMotion();
			
			if (_fxXMotion <= 0) {}
			else
			{
				_fxXOffset -= time.ElapsedGameTime.Milliseconds * _fxXMotion;
				_fxXOffset %= -256f;
			}

			if (_fxYMotion <= 0) {}
			else
			{
				_fxYOffset -= time.ElapsedGameTime.Milliseconds * _fxYMotion;
				_fxYOffset %= -256f;
			}
		}

		/// <summary>
		/// Renders mist like BathHousePool.
		/// </summary>
		internal void DrawMist(SpriteBatch b)
		{
			switch (_currentEffect)
			{
				case Effect.Mist:
				case Effect.Haze:
				case Effect.Stars:
				{
					for (var x = _fxPosition.X + _fxXOffset;
						x < Game1.graphics.GraphicsDevice.Viewport.Width + 256f;
						x += 256f)
					{
						for (var y = _fxPosition.Y;
							y < Game1.graphics.GraphicsDevice.Viewport.Height + 128f;
							y += 256f)
						{
							b.Draw(
								_fxTexture,
								new Vector2(x, y),
								new Rectangle(0, 0, 64, 64),
								Color.White * _fxOpacity,
								_fxRotationRad,
								Vector2.Zero,
								TextureScale,
								SpriteEffects.None,
								1f);
						}
					}

					break;
				}
				
				case Effect.Dark:
				case Effect.Nighttime:
				{
					const int sourceRectDimen = 160;
					const int sourceRectYPos = 272;
					
					var gradientSize = sourceRectDimen * _fxScale;
					var topW = Game1.graphics.GraphicsDevice.Viewport.Width;
					var topH = (int)(Game1.graphics.GraphicsDevice.Viewport.Height - gradientSize * TextureScale) / 2;
					var sideW = (int)(Game1.graphics.GraphicsDevice.Viewport.Width - gradientSize * TextureScale) / 2;
					var sideH = Game1.graphics.GraphicsDevice.Viewport.Height - topH * 2;

					var topleft = new Vector2(
						Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Center.X - gradientSize / 2 * TextureScale,
						Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Center.Y - gradientSize / 2 * TextureScale);

					// Darkness gradient
					b.Draw(
						_fxTexture,
						new Rectangle(
							(int)(topleft.X),
							(int)(topleft.Y),
							(int)(gradientSize * TextureScale),
							(int)(gradientSize * TextureScale)),
						new Rectangle(0, sourceRectYPos, (int)gradientSize, (int)gradientSize),
						Color.White * _fxOpacity,
						0f,
						Vector2.Zero,
						SpriteEffects.None,
						1f);

					// TODO: SYSTEM: Have Effect.Dark give a little more room for very large resolutions

					// Darkness fill for space between gradient and screen border
					var positions = new[]
					{
						new Vector2(topleft.X - sideW, topleft.Y - topH), // Top
						new Vector2(topleft.X + gradientSize * TextureScale, topleft.Y), // Right
						new Vector2(topleft.X - sideW, topleft.Y + gradientSize * TextureScale), // Bottom
						new Vector2(topleft.X - sideW, topleft.Y), // Left
					};

					// TODO: SYSTEM: Correct Effect.Dark after StuffAbove has been sorted

					for (var i = 0; i < positions.Length; ++i)
					{
						// TODO: TESTING: Conditional draw for Overlay Dark border
						if (i % 2 == 0 && topH <= 1f
						    || i % 2 == 1 && sideW <= 1f)
							continue;

						b.Draw(_fxTexture,
							new Rectangle(
								(int) positions[i].X,
								(int) positions[i].Y,
								i % 2 == 0 ? topW : sideW,
								i % 2 == 0 ? topH : sideH),
							new Rectangle(0, sourceRectYPos, 1, 1),
							Color.White * _fxOpacity,
							0f,
							Vector2.Zero,
							SpriteEffects.None,
							1f);
					}

					break;
				}

				case Effect.StuffAbove:
				{
					// TODO: CONTENT: Test Effect.StuffAbove

					var gap = new Vector2(
						Game1.graphics.GraphicsDevice.Viewport.Width - _fxTexture.Width * TextureScale,
						Game1.graphics.GraphicsDevice.Viewport.Height - _fxTexture.Height * TextureScale);
					
					b.Draw(_fxTexture,
						_fxPosition,
						Color.White);

					break;
				}
			}
		}
	}
}
