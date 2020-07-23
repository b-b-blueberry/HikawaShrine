using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using xTile.Dimensions;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Hikawa.GameObjects.Menus
{
	public class EmaMenu : IClickableMenu
	{
		public static readonly List<object> BundleCharacters = new List<object>
		{
			ModConsts.ReiNpcId,
			ModConsts.GrampsNpcId,
			ModConsts.AmiNpcId,
			ModConsts.YuuichiroNpcId,
		};

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
		
		// Source rects for various menu elements
		// Hikawa Bundles texture file:
		private readonly Rectangle EmaFrameSourceRect;
		private readonly Rectangle ItemSlotIconSourceRect;
		private readonly Rectangle EmaFudaSourceRect;
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

		// Lambda
		private IModHelper Helper => ModEntry.Instance.Helper;
		private ITranslationHelper i18n => ModEntry.Instance.Helper.Translation;
		private static ModData Data => ModEntry.Instance.SaveData;

		// Variable
		private string _hoverText;
		private int _hoveredButton; // Index of current hovered button in the ButtonDestRects list
		private Bundles _currentBundle; // Index of which bundle button was clicked in Root menu
		private bool _isNavigatingWithKeyboard;
		private float _animTimerBackground;
		private float _animTimerForeground;
		private float _animTimerInterface;
		private int _animWhichPieceToAnimate;
		private int _animWhichFrame;
		private int _pulseTimer;
		private int _whenToPulseTimer;

		private const int TimeToPulse = 2500;
		private const int PulseTime = 1000;
		private const int Scale = 4;

		private const int ForegroundAnimFrames = 3;

		public EmaMenu() : this(State.Opening, true) {}

		public EmaMenu(State state, bool addRootToStack)
		{
			if (addRootToStack)
				_stack.Push(State.Root);
			_stack.Push(state);
			
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
			var season = Game1.currentSeason switch
			{
				"spring" => 0,
				"summer" => 1,
				"autumn" => 2,
				_ => 3
			};

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
			ItemSlotIconSourceRect = new Rectangle(
				EmaFudaSourceRect.Width, EmaFudaSourceRect.Y + EmaFudaSourceRect.Height - 18, 18, 18);
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
		}

		internal static void LoadBundlesState()
		{
			Log.W("LoadBundlesState");

			// Reload missing data
			if (Data.BundlesThisSeason != null)
				return;

			Log.W("Null bundle data");
			Data.BundlesThisSeason = new List<Dictionary<List<object>, List<bool>>>();

			// Story bundle
			var bundle = new List<object>();
			for (var i = ModData.Chapter.None; i < ModData.Chapter.End; ++i)
				bundle.Add(i);
			Data.BundlesThisSeason.Add(new Dictionary<List<object>, List<bool>>
				{{ bundle, new List<bool>() }});

			// Artefacts bundle
			Data.BundlesThisSeason.Add(new Dictionary<List<object>, List<bool>>
				{{ new List<object>(), new List<bool>() }});

			// Produce bundle
			Data.BundlesThisSeason.Add(new Dictionary<List<object>, List<bool>>
				{{ new List<object>(), new List<bool>() }});

			// ??? bundle
			Data.BundlesThisSeason.Add(new Dictionary<List<object>, List<bool>>
				{{ new List<object>(), new List<bool>() }});

			// Friendship bundle
			Data.BundlesThisSeason.Add(new Dictionary<List<object>, List<bool>>
				{{ BundleCharacters, new List<bool>() }});

			ResetBundlesForNewSeason();
		}

		internal static void ResetBundlesForNewSeason()
		{
			Log.W("ResetBundlesForNewSeason");

			// TODO: CONTENT: Write up data for bundles per season

		}

		private void PopMenuStack(bool playSound)
		{
			if (_stack.Count < 1)
				return;

			Log.W($"Popping {_stack.Peek()}");
			_stack.Pop();

			if (playSound)
				Game1.playSound("bigDeSelect");

			if (!readyToClose() || _stack.Count > 0)
				return;
			Game1.exitActiveMenu();
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
			var count = Data.BundlesThisSeason[(int)_currentBundle].Count;
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
			Log.D($"receiveKeyPress: {key.ToString()}");

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

			const float animChance = 0.015f;
			const int animDuration = 3000;
			const int frameDuration = 200;

			var state = _stack.Peek();

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
			}

			return;

			if (_animTimerBackground > 0)
				_animTimerBackground -= time.ElapsedGameTime.Milliseconds;

			switch (state)
			{
				case State.Root:
					if (_animTimerBackground <= 0 && Game1.random.NextDouble() > animChance)
					{
						_animTimerBackground = animDuration;
					}
					break;
			}
		}

		public override void draw(SpriteBatch b)
		{
			if (_stack.Count < 1)
				return;

			const float blackoutOpacity = 0.75f;

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
			
			// Screen blackout
			if (state == State.Root)
				b.Draw(
					Game1.fadeToBlackRect,
					Game1.graphics.GraphicsDevice.Viewport.Bounds,
					Color.Black * blackoutOpacity);

			// Menu backgrounds:
			// TODO: CONTENT: Draw Ema menu background - Shrine

			// Background - Stardew panorama
			source = StardewPanoramaSourceRect;
			b.Draw(
				_texture,
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
					= Data.BundlesThisSeason[i].All(kv
						=> kv.Value.TrueForAll(v => v));

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
						= Data.BundlesThisSeason[(int)_currentBundle].All(kv
							=> kv.Value.TrueForAll(v => v));
					
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
