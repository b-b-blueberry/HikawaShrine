using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.TerrainFeatures;
using PyTK.CustomElementHandler;
using StardewValley;
using xTile.Dimensions;

namespace Hikawa.GameObjects
{
	public class HikawaBanana : FruitTree, ISaveElement
	{
		public HikawaBanana()
			: base()
		{
			flipped.Value = (Game1.random.NextDouble() < 0.5);
			health.Value = 99999999f;
			daysUntilMature.Value = 28;
			reload();
		}

		public HikawaBanana(int growthStage)
			: this()
		{
			this.growthStage.Value = growthStage;
			flipped.Value = (Game1.random.NextDouble() < 0.5);
			health.Value = 99999999f;
			daysUntilMature.Value = ModConsts.BigBananaBonanza;
			reload();
		}

		private void reload() {
			loadData();
			loadSprite();
		}
		
		public override void loadSprite()
		{
			try
			{
				if (texture == null)
				{
					texture = Game1.content.Load<Texture2D>("TileSheets\\fruitTrees");
				}
			}
			catch (Exception)
			{
			}
		}

		private void loadData()
		{
			var saplingIndex = ModEntry.Instance.JaApi.GetObjectId("Dark Seed");
			var data = Game1.content.Load<Dictionary<int, string>>("Data\\fruitTrees");
			if (data.ContainsKey(saplingIndex))
			{
				var rawData = data[saplingIndex].Split('/');
				treeType.Value = Convert.ToInt32(rawData[0]);
				indexOfFruit.Value = Convert.ToInt32(rawData[2]);
				fruitSeason.Value = Game1.currentSeason;
				Log.W("Reloaded HikawaBanana");
			}
			else if (saplingIndex != -1)
			{
				Log.E($"HikawaBanana (at {currentTileLocation}) sapling index {saplingIndex} not in fruitTrees?!");
			}
			else
			{
				Log.D($"Skipping HikawaBanana (at {currentTileLocation}), not yet JA indexed.");
			}
		}

		public override void dayUpdate(GameLocation environment, Vector2 tileLocation)
		{
			reload();

			if (health <= -99f)
				ModEntry.Instance.Helper.Reflection.GetField<bool>(this, "destroy").SetValue(true);

			if (struckByLightningCountdown > 0)
			{
				struckByLightningCountdown.Value--;
				if (struckByLightningCountdown <= 0)
				{
					fruitsOnTree.Value = 0;
				}
			}

			// todo: energised dark fruit for lightning, growth in rain, fast growth, growth ignores nearby objects

			var foundSomething = false;
			var surroundingTileLocationsArray = Utility.getSurroundingTileLocationsArray(tileLocation);
			for (var i = 0; i < surroundingTileLocationsArray.Length; i++)
			{
				var v = surroundingTileLocationsArray[i];
				var isClearHoeDirt = environment.terrainFeatures.ContainsKey(v) 
				                     && environment.terrainFeatures[v] is HoeDirt 
				                     && (environment.terrainFeatures[v] as HoeDirt).crop == null;
				if (environment.isTileOccupied(v, "", true) && !isClearHoeDirt)
				{
					var o = environment.getObjectAt((int)v.X, (int)v.Y);
					if (o == null || o.isPassable())
					{
						foundSomething = true;
						break;
					}
				}
			}
			if (!foundSomething || daysUntilMature <= 0)
			{
				if (daysUntilMature > ModConsts.BigBananaBonanza)
				{
					daysUntilMature.Value = ModConsts.BigBananaBonanza;
				}
				daysUntilMature.Value--;
				if (daysUntilMature <= 0)
				{
					growthStage.Value = 4;
				}
				else if (daysUntilMature <= ModConsts.BigBananaBonanza * 0.33f)
				{
					growthStage.Value = 2;
				}
				else if (daysUntilMature <= ModConsts.BigBananaBonanza * 0.66f)
				{
					growthStage.Value = 1;
				}
				else
				{
					growthStage.Value = 0;
				}
			}
			else if (foundSomething && growthStage.Value != 4)
			{
				ModEntry.Instance.Helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue()
					.broadcastGlobalMessage(
						"Strings\\UI:FruitTree_Warning", 
						true,
						Game1.objectInformation[indexOfFruit].Split('/')[4]);
			}
			if (growthStage == 4 
			    && (struckByLightningCountdown > 0 && !Game1.IsWinter 
			        || Game1.currentSeason.Equals(fruitSeason) || environment.IsGreenhouse)
			    && !stump)
			{
				fruitsOnTree.Value = Math.Min(3, fruitsOnTree + 1);
				if (environment.IsGreenhouse)
				{
					greenHouseTree.Value = true;
				}
			}
			if (stump)
			{
				fruitsOnTree.Value = 0;
			}

			// Propagate
			if (growthStage >= 5 && environment is Farm && Game1.random.NextDouble() < 0.15)
			{
				var xCoord = Game1.random.Next(-3, 4) + (int)tileLocation.X;
				var yCoord = Game1.random.Next(-3, 4) + (int)tileLocation.Y;
				var location = new Vector2(xCoord, yCoord);
				var noSpawn = environment.doesTileHaveProperty(xCoord, yCoord, "NoSpawn", "Back");
				if ((noSpawn == null || 
				     (!noSpawn.Equals("Tree") && !noSpawn.Equals("All") && !noSpawn.Equals("True")))
				    && environment.isTileLocationOpen(new Location(xCoord * 64, yCoord * 64))
				    && !environment.isTileOccupied(location) 
				    && environment.doesTileHaveProperty(xCoord, yCoord, "Water", "Back") == null
				    && environment.isTileOnMap(location))
				{
					environment.terrainFeatures.Add(location, new Tree(treeType, 0));
				}
			}
		}

		public override bool performToolAction(Tool t, int explosion, Vector2 tileLocation, GameLocation location)
		{
			// todo: survive hits
			return base.performToolAction(t, explosion, tileLocation, location);
		}
		
		public override bool seasonUpdate(bool onLoad)
		{
			fruitSeason.Value = Game1.currentSeason;
			return false;
		}

		public object getReplacement()
		{
			Log.W("Replacing Hikawa Banana");
			return new FruitTree();
		}

		public Dictionary<string, string> getAdditionalSaveData()
		{
			Log.W("Fetching Hikawa Banana");
			return new Dictionary<string, string>();
		}

		public void rebuild(Dictionary<string, string> additionalSaveData, object replacement)
		{
			Log.W("Rebuilding Hikawa Banana");
		}
	}
}
