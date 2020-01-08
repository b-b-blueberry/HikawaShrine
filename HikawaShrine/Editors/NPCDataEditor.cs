using System.Collections.Generic;
using System.IO;
using StardewModdingAPI;

namespace HikawaShrine.Editors
{
	internal class NPCDataEditor : IAssetEditor
	{
		public bool CanEdit<T>(IAssetInfo asset)
		{
			return asset.AssetNameEquals("Characters\\Dialogue\\rainy")
				|| asset.AssetNameEquals("Data\\NPCDispositions")
				|| asset.AssetNameEquals("Data\\NPCGiftTastes")
				//|| asset.AssetNameEquals("Data\\Quests")
				|| asset.AssetNameEquals("Strings\\animationDescriptions")
				|| asset.AssetNameEquals("animationDescriptions");
		}
		public void Edit<T>(IAssetData asset)
		{
			var data = asset.AsDictionary<string, string>().Data;
			var customData = ModEntry.Instance.Helper.Content.Load<IDictionary<string, string>>
				(Path.Combine(Const.AssetsPath, asset.AssetName + ".json"));
			foreach (var kv in customData)
				data.Add(kv);
		}
	}
}
