using System;
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
			Stars,
			Dark
		}

		private const float TextureScale = 4f;

		// Variables on-set
		private float _fxXMotion;
		private float _fxYMotion;
		private float _fxOpacity;
		private float _fxRotationRad;

		// Variables changing on-ticked
		private bool _shouldDrawEffects;
		private Texture2D _fxTexture;
		private float _fxXOffset;
		private float _fxYOffset;
		private Vector2 _fxPosition = Vector2.Zero;
		private Effect _currentEffect;

		internal OverlayEffectControl()
		{
			Set(Effect.Mist);
		}

		internal void Set(Effect whichEffect)
		{
			Reset();
			_currentEffect = whichEffect;
			switch (whichEffect)
			{
				case Effect.Mist:
					_fxXMotion = 0.05f;
					_fxYMotion = 0.02f;
					_fxOpacity = 0.8f;
					_fxRotationRad = (float)(90d * Math.PI / 180d);
					_fxTexture = Game1.temporaryContent.Load<Texture2D>(
						"LooseSprites\\steamAnimation");
					break;

				case Effect.Haze:
					_fxXMotion = 0.005f;
					_fxYMotion = 0.035f;
					_fxOpacity = 0.5f;
					_fxTexture = Game1.temporaryContent.Load<Texture2D>(
						"LooseSprites\\steamAnimation");
					break;

				case Effect.Stars:
					_fxYMotion = 0.5f;
					_fxOpacity = 0.8f;

					Log.E("No texture loaded for Effect.Stars.");
					Disable();
					break;

				case Effect.Dark:
					_fxTexture = ModEntry.Instance.Helper.Content.Load<Texture2D>(
						Path.Combine(ModConsts.SpritesPath, ModConsts.ExtraSpritesFile + ".png"));
					break;
			}
		}

		internal bool IsEnabled()
		{
			return _shouldDrawEffects;
		}

		internal void Enable(Effect whichEffect)
		{
			Log.W("Enabled mist");
			ModEntry.Instance.Helper.Events.Display.RenderedWorld += OnRenderedWorld;

			Set(whichEffect);

			_shouldDrawEffects = true;
			_fxPosition = new Vector2(-Game1.viewport.X, -Game1.viewport.Y);
		}

		internal void Disable()
		{
			Log.W("Disabled mist");
			Reset();
			_shouldDrawEffects = false;
			ModEntry.Instance.Helper.Events.Display.RenderedWorld -= OnRenderedWorld;
		}

		internal void Reset()
		{
			_currentEffect = Effect.None;
			_fxXMotion = _fxYMotion = _fxOpacity = _fxRotationRad = 0f;
			_fxPosition = Vector2.Zero;
			_fxTexture = null;
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

		internal void Update(GameTime time)
		{
			_fxPosition -= Game1.getMostRecentViewportMotion();

			if (_fxXMotion <= 0)
				return;
			_fxXOffset -= time.ElapsedGameTime.Milliseconds * _fxXMotion;
			_fxXOffset %= -256f;

			if (_fxYMotion <= 0)
				return;
			_fxYOffset -= time.ElapsedGameTime.Milliseconds * _fxYMotion;
			_fxYOffset %= -256f;
		}

		/// <summary>
		/// Renders mist like BathHousePool.
		/// </summary>
		internal void DrawMist(SpriteBatch b)
		{
			if (_currentEffect == Effect.Mist || _currentEffect == Effect.Haze || _currentEffect == Effect.Stars)
			{
				for (var x = _fxPosition.X + _fxXOffset;
					x < Game1.graphics.GraphicsDevice.Viewport.Width + 256f;
					x += 256f)
				{
					for (var y = _fxPosition.Y + _fxYOffset;
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
			}
			else if (_currentEffect == Effect.Dark)
			{
				// Darkness gradient
				const int gradientSize = 96;
				b.Draw(
					_fxTexture,
					new Rectangle(
						(int)(_fxPosition.X + _fxXOffset),
						(int)(_fxPosition.Y + _fxYOffset),
						(int)(gradientSize * TextureScale),
						(int)(gradientSize * TextureScale)),
					new Rectangle(0, 144, gradientSize, gradientSize),
					Color.White,
					0f,
					Vector2.Zero,
					SpriteEffects.None,
					1f);

				// Darkness fill for space between gradient and screen border
				var gap = new Vector2(
					Game1.graphics.GraphicsDevice.Viewport.Width - gradientSize * TextureScale,
					Game1.graphics.GraphicsDevice.Viewport.Height - gradientSize * TextureScale);
				var positions = new[]
				{
					new Vector2(_fxPosition.X, _fxPosition.Y - gap.Y), // Top
					new Vector2(_fxPosition.X - gap.X, _fxPosition.Y), // Left
					new Vector2(_fxPosition.X, _fxPosition.Y + gradientSize * TextureScale), // Bottom
					new Vector2(_fxPosition.X + gradientSize * TextureScale, _fxPosition.Y) // Right
				};
				for (var i = 0; i < 4; ++i)
				{
					// TODO: TESTING: Conditional draw for Overlay Dark border
					if (i % 2 == 0 && gap.X <= 1f)
						continue;
					if (i % 2 == 1 && gap.Y <= 1f)
						continue;

					b.Draw(_fxTexture,
						new Rectangle(
							(int)positions[i].X,
							(int)positions[i].Y,
							(int)gap.X,
							(int)gap.Y * (i % 2 == 0 ? 1 : 2)), // Stretch sides to fill out corners
						new Rectangle(0, 0, 1, 1),
						Color.White, 0f, Vector2.Zero, SpriteEffects.None, 1f);
				}
			}
		}
	}
}
