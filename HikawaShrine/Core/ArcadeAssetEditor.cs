using System.IO;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;

namespace HikawaShrine
{
	class ArcadeAssetEditor : IAssetEditor
	{ 
		public bool CanEdit<T>(IAssetInfo asset)
		{
			return asset.AssetNameEquals("LooseSprites\\Cursors");
		}
		public void Edit<T>(IAssetData asset)
		{
			if (asset.AssetNameEquals("LooseSprites\\Cursors"))
			{
				int tileSize = LightGunGame.LightGunGame.TileSize;
				Texture2D texture = Hikawa.SHelper.Content.Load<Texture2D>(
					Path.Combine(Const.AssetsPath, Const.MapsPath, Const.ArcadeSprites + ".png"));
				asset.AsImage().PatchImage(
					texture,
					new Microsoft.Xna.Framework.Rectangle(
							LightGunGame.LightGunGame.CROSSHAIR_X,
							LightGunGame.LightGunGame.CROSSHAIR_Y,
							LightGunGame.LightGunGame.CROSSHAIR_W, 
							LightGunGame.LightGunGame.CROSSHAIR_H),
					new Microsoft.Xna.Framework.Rectangle(32, 0, 16, 16));
			}
		}
	}
}
