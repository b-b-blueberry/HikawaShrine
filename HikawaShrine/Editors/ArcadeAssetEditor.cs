using System.IO;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;

namespace HikawaShrine.Editors
{
	internal class ArcadeAssetEditor : IAssetEditor
	{ 
		public bool CanEdit<T>(IAssetInfo asset)
		{
			return asset.AssetNameEquals("LooseSprites\\Cursors");
		}
		public void Edit<T>(IAssetData asset)
		{
			if (asset.AssetNameEquals("LooseSprites\\Cursors"))
			{
				var texture = ModEntry.Instance.Helper.Content.Load<Texture2D>(
					Path.Combine(Const.MapsPath, Const.ArcadeSpritesFile + ".png"));
				asset.AsImage().PatchImage(
					texture,
					new Microsoft.Xna.Framework.Rectangle(
							LightGunGame.LightGunGame.CrosshairX,
							LightGunGame.LightGunGame.CrosshairY,
							LightGunGame.LightGunGame.CrosshairW, 
							LightGunGame.LightGunGame.CrosshairH),
					new Microsoft.Xna.Framework.Rectangle(32, 0, 16, 16));
			}
		}
	}
}
