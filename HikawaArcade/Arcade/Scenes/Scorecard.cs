using HikawaArcade.Arcade.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HikawaArcade.Arcade.Scenes
{
    public class Scorecard : Scene
	{
		public override State Update(TimeSpan time)
		{
			return State.IsAlive;
		}

		public override void Draw(SpriteBatch b, Rectangle viewport)
		{
			base.Draw(b: b, viewport: viewport);
		}

		public override void Reset()
		{
			base.Reset();
		}

		public override Interfaces.ICopyable CopyTo(Interfaces.ICopyable target)
		{
			return base.CopyTo(target);
		}
	}
}
