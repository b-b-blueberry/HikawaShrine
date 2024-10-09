using Hikawa.Objects.Locations;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;

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
			// Requirements
			IEnumerable<(string name, bool isLoaded)> requirements = new Func<bool>[]
				{ Interfaces.SpaceCore, Interfaces.ContentPatcher, Interfaces.Hikawa }
				.Select(func => (func.Method.Name, func.Invoke()));
			if (requirements.Any(i => !i.isLoaded))
			{
				string failed = string.Join("\n", requirements.Where(i => !i.isLoaded).Select(i => i.name));
				Log.E($"One or more required mods were not found: {failed}");
				return false;
			}

            // Optionals
            IEnumerable<(string name, bool isLoaded)> optionals = new Func<bool>[]
				{ Interfaces.SailorStyles }
				.Select(func => (func.Method.Name, func.Invoke()));
			string found = string.Join("\n", optionals.Where(i => i.isLoaded).Select(i => i.name));
			if (!string.IsNullOrWhiteSpace(found))
			{
				Log.D($"Optional interfaces found: {found}",
					ModEntry.Config.DebugMode);
			}

			return true;
		}

		private static bool SpaceCore()
		{
			Interfaces.SpaceCoreAPI = Interfaces._registry.GetApi<ISpaceCoreAPI>(uniqueID: "spacechase0.SpaceCore");
			Interfaces.SpaceCoreAPI?.RegisterSerializerType(typeof(Shrine));
			Interfaces.SpaceCoreAPI?.RegisterSerializerType(typeof(House));
			Interfaces.SpaceCoreAPI?.RegisterSerializerType(typeof(Hall));

			return Interfaces.SpaceCoreAPI is not null;
		}

		private static bool ContentPatcher()
		{
			Interfaces.ContentPatcherAPI = Interfaces._registry.GetApi<IContentPatcherAPI>(uniqueID: "Pathoschild.ContentPatcher");
			Interfaces.ContentPatcherAPI?.RegisterToken(mod: Interfaces._manifest, name: "SeasonalOutfits", getValue: () =>
			{
				return [ModEntry.Config.SeasonalOutfits.ToString()];
			});
			return Interfaces.ContentPatcherAPI is not null;
		}

		private static bool Hikawa()
		{
			return Interfaces._registry.IsLoaded(ModConsts.ContentModID);
		}

		private static bool SailorStyles()
		{
			Interfaces.SailorAPI = Interfaces._registry.GetApi<ISailorStylesAPI>(uniqueID: "blueberry.SailorStyles");

			return Interfaces.SailorAPI is not null;
		}
	}
}
