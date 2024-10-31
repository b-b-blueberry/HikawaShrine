using System;
using Hikawa.Objects.Locations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace Hikawa.Objects.Critters
{
	public class HangingSprite : Critter
	{
		public const float DefaultRotation = MathF.PI * 0.5f;

		public readonly HangingSpriteEntry Data;

		public float DisplayRotation;
		public float Rotation;
		public float Velocity;
		public float Momentum;

		public float AnimCounter;
		public int AnimFrame;

        public HangingSprite(HangingSpriteEntry data)
        {
			this.Data = data;
			this.position = this.startingPosition = this.Data.Tile * Game1.tileSize;
			this.sprite = new(textureName: this.Data.TextureId)
			{
				SourceRect = this.Data.TextureRegion,
				ignoreSourceRectUpdates = true
			};
			this.ResetRotation();
		}

		public void ResetRotation()
		{
			this.Rotation = HangingSprite.DefaultRotation;
			this.DisplayRotation = -HangingSprite.DefaultRotation;
			this.Velocity = 0;
			this.Momentum = 1;

			this.AnimCounter = 0;
			this.AnimFrame = 0;
		}
		/*
		public void UpdateRotationShakeStyle(GameTime time)
		{
			float d = MathF.PI / 200f;
			this.RotationMax += MathF.Abs(Game1.windGust) * this.Data.Resistance * 1.5f;
			if (this.RotationMax > 0)
			{
				if (this.Direction != -1)
				{
					this.Rotation -= d;
					if (this.Rotation <= -this.RotationMax)
					{
						this.Direction = -1;
					}
				}
				else
				{
					this.Rotation += d;
					if (this.Rotation >= this.RotationMax)
					{
						this.Direction = 1;
					}
				}
				this.RotationMax = Math.Clamp(this.RotationMax - this.Data.Resistance, 0, 1);
			}
			float scale = MathF.PI * this.Data.Limit;
			float ratio = 1f - Math.Abs(this.Rotation) / 1f;
			float rotation = Utils.CircularFromRatio(ratio: ratio, offset: 0.5f, scale: 0.5f) * scale;
			rotation = MathF.PI * 0.265f - MathF.Sin(ratio);
			this.DisplayRotation = rotation;
		}
		*/
		public void UpdateRotation(GameTime time)
		{
			float dt = time.ElapsedGameTime.Milliseconds / 50f; // Elapsed time
			float wind = dt * -Game1.windGust / 2500f * this.Data.Limit; // Current scaled wind force
			this.Momentum = Math.Clamp(this.Momentum + wind, 0, 1);
			this.Velocity += wind;

			// Update sprite animation frame
			if (this.Data.AnimationFrames > 1)
			{
				int frame = this.AnimFrame;
				float drag = 0.00002f * dt * this.Momentum;
				this.AnimCounter += MathF.Abs(this.Velocity / 10f) + MathF.Abs(wind) - drag;
				if (this.AnimCounter > 0.001f / this.Data.AnimationSpeed)
				{
					this.AnimCounter = 0;
					++this.AnimFrame;
					this.AnimFrame %= this.Data.AnimationFrames;

				}
				else if (this.AnimCounter < -0.001f / this.Data.AnimationSpeed)
				{
					this.AnimCounter = 0;
					--this.AnimFrame;
					if (this.AnimFrame < 0)
						this.AnimFrame = this.Data.AnimationFrames - 1;

				}
				if (this.AnimFrame != frame)
				{
					Rectangle source = this.Data.TextureRegion;
					source.X += this.AnimFrame * this.sprite.SourceRect.Width;
					this.sprite.SourceRect = source;
				}
			}

			float d; // Change in distance
			float alpha; // Change in angle
			float theta = this.Rotation; // Current angle
			float v = this.Velocity; // Current velocity
			float g = this.Data.Resistance; // Constant force
			float accel = g * MathF.Cos(theta); // Current acceleration
			float r = (this.Data.TextureRegion.Width + this.Data.TextureRegion.Height) / 2f; // Radius (distance from axis)

			// How to simulate pendulum movement with high amplitude in C#
			// Apr 24, 2015
			// Leandro Caniglia
			// https://stackoverflow.com/a/29838014
			d = v * dt + accel * (MathF.Pow(dt, 2) / 2); // Calculate distance change
			v = (v + accel * dt) * this.Momentum; // Apply drag/damping/resistance force to velocity
			alpha = v * MathF.Acos(d / r); // Apply velocity to change in angle

			this.Velocity = v;
			this.Rotation += alpha;
			this.DisplayRotation = -HangingSprite.DefaultRotation + this.Rotation;
			this.Momentum = MathF.Max(0, this.Momentum - MathF.Pow(g, 2));
		}

		public override bool update(GameTime time, GameLocation environment)
		{
			this.UpdateRotation(time);

			return false;
		}

		public override void draw(SpriteBatch b)
		{
			//this.DrawInfo(b);
			this.DrawSprite(b);
		}

		public override void drawAboveFrontLayer(SpriteBatch b) {}

		public void DrawSprite(SpriteBatch b)
		{
			if (!Utility.isOnScreen(
				positionNonTile: this.position,
				acceptableDistanceFromScreen: Math.Max(this.Data.TextureRegion.Width, this.Data.TextureRegion.Height) * Game1.pixelZoom))
				return;

			b.Draw(
				texture: this.sprite.Texture,
				position: Game1.GlobalToLocal(Game1.viewport, this.position + this.Data.TextureOrigin * Game1.pixelZoom / 2f),
				sourceRectangle: this.sprite.SourceRect,
				color: Color.White,
				rotation: this.DisplayRotation,
				origin: this.Data.TextureOrigin,
				scale: Game1.pixelZoom,
				effects: SpriteEffects.None,
				layerDepth: this.Data.DrawBehind ? 0 : 1);
		}

		public void DrawInfo(SpriteBatch b)
		{
			var font = Game1.smallFont;
			var text = $"Gust: {Game1.windGust:0.00}\nGlobal: {WeatherDebris.globalWind:0.00}\nRot: {this.Rotation:0.00}\nDis: {this.DisplayRotation:0.00}";
			var size = font.MeasureString(text);
			var padding = new Vector2(8, 8);
			b.Draw(
				texture: Game1.staminaRect,
				destinationRectangle: new(Point.Zero, (size + padding * 2).ToPoint()),
				color: Color.White);
			Utility.drawBoldText(
				b: b,
				font: font,
				text: text,
				position: padding,
				color: Game1.textColor);
		}
	}
}
