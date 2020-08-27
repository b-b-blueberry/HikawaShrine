using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PyTK.Extensions;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using Object = StardewValley.Object;

namespace Hikawa.GameObjects.Menus
{
	public class EmaMenu : IClickableMenu
	{
		public class Bundle
		{
			public readonly Bundles bundle;
			public readonly int flavour;
			public readonly Dictionary<object, object> items = new Dictionary<object, object>();

			public Bundle() {}
			public Bundle(Bundles bundle, int flavour)
			{
				this.bundle = bundle;
				this.flavour = flavour;
			}

			[JsonConstructor]
			public Bundle(Bundles bundle, int flavour, Dictionary<object, object> items) : this(bundle, flavour)
			{
				this.items = items;
			}
		}

		public enum State
		{
			Opening,
			Root,
			Bundle,
			DropIn
		}

		public enum Bundles
		{
			Story, // Top
			Artefacts, // Left
			Produce, // Middle
			Other, // Right
			Friendship, // Bottom
			Count
		}

		public enum ArtefactBundles
		{
			Archaeological,
			Cultural,
			Mystery,
			Nautical,
			Skeletal,
			Count
		}

		public enum ProduceBundles
		{
			Fruit,
			Flowers,
			Animals,
			Fish,
			Food,
			Preserves,
			Count
		}

		public enum MagicBundles
		{

		}
		
		// Source rects for various menu elements
		// Hikawa Bundles texture file:
		private readonly Rectangle EmaFrameSourceRect;
		private readonly Rectangle EmaFudaSourceRect;
		private readonly Rectangle GradientSourceRect;
		private readonly Rectangle ItemSlotIconSourceRect;
		private readonly Rectangle BundleIconSourceRect;
		private readonly List<Rectangle> FoliageSourceRects;
		// TODO: CONTENT: Fill in bundle source rects when finished
		// Cursors:
		private readonly Rectangle StardewPanoramaSourceRect;
		private readonly Rectangle QuestionIconSourceRect = new Rectangle(330, 357, 7, 13);
		private readonly Rectangle ArrowIconSourceRect = new Rectangle(0, 192, 64, 64);
		private readonly Rectangle CloseIconSourceRect = new Rectangle(337, 494, 12, 12);
		// JunimoMenu:
		private readonly Rectangle GiftIconSourceRect = new Rectangle(548, 262, 18, 20);

		private readonly List<List<Rectangle>> ButtonDestRects = new List<List<Rectangle>> {
			new List<Rectangle>(), // Opening
			new List<Rectangle> // Root menu
			{
				new Rectangle(136, 133, 44, 32), // Top (Story)
				new Rectangle(87, 176, 44, 38), // Left (Artefacts)
				new Rectangle(141, 187, 38, 32), // Middle (Produce)
				new Rectangle(188, 189, 44, 32), // Right (???)
				new Rectangle(117, 235, 48, 32), // Bottom (Friendship)
			},
			new List<Rectangle>(), // Bundle
		};

		private readonly Stack<State> _stack = new Stack<State>();
		private readonly Texture2D _texture; // Texture for all custom menu elements
		
		private const int Scale = 4;
		private const int ForegroundAnimFrames = 3;
		private const int TimeToPulse = 2500;
		private const int PulseTime = 1000;
		private const float DebugClockTickSeconds = 2.5f;
		
		private static readonly Color SkyLowerStartColor = Color.SkyBlue;
		private static readonly Color SkyLowerEndColor = Color.Indigo;
		private static readonly Color SkyHigherStartColor = Color.DeepSkyBlue;
		private static readonly Color SkyHigherEndColor = Color.MidnightBlue;
		private static readonly Color SkyDarkColor = Color.Sienna;

		// Lambda
		private IModHelper Helper => ModEntry.Instance.Helper;
		private ITranslationHelper i18n => ModEntry.Instance.Helper.Translation;
		private static ModData Data => ModEntry.Instance.SaveData;

		// Variable
		private string _hoverText;
		private int _hoveredButton; // Index of current hovered button in the ButtonDestRects list
		private Bundles _currentBundle; // Index of which bundle button was clicked in Root menu
		private bool _isNavigatingWithKeyboard;
		private float _DEBUGtimer;
		private float _animTimerForeground;
		private float _animTimerInterface;
		private int _animWhichPieceToAnimate;
		private int _animWhichFrame;
		private int _pulseTimer;
		private int _whenToPulseTimer;
		private int _lastClockTime;
		private Vector3 _initialSkyHSL;
		private Vector3 _initialGradientHSL;
		private Color _skyLowerColour = SkyLowerStartColor;
		private Color _skyHigherColour = SkyHigherStartColor;

		public EmaMenu() : this(State.Opening, true) {}

		public EmaMenu(State state, bool addRootToStack)
		{
			if (addRootToStack)
				_stack.Push(State.Root);
			_stack.Push(state);

			Game1.displayHUD = false;
			
			var topLeft = Utility.getTopLeftPositionForCenteringOnScreen(width, height);
			xPositionOnScreen = (int) topLeft.X;
			yPositionOnScreen = (int) topLeft.Y;

			_texture = Helper.Content.Load<Texture2D>(
				Path.Combine(ModConsts.SpritesPath, $"{ModConsts.BundlesSpritesFile}.png"));

			LoadBundlesState();

			// Scale draw positions and dimensions for buttons
			foreach (var destRects in ButtonDestRects)
			{
				for (var j = 0; j < destRects.Count; ++j)
				{
					var dest = destRects[j];
					dest.X *= Scale;
					dest.Y *= Scale;
					dest.Width *= Scale;
					dest.Height *= Scale;
					destRects[j] = dest;
				}
			}

			// Choose sprite variants
			var currentStory = ModEntry.GetCurrentStory();
			var misty = currentStory.Key == ModData.Chapter.Mist && currentStory.Value == ModData.Progress.Started;
			var season = Utility.getSeasonNumber(Game1.currentSeason);

			season = 0;

			// TODO: CONTENT: Re-enable Ema menu spritesheet variants when ready and move to init()
			// Cursors:
			StardewPanoramaSourceRect = new Rectangle(
				150 + season == 0 ? 0 : 320 * (season - 1), 736, 320, 148);

			// Hikawa Bundles:
			EmaFrameSourceRect = new Rectangle(
				0, StardewPanoramaSourceRect.Width * season, StardewPanoramaSourceRect.Width, 360);
			EmaFudaSourceRect = new Rectangle(
				0, EmaFrameSourceRect.Height, EmaFrameSourceRect.Width, 180);
			GradientSourceRect = new Rectangle(
				EmaFudaSourceRect.Width, EmaFudaSourceRect.Y, 5, EmaFudaSourceRect.Height);
			ItemSlotIconSourceRect = new Rectangle(
				GradientSourceRect.X + GradientSourceRect.Width, GradientSourceRect.Y, 18, 18);
			BundleIconSourceRect = new Rectangle(
				0, 0, 0, 0);

			var rect = new Rectangle(
				140 * ForegroundAnimFrames * season, EmaFudaSourceRect.Y + EmaFudaSourceRect.Height, 140, 80);
			FoliageSourceRects = new List<Rectangle>();
			for (var i = 0; i < 4; ++i)
			{
				FoliageSourceRects.Add(new Rectangle(
					rect.X + (i != 1 ? 0 : rect.Width / 2),
					rect.Y + (i < 2 ? 0 : (i - 1) * rect.Height),
					i < 2 ? rect.Width / 2 : rect.Width,
					rect.Height));
			};
			
			ModEntry.OverlayEffectControl.Set(OverlayEffectControl.Effect.Nighttime);
		}

		protected override void cleanupBeforeExit()
		{
			Game1.displayHUD = true;
			ModEntry.OverlayEffectControl.Previous();

			base.cleanupBeforeExit();
		}

		internal static void LoadBundlesState()
		{
			Log.W("LoadBundlesState");

			if (Data.BundlesThisSeason != null)
				return;
			Data.BundlesThisSeason = new List<Bundle>();
			ResetBundlesForNewSeason();
		}

		internal static void ResetBundlesForNewSeason()
		{
			ResetBundlesForNewSeason(true, false);
		}

		internal static void ResetBundlesForNewSeason(bool actuallyReset, bool ignoreLastSeason)
		{
			// TODO: DEBUG: Test ResetBundlesForNewSeason in games spanning more than one season (or year) in a single session

			var season = Game1.dayOfMonth < 27
				? Game1.currentSeason
				: Utility.getSeasonNameFromNumber(Utility.getSeasonNumber(Game1.currentSeason) % 4);
			
			Log.W($"ResetBundlesForNewSeason(actuallyReset: {actuallyReset}) - season: {season}");

			var locationData = Game1.content.Load<Dictionary<string, string>>
				("Data\\Locations");
			var recipeData = Game1.content.Load<Dictionary<string, string>>
				("Data\\CookingRecipes");
			var cropData = Game1.content.Load<Dictionary<int, string>>
				("Data\\Crops");
			var fruitTreeData = Game1.content.Load<Dictionary<int, string>>
				("Data\\fruitTrees");
			var shellfish = new[] {372, 392, 393, 394, 397, 715, 716, 717, 718, 719, 720, 721, 722, 723};

			// All fish available in any location for this season, where seasonal fish ID isn't -1 (none)
			var seasonalFish = new List<int>();
			seasonalFish = locationData.Values.Select(data => data
						.Split('/')[3 + Utility.getSeasonNumber(season)]
						.Split(' ').ToList().ConvertAll(s => (int)float.Parse(s))
						.Where(fish => fish > 1))
					.Aggregate(seasonalFish, (current, dataValues)
						=> current.Union(dataValues).ToList());

			// Default item IDs for filler items not necessarily tailored to the player's farm
			var produceFlavourIds = new Dictionary<ProduceBundles, List<int>>
			{
				{ ProduceBundles.Animals, new List<int> {186, 424, 426, 428, 438, 444, 446} },
				{ ProduceBundles.Fish, new List<int> {128, 129, 130, 131, 132, 136, 137,
					138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 154,
					155, 156, 158, 159, 160, 161, 162, 164, 165, 267, 269, 698, 699, 700, 701,
					702, 704, 705, 706, 707, 708, 734, 775, 795, 796, 798, 799} },
				{ ProduceBundles.Fruit, new List<int> {628, 629, 630, 631, 632, 633} },
				{ ProduceBundles.Flowers, new List<int> {376, 591, 593, 595, 597} },
				{ ProduceBundles.Food, new List<int> {} },
				{ ProduceBundles.Preserves, new List<int> {303, 304, 344, 346, 348, 350, 459} },
			};
			var produceQuantities = new Dictionary<ProduceBundles, int>
			{
				{ProduceBundles.Animals, 3},
				{ProduceBundles.Fish, 5},
				{ProduceBundles.Fruit, 3},
				{ProduceBundles.Flowers, 9},
				{ProduceBundles.Food, 1},
				{ProduceBundles.Preserves, 6},
			};
			var artefactFlavourIds = new Dictionary<ArtefactBundles, List<int>>
			{
				{ ArtefactBundles.Archaeological, new List<int> {100, 101, 105, 110, 111, 112, 115, 118, 120} },
				{ ArtefactBundles.Cultural, new List<int> {100, 106, 113, 119, 123,} },
				{ ArtefactBundles.Mystery, new List<int> {103, 104, 108, 121, 122, 126, 127} },
				{ ArtefactBundles.Nautical, new List<int> {116, 117, 586, 587, 588, 589} },
				{ ArtefactBundles.Skeletal, new List<int> {579, 580, 581, 582, 583, 584, 585} },
			};

			List<Bundle> bundles = new List<Bundle>();
			Bundle bundle;
			List<int> ids, uniqueIds = new List<int>();
			int flavour, itemSlots, quantity, quality, averageAmount;
			
			// Story bundle:

			bundle = new Bundle(Bundles.Story, -1);
			for (var i = ModData.Chapter.None; i < ModData.Chapter.End; ++i)
				bundle.items.Add(i, null);
			bundles.Add(bundle);

			// Produce bundle:

			averageAmount = 5;
			quantity = Game1.random.Next(averageAmount - 2, averageAmount + 1);
			itemSlots = averageAmount - 1 + (averageAmount - quantity);
			
			do flavour = Game1.random.Next(0, (int) ProduceBundles.Count);
			while (!ignoreLastSeason 
			       && Data.BundlesThisSeason.Any(bundle => 
				       bundle.bundle == Bundles.Produce
				       && Data.BundlesThisSeason[(int) Bundles.Produce].flavour == flavour));
			switch (flavour)
			{
				case (int) ProduceBundles.Food:
				{
					flavour = (int) ProduceBundles.Food;
					bundle = new Bundle(Bundles.Produce, flavour);
					ids = produceFlavourIds[(ProduceBundles) flavour];
					quantity = produceQuantities[(ProduceBundles) flavour];
					quality = Game1.year switch
					{
						1 => 0,
						2 => 2,
						3 => 2,
						_ => 4
					};

					var recipes = Utility.GetAllPlayerUnlockedCookingRecipes();
					foreach (var data in recipeData.Where((pair, i) => recipes.Contains(pair.Key)))
					{
						var ingredients = data.Value.Split('/')[0].Split(' ')
							.ToList().ConvertAll(int.Parse);
						// Include categories (<0) and ingredients (>10) values only
						// hopefully no recipes have a quantity > 10 for some ingredient
						var seasonalIngredients = ingredients.Where(id => id < 0 || id > 10).ToList();
						var goodIngredients = seasonalIngredients.ConvertAll(id => id < 0);
						for (var i = 0; i < seasonalIngredients.Count; ++i)
						{
							var id = seasonalIngredients[i];
							var ingredient = new Object(id, 1);

							Log.D($"{data.Key}: Checking ingredient [{i}] {ingredient.Name} ({id})");

							// Avoid out-of-season vegetables and flowers
							if (ingredient.Category == -75 || ingredient.Category == -80
							    && cropData.Values.Any(crop => 
								    int.Parse(crop.Split('/')[3]) == id 
								    && crop.Split('/')[1] != season))
							{
								Log.D($"{ingredient.Name} was a crop from another season.");
								break;
							}

							// Avoid out-of-season fruits
							if (ingredient.Category == -79
							    && fruitTreeData.Any(pair =>
								    int.Parse(pair.Value.Split('/')[2]) == id
								    && pair.Value.Split('/')[1] != season))
							{
								Log.D($"{ingredient.Name} was a fruit tree from another season.");
								break;
							}

							// Avoid out-of-season fish
							if (ingredient.Category == -4
								&& !seasonalFish.Contains(id)
								&& (!shellfish.Contains(id)
								    && (season == "spring"
								        || season == "summer")))
							{
								Log.D($"{ingredient.Name} was a fish from another season.");
								break;
							}

							goodIngredients[i] = true;
						}

						Log.D(goodIngredients.Aggregate("Good ingredients: ", (s, b) => $"{s} {b}"));

						if (!goodIngredients.All(good => good))
							continue;

						Log.D(seasonalIngredients.Aggregate($"All ingredients for {data.Key} were OK", (s, id) =>
							$"{s}, {new Object(id, 1).Name} ({id})"));
						ids.Add(int.Parse(data.Value.Split('/')[2]));
					}

					if (ids.Count < 4)
						goto case (int) ProduceBundles.Flowers;

					Utility.Shuffle(Game1.random, ids);
					foreach (var id in ids)
					{
						if (bundle.items.Count >= itemSlots)
							break;
						bundle.items.Add(new Object(id,
							quantity) {quality = {quality}}, null);
					}

					break;
				}

				case (int) ProduceBundles.Preserves:
				{
					flavour = (int) ProduceBundles.Preserves;
					bundle = new Bundle(Bundles.Produce, flavour);
					ids = produceFlavourIds[(ProduceBundles) flavour];
					quantity = produceQuantities[(ProduceBundles) flavour];
					quality = Game1.year switch
					{
						1 => 0,
						2 => 2,
						3 => 2,
						_ => 4
					};

					Utility.Shuffle(Game1.random, ids);
					foreach (var id in ids)
					{
						var produce = new Object(id, 1);

						if (bundle.items.Count >= itemSlots - 1)
							break;
						
						if (produce.Price > 500)
							quantity = Math.Max(1, quantity / 2 + 1);
						bundle.items.Add(new Object(id,
							quantity) {quality = {quality}}, null);
					}

					// Add any unique objects to the bundle
					if (uniqueIds.Count < 1 || Game1.random.NextDouble() < 0.66f)
						break;
					var unique = new Object(uniqueIds[Game1.random.Next(uniqueIds.Count - 1)], 1);
					unique.Stack = unique.Price < 500 ? 3 : 1;
					bundle.items.Add(unique, null);

					break;
				}

				case (int) ProduceBundles.Fish:
				{
					flavour = (int) ProduceBundles.Fish;
					bundle = new Bundle(Bundles.Produce, flavour);
					ids = produceFlavourIds[(ProduceBundles) flavour];
					quantity = produceQuantities[(ProduceBundles) flavour];
					quality = Game1.year switch
					{
						1 => 0,
						2 => 2,
						3 => 2,
						_ => 4
					};

					if (Game1.getFarm().buildings.Any(building => building is FishPond))
						// Pearl, Aged Roe, Caviar, Squid Ink
						uniqueIds = new List<int> {797, 447, 445, 814};

					if (season == "winter")
						// Pearl, Midnight Squid, Spookfish, Blobfish
						uniqueIds = uniqueIds.Union(new List<int>{797, 798, 799, 800}).ToList();
					
					if (Game1.random.NextDouble() < 0.35
						&& season == "spring" || season == "summer")
					{
						// Fetch a selection of items based on shellfish and crabbing loot:

						// Clam, Nautilus Shell, Coral, Rainbow Shell, Sea Urchin
						var bits = new[] {372, 392, 393, 394, 397};
						// Cockle, Mussel, Shrimp, Snail, Periwinkle, Oyster
						var small = new[] {718, 719, 720, 721, 722, 723};
						// Crayfish, Lobster, Crab
						var big = new[] {715, 716, 717};
						// Chowder, Fish Stew, Lobster Bisque, Shrimp Cocktail
						var foods = new[] {727, 728, 730, 732, 733};
						for (var i = 0; i < 2; ++i)
						{
							var which = Game1.random.Next(small.Length - 1);
							bundle.items.Add(new Object(small[which], 9)
								{quality = {quality}}, null);
							small[which] = small[small.Length - 1 - which];
						}
						bundle.items.Add(new Object(bits[Game1.random.Next(bits.Length - 1)], 5),
							null);
						bundle.items.Add(new Object(big[Game1.random.Next(big.Length - 1)], 3)
							{quality = {quality}}, null);
						bundle.items.Add(new Object(foods[Game1.random.Next(foods.Length - 1)], 1),
							null);
					}
					else
					{
						Utility.Shuffle(Game1.random, ids);
						foreach (var id in ids)
						{
							var produce = new Object(id, 1);
							Log.D($"Checking seasonal fish: {produce.Name} (in season: {seasonalFish.Any(fish => fish == id)})");
							// Avoid out-of-season fish
							if (seasonalFish.All(fish => fish != id))
								continue;
							if (bundle.items.Count >= itemSlots - 1)
								break;
						
							if (produce.Price > 500)
								quantity = Math.Max(1, quantity / 2 + 1);
							bundle.items.Add(new Object(id, quantity)
								{quality = {quality}}, null);
						}

						// Add any unique items here, we don't care about them for the shellfish bundle
						if (uniqueIds.Count < 1 || Game1.random.NextDouble() < 0.66f)
							break;
						var unique = new Object(uniqueIds[Game1.random.Next(uniqueIds.Count - 1)],
							1);
						unique.Stack = unique.Price < 300 ? 5 : 3;
						bundle.items.Add(unique, null);
					}

					break;
				}

				case (int) ProduceBundles.Animals:
				{
					flavour = (int) ProduceBundles.Animals;
					bundle = new Bundle(Bundles.Produce, flavour);
					ids = produceFlavourIds[(ProduceBundles) flavour];
					quantity = produceQuantities[(ProduceBundles) flavour];
					quality = Game1.year switch
					{
						1 => 0,
						2 => 2,
						3 => 2,
						_ => 4
					};

					// Ask for quality animal products the player is likely to earn without changing their farm goals
					var animals = Game1.getFarm().getAllFarmAnimals();
					var buildings = Game1.getFarm().buildings;
					
					if (buildings.Any(building => building.buildingType.Value.EndsWith("Coop")))
						// Mayonnaise
						uniqueIds = uniqueIds.Union(new[] { 306, 307, 308 }).ToList();

					if (buildings.Any(building => building.buildingType.Value.EndsWith(" Coop")))
						// Rabbit's Foot
						uniqueIds.Add(446);
					
					if (buildings.Any(building =>
							building.buildingType.Value.EndsWith(" Coop") 
							|| animals.Any(animal => animal.type.Contains("Dinosaur"))
							|| Game1.getAllFarmers().Any(f => f.hasUnlockedSkullDoor 
							                                  || Game1.player.archaeologyFound.ContainsKey(107))))
						// Dinosaur Mayonnaise
						uniqueIds.Add(807);

					if (season != "winter" && animals.Any(animal => animal.type.Contains("Pig")) 
						|| (buildings.Any(building => building.buildingType.Value.Contains("Deluxe Barn")) 
						    && !animals.Any(animal => animal.type.Contains("Sheep"))))
						// Truffle, Truffle Oil
						uniqueIds = uniqueIds.Union(new []{430, 432}).ToList();

					if (buildings.Any(building => building.buildingType.Value.Contains("Slime")))
						// Slime
						uniqueIds.Add(766);
					
					var uniqueCount = Math.Min(Game1.random.Next(2, itemSlots), uniqueIds.Count);

					Utility.Shuffle(Game1.random, ids);
					foreach (var id in ids)
					{
						if (bundle.items.Count >= itemSlots - uniqueCount)
							break;
						// Please don't ask for iridium quality crafting ingredients
						bundle.items.Add(new Object(id, quantity)
							{Quality = new Object(id, 1).Category == -18 ? quality : 0}, null);
					}

					// Add any unique objects to the bundle
					for (var i = 0; i < uniqueCount; ++i)
					{
						var which = Game1.random.Next(uniqueIds.Count - 1);
						
						// Please don't ask for iridium quality crafting ingredients
						var unique = new Object(uniqueIds[which], 1);
						unique.Quality = unique.Category == -18 ? quality : 0;
						unique.Stack = unique.Price < 350 ? 5 : 3;
						bundle.items.Add(unique, null);

						uniqueIds[which] = uniqueIds[uniqueIds.Count - 1 - which];
					}
					
					if (bundle.items.Count < 4)
						goto case (int) ProduceBundles.Fish;

					break;
				}

				case (int) ProduceBundles.Flowers:
				{
					flavour = (int) ProduceBundles.Flowers;
					bundle = new Bundle(Bundles.Produce, flavour);
					ids = produceFlavourIds[(ProduceBundles) flavour];
					quantity = produceQuantities[(ProduceBundles) flavour];
					quality = Game1.year switch
					{
						1 => 0,
						2 => 2,
						3 => 2,
						_ => 4
					};

					foreach (var id in ids)
					{
						var seed = cropData.FirstOrDefault(pair =>
							int.Parse(pair.Value.Split('/')[3]) == id).Key;
						var flower = new Crop(seed, -1, -1);
						// Fetch seasonal flowers
						if (!flower.seasonsToGrowIn.Contains(season))
							continue;
						if (bundle.items.Count >= itemSlots - 1)
							break;
						bundle.items.Add(new Object(flower.indexOfHarvest.Value,
							quantity) {quality = {quality}}, null);
					}

					// Add some seasonal forage flowers, or a sunflower crop for Autumn
					var which = season switch
					{
						"winter" => new [] {283, 418},
						"fall" => new [] {421},
						"summer" => new [] {402},
						_ => new [] {18, 19}
					};
					bundle.items.Add(new Object(which[Game1.random.Next(which.Length - 1)], 5), null);

					// Add seasonal honey to the bundle
					bundle.items.Add(new Object(340,
						Game1.year < 3 ? 3 : 5) {Name = "Seasonal Honey"}, null);

					// TODO: HINT: Use preservedParentSheetIndex on honey to determine the season of the flower later on
					// or just honeyType probably really

					if (ids.Count < 4)
						goto case (int) ProduceBundles.Animals;

					break;
				}

				case (int) ProduceBundles.Fruit:
				{
					flavour = (int) ProduceBundles.Fruit;
					bundle = new Bundle(Bundles.Produce, flavour);
					ids = produceFlavourIds[(ProduceBundles) flavour];
					quantity = produceQuantities[(ProduceBundles) flavour];

					quality = Game1.year switch
					{
						1 => 0,
						2 => 0,
						3 => 1,
						4 => 2,
						_ => 4,
					};
					ids = ids.Union(ModEntry.Instance.JaApi.GetAllFruitTreeIds().Values).ToList();

					if (ids.Count < 4)
						goto case (int) ProduceBundles.Preserves;

					Utility.Shuffle(Game1.random, ids);
					foreach (var id in ids)
					{
						var tree = new FruitTree(id);
						// Fetch seasonal tree products
						if (tree.fruitSeason.Value != season)
							continue;
						if (bundle.items.Count >= itemSlots)
							break;
						bundle.items.Add(new Object(tree.indexOfFruit.Value,
							quantity) {quality = {quality}}, null);
					}
					
					// Add tapped tree products
					var tapperProducts = new[] {724, 725, 726};
					bundle.items.Add(new Object(
						tapperProducts[Game1.random.Next(0, 3)],
						3 + Game1.random.Next(2)), null);

					break;
				}
			}
			bundles.Add(bundle);

			// Artefacts bundle:

			do flavour = Game1.random.Next(0, (int) ArtefactBundles.Count);
			while (!ignoreLastSeason 
			       && Data.BundlesThisSeason.Any(bundle =>
				       bundle.bundle == Bundles.Artefacts
				       && Data.BundlesThisSeason[(int) Bundles.Artefacts].flavour == flavour));
			bundle = new Bundle(Bundles.Artefacts, flavour);
			ids = artefactFlavourIds[(ArtefactBundles) flavour];

			itemSlots = Math.Min(6, Game1.random.Next(4, Math.Max(4, ids.Count - 2)));
			quantity = Game1.year < 5 ? 1 : 2;
			
			Utility.Shuffle(Game1.random, ids);
			foreach (var id in ids)
			{
				var obj = new Object(id, quantity);
				if (bundle.items.Count >= itemSlots)
					break;
				bundle.items.Add(obj, null);
			}
			bundles.Add(bundle);

			// Other bundle:

			// ...

			bundle = new Bundle(Bundles.Other, -1);
			bundles.Add(bundle);

			// Friendship bundle:
			
			var characters = new List<object>
			{
				ModConsts.ReiNpcId,
				ModConsts.GrampsNpcId,
				ModConsts.AmiNpcId,
				ModConsts.YuuichiroNpcId,
			};
			bundle = new Bundle(Bundles.Friendship, -1);
			foreach (var chara in characters.Where(chara => !bundle.items.ContainsKey(chara)))
				bundle.items.Add(chara, Game1.player.tryGetFriendshipLevelForNPC((string) chara));
			bundles.Add(bundle);

			// Finally, apply changes to bundles in mod save data
			if (actuallyReset)
				Data.BundlesThisSeason = bundles;

			PrintCurrentBundles(bundles);
		}

		internal static void PrintCurrentBundles(List<Bundle> bundles)
		{
			Log.D("Bundles for this season:");
			foreach (var bung in bundles.Where(b => b.items.Count > 0))
			{
				Log.D(bung.items.Aggregate($"\n{bung.bundle} ({bung.flavour}) - ", (s1, pair) 
					=> s1 + (pair.Key.GetType() == typeof(Object)
						? (pair.Key as Object).Name
						: pair.Key) + ", ") );
			}
		}

		private void PopMenuStack(bool playSound)
		{
			if (_stack.Count < 1)
				return;
			_stack.Pop();

			if (playSound)
				Game1.playSound("bigDeSelect");

			if (!readyToClose() || _stack.Count > 0)
				return;
			Game1.exitActiveMenu();
			cleanupBeforeExit();
		}

		private void ClickButton(int button, bool playSound)
		{
			if (_stack.Count < 1)
				return;
			
			// TODO: SYSTEM: Fill in Ema ClickButton

			_hoverText = "";
			var state = _stack.Peek();
			switch (state) {
				case State.Root:
					// Enter the Bundle menu focused around the bundle that was clicked on
					_stack.Push(State.Bundle);
					_currentBundle = (Bundles) button;
					ResetBundleButtons();
					break;

				case State.Bundle:
					switch (button)
					{
						// Return
						case 0:
							PopMenuStack(true);
							playSound = false;
							break;

						// Left/Right
						case 1:
							_currentBundle = _currentBundle == 0 ? Bundles.Count - 1 : --_currentBundle;
							ResetBundleButtons();
							break;
						case 2:
							_currentBundle = _currentBundle == Bundles.Count - 1 ? 0 : ++_currentBundle;
							ResetBundleButtons();
							break;
					}

					break;

				case State.DropIn:
					break;
			}

			if (playSound)
				Game1.playSound("bigSelect");
		}

		private void ResetBundleButtons()
		{
			const int yOffset = 16;
			const float yRate = 1.2f;
			const float xRate = 1.2f;

			var buttonArea = new Rectangle(100, 80, 128, 64);
			var buttons = new List<Rectangle>();
			var count = Data.BundlesThisSeason[(int)_currentBundle].items.Count;
			Rectangle source;

			// Reset return button
			source = CloseIconSourceRect;
			buttons.Add(new Rectangle(
				xPositionOnScreen * 2 / 4 * 3 + (source.Width - 1) * Scale,
				yPositionOnScreen / 3,
				source.Width * Scale,
				source.Height * Scale));

			// Reset left/right buttons
			source = ArrowIconSourceRect;
			for (var i = 0; i < 2; ++i)
			{
				buttons.Add(new Rectangle(
					xPositionOnScreen * 2 / 5 * (i == 0 ? 1 : 4) - source.Width / 2,
					yPositionOnScreen * 2 / 2 + yOffset - source.Height / 2,
					source.Width,
					source.Height));
			}
			
			// TODO: TEST: Bundle item drop-in buttons positioning
			// Reset bundle item drop-in buttons
			source = ItemSlotIconSourceRect;
			for (var i = 0; i < count; ++i)
			{
				// Arrange buttons to fit in a shallow up-ended horseshoe, hopefully clustering at the centre for smaller bundles
				var buttonsFromCentre = count / 2 - i;
				var x = buttonArea.X
				        + buttonArea.Width / 2
				        + ((int)((buttonsFromCentre * xRate) / 100 * buttonArea.Width / 4)
				           * buttonsFromCentre < 0 ? -1 : 1)
						- source.Width / 2;
				var y = buttonArea.Y
				        + (int)((Math.Abs(buttonsFromCentre) * yRate * yOffset) / 100 * buttonArea.Height);
				buttons.Add(new Rectangle(x * Scale, y * Scale, source.Width * Scale, source.Height * Scale));
			}

			ButtonDestRects[(int) State.Bundle] = buttons;
			Log.W($"Bundle item buttons: {buttons.Count}");
		}

		private static void SetEveningTenMinuteHue(ref Color color, ref Vector3 initialHSL, Vector3 deltaHSL)
		{
			var hsl = ModEntry.ColorConverter.RGBtoHSL(color);
			if (hsl == Vector3.Zero)
				return;
			
			if (initialHSL == Vector3.Zero)
				initialHSL = hsl;

			var steps = Game1.getTrulyDarkTime() - Game1.getStartingToGetDarkTime();
			var current = Math.Max(0, Math.Min(steps, Game1.timeOfDay - Game1.getStartingToGetDarkTime()));
			
			float target, range, step;

			if (deltaHSL.X > 0f)
			{
				target = initialHSL.X + deltaHSL.X / 255f;
				range = target - initialHSL.X;
				step = range / steps;
				hsl.X = initialHSL.X + step * current;
			}
			if (deltaHSL.Y > 0f)
			{
				target = deltaHSL.Y;
				range = target - initialHSL.Y;
				step = range / steps;
				hsl.Y = initialHSL.Y + step * current;
			}
			if (deltaHSL.Z > 0f)
			{
				target = deltaHSL.Z;
				range = target - initialHSL.Z;
				step = range / steps;
				hsl.Z = initialHSL.Z + step * current;
			}
			
			color = ModEntry.ColorConverter.HSLtoRGB(hsl, color);
		}

		private static void SetEveningTenMinuteColor(ref Color color, Color initialColor, Color targetColor)
		{
			// Sample the timespan between evening and night into even increments between our initial and target colours
			var steps = (int)Math.Round((float)Game1.getTrulyDarkTime() - Game1.getStartingToGetDarkTime());
			// Current sample is a point at the difference between current time and evening time
			var current = Game1.timeOfDay - Game1.getStartingToGetDarkTime();
			// 24-hour time as an integer counts 10, 20, 30, 40, 50, 00 -- add the remainder of the difference for a smooth transition
			// Also squeeze that normal current sample time arithmetic between min and max values to stop us leaving our colour range
			current = Math.Max(0, Math.Min(steps, current + Game1.timeOfDay % 100 * 6 / 10));
			
			// Each colour channel is set to some point (current sample) along the range for that channel to fit the 24hr time of day
			float range;
			
			range = targetColor.R - initialColor.R;
			color.R = (byte)(int)Math.Round(initialColor.R + range / steps * current);

			range = targetColor.G - initialColor.G;
			color.G = (byte)(int)Math.Round(initialColor.G + range / steps * current);

			range = targetColor.B - initialColor.B;
			color.B = (byte)(int)Math.Round(initialColor.B + range / steps * current);

			if (false)
				Log.W($"RGB at {Game1.timeOfDay} - Steps: {steps} - Step: {range / steps} - Current: {current}"
				      + $"\nColour: {color.ToString()} - Target: {targetColor.ToString()}");
		}

		public override void snapToDefaultClickableComponent()
		{
			currentlySnappedComponent = getComponentWithID(0);
			snapCursorToCurrentSnappedComponent();
		}

		public override void performHoverAction(int x, int y)
		{
			if (_stack.Count < 1)
				return;
			
			var wasHovered = _hoveredButton > -1;
			_hoveredButton = _isNavigatingWithKeyboard ? _hoveredButton : -1;
			_hoverText = _isNavigatingWithKeyboard ? _hoverText : "";
			
			var state = _stack.Peek();
			if (ButtonDestRects[(int) state].Count > 0)
			{
				var emaOrigin = new Vector2(
					xPositionOnScreen - EmaFrameSourceRect.Width / 2 * Scale,
					yPositionOnScreen - EmaFrameSourceRect.Height / 2 * Scale);
				var buttons = ButtonDestRects[(int) state];
				var button = buttons.FindIndex(
					bounds => new Rectangle(
						(int)emaOrigin.X + bounds.X, (int)emaOrigin.Y + bounds.Y, bounds.Width, bounds.Height).Contains(x, y));
				if (button != -1)
				{
					_hoverText = state switch
					{
						State.Root => i18n.Get($"string.menu.bundles.{button}_inspect"),
						_ => _hoverText
					};
					_hoveredButton = button;
				}
			}

			if (!wasHovered && _hoveredButton != -1)
				Game1.playSound("Cowboy_gunshot");
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			if (_stack.Count < 1)
				return;

			base.receiveLeftClick(x, y, playSound);

			if (Game1.activeClickableMenu == null)
				return;

			_isNavigatingWithKeyboard = false;
			var emaOrigin = new Vector2(
				xPositionOnScreen - EmaFrameSourceRect.Width / 2 * Scale,
				yPositionOnScreen - EmaFrameSourceRect.Height / 2 * Scale);
			var state = _stack.Peek();
			var buttons = ButtonDestRects[(int) state];
			var button = buttons.FindIndex(
				bounds => new Rectangle(
					(int)emaOrigin.X + bounds.X, (int)emaOrigin.Y + bounds.Y, bounds.Width, bounds.Height).Contains(x, y));
			if (button != -1)
			{
				ClickButton(button, true);
			}
		}

		public override void receiveRightClick(int x, int y, bool playSound = true)
		{
			_isNavigatingWithKeyboard = false;
			PopMenuStack(playSound);
		}
		
		public override void receiveGamePadButton(Buttons b)
		{
			Log.D($"receiveGamePadButton: {b.ToString()}");

			// TODO: SYSTEM: Keep GamePadButtons inputs up-to-date with KeyPress and Click behaviours

			if (b == Buttons.RightTrigger)
				return;
			else if (b == Buttons.LeftTrigger)
				return;
			else if (b == Buttons.B)
				PopMenuStack(true);
		}

		public override void receiveKeyPress(Keys key)
		{
			//Log.D($"receiveKeyPress: {key.ToString()}");

			if (_stack.Count < 1)
				return;

			base.receiveKeyPress(key);
			
			var button = -1;
			var state = _stack.Peek();
			switch (state)
			{
				case State.Root:
				{
					// Navigate bundle buttons
					if (Game1.options.doesInputListContain(Game1.options.moveUpButton, key))
						button = _hoveredButton == 4 ? 2 : 0;
					else if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, key))
						button = _hoveredButton == 3 ? 2 : 1;
					else if (Game1.options.doesInputListContain(Game1.options.moveRightButton, key))
						button = _hoveredButton == 1 ? 2 : 3;
					else if (Game1.options.doesInputListContain(Game1.options.moveDownButton, key))
						button = _hoveredButton == 0 ? 2 : 4;

					_isNavigatingWithKeyboard = button != -1;
					if (_isNavigatingWithKeyboard)
					{
						_pulseTimer = 0;
						_whenToPulseTimer = TimeToPulse;
						var point = ButtonDestRects[(int) state][button].Center;
						Game1.setMousePosition(point.X - 31, point.Y + 31);
					}
					_hoveredButton = button;
					break;
				}

				case State.Bundle:
				{
					// Navigate left/right buttons
					if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, key))
						button = (int)(_currentBundle == 0 ? Bundles.Count - 1 : --_currentBundle);
					else if (Game1.options.doesInputListContain(Game1.options.moveRightButton, key))
						button = (int)(_currentBundle == Bundles.Count - 1 ? 0 : ++_currentBundle);
					_isNavigatingWithKeyboard = button != -1;
					if (_isNavigatingWithKeyboard)
					{
						_currentBundle = (Bundles) button;
					}

					break;
				}
			}

			if (Game1.options.doesInputListContain(Game1.options.useToolButton, key) 
			    || Game1.options.doesInputListContain(Game1.options.actionButton, key))
			{
				_isNavigatingWithKeyboard = true;
				ClickButton(button, true);
			}

			if (Game1.options.doesInputListContain(Game1.options.menuButton, key)
			    || Game1.options.doesInputListContain(Game1.options.journalButton, key))
			{
				PopMenuStack(true);
			}
		}
		
		public override void update(GameTime time)
		{
			if (_stack.Count < 1)
				return;

			const float animChance = 0.04f;
			const int animDuration = 3000;
			const int frameDuration = 200;

			var state = _stack.Peek();
			if (Game1.timeOfDay != _lastClockTime)
			{
				//Log.W($"Clock ticked to {Game1.timeOfDay}");
				_lastClockTime = Game1.timeOfDay;
				//SetEveningTenMinuteHue(ref _skyLowerColour, ref _initialSkyHSL, new Vector3(40, -1, 0.5f));
				//SetEveningTenMinuteHue(ref _skyHigherColour, ref _initialGradientHSL, new Vector3(20, -1, 0.5f));
				//Log.D($"{Game1.timeOfDay}:");
				SetEveningTenMinuteColor(ref _skyLowerColour, SkyLowerStartColor, SkyLowerEndColor);
				SetEveningTenMinuteColor(ref _skyHigherColour, SkyHigherStartColor, SkyHigherEndColor);
			}

			if (_hoveredButton == -1
				&& Game1.getOldMouseX() != Game1.getMouseX() || Game1.getOldMouseY() != Game1.getMouseY())
				_isNavigatingWithKeyboard = false;

			if (_animTimerForeground > 0)
			{
				const int frames = ForegroundAnimFrames - 1;
				_animTimerForeground -= time.ElapsedGameTime.Milliseconds;
				_animWhichFrame = frames - Math.Min(
					frames, Math.Abs(frames - (int)(
						(frames * 2 - 1) * (_animTimerForeground / (frameDuration * (frames * 2 - 1))))));
				//Log.W($"Piece: {_animWhichPieceToAnimate} - Frame: {_animWhichFrame} - Timer: {_animTimerForeground}");
			}
			else if (Game1.currentSeason != "winter" && Game1.random.NextDouble() < animChance)
			{
				_animWhichPieceToAnimate = Game1.random.Next(0, 4);
				_animTimerForeground = animDuration;
				//Log.W($"Animating {_animWhichPieceToAnimate} for {_animTimerForeground}");
			}

			// TODO: DEBUG: Remove this debug timer for ticking the clock in the menu
			/*
			if (_DEBUGtimer > 0)
				_DEBUGtimer -= time.ElapsedGameTime.Milliseconds;
			else
			{
				_DEBUGtimer = DebugClockTickSeconds * 1000f;
				Game1.performTenMinuteClockUpdate();
			}
			*/

			return;
		}

		public override void draw(SpriteBatch b)
		{
			if (_stack.Count < 1)
				return;

			const float blackoutOpacity = 0.4f;
			const float skyBlackoutMaxOpacity = 0.15f;

			var state = _stack.Peek();
			var view = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea();
			var source = EmaFrameSourceRect;
			var emaOrigin = new Vector2(
				xPositionOnScreen - source.Width / 2 * Scale,
				yPositionOnScreen - source.Height / 2 * Scale);
			
			// Icon pulse
			if (_pulseTimer > 0)
				_pulseTimer -= Game1.currentGameTime.ElapsedGameTime.Milliseconds;
			if (_whenToPulseTimer >= 0)
			{
				_whenToPulseTimer -= Game1.currentGameTime.ElapsedGameTime.Milliseconds;
				if (_whenToPulseTimer <= 0)
				{
					_whenToPulseTimer = TimeToPulse;
					_pulseTimer = PulseTime;
				}
			}
			var scalePulse = 1f / (Math.Max(300f, Math.Abs(_pulseTimer % 1000 - 500)) / 500f);
			
			// Background - Stardew panorama
			source = StardewPanoramaSourceRect;
			if (false)
			b.Draw(
				Game1.mouseCursors,
				new Rectangle(
					xPositionOnScreen - source.Width / 2 * Scale,
					yPositionOnScreen - source.Height / 2 * Scale,
					source.Width * Scale,
					source.Height * Scale),
				source,
				Color.White,
				0f,
				Vector2.Zero,
				SpriteEffects.None,
				0.99f);

			// Background blackout
			b.Draw(
				Game1.fadeToBlackRect,
				view,
				Color.LightGray * 0.5f);

			// Sky colour fill
			if (false)
			b.Draw(
				Game1.fadeToBlackRect,
				view,
				_skyLowerColour);

			// Sky colour gradient overlay
			if (false)
			source = GradientSourceRect;
			if (false)
			b.Draw(
				_texture,
				view,
				source,
				_skyHigherColour);
			
			// Sky night blackout
			if (state == State.Root)
				b.Draw(
					Game1.fadeToBlackRect,
					view,
					Color.Black * skyBlackoutMaxOpacity * ModEntry.GetProgressFromEveningIntoNighttime());

			// Midground - Ema frame
			source = EmaFrameSourceRect;
			b.Draw(
				_texture,
				new Rectangle(
					(int) emaOrigin.X,
					(int) emaOrigin.Y,
					source.Width * Scale,
					source.Height * Scale),
				source,
				Color.White,
				0f,
				Vector2.Zero,
				SpriteEffects.None,
				0.99f);
			
			const int frames = 5;
			const int timescale = 100;
			_animTimerInterface += Game1.currentGameTime.ElapsedGameTime.Milliseconds;
			_animTimerInterface %= frames * timescale;

			source = QuestionIconSourceRect;
			for (var i = 0; i < Data.BundlesThisSeason.Count; ++i)
			{
				var completed
					= Data.BundlesThisSeason[i].items.All(kv
						=> kv.Key is Object a && kv.Value is Object b && a.Stack == b.Stack);

				// Draw question mark over unfinished bundles
				//if (!completed && state == State.Root)
				if (i == 3) // TODO: DEBUG: Change completed conditions back to usual
				{
					var button = ButtonDestRects[(int) State.Root][i];
					var frame = _animTimerInterface / timescale;
					b.Draw(
						Game1.mouseCursors,
						emaOrigin + new Vector2(
							button.Center.X - source.Width / 2 - 8,
							button.Center.Y - source.Height / 3 * 5),
						new Rectangle(
							source.X + source.Width * (int) frame,
							source.Y,
							source.Width,
							source.Height),
						Color.White,
						0f,
						Vector2.Zero,
						Scale,
						SpriteEffects.None,
						0.99f + 1f / 10000f);
					continue;
				}
				
				// Draw art over finished bundles
				continue;
				source = BundleIconSourceRect;
				source.Y *= i;
				b.Draw(
					_texture,
					ButtonDestRects[(int) State.Root][i],
					source,
					Color.White,
					0f,
					Vector2.Zero,
					SpriteEffects.None,
					0.99f + 1f / 10000f);
			}

			// Menu elements
			switch (state)
			{
				case State.Root:
				{
					if (_hoveredButton > -1 && _isNavigatingWithKeyboard)
					{
						// Draw pointer at hovered Ema buttons
						var button = ButtonDestRects[(int) State.Root][_hoveredButton];
						b.Draw(
							Game1.mouseCursors,
							new Vector2(
								button.X + button.Width,
								button.Y + button.Height / 2),
							new Rectangle(0, 16, 16, 16),
							Color.White,
							0f,
							Vector2.Zero,
							Scale * scalePulse,
							SpriteEffects.None,
							1f);
					}
					
					break;
				}

				case State.Bundle:
				{
					const int maxOffset = 16;
					const int aBigNumber = 357; // high calibre!
					var yOffset = ((int)_currentBundle * aBigNumber / maxOffset) % maxOffset * Scale;

					// Screen blackout over the Ema
					b.Draw(
						Game1.fadeToBlackRect,
						Game1.graphics.GraphicsDevice.Viewport.Bounds,
						Color.Black * blackoutOpacity);

					// Bundle menu backdrop
					source = EmaFudaSourceRect;
					b.Draw(
						_texture,
						new Rectangle(
							xPositionOnScreen - source.Width * Scale / 2,
							yPositionOnScreen - source.Height * Scale / 2 + yOffset,
							source.Width * Scale,
							source.Height * Scale),
						source,
						Color.White,
						0f,
						Vector2.Zero,
						_currentBundle == Bundles.Story || _currentBundle == Bundles.Produce
							? SpriteEffects.FlipHorizontally
							: SpriteEffects.None,
						0.99f + 1f / 10000f);
					
					var completed
						= Data.BundlesThisSeason[(int)_currentBundle].items.All(kv
							=> kv.Key is Object a && kv.Value is Object b && a.Stack == b.Stack);

					// Return to Root button
					source = CloseIconSourceRect;
					b.Draw(
						Game1.mouseCursors,
						ButtonDestRects[(int) State.Bundle][0], 
						source,
						Color.White,
						0f,
						Vector2.Zero,
						SpriteEffects.None,
						1f);

					// Left/right navigation buttons
					source = ArrowIconSourceRect;
					for (var i = 1; i < 3; ++i)
					{
						source.Y = ArrowIconSourceRect.Y + ArrowIconSourceRect.Height * (i % 2);
						b.Draw(
							Game1.mouseCursors,
							ButtonDestRects[(int) State.Bundle][i], 
							source,
							Color.White,
							0f,
							Vector2.Zero,
							SpriteEffects.None, // Sprites are in the sheet as right, then left
							1f);
					}
					
					// Item drop-in slots
					source = ItemSlotIconSourceRect;
					for (var i = 3; i < ButtonDestRects[(int) State.Bundle].Count; ++i)
					{
						var dest = ButtonDestRects[(int) State.Bundle][i];
						b.Draw(
							_texture,
							new Rectangle(
								dest.X,
								dest.Y + yOffset,
								dest.Width,
								dest.Height), 
							source,
							Color.White,
							0f,
							Vector2.Zero,
							SpriteEffects.None,
							0.99f + 1f / 10000f + 1f / 10000f);
					}

					break;
				}
			}

			// TODO: SYSTEM: Animate foreground foliage in EmaMenu, excluding (season == 3) variant
			// Foreground - Foliage
			for (var i = 0; i < 4; ++i)
			{
				source = FoliageSourceRects[i];
				if (i != _animWhichPieceToAnimate) {}
				else
					source.X += _animWhichFrame * FoliageSourceRects[2].Width;
				b.Draw(
					_texture,
					new Rectangle(
						i % 2 == 0 ? 0 : view.Width - source.Width * Scale,
						i < 2 ? 0 : view.Height - source.Height * Scale,
						source.Width * Scale,
						source.Height * Scale),
					source,
					Color.White,
					0f,
					Vector2.Zero,
					SpriteEffects.None,
					0.99f + 1f / 10000f + 1f / 10000f + 1f / 10000f);
			}
			
			// Screen blackout for nighttime
			if (true)
				b.Draw(
					Game1.fadeToBlackRect,
					view,
					SkyDarkColor * ModEntry.GetProgressFromEveningIntoNighttime() * skyBlackoutMaxOpacity);

			// Debug text
			if (false)
				SpriteText.drawStringHorizontallyCenteredAt(b,
					state.ToString(),
					view.Center.X,
					view.Center.Y + view.Height / 4);

			// Upper right close button
			base.draw(b);

			// Hover text
			if (_hoverText.Length > 0)
				drawHoverText(b, _hoverText, Game1.dialogueFont);

			// Cursor
			Game1.mouseCursorTransparency = !_isNavigatingWithKeyboard ? 1f : 0f;
			drawMouse(b);
		}
	}
}
