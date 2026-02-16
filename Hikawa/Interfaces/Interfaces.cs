using System;
using System.Collections.Generic;
using System.Linq;
using Hikawa.Objects.Decor;
using Hikawa.Objects.Items;
using Hikawa.Objects.Locations;
using StardewModdingAPI;

namespace Hikawa.Interfaces
{
	public static class Interfaces
	{
		internal static ISpaceCoreAPI SpaceCoreAPI;
		internal static IContentPatcherAPI ContentPatcherAPI;
		internal static ISailorStylesAPI SailorAPI;

		private static IManifest _manifest;
        private static IModRegistry _registry;

        /// <summary>
        /// Registers APIs with the given mod manifest.
        /// </summary>
        public static bool Init(IManifest manifest, IModRegistry registry)
		{
			Interfaces._manifest = manifest;
			Interfaces._registry = registry;

			return Interfaces.RegisterApis();
		}

		private static bool RegisterApis()
        {
			var requirements = new Dictionary<string, Func<string, bool>>
			{
				{
					"spacechase0.SpaceCore", Interfaces.SpaceCore
				},
				{
					"Pathoschild.ContentPatcher", Interfaces.ContentPatcher
				},
				{
					"bbc", Interfaces.Hikawa
				},
				{
					"blueberry.SailorStyles", Interfaces.SailorStyles
				},
			};
			IEnumerable<(string uniqueId, bool isLoaded)> results = requirements
				.Select(pair => (pair.Key, pair.Value.Invoke(pair.Key)));
			if (results.Any(i => !i.isLoaded))
			{
				string failed = string.Join(Environment.NewLine, results.Where(i => !i.isLoaded).Select(i => i.uniqueId));
				Log.E($"One or more required mods were not found:{Environment.NewLine}\t{failed}");
				return false;
			}
			return true;
		}

		private static bool SpaceCore(string uniqueId)
		{
			Interfaces.SpaceCoreAPI = Interfaces._registry.GetApi<ISpaceCoreAPI>(uniqueID: uniqueId);
			Interfaces.SpaceCoreAPI?.RegisterSerializerType(typeof(Shrine));
			Interfaces.SpaceCoreAPI?.RegisterSerializerType(typeof(House));
			Interfaces.SpaceCoreAPI?.RegisterSerializerType(typeof(Hall));
			Interfaces.SpaceCoreAPI?.RegisterSerializerType(typeof(Grove));
			Interfaces.SpaceCoreAPI?.RegisterSerializerType(typeof(Island));
            Interfaces.SpaceCoreAPI?.RegisterSerializerType(typeof(Bow));
            Interfaces.SpaceCoreAPI?.RegisterSerializerType(typeof(Kite));
            Interfaces.SpaceCoreAPI?.RegisterSerializerType(typeof(BugTool));
            Interfaces.SpaceCoreAPI?.RegisterSerializerType(typeof(BugFurniture));
			Interfaces.SpaceCoreAPI?.RegisterSerializerType(typeof(ShrubTool));
			Interfaces.SpaceCoreAPI?.RegisterSerializerType(typeof(Shrub));

			return Interfaces.SpaceCoreAPI is not null;
		}

		private static bool ContentPatcher(string uniqueId)
		{
			Interfaces.ContentPatcherAPI = Interfaces._registry.GetApi<IContentPatcherAPI>(uniqueID: uniqueId);
			return Interfaces.ContentPatcherAPI is not null;
		}

		private static bool Hikawa(string uniqueId)
		{
			return Interfaces._registry.IsLoaded(uniqueId);
		}

		private static bool SailorStyles(string uniqueId)
		{
			Interfaces.SailorAPI = Interfaces._registry.GetApi<ISailorStylesAPI>(uniqueID: uniqueId);

			return true;
		}
	}
}
