using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;

namespace Hikawa.Core
{
	//todo: add List<OverlayEffect> for stacking effects
	//eg. shadow under player + haze
	
	internal class OverlayEffectControl
	{
		internal enum Effect
		{
			Mist,
			Haze,
			Stars
		}

		private const float TextureScale = 4f;
		private float _fxXMotion;
		private float _fxYMotion;
		private float _fxOpacity;
		private float _fxRotationRad;

		private bool _shouldDrawEffects;
		private Texture2D _fxAnimation;
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
			_currentEffect = whichEffect;
			switch (whichEffect)
			{
				case Effect.Mist:
				{
					_fxXMotion = 0.05f;
					_fxYMotion = 0.02f;
					_fxOpacity = 0.8f;
					_fxRotationRad = (float)(90d * Math.PI / 180d);
					_fxAnimation = Game1.temporaryContent.Load<Texture2D>(
						"LooseSprites\\steamAnimation");
					break;
				}
				case Effect.Haze:
				{
					_fxXMotion = 0.005f;
					_fxYMotion = 0.035f;
					_fxOpacity = 0.5f;
					_fxRotationRad = 0f;
					_fxAnimation = Game1.temporaryContent.Load<Texture2D>(
						"LooseSprites\\steamAnimation");
					break;
				}
				case Effect.Stars:
				{
					_fxXMotion = 0f;
					_fxYMotion = 0.5f;
					_fxOpacity = 0.8f;
					_fxRotationRad = 0f;

					Log.E("No texture loaded for Effect.Stars.");
					Disable();

					break;
				}
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
			_fxAnimation = null;
			_shouldDrawEffects = false;
			ModEntry.Instance.Helper.Events.Display.RenderedWorld -= OnRenderedWorld;
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
			_fxXOffset -= time.ElapsedGameTime.Milliseconds * _fxXMotion;
			_fxXOffset %= -256f;
			_fxYOffset -= time.ElapsedGameTime.Milliseconds * _fxYMotion;
			_fxYOffset %= -256f;
			_fxPosition -= Game1.getMostRecentViewportMotion();
		}

		/// <summary>
		/// Renders mist like BathHousePool.
		/// </summary>
		internal void DrawMist(SpriteBatch b)
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
						_fxAnimation,
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
	}

}
