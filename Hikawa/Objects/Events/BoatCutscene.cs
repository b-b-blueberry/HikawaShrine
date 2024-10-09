using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.Minigames;

namespace Hikawa.Objects.Events
{
	public class WaterFeaturePool
	{
		protected List<WaterFeature> _items;

		public List<WaterFeature> Items { get => this._items; }

		public WaterFeaturePool(int size)
		{
			this._items = new List<WaterFeature>(size);
		}

		public WaterFeature Get()
		{
			// Add new item if below capacity
			if (this._items.Count < this._items.Capacity)
				this._items.Add(new());
			// Recycle best available item if at capacity
			return this._items.MaxBy(item => item.Offset.X);
		}
	}

	public class WaterFeature
	{
		public int Index;
		public Vector2 Offset;
		public Vector2 Motion;
		public float Scale;

		public WaterFeature() {}

		public WaterFeature Set(int index, Vector2 offset, Vector2 motion, float scale)
		{
			this.Index = index;
			this.Offset = offset;
			this.Motion = motion;
			this.Scale = scale;

			return this;
		}

		public void Update(GameTime time)
		{
			this.Offset += this.Motion * time.ElapsedGameTime.Milliseconds;
		}
	}

	public class Chara(Texture2D texture, Rectangle source, Rectangle bounceSource, Vector2 offset, float bounceScale)
	{
		public readonly Texture2D Texture = texture;
		public readonly Rectangle Source = source;
		public readonly Rectangle BounceSource = bounceSource;

		public Vector2 Offset = offset;
		public float BounceScale = bounceScale;
	}

	public class Bird(Texture2D texture, Rectangle source, Vector2 offset, float scale, int frames, int defaultFrame = 0, bool isAnimating = false, Vector2? motion = null)
	{
		public readonly Texture2D Texture = texture;
		public readonly Rectangle Source = source;

		public Vector2 Offset = offset;
		public float Scale = scale;
		public int Frames = frames;
		public int DefaultFrame = defaultFrame;
		public bool IsAnimating = isAnimating;
		public Vector2 Motion = motion ?? Vector2.Zero;

		public void Update(GameTime time)
		{
			if (Game1.random.NextDouble() < 0.05f)
				this.Offset += this.Motion * time.ElapsedGameTime.Milliseconds;
			if (Game1.random.NextDouble() < 0.001f)
				this.IsAnimating = !this.IsAnimating;
		}
	}

	public class BoatCutscene : IMinigame
	{
		private readonly Texture2D Texture;
		private readonly Texture2D SkyTexture;
		private readonly Color SeaColor;
		private readonly Color SkyColor;
		private readonly bool IsFlipped;
		private readonly bool IsNight;
		private readonly Farmer FakeFarmer;
		private readonly Bird[] Birds;
		private readonly Chara[] Charas;
		private readonly string DestinationLocation;
		private readonly Vector2 DrawFlip;
		private readonly float Scale = 2f;

		// Scene
		private float _timer;
		private bool _isEnded;

		private bool IsFadingOut => this._timer > 10000f;

		// Water features
		private WaterFeaturePool _waterFeatures;
		private float _waterFeatureVelocity = 1f;

		// Birds
		private float _birdVelocity = -0.05f;
		private float _birdDistance;

		// Audio
		private ICue _engineCue;

		// Boat
		private float _boatAcceleration = -0.05f;
		private float _boatVelocity;
		private float _boatDistance;
		private Vector2 _boatOffset;

		// Bounce
		private bool _isBigBounce;
		private int _bounceOffset;
		private float _bounceVelocity;
		private float _bounceGravity = -0.0055f;


		public BoatCutscene()
		{
			/*
cs Game1.currentMinigame = new Hikawa.Objects.Events.BoatCutscene();
			*/

			this.IsFlipped = false;

			// Scene
			this.IsNight = Game1.isDarkOut(Game1.getFarm());
			this.Texture = ModEntry.Sprites;
			this.SkyTexture = Game1.temporaryContent.Load<Texture2D>(
				this.IsNight
					? "LooseSprites/Cloudy_Ocean_BG_Night"
					: "LooseSprites/Cloudy_Ocean_BG");

			Color[] pixels = new Color[this.SkyTexture.Width * this.SkyTexture.Height];
			this.SkyTexture.GetData(pixels);
			this.SkyColor = pixels.First();
			this.SeaColor = pixels.Last();
			if (this.IsFlipped)
			{
				this._boatAcceleration *= -1;
				this._birdVelocity *= -1;
			}
			this.DestinationLocation = this.IsFlipped ? ModConsts.MapShrine : ModConsts.MapVolleyball;
			this.DrawFlip = this.IsFlipped ? new(x: -1, y: 1) : new(x: 1, y: 1);

			// Actors
			this.FakeFarmer = this.CreateFakeFarmer(flip: this.IsFlipped);
			this.Birds = this.CreateBirds().ToArray();
			this.Charas = this.CreateCharas().ToArray();
			this._waterFeatures = new WaterFeaturePool(256);
			for (int i = 0; i < this._waterFeatures.Items.Capacity / 3; ++i)
			{
				this.CreateWaterFeature(randomX: true);
			}

			// BoatJourney.cs
			Game1.globalFadeToClear();
			Game1.changeMusicTrack(
				newTrackName: "cowboy_outlawsong",
				track_interruptable: false,
				music_context: MusicContext.MiniGame);
			this.changeScreenSize();
		}

		private Farmer CreateFakeFarmer(bool flip)
		{
			flip = !flip; // flip flip!
			Farmer who = Game1.player.CreateFakeEventFarmer();
			int frame = 117;
			who.FarmerSprite.setCurrentSingleFrame(which: frame, secondaryArm: false, flip: flip);
			who.FacingDirection = flip ? Game1.left : Game1.right;
			return who;
		}

		private List<Chara> CreateCharas()
		{
			List<Chara> charas = [
				new(texture: this.Texture, source: new(160, 80, 16, 32), bounceSource: new(160, 48, 16, 32), offset: new(28, -8), bounceScale: 1.3f),
				new(texture: this.Texture, source: new(144, 80, 16, 32), bounceSource: new(144, 48, 16, 32), offset: new(44, 2), bounceScale: 1.2f),
				new(texture: this.Texture, source: new(144, 112, 16, 16), bounceSource: new(160, 112, 16, 16), offset: new(34, 28), bounceScale: 1.1f)
			];
			return charas;
		}

		private List<Bird> CreateBirds()
		{
			Vector2 birdTile = new(24, 24);
			Vector2 birdRange = new(28, 8);
			Vector2 birdMotion = new(0.05f, 0.025f);
			Vector2 randomOffset() => new Vector2(
				x: (float)(-birdRange.X / 4 + Game1.random.NextDouble() * birdRange.X / 2),
				y: (float)(-birdRange.Y / 4 + Game1.random.NextDouble() * birdRange.Y / 2))
				* birdTile;
			Vector2 randomMotion() => new Vector2(
				x: -0.5f + (float)Game1.random.NextDouble(),
				y: -0.5f + (float)Game1.random.NextDouble())
				* birdMotion;
			Rectangle gullSource = new(0, 256, 32, 32);
			Rectangle doveSource = new(388, 1894, 24, 22);
			Vector2 offset = randomOffset();
			List<Bird> birds = [
				new(texture: this.Texture, source: gullSource, offset: offset + new Vector2(0, -64), scale: 0.75f, frames: 6, motion: randomMotion()),
				new(texture: this.Texture, source: gullSource, offset: offset + new Vector2(-24, -56), scale: 1f, frames: 6, motion: randomMotion()),
				new(texture: this.Texture, source: gullSource, offset: offset + new Vector2(12, -50), scale: 1.25f, frames: 6, motion: randomMotion()),
				new(texture: this.Texture, source: gullSource, offset: offset + new Vector2(56, -64), scale: 1f, frames: 6, motion: randomMotion())
			];
			for (int bird = 0; bird < Game1.random.Next(4); ++bird)
			{
				birds.Add(new(texture: this.Texture, source: gullSource, offset: offset + randomOffset() * 2, scale: (float)(1f + (offset.Y / birdTile.Y / birdRange.Y)), frames: 6));
			}
			if (Game1.random.NextDouble() < 0.25f && (int)Game1.stats.Get("childrenTurnedToDoves") > 0)
			{
				birds.Add(new(texture: Game1.mouseCursors, source: doveSource, offset: offset + randomOffset() * 2, scale: (float)(1f + (offset.Y / birdTile.Y / birdRange.Y)), frames: 6, defaultFrame: 4, isAnimating: true));
			}
			return birds;
		}

		private void CreateWaterFeature(bool randomX = false)
		{
			Vector2 waterFeatureTile = new(x: 24, y: 24);
			Vector2 waterFeatureRange = new(x: 32, y: 8.5f);
			float scale = (float)Game1.random.NextDouble();
			WaterFeature wf = this._waterFeatures.Get().Set(
				index: Game1.random.Next(6),
				offset: new Vector2(
					x: -waterFeatureRange.X / 2,
					y: scale * waterFeatureRange.Y)
					* waterFeatureTile,
				motion: new(x: this._waterFeatureVelocity * scale, y: 0),
				scale: scale);
			if (randomX)
			{
				wf.Offset.X += (float)(Game1.random.NextDouble() * waterFeatureRange.X * waterFeatureTile.X);
			}
		}

		private void Bounce(float velocity)
		{
			this._isBigBounce = velocity > 3f;
			this._bounceVelocity = velocity;
			this._bounceOffset = -1;
			Game1.playSound("pullItemFromWater");
		}

		private void AfterBounce()
		{
			this._isBigBounce = false;
			this._bounceOffset = 0;
			this._bounceVelocity = 0;
			Game1.playSound("dropItemInWater");
		}

		private void UpdateBoatEngineSound()
		{
			return;

			float volume = 0.5f;

			if (this._engineCue is null)
			{
				Game1.playSound("heavyEngine", out this._engineCue);
				this._engineCue.Pause();
				this._engineCue.SetVariable("Frequency", 75f);
				this._engineCue.SetVariable("Volume", 100f * Game1.options.ambientVolumeLevel * volume);
				this._engineCue.Resume();
			}
			if (!this._engineCue.IsPlaying)
			{
				this._engineCue.Play();
			}

			this._engineCue.Pitch = 0.75f - this._boatVelocity / 25f;

			if (this.IsFadingOut)
			{
				this._engineCue.SetVariable("Volume", (1 - Game1.fadeToBlackAlpha) * 100f * Game1.options.ambientVolumeLevel * volume);
			}
		}

		public void AfterFade()
		{
			// BoatJourney.cs
			// not sure why we need this?
			return;
			Game1.currentMinigame = null;
			Game1.globalFadeToClear();
			if (Game1.currentLocation.currentEvent is not null)
			{
				Game1.currentLocation.currentEvent.CurrentCommand++;
				Game1.currentLocation.temporarySprites.Clear();
			}
		}

		private Color GetWaterColorForSeason()
		{
			// BoatJourney.cs
			return Game1.season switch
			{
				Season.Summer => new Color(51, 90, 174),
				Season.Fall => new Color(56, 70, 128),
				Season.Winter => new Color(43, 74, 164),
				_ => new Color(49, 79, 155),
			};
		}

		private void OnUpdateTicked(GameTime time)
		{
			Game1.mouseCursor = Game1.cursor_none;

			int ms = time.ElapsedGameTime.Milliseconds;
			this._timer += ms;

			bool isAccelerating = this._timer > 7000f;

			// Looping audio cue
			this.UpdateBoatEngineSound();

			// Water features
			foreach (WaterFeature wf in this._waterFeatures.Items)
				wf?.Update(time);
			if (Game1.random.NextDouble() < 0.175f)
			{
				this.CreateWaterFeature();
			}

			// Boat bounce
			if (this._bounceOffset != 0)
			{
				this._bounceVelocity += this._bounceGravity * ms;
				this._bounceOffset -= (int)this._bounceVelocity;
				if (this._bounceOffset >= 0)
				{
					this.AfterBounce();
				}
			}
			else if (!isAccelerating && Game1.random.NextDouble() < 0.005f)
			{
				this.Bounce(velocity: (float)(2f + Game1.random.NextDouble() * 2f));
			}

			// Bird animations
			this._birdDistance += this._birdVelocity;
			foreach (Bird bird in this.Birds)
				bird.Update(time);

			// Cutscene progress
			if (isAccelerating)
			{
				// boat speeds away
				this._boatVelocity += this._boatAcceleration;
				this._boatDistance += this._boatVelocity;
			}
			if (this.IsFadingOut)
			{
				// start fade-out to end
				if (!this._isEnded && !Game1.globalFade)
				{
					Game1.globalFadeToBlack(delegate
					{
						this._isEnded = true;

						this._engineCue?.Pause();
						Game1.stopMusicTrack(MusicContext.MiniGame);
						Game1.changeMusicTrack("none", track_interruptable: false, music_context: MusicContext.MiniGame);
						//Game1.warpFarmer(this.DestinationLocation, 21, 43, 0);
					});
				}
			}
		}

		private void OnViewportChanged()
		{
			// do nothing
		}

		private void OnClosed()
		{
			this._engineCue?.Pause();
			Game1.stopMusicTrack(MusicContext.MiniGame);
		}

		private void OnDraw(SpriteBatch b)
		{
			if (this._isEnded)
				return;

			b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);

			b.Draw(texture: Game1.staminaRect,
				destinationRectangle: new(0, 0, Game1.viewport.Width, Game1.viewport.Height / 2),
				color: this.SkyColor);
			b.Draw(texture: Game1.staminaRect,
				destinationRectangle: new(0, Game1.viewport.Height / 2, Game1.viewport.Width, Game1.viewport.Height / 2),
				color: this.SeaColor);

			Vector2 centre = new Vector2(
				x: Game1.viewport.Width,
				y: Game1.viewport.Height) / 2;

			// sky & sea
			for (int i = 0; i < 1 + Math.Max(1, Game1.viewport.Width / this.SkyTexture.Width); ++i)
			{
				float timeScale = 0.0025f;
				int timeWindow = 300;
				float timeRatio = (float)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds * timeScale) % timeWindow) / timeWindow;
				b.Draw(
					texture: this.SkyTexture,
					position: centre
						+ new Vector2(x: 0, y: -80) * this.Scale
						+ new Vector2(x: this.SkyTexture.Width * timeRatio, y: 0) * this.Scale
						- new Vector2(x: this.SkyTexture.Width, y: 0) * this.Scale * i
					,
					sourceRectangle: this.SkyTexture.Bounds,
					color: Color.White,
					rotation: 0f,
					origin: this.SkyTexture.Bounds.Size.ToVector2() / 2,
					scale: this.Scale,
					effects: i % 2 == 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally,
					layerDepth: 0f);
			}
			foreach (WaterFeature wf in this._waterFeatures.Items)
			{
				if (wf is null)
					continue;

				Rectangle source = new(
					x: wf.Index * 32,
					y: 288,
					width: 32,
					height: 16);
				b.Draw(
					texture: this.Texture,
					position: centre
						+ new Vector2(x: 0, y: -24) * this.Scale
						+ wf.Offset * this.DrawFlip * this.Scale
					,
					sourceRectangle: source,
					color: Color.White,
					rotation: 0f,
					origin: source.Size.ToVector2() / 2,
					scale: this.Scale * wf.Scale * 2f,
					effects: this.IsFlipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
					layerDepth: 0.1f);
			}

			float boatBounceScale = this.Scale * Math.Max(0, -this._bounceOffset / 72f);

			// boat
			if (true) {
				Rectangle source = new(
					x: 0,
					y: 48,
					width: 144,
					height: 80);
				this._boatOffset.X = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 600f) * 9f;
				this._boatOffset.Y = 60 + (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 300f) * 3f;
				b.Draw(
					texture: this.Texture,
					position: centre
						+ this._boatOffset * this.Scale
						+ new Vector2(x: this._boatDistance, y: this._bounceOffset) * this.Scale
					,
					sourceRectangle: source,
					color: Color.White,
					rotation: 0f,
					origin: source.Size.ToVector2() / 2,
					scale: this.Scale,
					effects: this.IsFlipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
					layerDepth: 0.5f);
				Rectangle shadowSource = new(
					x: 0,
					y: 304,
					width: 144,
					height: 32);
				b.Draw(
					texture: this.Texture,
					position: centre
						+ this._boatOffset * this.Scale
						+ new Vector2(x: this._boatDistance - this._bounceOffset * 0.35f * this.DrawFlip.X, y: 0) * this.Scale
						+ new Vector2(x: 0, y: source.Height - shadowSource.Height) * this.Scale / 2
					,
					sourceRectangle: shadowSource,
					color: Color.White * 0.5f,
					rotation: 0f,
					origin: shadowSource.Size.ToVector2() / 2,
					scale: this.Scale - boatBounceScale,
					effects: this.IsFlipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
					layerDepth: 0.45f);
			}

			// water wake/spray
			if (true)
			{
				Rectangle source = new(
					x: 0,
					y: 128,
					width: 192,
					height: 32);
				source.Y += source.Height * (int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 100 % 4);
				b.Draw(
					texture: this.Texture,
					position: centre
						+ this._boatOffset * this.Scale
						+ new Vector2(x: this._boatDistance, y: 0) * this.Scale
						+ new Vector2(x: 24, y: 24) * this.Scale * this.DrawFlip
					,
					sourceRectangle: source,
					color: Color.White,
					rotation: 0f,
					origin: source.Size.ToVector2() / 2,
					scale: this.Scale - boatBounceScale,
					effects: this.IsFlipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
					layerDepth: 0.55f);
			}

			// player
			if (true) {
				Farmer who = this.FakeFarmer;
				Vector2 position = centre
					+ this._boatOffset * this.Scale
					+ new Vector2(x: this._boatDistance - this._bounceOffset * 1.1f * 0.075f * this.DrawFlip.X, y: this._bounceOffset) * this.Scale
					+ new Vector2(x: 3, y: -7) * this.Scale * this.DrawFlip;
				Vector2 origin = who.FarmerSprite.SourceRect.Size.ToVector2() / 2;
				who.FarmerRenderer.draw(
					b: b,
					animationFrame: who.FarmerSprite.CurrentAnimationFrame,
					currentFrame: who.FarmerSprite.CurrentFrame,
					sourceRect: who.FarmerSprite.SourceRect,
					position: position,
					origin: origin,
					layerDepth: 1,
					overrideColor: Color.White,
					rotation: 0,
					scale: 1 / this.Scale,
					who: who,
					facingDirection: this.FakeFarmer.FacingDirection);
			}

			// characters
			foreach (Chara chara in this.Charas)
			{
				Rectangle rect = this._isBigBounce ? chara.BounceSource : chara.Source;
				b.Draw(
					texture: chara.Texture,
					position: centre
						+ this._boatOffset * this.Scale
						+ new Vector2(x: this._boatDistance - this._bounceOffset * chara.BounceScale * 0.075f * this.DrawFlip.X, y: this._bounceOffset * chara.BounceScale) * this.Scale
						+ chara.Offset * this.Scale * this.DrawFlip
					,
					sourceRectangle: rect,
					color: Color.White,
					rotation: 0f,
					origin: rect.Size.ToVector2() / 2,
					scale: this.Scale,
					effects: this.IsFlipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
					layerDepth: 0.75f);
			}

			// seagulls
			foreach (Bird bird in this.Birds)
			{
				Rectangle rect = bird.Source.Clone();
				rect.X = bird.IsAnimating
					? rect.X + rect.Width * (int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 100 % bird.Frames)
					: rect.X + rect.Width * bird.DefaultFrame;
				// bird
				b.Draw(
					texture: bird.Texture,
					position: centre
						+ new Vector2(x: this._birdDistance, y: 0) * this.Scale
						+ bird.Offset * this.Scale
					,
					sourceRectangle: rect,
					color: Color.White,
					rotation: 0f,
					origin: rect.Size.ToVector2() / 2,
					scale: this.Scale * bird.Scale,
					effects: this.IsFlipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
					layerDepth: 0.9f + bird.Offset.Y / 10000f);
				// shadow
				b.Draw(
					texture: bird.Texture,
					position: centre
						+ new Vector2(x: this._birdDistance, y: 0) * this.Scale
						+ new Vector2(x: bird.Offset.Y * 1f, y: centre.Y * 0.333f) * this.Scale
						+ bird.Offset * this.Scale
					,
					sourceRectangle: rect,
					color: Color.Black * 0.075f * bird.Scale * 2,
					rotation: 0f,
					origin: rect.Size.ToVector2() / 2,
					scale: this.Scale * bird.Scale * 0.5f,
					effects: this.IsFlipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
					layerDepth: 0.25f);
			}

			b.End();
		}

		#region IMinigame

		public void changeScreenSize()
		{
			this.OnViewportChanged();
		}

		public bool doMainGameUpdates()
		{
			return false;
		}

		public void draw(SpriteBatch b)
		{
			this.OnDraw(b);
		}

		public bool forceQuit()
		{
			return false;
		}

		public void leftClickHeld(int x, int y)
		{
		}

		public string minigameId()
		{
			return null;
		}

		public bool overrideFreeMouseMovement()
		{
			return Game1.options.SnappyMenus;
		}

		public void receiveEventPoke(int data)
		{
		}

		public void receiveKeyPress(Keys k)
		{
			if (k is Keys.Escape)
			{
				this.forceQuit();
			}
		}

		public void receiveKeyRelease(Keys k)
		{
		}

		public void receiveLeftClick(int x, int y, bool playSound = true)
		{
		}

		public void receiveRightClick(int x, int y, bool playSound = true)
		{
		}

		public void releaseLeftClick(int x, int y)
		{
		}

		public void releaseRightClick(int x, int y)
		{
		}

		public bool tick(GameTime time)
		{
			this.OnUpdateTicked(time);
			return false;// this._isEnded;
		}

		public void unload()
		{
			this.OnClosed();
		}

		#endregion
	}
}
