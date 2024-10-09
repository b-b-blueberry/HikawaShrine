using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hikawa.Match3
{
	public abstract class Actor<TData, TState> where TData : ActorData
	{
		public string Name;
		public TData Data;
		public TState State
		{
			get => this._state;
			set
			{
				this.PortraitIndex = Math.Min(this.Data.TextureRegions.Count - 1, (int)(object)value);
				this.PortraitSize = this.Data.TextureRegions[this.PortraitIndex].Size.ToVector2();
				this.PortraitTime = 0;
				this._state = value;
			}
		}
		public int PortraitIndex;
		public long PortraitTime;
		public Vector2 PortraitSize;
		public Vector2 DrawPixel;

		protected TState _state;

		public virtual Actor<TData, TState> Set(string name, TData data, TState state)
		{
			this.Name = name;
			this.Data = data;
			this.State = state;
			return this;
		}

		public virtual Actor<TData, TState> Reset()
		{
			this.Name = null;
			this.Data = null;
			return this;
		}

		public virtual void Draw(SpriteBatch b, Vector2 position, float scale, bool flip = false)
		{
			b.Draw(
				texture: this.Data.Texture,
				position: position,
				sourceRectangle: this.Data.TextureRegions[this.PortraitIndex],
				color: Color.White,
				rotation: 0,
				origin: Vector2.Zero,
				scale: scale,
				effects: flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
				layerDepth: 1);
		}
	}

	public class Character : Actor<CharacterData, CharacterState>
	{
		public bool ContainsCursor(int x, int y, float scale)
		{
			return x > this.DrawPixel.X
				&& y > this.DrawPixel.Y
				&& x < this.DrawPixel.X + this.PortraitSize.X * scale
				&& y < this.DrawPixel.Y + this.PortraitSize.Y * scale;
		}
	}

	public class Enemy : Actor<EnemyData, EnemyState>
	{
		public int Life;
		public int Power;
		public int AttackDrawTimer;
		public Vector2 AttackDrawPixel;
		public float AttackRotation;

		public override Actor<EnemyData, EnemyState> Set(string name, EnemyData data, EnemyState state)
		{
			base.Set(name, data, state);

			this.Life = this.Data.LifeInitial;
			this.Power = this.Data.PowerInitial;
			this.AttackDrawTimer = 0;

			return this;
		}
	}
}
