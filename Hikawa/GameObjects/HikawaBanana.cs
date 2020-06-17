using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.TerrainFeatures;
using PyTK.CustomElementHandler;
using StardewModdingAPI;
using StardewValley;
using xTile.Dimensions;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Hikawa.GameObjects
{
	public class HikawaBanana : FruitTree, ISaveElement
	{
		private readonly IReflectedField<List<Leaf>> _leaves;
		private readonly IReflectedField<float> _alpha;
		private readonly IReflectedField<float> _shakeRotation;
		private readonly IReflectedField<float> _shakeTimer;

		public HikawaBanana()
		{
			flipped.Value = (Game1.random.NextDouble() < 0.5);
			health.Value = 999999999f;
			daysUntilMature.Value = ModConsts.BigBananaBonanza;

			Reload();

			_leaves = ModEntry.Instance.Helper.Reflection.GetField<List<Leaf>>(this, "leaves");
			_alpha = ModEntry.Instance.Helper.Reflection.GetField<float>(this, "alpha");
			_shakeRotation = ModEntry.Instance.Helper.Reflection.GetField<float>(this, "shakeRotation");
			_shakeTimer = ModEntry.Instance.Helper.Reflection.GetField<float>(this, "shakeTimer");
		}

		public HikawaBanana(int growthStage)
			: this()
		{
			this.growthStage.Value = growthStage;
			Reload();
		}

		private void Reload() {
			Log.W("Reloading Hikawa Banana");
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
				Log.E($"HikawaBanana sapling index {saplingIndex} not in fruitTrees?!");
			}
			else
			{
				Log.D($"Skipping HikawaBanana, not yet JA indexed.");
			}
		}

		public override void dayUpdate(GameLocation environment, Vector2 tileLocation)
		{
			Reload();

			if (health <= -99f)
				ModEntry.Instance.Helper.Reflection.GetField<bool>(this, "destroy").SetValue(true);

			health.Value = 999999999f; // that should do it

			if (struckByLightningCountdown > 0)
			{
				struckByLightningCountdown.Value--;
				if (struckByLightningCountdown <= 0)
				{
					fruitsOnTree.Value = 0;
				}
			}

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
			// Resist damage from tools
			location.playSound("fishingRodBend");
			Game1.player.jitterStrength = 1f;
			return false;
		}
		
		public override bool seasonUpdate(bool onLoad)
		{
			fruitSeason.Value = Game1.currentSeason;
			return false;
		}
		
		/// <summary>
		/// Code mostly lifted from StardewValley:FruitTree.cs:draw()
		/// </summary>
		/// <param name="spriteBatch"></param>
		/// <param name="tileLocation"></param>
		public override void draw(SpriteBatch spriteBatch, Vector2 tileLocation)
		{
			var alpha = _alpha.GetValue();
			if (growthStage < 4)
			{
				var positionOffset = new Vector2(
					(float)Math.Max(-8.0, Math.Min(64.0, Math.Sin((double)(tileLocation.X * 200f) / (Math.PI * 2.0)) * -16.0)), 
					(float)Math.Max(-8.0, Math.Min(64.0, Math.Sin((double)(tileLocation.X * 200f) / (Math.PI * 2.0)) * -16.0))) / 2f;
				var sourceRect = Rectangle.Empty;
				switch (growthStage)
				{
					case 0:
						sourceRect = new Rectangle(0, treeType * 5 * 16, 48, 80);
						break;
					case 1:
						sourceRect = new Rectangle(48, treeType * 5 * 16, 48, 80);
						break;
					case 2:
						sourceRect = new Rectangle(96, treeType * 5 * 16, 48, 80);
						break;
					default:
						sourceRect = new Rectangle(144, treeType * 5 * 16, 48, 80);
						break;
				}
				spriteBatch.Draw(
					texture, 
					Game1.GlobalToLocal(Game1.viewport, new Vector2(
						tileLocation.X * 64f + 32f + positionOffset.X, 
						tileLocation.Y * 64f - sourceRect.Height + 128f + positionOffset.Y)),
					sourceRect, Color.White,
					_shakeRotation.GetValue(),
					new Vector2(24f, 80f),
					4f,
					flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
					getBoundingBox(tileLocation).Bottom / 10000f - tileLocation.X / 1000000f);
			}
			else
			{
				if (!stump)
				{
					spriteBatch.Draw(
						texture, 
						Game1.GlobalToLocal(
							Game1.viewport, 
							new Vector2(tileLocation.X * 64f + 32f, tileLocation.Y * 64f + 64f)),
						new Rectangle(
							(12 + (greenHouseTree ? 1 : Utility.getSeasonNumber(Game1.currentSeason)) * 3) * 16, 
							treeType * 5 * 16 + 64, 
							48, 
							16), 
						struckByLightningCountdown > 0 ? Color.Gray * alpha : Color.White * alpha, 
						0f, new Vector2(24f, 16f), 
						4f, 
						flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 
						1E-07f);

					spriteBatch.Draw(
						texture, 
						Game1.GlobalToLocal(
							Game1.viewport, 
							new Vector2(tileLocation.X * 64f + 32f, tileLocation.Y * 64f + 64f)), 
						new Rectangle(
							(12 + (greenHouseTree ? 1 : Utility.getSeasonNumber(Game1.currentSeason)) * 3) * 16, 
							treeType * 5 * 16,
							48,
							64), 
						struckByLightningCountdown > 0 ? Color.Gray * alpha : Color.White * alpha,
						_shakeRotation.GetValue(),
						new Vector2(24f, 80f),
						4f,
						flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
						getBoundingBox(tileLocation).Bottom / 10000f + 0.001f - tileLocation.X / 1000000f);
				}
				if (health >= 1f)
				{
					spriteBatch.Draw(
						texture,
						Game1.GlobalToLocal(
							Game1.viewport,
							new Vector2(
								tileLocation.X * 64f + 32f + (_shakeTimer.GetValue() > 0f
									? (float)Math.Sin(Math.PI * 2.0 / _shakeTimer.GetValue()) * 2f
									: 0f),
								tileLocation.Y * 64f + 64f)),
						new Rectangle(
							384,
							treeType * 5 * 16 + 48,
							48,
							32),
						struckByLightningCountdown > 0 ? Color.Gray * alpha : Color.White * alpha,
						0f,
						new Vector2(24f, 32f),
						4f,
						flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
						stump
							? getBoundingBox(tileLocation).Bottom / 10000f
							: getBoundingBox(tileLocation).Bottom / 10000f - 0.001f - tileLocation.X / 1000000f);
				}
				for (var i = 0; i < fruitsOnTree; i++)
				{
					switch (i)
					{
					case 0:
						spriteBatch.Draw(Game1.objectSpriteSheet, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileLocation.X * 64f - 64f + tileLocation.X * 200f % 64f / 2f, tileLocation.Y * 64f - 192f - tileLocation.X % 64f / 3f)), Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, ((int)struckByLightningCountdown > 0) ? 382 : ((int)indexOfFruit), 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)getBoundingBox(tileLocation).Bottom / 10000f + 0.002f - tileLocation.X / 1000000f);
						break;
					case 1:
						spriteBatch.Draw(Game1.objectSpriteSheet, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileLocation.X * 64f + 32f, tileLocation.Y * 64f - 256f + tileLocation.X * 232f % 64f / 3f)), Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, ((int)struckByLightningCountdown > 0) ? 382 : ((int)indexOfFruit), 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)getBoundingBox(tileLocation).Bottom / 10000f + 0.002f - tileLocation.X / 1000000f);
						break;
					case 2:
						spriteBatch.Draw(Game1.objectSpriteSheet, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileLocation.X * 64f + tileLocation.X * 200f % 64f / 3f, tileLocation.Y * 64f - 160f + tileLocation.X * 200f % 64f / 3f)), Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, ((int)struckByLightningCountdown > 0) ? 382 : ((int)indexOfFruit), 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.FlipHorizontally, (float)getBoundingBox(tileLocation).Bottom / 10000f + 0.002f - tileLocation.X / 1000000f);
						break;
					}
				}
			}
			foreach (var leaf in _leaves.GetValue())
			{
				spriteBatch.Draw(texture,
					Game1.GlobalToLocal(Game1.viewport, leaf.position),
					new Rectangle((24 + Utility.getSeasonNumber(Game1.currentSeason)) * 16,
						treeType * 5 * 16, 8, 8),
					Color.White,
					leaf.rotation,
					Vector2.Zero,
					4f,
					SpriteEffects.None,
					getBoundingBox(tileLocation).Bottom / 10000f + 0.01f);
			}
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
			Reload();
		}
	}
}
