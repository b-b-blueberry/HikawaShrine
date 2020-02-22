using StardewModdingAPI;
using xTile;
using PyTK.Extensions;

namespace HikawaShrine.Editors
{
	class MapLoader : IAssetLoader
	{
		private readonly IModHelper _helper;

		public MapLoader()
		{
			_helper = ModEntry.Instance.Helper;
		}

		public bool CanLoad<T>(IAssetInfo asset)
		{
			return asset.AssetNameEquals($@"Maps/{Const.ShrineId}")
				|| asset.AssetNameEquals($@"Maps/{Const.ShrineEntryId}");
		}

		public T Load<T>(IAssetInfo asset)
		{
			if (asset.AssetNameEquals($@"Maps/{Const.ShrineId}"))
			{
				var map = _helper.Content.Load<Map>($@"assets/Maps/{Const.ShrineId}.tbin");

				// Additional map layers
				if (map.GetLayer(Const.LayerNearBack) != null)
					map.GetLayer(Const.LayerNearBack).Properties["DrawAbove"] = "Back";
				map.enableMoreMapLayers();

				// Return warping
				var exitX = 30;
				var exitY = map.GetLayer("Back").LayerHeight;
				var destX = 70;
				var destY = 10;
				var dest = "Farm";

				for (var i = 0; i < 3; ++i)
					map.GetLayer("Back").Tiles[exitX + i, exitY + i].Properties["Action"]
						= $"Warp {destX + i} {destY + i} {dest}";

				return (T)(object)map;
			}
			if (asset.AssetNameEquals($@"Maps/{Const.ShrineEntryId}"))
			{
				// Warping to shrine

				// . . .

				// Warping to bus stop

				// . . .

				// Warping to Boarding House

				// . . .
			}

			return (T)(object)null;
		}
	}
}
