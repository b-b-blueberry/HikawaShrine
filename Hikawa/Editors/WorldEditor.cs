using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

using StardewModdingAPI;
using StardewValley;

using xTile;
using xTile.Dimensions;
using xTile.ObjectModel;
using xTile.Tiles;

namespace Hikawa.Editors
{
	internal class WorldEditor : IAssetEditor, IAssetLoader
	{
		private readonly IModHelper _helper;
		private readonly bool _debugMode;

		public WorldEditor(IModHelper helper)
		{
			_helper = helper;
			_debugMode = ModEntry.Instance.Config.DebugMode;
		}

		/* Loader */

		public bool CanLoad<T>(IAssetInfo asset)
		{
			return asset.AssetNameEquals(Path.Combine(ModConsts.SpritesPath, ModConsts.ExtraSpritesFile));
		}
		
		public T Load<T>(IAssetInfo asset)
		{
			Log.D($"Loaded custom asset ({asset.AssetName})",
				_debugMode);
			
			if (asset.AssetNameEquals(Path.Combine(ModConsts.SpritesPath, ModConsts.ExtraSpritesFile)))
				return (T)(object)_helper.Content.Load<Texture2D>(
					Path.Combine(ModConsts.SpritesPath, ModConsts.ExtraSpritesFile));
			return (T) (object) null;
		}
		
		/* Editor */
		
		private static float NearestMultiple(float value, float multiple)
		{
			return (float) Math.Round((decimal) value / (decimal) multiple,
				MidpointRounding.AwayFromZero) * multiple;
		}

		public bool CanEdit<T>(IAssetInfo asset)
		{
			return false 
			       || asset.AssetNameEquals(@"Characters/schedules/Haley")
			       || asset.AssetNameEquals(@"Characters/blueberry.Hikawa.Rei")
			       || asset.AssetNameEquals(@"Data/Locations")
			       || asset.AssetNameEquals(@"Maps/townInterior")
			       || asset.AssetNameEquals(@"Maps/Saloon")
			       || asset.AssetNameEquals(@"Strings/StringsFromMaps")
			       || asset.AssetNameEquals(@"TileSheets/BuffsIcons")
				;
		}
		
		public void Edit<T>(IAssetData asset)
		{
			try
			{
				Log.D($"Editing {asset.AssetName}",
					_debugMode);
				List<string> conflicts = null;
				if (asset.AssetNameEquals(@"Characters/schedules/Haley"))
				{
					// Move binch, get out the way
					var data = asset.AsDictionary<string, string>().Data;
					foreach (var key in data.Keys.ToList())
					{
						if (key.Contains("summer"))
						{
							data.TryGetValue(key, out var value);
							if (value != null)
								data[key] = value.Replace("Town 90 91", "Town 89 93");
						}
					}
				}
				else if (asset.AssetNameEquals(@"Data/Locations"))
				{
					// Patch in location forage, fish, and artefact data
					var data = asset.AsDictionary<string, string>().Data;
					var source = _helper.Content.Load<Dictionary<string, string>>(
						$"{ModConsts.ForagePath}.json");
					conflicts = FindDictionaryConflicts(data, source);
					foreach (var pair in source.Where(pair => !conflicts.Contains(pair.Key)))
						data.Add(pair);
					Log.D("Added location data for"
					      + $"{source.Aggregate("", (s, location) => $"{s}, {location.Key}")}",
						_debugMode);
				}
				else if (asset.AssetNameEquals(@"Maps/Saloon"))
				{
					// TODO: CONTENT: Make arcade machine conditional based on story progression
					// have an overnight event adding it to the location

					// Saloon - Sailor V Arcade Machine
					var saloonMap = asset.GetData<Map>();
					var x = ModConsts.ArcadeMachinePosition.X;
					var y = ModConsts.ArcadeMachinePosition.Y;

					var tilesheetPath =
						_helper.Content.GetActualAssetKey(
							Path.Combine(ModConsts.SpritesPath, $"{ModConsts.ExtraSpritesFile}.png"));
					if (tilesheetPath == null)
						Log.E("Failed to load extras tilesheet.");
					var tilesheetPng = _helper.Content.Load<Texture2D>(tilesheetPath);

					const BlendMode blendMode = BlendMode.Additive;
					var tileSheet = new TileSheet(
						ModConsts.ExtraSpritesFile,
						saloonMap,
						tilesheetPath,
						new Size(tilesheetPng.Width, tilesheetPng.Height),
						new Size(16, 16));
					saloonMap.AddTileSheet(tileSheet);
					saloonMap.LoadTileSheets(Game1.mapDisplayDevice);
					var layer = saloonMap.GetLayer("Front");
					StaticTile[] tileAnim =
					{
						new StaticTile(layer, tileSheet, blendMode, 1),
						new StaticTile(layer, tileSheet, blendMode, 3)
					};

					layer.Tiles[x, y] = new AnimatedTile(
						layer, tileAnim, 1000);
					layer = saloonMap.GetLayer("Buildings");
					layer.Tiles[x, y + 1] = new StaticTile(
						layer, tileSheet, blendMode, 2);
					layer.Tiles[x, y + 1].Properties.Add("Action",
						new PropertyValue(ModConsts.ArcadeMinigameId));
					layer = saloonMap.GetLayer("Back");
					layer.Tiles[x, y + 2] = new StaticTile(
						layer, saloonMap.GetTileSheet("1"), blendMode, 1272);
				}
				else if (asset.AssetNameEquals(@"Maps/townInterior"))
				{
					// Patch in a single tile to be used in HikawaHouse
					// Stitch together a pair of tiles at (128, 96) and (160, 176) to make a new tile in an empty space at (96, 384)
					var texture = asset.GetData<Texture2D>();
					var pixels = new Color[texture.Width * texture.Height];
					texture.GetData(pixels);
					var w = texture.Width;
					for (var x = 0; x < 16; ++x)
					for (var y = 0; y < 16; ++y)
						pixels[96 + x + (384 + y) * w] = pixels[(y < 8 ? 128 : 160) + x + ((y < 8 ? 96 : 176) + y) * w];
					texture.SetData(pixels);
				}
				else if (asset.AssetNameEquals(@"Strings/StringsFromMaps"))
				{
					// Patch in location action strings from i18n default file
					var data = asset.AsDictionary<string, string>().Data;
					var source = _helper.Content.Load<Dictionary<string, string>>(
						$"{ModConsts.StringsPath}.json");
					source = source.Where(pair => pair.Key.StartsWith("world.")).ToDictionary(
						pair => $"{ModConsts.ContentPrefix}{pair.Key.Split(new []{'.'}, 2)[1]}",
						pair => pair.Value);
					conflicts = FindDictionaryConflicts(data, source);
					foreach (var pair in source.Where(pair => !conflicts.Contains(pair.Key)))
						data.Add(pair);
				}
				else if (asset.AssetNameEquals(@"TileSheets/BuffsIcons"))
				{
					// Append sprites to the asset:
					const int spriteSize = 16; //px
					var source = _helper.Content.Load<Texture2D>(
						Path.Combine(ModConsts.SpritesPath, $"{ModConsts.BuffIconSpritesFile}.png"));
					var dest = asset.AsImage();
					var sourceRect = new Rectangle(0, 0, source.Width, source.Height);

					// Align the sprites to the asset tile dimensions
					var ypos = Math.Min(
						dest.Data.Bounds.Height,
						(int) NearestMultiple(dest.Data.Bounds.Height, spriteSize));
					var destRect = new Rectangle(0, ypos, source.Width, source.Height);

					// Substitute the asset with a taller version to accomodate our sprites
					var original = dest.Data;
					var texture = new Texture2D(Game1.graphics.GraphicsDevice, original.Width, destRect.Bottom);
					dest.ReplaceWith(texture);
					dest.PatchImage(original);

					// Patch the sprites into the expanded asset
					dest.PatchImage(source, sourceRect, destRect);

					// Update index for our elements in the asset
					ModEntry.InitialBuffIconIndex = ypos / spriteSize * texture.Width / spriteSize;

					Log.D($"Initial buff icon index: {ModEntry.InitialBuffIconIndex}",
						_debugMode);
				}

				if (conflicts != null && conflicts.Any())
				{
					Log.E($"Found conflicts adding to '{asset.AssetName}':"
					      + $"\n{conflicts.Aggregate("", (s, s1) => $"{s}\n{s1}")}");
				}
			}
			catch (Exception e)
			{
				Log.E($"{MethodBase.GetCurrentMethod().DeclaringType}::{MethodBase.GetCurrentMethod().Name}"
				      + $" failed to patch asset {asset.AssetName}:\n{e}");
			}
		}

		private static List<string> FindDictionaryConflicts(IDictionary<string, string> dest,
			Dictionary<string, string> source)
		{
			return source.Keys.Where(dest.ContainsKey).ToList();
		}
	}
}
