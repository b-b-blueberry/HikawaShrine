using System;
using System.Collections.Generic;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Netcode;

using StardewValley;
using Object = StardewValley.Object;

namespace Hikawa.GameObjects
{
	public class HikawaShrine : GameLocation
	{
		[XmlIgnore]
		private readonly NetObjectList<FarmAnimal> _shrineAnimals = new NetObjectList<FarmAnimal>();

		public HikawaShrine() {}

		public HikawaShrine(string map, string name)
			: base(map, name)
		{
			for (var i = 0; i < 2; ++i)
			{
				_shrineAnimals.Add(new FarmAnimal("White Chicken", ModEntry.Multiplayer.getNewID(), -1));
				_shrineAnimals[i].Position = new Vector2(49 + 2 * i, 22 + i) * 64f;
				_shrineAnimals[i].age.Value = _shrineAnimals[i].ageWhenMature.Value;
				_shrineAnimals[i].reloadData();
			}
		}

		public bool IsTileClearForSpawning(Vector2 position)
		{
			var x = (int)position.X;
			var y = (int)position.Y;
			objects.TryGetValue(position, out var o);
			return o == null
			       && doesTileHaveProperty(x, y,
				       "Spawnable", "Back") != null
			       && !doesEitherTileOrTileIndexPropertyEqual(x, y,
				       "Spawnable", "Back", "F")
			       && isTileLocationTotallyClearAndPlaceable(x, y)
			       && getTileIndexAt(x, y, "AlwaysFront") == -1
			       && getTileIndexAt(x, y, "Front") == -1
			       && !isBehindBush(position)
			       && (Game1.random.NextDouble() < 0.1 || !isBehindTree(position));
		}
		
		/// <summary>
		/// Adds forage to the grassy edges of the map.
		/// Mostly lifted from StardewValley.GameLocation.cs:spawnObjects().
		/// </summary>
		public void SpawnForage()
		{
			Log.D($"Spawning forage on {Name} (currently {numberOfSpawnedObjectsOnMap})");
			const int limitPerMap = 3;
			const int retries = 10;
			Vector2 position;

			// Spawn chicken eggs
			position = new Vector2(Game1.random.Next(25, 35), Game1.random.Next(16, 23));
			if (Game1.random.NextDouble() < 0.06f)
			{
				if (!IsTileClearForSpawning(position))
					position = new Vector2(Game1.random.Next(42, 47), Game1.random.Next(16, 20));
				if (IsTileClearForSpawning(position))
				{
					var roll = Game1.random.NextDouble();
					if (dropObject(new Object(
							position, roll > 0.4f ? 176 : roll > 0.1f ? 180 : roll > 0.04f ? 174 : 182),
						position * 64f,
						Game1.viewport,
						true))
						Log.D("Chicken egg!!!! wow!!",
							ModEntry.Instance.Config.DebugMode);
				}
			}

			// Spawn location forage items
			var forageData = ModEntry.Instance.Helper.Content.Load<Dictionary<string, string>>(
				$"{ModConsts.ForagePath}.json");
			if (!forageData.ContainsKey(name))
			{
				Log.E($"No forage data found for map {name} ({this})");
				return;
			}
			var rawData = forageData[name].Split('/')[Utility.getSeasonNumber(Game1.currentSeason)];
			if (rawData.Equals("-1") || numberOfSpawnedObjectsOnMap >= limitPerMap)
				return;

			// TODO: DEBUG: Does HikawaShrine forage spawning actually restrict to the playable area?

			var objectData = rawData.Split(' ');
			var numberToSpawn = Game1.random.Next(1, Math.Min(limitPerMap - 1, limitPerMap + 1 - numberOfSpawnedObjectsOnMap));
			for (var k = 0; k < numberToSpawn; k++)
			{
				for (var j = 0; j < retries; j++)
				{
					var x = Game1.random.Next(map.DisplayWidth / 64);
					var y = Game1.random.Next(map.DisplayHeight / 64);
					position = new Vector2(x, y);
					
					var whichObject = Game1.random.Next(objectData.Length / 2) * 2;
					if (IsTileClearForSpawning(position)
					    && Game1.random.NextDouble() < double.Parse(objectData[whichObject + 1])
					    && dropObject(new Object(
							position, 
							int.Parse(objectData[whichObject])), 
						new Vector2(x * 64, y * 64), 
						Game1.viewport,
						true))
					{
						++numberOfSpawnedObjectsOnMap;
						break;
					}
				}
			}
		}

		public override void DayUpdate(int dayOfMonth)
		{
			base.DayUpdate(dayOfMonth);
			SpawnForage();
		}

		protected override void initNetFields()
		{
			base.initNetFields();
			base.NetFields.AddFields(_shrineAnimals);
		}

		public override void UpdateWhenCurrentLocation(GameTime time)
		{
			base.UpdateWhenCurrentLocation(time);
			foreach (var animal in _shrineAnimals)
				animal.updateWhenCurrentLocation(time, this);
		}

		public override void draw(SpriteBatch spriteBatch)
		{
			base.draw(spriteBatch);
			foreach (var animal in _shrineAnimals)
				animal.draw(spriteBatch);
		}
	}
}
