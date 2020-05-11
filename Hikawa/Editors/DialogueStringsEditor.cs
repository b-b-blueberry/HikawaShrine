using StardewModdingAPI;

namespace Hikawa.Editors
{
	class DialogueStringsEditor : IAssetEditor
	{
		private IModHelper _helper;

		public DialogueStringsEditor()
		{
			_helper = ModEntry.Instance.Helper;
		}

		public bool CanEdit<T>(IAssetInfo asset)
		{
			return asset.AssetName.EndsWith(@"Portraits") ||
			       (asset.AssetName.StartsWith(@"Characters") && asset.AssetName.Split('\\').Length < 3);
		}

		public void Edit<T>(IAssetData asset)
		{
			Log.D($"Editing {asset.AssetName}");

			var jp = ModEntry.Instance.Config.JapaneseNames;

		}
	}
}
