using System.Collections.Generic;
using System.IO;
using System.Linq;
using StardewModdingAPI;

namespace Hikawa.Editors
{
	internal class EventEditor : IAssetEditor
	{
		private readonly ITranslationHelper _i18n;

		public EventEditor(IModHelper helper)
		{
			_i18n = helper.Translation;
		}

		public bool CanEdit<T>(IAssetInfo asset)
		{
			return asset.AssetNameEquals(@"Data/Events/Town");
		}
		public void Edit<T>(IAssetData asset)
		{
			var target = asset.AsDictionary<string, string>().Data;
			var source = ModEntry.Instance.Helper.Content.Load<IDictionary<string, string>>(
				$"{ModConsts.EventsPath}.json");

			if (asset.AssetNameEquals(@"Data/Events/Town"))
			{
				// Story: Stock
				var keyvalue = source.First(kvp => kvp.Key.StartsWith("46430001"));
				if (keyvalue.Value == null)
				{
					Log.E("Failed to read from Hikawa Events.");
					return;
				}

				var formattedValue = string.Format(keyvalue.Value,
					_i18n.Get("event.story.stock.0000"),
					_i18n.Get("event.story.stock.0001"),
					_i18n.Get("event.story.stock.0002"),
					_i18n.Get("event.story.stock.0003"),
					_i18n.Get("event.story.stock.0004"),
					_i18n.Get("event.story.stock.0005"));
				target[keyvalue.Key] = formattedValue;
			}
		}
	}
}
