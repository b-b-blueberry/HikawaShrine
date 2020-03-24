using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace Hikawa.Editors
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
			if (Game1.currentMinigame == null
			    || Game1.currentMinigame.minigameId() != ModConsts.ArcadeMinigameId) return;

			// Patch in a custom crosshair cursor for the Sailor V shoot 'em up
			var texture = _helper.Content.Load<Texture2D>(
				Path.Combine("assets", ModConsts.SpritesDirectory, 
					$"{ModConsts.ArcadeSpritesFile}.png"));
			asset.AsImage().PatchImage(
				texture,
				new Rectangle(
					LightGunGame.LightGunGame.CrosshairX,
					LightGunGame.LightGunGame.CrosshairY,
					LightGunGame.LightGunGame.CrosshairW,
					LightGunGame.LightGunGame.CrosshairH),
				new Rectangle(0, 0, 16, 16));
		}
	}
}
