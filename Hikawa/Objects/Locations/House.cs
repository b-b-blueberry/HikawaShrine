using System;
using System.Xml.Serialization;
using StardewValley;

namespace Hikawa.Objects.Locations
{
	[XmlType($"{ModConsts.SpaceCoreXmlPrefix}{nameof(House)}")] // SpaceCore serialisation signature
	public class House : GameLocation
	{
		[XmlIgnore]
		public HearthLight HearthLight;
		
		public House() : base() {}

		public House(string filename, string locationName) : base(filename, locationName) {}

		public static GameLocation Get()
		{
			return Game1.getLocationFromName(ModEntry.ModData.MapHouse);
		}

		public override void UpdateWhenCurrentLocation(GameTime time)
		{
			base.UpdateWhenCurrentLocation(time);

			if (this.HearthLight is not null)
			{
				// smoke
				if (time.TotalGameTime.Ticks % 66 == 0)
				{
					var sprite = TemporaryAnimatedSprite.GetTemporaryAnimatedSprite(
						textureName: "LooseSprites/Cursors",
						sourceRect: new Rectangle(372, 1956, 10, 10),
						position: this.HearthLight.position.Value
							+ new Vector2(-42),
						flipped: false,
						alphaFade: 0.002f,
						color: new Color(r: 165, g: 110, b: 110));
					sprite.alpha = 0.6f;
					sprite.motion = new Vector2(0f, -0.5f);
					sprite.acceleration = new Vector2(0.0015f, 0f);
					sprite.interval = 99999f;
					sprite.layerDepth = 1f;
					sprite.scale = Game1.pixelZoom / 2;
					sprite.scaleChange = 0.02f;
					sprite.rotationChange = Game1.random.Next(-2, 3) * (float)Math.PI / 256f;
					this.TemporarySprites.Add(sprite);
				}
				// embers
				if (time.TotalGameTime.Ticks % 66 == 0)
				{
					Color[] colours = [Color.White, Color.OrangeRed, Color.Gold, Color.RosyBrown];
					var sprite = TemporaryAnimatedSprite.GetTemporaryAnimatedSprite(
						textureName: null,
						sourceRect: new Rectangle(0, 0, 1, 1),
						position: this.HearthLight.position.Value
							+ new Vector2(x: -Game1.tileSize, y: 0f) / 2f
							+ new Vector2(x: (float)Game1.random.NextDouble() * Game1.tileSize, y: (float)Game1.random.NextDouble() * Game1.tileSize / 4f),
						flipped: false,
						alphaFade: 0.0025f,
						color: colours[Game1.random.Next(colours.Length)]);
					sprite.alpha = 0.4f + (float)Game1.random.NextDouble() * 0.4f;
					sprite.motion = new Vector2(0f, -0.25f);
					sprite.acceleration = new Vector2(0.0015f, 0f);
					sprite.interval = 99999f;
					sprite.xPeriodic = true;
					sprite.xPeriodicLoopTime = 1500f;
					sprite.xPeriodicRange = Game1.tileSize / 8 + (float)(Game1.random.NextDouble() * Game1.tileSize / 8);
					sprite.layerDepth = 1f;
					sprite.scale = Game1.pixelZoom;
					sprite.scaleChange = -0.02f;
					sprite.texture = Game1.staminaRect;
					this.TemporarySprites.Add(sprite);
				}
			}
		}

		protected override void resetLocalState()
		{
			base.resetLocalState();
		}

		protected override void resetSharedState()
		{
			base.resetSharedState();

			Utils.ApplyCustomSharedMapProperties(this);

			if (this.sharedLights.TryGetValue(HearthLight.GetId(where: this, which: 0), out LightSource light))
				this.HearthLight = light as HearthLight;

			// fire
			/*
			where.TemporarySprites.Add(new TemporaryAnimatedSprite(
				textureName: "LooseSprites/Cursors",
				sourceRect: new Rectangle(276, 1985, 12, 11),
				animationInterval: 50f,
				animationLength: 4,
				numberOfLoops: 99999,
				position: position,
				flicker: true,
				flipped: false,
				layerDepth: 0.0576f,
				alphaFade: 0f,
				color: Color.White,
				scale: Game1.pixelZoom,
				scaleChange: 0f,
				rotation: 0f,
				rotationChange: 0f)
			{
				light = true,
				lightRadius = 3f,
				lightcolor = 
			});
			*/

			// Bedroom door
			// forget it i hate doors
			/*const string layerName = "Buildings";
			Layer layer = where.Map.GetLayer(layerName);
			foreach (Point point in where.interiorDoors.Keys)
			{
				const int blankTileIndex = 1;
				InteriorDoor door = where.interiorDoors.Doors.First(door => door.Position == point);
				TileSheet tileSheet = where.Map.GetTileSheet(ModConsts.HouseTilesheetName);
				Point spriteSize = new (x: 32, y: 36);
				Point tileSize = new (x: 3, y: 2);
				Point tileNeighbourPoint = point + new Point(x: 1, y: 0);
				Vector2 spritePosition = Utility.PointToVector2(
					point
					+ new Point(0, -1)) * Game1.tileSize
					+ (new Vector2(0, 1 - (spriteSize.Y % Game1.smallestTileSize)) * Game1.pixelZoom);
				TemporaryAnimatedSprite sprite = new (
					textureName: tileSheet.ImageSource,
					sourceRect: new (
						x: (tileSheet.SheetWidth * Game1.smallestTileSize) - spriteSize.X,
						y: (tileSheet.SheetHeight * Game1.smallestTileSize) - spriteSize.Y,
						width: spriteSize.X,
						height: spriteSize.Y),
					animationInterval: 0,
					animationLength: 0,
					numberOfLoops: 1,
					position: spritePosition,
					flicker: false,
					flipped: false,
					layerDepth: ((point.Y + 1) * Game1.tileSize - 12) / 10000f,
					alphaFade: 0f,
					color: Color.White,
					scale: Game1.pixelZoom,
					scaleChange: 0f,
					rotation: 0f,
					rotationChange: 0f)
				{
					holdLastFrame = true,
					paused = true,
					motion = new (5, 0),
					endFunction = delegate (int extraInfo)
					{
						layer.Tiles[tileNeighbourPoint.X, tileNeighbourPoint.Y] = null;
					}
				};
				door.Sprite = sprite;
				layer.Tiles[tileNeighbourPoint.X, tileNeighbourPoint.Y] = new StaticTile(
					layer: layer,
					tileSheet: tileSheet,
					blendMode: BlendMode.Alpha,
					tileIndex: blankTileIndex);
				Log.D($"Door created.\n\tDoor tile: {point}\n\tNeighbouring tile: {tileNeighbourPoint}\n\tDoor sprite: {door.Sprite.initialPosition}\n\t({door.Sprite.sourceRect})");
			}*/

			// TODO: DEBUG: Seasonal rei house changes are currently blocked
			return;
			/*
			// Seasonal tiles
			// Butsudan
			int season = Utility.getSeasonNumber(Game1.currentSeason);
			TileSheet tilesheet = where.Map.GetTileSheet(ModConsts.IndoorsSpritesFile);
			Layer buildings = where.Map.GetLayer("Buildings");
			Layer front = where.Map.GetLayer("Front");
			int rowIncrement = tilesheet.SheetWidth;
			int index = 218;
			if (Utils.IsItObonYet())
			{
				// Obon
				buildings.Tiles[16, 3].TileIndex = index;
				buildings.Tiles[17, 3].TileIndex = index + 1;
				buildings.Tiles[16, 4].TileIndex = index + rowIncrement;
				buildings.Tiles[17, 4].TileIndex = index + rowIncrement + 1;
			}
			else
			{
				// Seasonal
				index = 214 + season;
				buildings.Tiles[17, 3].TileIndex = index;
				buildings.Tiles[17, 4].TileIndex = index + rowIncrement;
			}
			// Window flowers
			index = 244 + season;
			buildings.Tiles[3, 15].TileIndex = index;
			buildings.Tiles[3, 16].TileIndex = index + rowIncrement;
			// Table or kotatsu
			index = 276 + season / 2 * 2;
			front.Tiles[5, 15].TileIndex = index;
			front.Tiles[6, 15].TileIndex = index + 1;
			buildings.Tiles[5, 16].TileIndex = index + rowIncrement;
			buildings.Tiles[6, 16].TileIndex = index + rowIncrement + 1;
			buildings.Tiles[5, 17].TileIndex = index + rowIncrement * 2;
			buildings.Tiles[6, 17].TileIndex = index + rowIncrement * 2 + 1;
			buildings.Tiles[6, 16].Properties["Action"] = $"Message \"{ModConsts.ContentPrefix}house.1{season / 2}\"";
			break;*/
		}

		public override void cleanupBeforePlayerExit()
		{
			Utils.ResetCustomSharedMapProperties(this);

			base.cleanupBeforePlayerExit();
		}
	}
}
