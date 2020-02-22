using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace HikawaShrine.Editors
{
	internal class ArcadeEditor : IAssetEditor
	{
		private readonly IModHelper _helper;

		public ArcadeEditor()
		{
			_helper = ModEntry.Instance.Helper;
		}

		public bool CanEdit<T>(IAssetInfo asset)
		{
			return asset.AssetNameEquals(@"LooseSprites/Cursors");
		}

		public void Edit<T>(IAssetData asset)
		{
			// Patch in a custom crosshair cursor for the Sailor V shoot 'em up
			if (Game1.currentMinigame != null && Game1.currentMinigame.minigameId().Equals(Const.ArcadeMinigameId))
			{
				var texture = _helper.Content.Load<Texture2D>(
					Path.Combine("assets", "Maps", $"{Const.ArcadeSpritesFile}.png"));
				asset.AsImage().PatchImage(
					texture,
					new Rectangle(
						LightGunGame.LightGunGame.CrosshairX,
						LightGunGame.LightGunGame.CrosshairY,
						LightGunGame.LightGunGame.CrosshairW,
						LightGunGame.LightGunGame.CrosshairH),
					new Rectangle(32, 0, 16, 16));
			}
		}
	}
}
