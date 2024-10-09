using System;
using System.Collections.Generic;
using System.IO;
using Hikawa.Objects.Locations;
using Hikawa.Objects.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Events;
using xTile.Dimensions;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Hikawa.Objects.Events
{
	public class CrystalBall : ICustomEventScript
	{
		private float _cameraProgress;

		private bool _terminate;
		private bool _awaitingInput;

		private bool _cameraPanning;
		private float _cameraRate;

		private Phase _phase;
		private int _timer;

		private bool _isGlareFading;
		private bool _isHandFading;
		private float _glareAlpha;
		private float _handAlpha;

		private Vector2 _resolution;
		private Vector2 _glareOffset;
		private Vector2 _backgroundOffset;
		private Vector2 _foregroundOffset;

		private enum Phase
		{
			Start = -5,
			Intro = -4,
			WaitForInput = -3,
			WaitForCamera = -2,
			FadeInGlare = -1,
			Fireworks = 0,
			WaitForZap = 1,
			Zap1 = 2,
			Zap2 = 3,
			Zap3 = 4,
			WaitForThing = 5,
			DoThing = 6,
			Outro = 7
		}


		public CrystalBall()
		{
			Log.W("CrystalBall");

			this._resolution = new Vector2(480, 640) * Game1.pixelZoom;
			this._glareOffset = Vector2.Zero;
			this._phase = Phase.Start;

			this._cameraRate = 1;
			this._cameraProgress = 0;
			/*
			this._timer = 6250;
			this._phase = Phase.WaitForInput;
			this._awaitingInput = false;
			*/
			Game1.background = null;
		}

		void ICustomEventScript.draw(SpriteBatch b)
		{
			EventSprites.DrawBlack(b: b);

			if (this._phase is < Phase.WaitForCamera or > Phase.DoThing)
				return;

			Rectangle view = Game1.graphics.GraphicsDevice.Viewport.Bounds;
			Rectangle drawArea = new(
				x: view.Center.X - (int)this._resolution.X / 2,
				y: view.Center.Y - (int)this._resolution.Y / 2,
				width: (int)this._resolution.X,
				height: (int)this._resolution.Y);

			this._backgroundOffset = new Vector2(x: 0, y: view.Height * 0.25f - view.Height * this._cameraProgress);
			this._foregroundOffset = this._backgroundOffset;

			EventSprites.DrawStarrySky(b: b, area: drawArea, position: this._backgroundOffset);
			//EventSprites.DrawFullMoon(b: b, area: drawArea, position: this._backgroundOffset + new Vector2(-24, -208) * Game1.pixelZoom);

			EventSprites.DrawGlare(b: b, area: drawArea, position: /*this._foregroundOffset + */this._glareOffset, glareAlpha: this._glareAlpha, handAlpha: this._handAlpha);

			EventSprites.DrawShrineRoof(b: b, area: drawArea, position: this._foregroundOffset);
			EventSprites.DrawShrineTrees(b: b, area: drawArea, position: this._foregroundOffset);
		}

		void ICustomEventScript.drawAboveAlwaysFront(SpriteBatch b)
		{
			if (ModEntry.Config.DebugMode)
			{
				b.DrawString(
					spriteFont: Game1.smallFont,
					text: "res " + this._resolution,
					position: Vector2.Zero,
					color: Color.White);
				b.DrawString(
					spriteFont: Game1.smallFont,
					text: "camera " + this._cameraProgress,
					position: new Vector2(0, 32),
					color: Color.White);
				b.DrawString(
					spriteFont: Game1.smallFont,
					text: "offset " + this._backgroundOffset,
					position: new Vector2(0, 64),
					color: Color.White);
			}
		}

		bool ICustomEventScript.update(GameTime time, Event e)
		{
			if (this._terminate)
				return true;

			// FRAME UPDATES
			Game1.viewport.X = -64000;
			Game1.viewport.Y = -64000;
			this._timer += time.ElapsedGameTime.Milliseconds;
			this._glareOffset.Y = 6f * (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / (Math.PI * 384f));

			// PHASE 1 -- INPUT
			if (this._timer > 3333 && this._phase == Phase.Start)
			{
				if (this._awaitingInput && !Game1.dialogueUp)
				{
					Log.D("Phase 1 end");

					this._awaitingInput = false;
					this._phase = Phase.Intro;
					this._timer = 3333;
				}
				else if (!Game1.dialogueUp)
				{
					Log.W("Phase 1: Monologue 1");
					
					//Game1.drawObjectDialogue(ModEntry.I18n.Get("talk.story.plant.mono1"));
					this._awaitingInput = true;
				}
			}

			// PHASE 1 AND A BIT -- INPUT
			if (this._timer > 5000 && this._phase == Phase.Intro)
			{
				if (this._awaitingInput && !Game1.dialogueUp)
				{
					Log.D("Phase 1 end");

					this._awaitingInput = false;
					this._phase = Phase.WaitForInput;
					this._timer = 5000;
				}
				else if (!Game1.dialogueUp)
				{
					Log.W("Phase 1 and a bit: Monologue 1 plus");
					
					//Game1.drawObjectDialogue(ModEntry.I18n.Get("talk.story.plant.mono1plus"));
					this._awaitingInput = true;
				}
			}

			// PHASE 2 -- TIMER
			// After first monologue, fade out of black, then start panning to target from our start point
			if (this._timer > 6250 && this._phase == Phase.WaitForInput && !this._awaitingInput)
			{
				Log.W("Phase 2: Wait and pan");

				Game1.nonWarpFade = true;
				Game1.globalFadeToClear(afterFade: null, fadeSpeed: 0.005f);
				if (Game1.musicPlayerVolume > 0f)
				{
					Game1.changeMusicTrack("none");
					//Game1.playSound(ModConsts.ContentPrefix + "dark_despair");
				}

				this._timer = 6250;
				this._phase = Phase.WaitForCamera;
				this._cameraPanning = true;
			}
			
			// Pan camera to our target
			if (this._cameraPanning)
			{
				const float cameraGoal = 1;
				const float cameraRateScale = 5000f;
				const float cameraRateMin = 0.075f;
				const float cameraRateDelta = 0.00165f;

				if (this._cameraRate > cameraRateMin)
					this._cameraRate -= cameraRateDelta;
				this._cameraProgress += time.ElapsedGameTime.Milliseconds * this._cameraRate / cameraRateScale;

				if (this._cameraProgress >= cameraGoal)
				{
					Log.D($"Reached target");

					this._cameraProgress = cameraGoal;
					this._cameraPanning = false;
					this._phase = Phase.FadeInGlare;
					this._timer = 9001;
				}
			}
			
			// Fade in the crystal ball
			if (this._timer > 8400 && this._cameraPanning && !this._isGlareFading && this._phase == Phase.Fireworks)
				this._isGlareFading = true;
			else if (this._isGlareFading && _glareAlpha >= 1f)
				this._isGlareFading = false;
			else if (this._isGlareFading)
				this._glareAlpha += (this._glareAlpha < 0.3f 
					? 0.03f 
					: (this._glareAlpha < 0.6f 
						? 0.045f 
						: 0.06f)) / time.ElapsedGameTime.Milliseconds;

			if (this._timer > 9500 && this._cameraPanning && !this._isHandFading && this._phase == Phase.Fireworks)
				this._isHandFading = true;
			else if (this._isHandFading && this._handAlpha >= 1f)
				this._isHandFading = false;
			else if (this._isHandFading)
				this._handAlpha += (this._handAlpha < 0.3f 
					? 0.035f 
					: (this._handAlpha < 0.6f 
						? 0.05f 
						: 0.065f)) / time.ElapsedGameTime.Milliseconds;

			// PHASE 3 -- INPUT
			// After panning to the target, start the fireworks
			if (this._timer > 10500 && !this._cameraPanning && this._phase == Phase.FadeInGlare)
			{
				if (this._awaitingInput && !Game1.dialogueUp)
				{
					Log.D("Phase 3 end");

					this._awaitingInput = false;
					this._phase = Phase.Fireworks;
				}
				else if (!Game1.dialogueUp)
				{
					Log.W("Phase 3: Monologue 2");
					
					Game1.drawObjectDialogue(ModEntry.I18n.Get("talk.story.plant.mono2"));
					this._timer = 10500;
					this._awaitingInput = true;
				}
			}
			
			// PHASE 3 AND A BIT -- TIMER
			if (this._timer > 11250 && this._phase == Phase.Fireworks)
			{
				this._phase = Phase.WaitForZap;
			}

			// Fade out the crystal ball
			if (this._phase >= Phase.WaitForZap && this._handAlpha > 0f)
			{
				this._handAlpha -= (this._handAlpha < 0.3f
					? 0.03f
					: (this._handAlpha < 0.6f
						? 0.045f
						: 0.06f)) / time.ElapsedGameTime.Milliseconds;
			}

			// PHASE 4 -- TIMER
			// Start the fireworks after dialogue is closed
			if (this._handAlpha < 0.7f && this._phase == Phase.WaitForZap)
			{
				Log.W("Phase 4: Fireworks");
				Log.W("Zap 1");

				this._phase = Phase.Zap1;
				/*Vector2 where = new Vector2(this._targetPosition.X - 3, this._targetPosition.Y - 1);
				Farm.LightningStrikeEvent lightningStrike = new Farm.LightningStrikeEvent
				{
					createBolt = true,
					boltPosition = where * Game1.tileSize
				};
				_farm.lightningStrikeEvent.Fire(lightningStrike);*/
				Game1.playSound("thunder");
			}
			if (this._handAlpha < 0.55f && this._phase == Phase.Zap1)
			{
				Log.W("Zap 2");

				this._phase = Phase.Zap2;
				/*Vector2 where = new Vector2(this._targetPosition.X + 1, this._targetPosition.Y + 2);
				Farm.LightningStrikeEvent lightningStrike = new Farm.LightningStrikeEvent
				{
					createBolt = true,
					boltPosition = where * Game1.tileSize
				};
				_farm.lightningStrikeEvent.Fire(lightningStrike);*/
				Game1.playSound("thunder");
			}
			if (this._handAlpha < 0.15f && this._phase == Phase.Zap2)
			{
				Log.W("Zap 3");

				this._phase = Phase.Zap3;
				this._timer = 11250;
				/*Vector2 where = new Vector2(this._targetPosition.X + 1, this._targetPosition.Y + 2);
				Farm.LightningStrikeEvent lightningStrike = new Farm.LightningStrikeEvent
				{
					createBolt = true,
					boltPosition = where * 64f,
					bigFlash = true
				};
				_farm.lightningStrikeEvent.Fire(lightningStrike);

				Game1.currentLightSources.Add(new LightSource(
					1, 
					this._targetPosition, 
					1f, 
					Color.Black, 
					942069));*/
			}

			// Wait a moment before sticking the banana
			if (!(this._handAlpha > 0) && this._phase == Phase.Zap3 && this._timer > 11250)
			{
				this._phase = Phase.WaitForThing;
			}

			// PHASE 5 -- TIMER
			// Throw down that banana
			if (!(this._handAlpha > 0f) && this._phase == Phase.WaitForThing)
			{
				Log.W("Phase 5: Plant");

				this._phase = Phase.DoThing;
			}
			
			// Fade out the crystal glare
			if (this._phase >= Phase.Zap3)
				this._glareAlpha -= (this._glareAlpha < 0.3f
					? 0.04f
					: (this._glareAlpha < 0.6f
						? 0.05f
						: 0.06f)) / time.ElapsedGameTime.Milliseconds;

			// PHASE 0 -- TIMER
			// Fade to black outro
			if (this._timer > 18000 && !Game1.fadeToBlack && this._phase == Phase.DoThing)
			{
				if (!Game1.dialogueUp && this._awaitingInput)
				{
					this._timer = 22000;
				}

				Log.W("Phase 0: Fade out");
				
				Game1.globalFadeToBlack(AfterLastFade);
				Game1.changeMusicTrack("none");
				Game1.freezeControls = false;
				this._phase = Phase.Outro;
				this._awaitingInput = true;
			}
			
			// End the rain
			if (this._timer > 25000 && !Game1.dialogueUp && !this._terminate && this._phase == Phase.Outro)
			{
				Log.D("End of CrystalBall");

				this._terminate = true;
			}

			return false;
		}
		
		public void AfterLastFade()
		{
			Game1.globalFadeToClear();
			Game1.drawObjectDialogue(ModEntry.I18n.Get("talk.story.plant.mono3"));
		}
	}
}
