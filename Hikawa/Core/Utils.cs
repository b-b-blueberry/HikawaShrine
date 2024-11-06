using System;
using System.Collections.Generic;
using Hikawa.Modules;
using Hikawa.Objects.Critters;
using Hikawa.Objects.Locations;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using xTile.ObjectModel;

namespace Hikawa
{
	internal static class Utils
	{
		#region Vector operations

		internal static class Vector
		{
			public static Vector2 PointAt(Vector2 origin, Vector2 target)
			{
				return target - origin;
			}

			public static double RadiansBetween(Vector2 va, Vector2 vb)
			{
				return Math.Atan2(y: vb.Y - va.Y, x: vb.X - va.X);
			}

			public static Vector2 MotionTo(Vector2 origin, Vector2 target)
			{
				return Vector2.Normalize(Utils.Vector.PointAt(origin: origin, target: target));
			}

			public static Vector2 Abs(Vector2 vector)
			{
				return Utils.Vector.Abs(x: Math.Abs(vector.X), y: Math.Abs(vector.Y));
			}

			public static Vector2 Abs(float x, float y)
			{
				return new Vector2(x: Math.Abs(x), y: Math.Abs(y));
			}
		}

		#endregion

		#region Colour operations

		internal class ColorConverter
		{
			// thanks www.easyrgb.com
			public static Vector3 RGBtoHSL(Color color)
			{
				return RGBtoHSL(color.R, color.G, color.B);
			}

			public static Vector3 RGBtoHSL(float r, float g, float b)
			{
				float h, s, l;
				r /= 255f;
				g /= 255f;
				b /= 255f;

				var min = Math.Min(r, Math.Min(g, b));
				var max = Math.Max(r, Math.Max(g, b));
				var range = max - min;
				l = (max + min) / 2f;

				if (!(Math.Abs(0 - range) > 0.001f))
					return Vector3.Zero;

				s = l < 0.5 ? range / (max + min) : range / (2 - max - min);

				var deltaR = ((max - r) / 6 + range / 2) / range;
				var deltaG = ((max - g) / 6 + range / 2) / range;
				var deltaB = ((max - b) / 6 + range / 2) / range;

				if (Math.Abs(max - r) < 0.001f)
					h = deltaB - deltaG;
				else if (Math.Abs(max - g) < 0.001f)
					h = 1 / 3f + deltaR - deltaB;
				else if (Math.Abs(max - b) < 0.001f)
					h = 2 / 3f + deltaG - deltaR;
				else
					h = 0f;

				if (h < 0)
					h += 1;
				if (h > 1)
					h -= 1;

				return new Vector3(h, s, l);
			}

			public static Color HSLtoRGB(Vector3 hsl, Color color)
			{
				return HSLtoRGB(hsl.X, hsl.Y, hsl.Z, color);
			}

			public static Color HSLtoRGB(float h, float s, float l, Color color)
			{
				float x, y;
				int r, g, b;

				y = l < 0.5f ? l * (1 + s) : (l + s) - (s * l);
				x = 2 * l - y;
				r = (int)Math.Round(255 * HtoRGB(x, y, h + 1 / 3f));
				g = (int)Math.Round(255 * HtoRGB(x, y, h));
				b = (int)Math.Round(255 * HtoRGB(x, y, h - 1 / 3f));

				color.R = (byte)r;
				color.G = (byte)g;
				color.B = (byte)b;

				return color;
			}

			private static float HtoRGB(float alpha, float beta, float h)
			{
				if (h < 0)
					h += 1;
				if (h > 1)
					h -= 1;
				if (6 * h < 1)
					return (alpha + (beta - alpha) * 6 * h);
				if (2 * h < 1)
					return beta;
				if (3 * h < 2)
					return alpha + (beta - alpha) * ((2 / 3f) - h) * 6;
				return alpha;
			}
		}

		#endregion

		#region Dialogue methods

		internal static void CreateInspectDialogue(string dialogue)
		{
			Game1.drawDialogueNoTyping(dialogue);
		}

		internal static void CreateQuestionDialogue(string question, List<Response> answers)
		{
			Game1.currentLocation.createQuestionDialogue(question, answers.ToArray(), DialogueAnswers);
		}

		/// <summary>
		/// Creates a hybrid dialogue box using features of inspectDialogue and questionDialogue.
		/// A series of dialogues is presented, with the final dialogue having assigned responses.
		/// </summary>
		internal static void CreateInspectThenQuestionDialogue(List<string> dialogues, List<Response> answerChoices)
		{
			Game1.currentLocation.afterQuestion = DialogueAnswers;
			Game1.activeClickableMenu = new MultipleDialogueQuestion(dialogues, answerChoices);
			Game1.dialogueUp = true;
			Game1.player.canMove = false;
		}

		private static void DialogueAnswers(Farmer who, string answer)
		{
			if (string.IsNullOrEmpty(answer) || answer == "cancel")
				return;

			string[] split = answer.Split(' ');
			Log.W($"Response: {answer}");
			switch (split[0])
			{
				case "offer_yes":
				{
					if (Game1.currentLocation is Shrine shrine)
					{
						shrine.StartBellSequence(who: who);
					}
					break;
				}
				case "offer_no":
				{
					break;
				}
				case "wardrobe_yes":
					Game1.playSound("doorCreakReverse");
					break;

				case "wardrobe_no":
					Game1.playSound("doorCreakReverse");
					break;

				default:
					Log.E($"Invalid dialogue key: {answer}");
					break;
			}
		}

		#endregion

		#region Map operations

		public static List<Vector2> GetTilesWithProperty(GameLocation where, string layer, string property, PropertyValue value = null, bool onlyOne = false)
		{
			List<Vector2> tiles = [];
			var l = where?.Map?.GetLayer(layer);
			if (l is null)
				return tiles;
			for (int x = 0; x < l.LayerWidth; ++x)
			{
				for (int y = 0; y < l.LayerHeight; ++y)
				{
					if (l.Tiles[x, y]?.Properties?.TryGetValue(property, out PropertyValue v) is bool b && b && (value is null || v.ToString() == value.ToString()))
					{
						tiles.Add(new(x, y));
						if (onlyOne)
							return tiles;
					}
				}
			}
			return tiles;
		}

		public static void ResetCustomSharedMapProperties(GameLocation where)
		{
			where.critters?.RemoveAll(c => c is HangingSprite);
			where.critters?.RemoveAll(c => c is LightTile);
			where.sharedLights?.RemoveWhere(pair => pair.Key.StartsWith(ModEntry.ModData.HearthLightBaseId));
		}

		public static void ApplyCustomSharedMapProperties(GameLocation where)
		{
			// Shrine trees
			if (ModEntry.ModData.ShrineTrees?.TryGetValue(where.Name, out List<ShrineTreesEntry> trees) == true)
			{
				foreach (ShrineTreesEntry entry in trees)
				{
					if (ModEntry.ModData.ShrineTreeDefinitions?.TryGetValue(entry.GlobalId, out ShrineTreeDefinitionsEntry definition) == true)
					{
						where.terrainFeatures.TryAdd(entry.Tile, new ShrineTree(entry, definition));
					}
				}
			}

			// Hanging sprites
			if (ModEntry.ModData.HangingSprites?.TryGetValue(where.Name, out List<HangingSpriteEntry> sprites) == true)
			{
				foreach (HangingSpriteEntry entry in sprites)
				{
					where.addCritter(new HangingSprite(entry));
				}
			}

			// Light tiles
			if (ModEntry.ModData.LightTiles?.TryGetValue(where.Name, out List<LightTileEntry> lightTiles) == true)
			{
				foreach (LightTileEntry entry in lightTiles)
				{
					where.addCritter(new LightTile(entry));
				}
			}

			// Lights
			if (ModEntry.ModData.Lights?.TryGetValue(where.Name, out List<LightEntry> lights) == true)
			{
				int i = 0, j = 0;
				foreach (LightEntry entry in lights)
				{
					LightSource light;
					Vector2 position = (entry.Tile + new Vector2(0.5f)) * Game1.tileSize;
					if (entry.TextureName is not null)
					{
						light = new HearthLight(
							id: HearthLight.GetId(where: where, which: i),
							data: entry,
							position: position);
						++i;
					}
					else
					{
						light = new LightSource(
							id: HearthLight.GetId(where: where, which: j) + "_light",
							textureIndex: entry.TextureIndex,
							position: position,
							radius: entry.Scale,
							color: entry.Color);
						++j;
					}
					where.sharedLights.Add(key: light.Id, value: light);
				}
			}
		}

		#endregion

		#region Special effects

		public static void CreateSparkleAtTile(GameLocation where, Vector2 tile)
		{
			Rectangle source = new(272, 0, 16, 16);
			var sprite = TemporaryAnimatedSprite.GetTemporaryAnimatedSprite(
				textureName: AssetManager.ExtraSpritesAssetName,
				sourceRect: source,
				position: tile * Game1.tileSize,
				flipped: false,
				alphaFade: 0f,
				color: Color.White);
			sprite.alpha = 0f;
			sprite.alphaFade = -0.035f;
			sprite.alphaFadeFade = -0.00075f;
			sprite.interval = 1500f;
			sprite.layerDepth = 0f;
			sprite.scale = Game1.pixelZoom;
			sprite.rotationChange = (float)(Math.PI * 2f / 10f * sprite.interval);
			where.TemporarySprites.Add(sprite);
		}

		#endregion

		#region Miscellaneous methods

		public static float GetPreciseTimeOfDay(int time)
		{
			return (int)((time - time % 100) + (time % 100 / 10) * 16.66f)
				+ Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameTenMinutes * 16.6f;
		}

		/// <summary>
		/// Checks whether the player has agency during gameplay, cutscenes, and input sessions.
		/// </summary>
		public static bool IsPlayerAgencyLost(bool ignorePlayerCannotMove = false)
        {
			// HOUSE RULES
			return !Context.IsWorldReady || (!ignorePlayerCannotMove && !Context.CanPlayerMove)
					|| Game1.game1 is null || Game1.currentLocation is null || Game1.player is null // No unplayable games
					|| !Game1.game1.IsActive // No alt-tabbed game state
					|| Game1.player.IsBusyDoingSomething(); // Nothing else
        }

		public static bool IsPlayerSwimming()
		{
			return Game1.player.swimming.Value && !Game1.player.bathingClothes.Value;
        }

        internal static void ResetAnimationVars()
		{
			Game1.player.Halt();
			Game1.player.completelyStopAnimatingOrDoingAction();
			ModEntry.State.Value.AnimationExtraFloat = ModEntry.State.Value.AnimationStage = ModEntry.State.Value.AnimationTimer = 0;
			ModEntry.State.Value.AnimationTarget = Vector2.Zero;
			ModEntry.State.Value.AnimationFlag = false;
		}

		internal static bool IsCharacterInBathHousePool(Character who)
		{
			return who.currentLocation is BathHousePool pool && pool.isWaterTile(xTile: who.TilePoint.X, yTile: who.TilePoint.Y);
		}
		/*
		internal static float GetProgressFromEveningIntoNighttime()
		{
			var now = Game1.timeOfDay;
			var start = Game1.getStartingToGetDarkTime();
			var end = Game1.getTrulyDarkTime();
			return Math.Min(1f, (float)(now - start) / (end - start));
		}
		*/
		internal static bool IsItObonYet()
		{
			return Game1.season == Season.Summer && Game1.dayOfMonth > 27
				   || Game1.season == Season.Fall && Game1.dayOfMonth < 3;
		}

		internal static bool TryPlaySound(string cueName)
		{
			return !string.IsNullOrEmpty(cueName) && Game1.soundBank.Exists(name: cueName) && Game1.playSound(cueName: cueName);
		}

		internal static float CircularFromRatio(float ratio)
		{
			return 0.5f + 0.5f * MathF.Sin(-MathF.PI * 0.5f + ratio * MathF.PI * 2f);
		}

		internal static float SharpCircularFromRatio(float ratio)
		{
			return MathF.Cos(-MathF.PI * 0.5f + ratio * MathF.PI);
		}

		internal static float RatioFromPreciseTime(int startTime, int endTime, bool isCircular = false)
		{
			int range = endTime - startTime;
			float ratio = Math.Clamp((ModEntry.State.Value.PreciseTime - startTime) / range, 0f, 1f);
			return isCircular ? Utils.CircularFromRatio(ratio) : ratio;
		}

		#endregion
	}
}
