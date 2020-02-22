using StardewModdingAPI;

namespace HikawaShrine.Editors
{
	class TestEditor : IAssetEditor
	{
		private IModHelper _helper;

		public TestEditor()
		{
			_helper = ModEntry.Instance.Helper;
		}

		public bool CanEdit<T>(IAssetInfo asset)
		{
			return asset.AssetName.StartsWith(@"Portraits") ||
			       (asset.AssetName.StartsWith(@"Characters") && asset.AssetName.Split('\\').Length < 3);
		}

		public void Edit<T>(IAssetData asset)
		{
			Log.D($"Editing {asset.AssetName}");
		}
	}
}
