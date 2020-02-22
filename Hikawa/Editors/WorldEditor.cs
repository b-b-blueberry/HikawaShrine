using System.IO;
using StardewModdingAPI;
using StardewValley;
using xTile;
using xTile.Dimensions;
using xTile.ObjectModel;
using xTile.Tiles;

namespace HikawaShrine.Editors
{
	internal class WorldEditor : IAssetEditor
	{
		private readonly IModHelper _helper;

		public WorldEditor()
		{
			_helper = ModEntry.Instance.Helper;
		}

		public bool CanEdit<T>(IAssetInfo asset)
		{
			return asset.AssetNameEquals(@"Maps/Saloon");
		}

		public void Edit<T>(IAssetData asset)
		{
			Log.D($"Editing {asset.AssetName}",
				ModEntry.Instance.Config.DebugMode);

			if (asset.AssetNameEquals(@"Maps/Saloon"))
			{
				// todo: make arcade machine conditional based on story progression
				// have an overnight event adding it to the location
				
				// Saloon - Sailor V Arcade Machine
				var map = asset.GetData<Map>();
				var coordsX = (int) Const.ArcadeMachinePosition.X;
				var coordsY = (int) Const.ArcadeMachinePosition.Y;
				var tileSheetPath = _helper.Content.GetActualAssetKey(
					Path.Combine("assets", "Maps", $"{Const.MiscSpritesFile}.png"));

				const BlendMode blendMode = BlendMode.Additive;
				var tileSheet = new TileSheet(
					Const.MiscSpritesFile,
					map,
					tileSheetPath,
					new Size(48, 16),
					new Size(16, 16));
				map.AddTileSheet(tileSheet);
				map.LoadTileSheets(Game1.mapDisplayDevice);
				var layer = map.GetLayer("Front");
				StaticTile[] tileAnim = {
					new StaticTile(layer, tileSheet, blendMode, 0),
					new StaticTile(layer, tileSheet, blendMode, 2)
				};

				layer.Tiles[coordsX, coordsY] = new AnimatedTile(layer, tileAnim, 1000);
				layer = map.GetLayer("Buildings");
				layer.Tiles[coordsX, coordsY + 1] = new StaticTile(layer, tileSheet, blendMode, 1);
				layer.Tiles[coordsX, coordsY + 1].Properties.Add("Action", new PropertyValue(Const.ArcadeMinigameId));
				layer = map.GetLayer("Back");
				layer.Tiles[coordsX, coordsY + 2] = new StaticTile(layer, map.GetTileSheet("1"), blendMode, 1272);
			}
			else if (asset.AssetNameEquals(@"Maps/BusStop"))
			{
				// todo: patch in the shrine entrance

				// . . .
			}
		}
	}
}
