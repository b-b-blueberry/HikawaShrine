using System;
using System.IO;
using System.Linq;

using StardewModdingAPI;
using StardewValley;

using Microsoft.Xna.Framework.Graphics;
using xTile;
using xTile.Dimensions;
using xTile.ObjectModel;
using xTile.Tiles;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Hikawa.Editors
{
	internal class WorldEditor : IAssetEditor, IAssetLoader
	{
		private readonly IModHelper _helper;

		public WorldEditor(IModHelper helper)
		{
			_helper = helper;
		}

		/* Loader */

		public bool CanLoad<T>(IAssetInfo asset)
		{
			return asset.AssetNameEquals(Path.Combine(ModConsts.SpritesPath, ModConsts.ExtraSpritesFile));
		}
		
		public T Load<T>(IAssetInfo asset)
		{
			Log.D($"Loaded custom asset ({asset.AssetName})",
				ModEntry.Instance.Config.DebugMode);
			
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
			return asset.AssetNameEquals(@"TileSheets/BuffsIcons")
			       || asset.AssetNameEquals(@"Characters/schedules/Haley") 
			       || asset.AssetNameEquals(@"Maps/Saloon")
			       || asset.AssetNameEquals(@"Maps/Town");
		}
		
		public void Edit<T>(IAssetData asset)
		{
			Log.D($"Editing {asset.AssetName}",
				ModEntry.Instance.Config.DebugMode);

			if (asset.AssetNameEquals(@"TileSheets/BuffsIcons"))
			{
				Log.D($"Patching {asset.AssetName}",
					ModEntry.Instance.Config.DebugMode);

				// Append sprites to the asset:
				const int spriteSize = 16;//px
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

				Log.D($"New buff icon index: {ModEntry.InitialBuffIconIndex}",
					ModEntry.Instance.Config.DebugMode);
			}
			else if (asset.AssetNameEquals(@"Characters/schedules/Haley"))
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
			else if (asset.AssetNameEquals(@"Maps/Saloon"))
			{
				// TODO: CONTENT: Make arcade machine conditional based on story progression
				// have an overnight event adding it to the location
				
				// Saloon - Sailor V Arcade Machine
				var saloonMap = asset.GetData<Map>();
				var x = ModConsts.ArcadeMachinePosition.X;
				var y = ModConsts.ArcadeMachinePosition.Y;
				try
				{
					var tilesheetPath =
						_helper.Content.GetActualAssetKey(
							Path.Combine(ModConsts.SpritesPath, $"{ModConsts.ExtraSpritesFile}.png"));
					if (tilesheetPath == null)
						Log.E("WorldEditor failed to load extras tilesheet.");
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
				catch (Exception ex)
				{
					Log.E("WorldEditor failed to patch Saloon:\n" + ex);
				}
			}
			else if (asset.AssetNameEquals(@"Maps/Town"))
			{
				/*
				try
				{
					var tilesheetPath =
						_helper.Content.GetActualAssetKey(
							Path.Combine(ModConsts.AssetsPath, "Maps", $"{ModConsts.OutdoorsTilesheetFile}.png"));
					if (tilesheetPath == null)
						Log.E("WorldEditor failed to load outdoors tilesheet.");
					var tilesheetPng = _helper.Content.Load<Texture2D>(tilesheetPath);

					var townMap = asset.GetData<Map>();
					var newMap = _helper.Content.Load<Map>(
						Path.Combine(ModConsts.AssetsPath, "Maps", $"{ModConsts.TownSnippetId}.tbin"));
					if (newMap == null)
					{
						Log.E($"WorldEditor failed to load {ModConsts.TownSnippetId} snippet.");
						return;
					}

					// Tilesheets
					var tilesheet = new TileSheet(
						ModConsts.OutdoorsTilesheetFile,
						townMap,
						tilesheetPath,
						new Size(tilesheetPng.Width, tilesheetPng.Height),
						new Size(16, 16));
					townMap.AddTileSheet(tilesheet);
					townMap.LoadTileSheets(Game1.mapDisplayDevice);

					// Joja stockade
					// TODO: SYSTEM: File check for !explosion flag

					// Apply tilesheets to snippet
					var tilesheetNames = new List<string>();
					foreach (var snippetTilesheet in newMap.TileSheets.ToList())
						tilesheetNames.Add(snippetTilesheet.Id);
					foreach (var tilesheetName in tilesheetNames)
					{
						var townTilesheet = townMap.GetTileSheet(tilesheetName);
						var newTilesheet = newMap.GetTileSheet(tilesheetName);
						Log.D("Image sources: " +
						      $"town: {townTilesheet?.ImageSource}, " +
						      $"snippet: {newTilesheet?.ImageSource}");
						if (newTilesheet != null)
							newTilesheet.ImageSource = townTilesheet?.ImageSource;
					}
					// Add townInteriors tilesheet for Joja supplies
					if (true)
					{
						townMap.AddTileSheet(new TileSheet(
							"z_vanilla_interior",
							townMap,
							"Maps/townInterior",
							new Size(512, 1088),
							new Size(16, 16)));
						townMap.LoadTileSheets(Game1.mapDisplayDevice);
					}

					// Patch in the map snippet
					const int xOffset = 7;
					var w = townMap.DisplayWidth;
					var h = townMap.DisplayHeight;
					foreach (var newLayer in newMap.Layers)
					{
						// Add layers
						var layer = townMap.GetLayer(newLayer.Id);
						if (layer == null)
						{
							layer = new Layer(
								newLayer.Id, 
								townMap,
								newLayer.LayerSize, 
								newLayer.TileSize);
							townMap.AddLayer(layer);
							Log.D($"Added new layer {layer.Id} to {townMap.Id}.");
						}

						// Replace tiles
						for (var y = 0; y < h; ++y)
						{
							for (var x = 0; x < w; ++x)
							{
								Log.W($"Tilesheet IDs:");
								Log.W($"town: {layer.Tiles[x, y].TileSheet.Id}, " +
								      $"snippet: {newLayer.Tiles[x, y].TileSheet.Id}");
								layer.Tiles[x, y] = newLayer.Tiles[x + xOffset, y];
							}
						}
					}

					// Entry warping
					newMap.Properties.TryGetValue("Warp", out var warpValue);
					Log.D($"Snippet Warp:  {warpValue}");
					if (warpValue == null)
						Log.E($"WorldEditor failed to read Warp properties from {ModConsts.TownSnippetId} snippet.");
					var currentOffset = 0;
					warpValue = warpValue.ToString().Replace("-90", (currentOffset++).ToString());
					townMap.Properties["Warp"] += ' ' + warpValue;
					Log.D($"Town New Warp: {warpValue}");
				}
				catch (Exception ex)
				{
					Log.E("WorldEditor failed to patch Town:\n" + ex);
				}
				*/
			}
		}
	}
}
