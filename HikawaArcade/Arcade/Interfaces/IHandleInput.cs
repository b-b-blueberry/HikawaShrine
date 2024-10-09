using Microsoft.Xna.Framework.Input;

namespace HikawaArcade.Arcade.Interfaces
{
	interface IHandleInput
	{
		public void HandleInput(Keys k);
		public void HandleInputReleased(Keys k);
		public void HandleClick(int x, int y);
		public void HandleClickReleased(int x, int y);
	}
}
