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

namespace Hikawa.GameObjects.Menus
{
	public class EmaMenu : IClickableMenu
	{
		public List<object> BundleCharacters = new List<object>
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

		public enum RootButtons
		{
			Story, // Top
			Artefacts, // Left
			Food, // Middle
			Other, // Right
			Friendship // Bottom
		}

		private readonly Rectangle MenuMidgroundSourceRect = new Rectangle(166, 315, 320, 180);
		private readonly Rectangle BundleIconSourceRect = new Rectangle(0, 0, 0, 0);
		// TODO: CONTENT: Fill in bundle source rects when finished

		private static readonly List<List<Rectangle>> MenuButtons = new List<List<Rectangle>> {
			new List<Rectangle>(), // Closed
			new List<Rectangle> // Root menu
			{
				new Rectangle(138, 43, 44, 32), // Top (Story)
				new Rectangle(88, 92, 44, 32), // Left (Artefacts)
				new Rectangle(142, 99, 38, 32), // Middle (Food)
				new Rectangle(190, 100, 44, 32), // Right (???)
				new Rectangle(118, 146, 48, 32), // Bottom (Friendship)
			},
		};

		private readonly Stack<State> _stack = new Stack<State>();
		private readonly Texture2D _texture; // Texture for all custom menu elements
		private readonly int _season; // Decides sprite variants via X offset in texture source
		private readonly bool _misty; // Ditto

		// Lambda
		private IModHelper Helper => ModEntry.Instance.Helper;
		private ITranslationHelper i18n => ModEntry.Instance.Helper.Translation;
		private ModData Data => ModEntry.Instance.SaveData;

		// Variable
		private string _hoverText;
		private int _hoveredButton; // Index of current hovered button in the MenuButtons list
		private int _lastActiveButton; // Index of which button was clicked in MenuButtons
		private bool _isNavigatingWithKeyboard;
		private float _animationTimer;
		private int _pulseTimer;
		private int _whenToPulseTimer;

		private const int timeToPulse = 2500;
		private const int pulseTime = 1000;

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

			// Choose sprite variants
			var currentStory = ModEntry.GetCurrentStory();
			_misty = currentStory.Key == ModData.Chapter.Mist && currentStory.Value == ModData.Progress.Started;
			_season = Game1.currentSeason switch
			{
				"spring" => 1,
				"summer" => 2,
				"autumn" => 3,
				_ => 4
			};
		}
		
		internal void LoadBundlesState()
		{
			// Reload missing data
			if (Data.BundlesThisSeason != null)
				return;

			Log.W("Null Bundles data, resetting");
			Data.BundlesThisSeason = new List<Dictionary<List<object>, List<bool>>>();

			// TODO: CONTENT: Write up data for bundles per season

			// Story bundle
			var bundle = new List<object>();
			for (var i = ModData.Chapter.None; i < ModData.Chapter.End; ++i)
				bundle.Add(i);
			Data.BundlesThisSeason.Add(new Dictionary<List<object>, List<bool>>
			{
				{ bundle, new List<bool>() }
			});

			// Artefacts bundle
			bundle.Clear();

			// Food bundle
			bundle.Clear();

			// ??? bundle
			bundle.Clear();

			// Friendship bundle
			Data.BundlesThisSeason.Add(new Dictionary<List<object>, List<bool>>
			{
				{ BundleCharacters, new List<bool>() }
			});
		}

		private void PopMenuStack(bool playSound)
		{
			if (_stack.Count < 1)
				return;

			Log.W($"Popping {_stack.Peek()}");

			_stack.Pop();
			if (!readyToClose() || _stack.Count > 0)
				return;

			Game1.exitActiveMenu();
			if (playSound)
				Game1.playSound("bigDeSelect");
		}

		private void ClickButton(bool playSound)
		{
			if (_stack.Count < 1)
				return;
			
			if (playSound)
				Game1.playSound("bigSelect");

			_lastActiveButton = _hoveredButton;

			// TODO: SYSTEM: Fill in Ema ClickButton

			var state = _stack.Peek();
			switch (state) {
				case State.Root:
					_stack.Push(State.Bundle);
					break;

				case State.Bundle:
					break;

				case State.DropIn:
					break;
			}
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
			_hoverText = "";
			
			var state = _stack.Peek();
			if (MenuButtons[(int) state].Count > 0)
			{
				var buttons = MenuButtons[(int) state];
				var button = buttons.FindIndex(bounds => bounds.Contains(x, y));
				if (button != -1)
				{
					_hoverText = state switch
					{
						State.Root => i18n.Get($"string.menu.bundles.{_hoveredButton}_inspect"),
						_ => _hoverText
					};
					_hoveredButton = button;
				}
			}

			if (wasHovered && _hoveredButton == -1)
				Game1.playSound("dwop");
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			if (_stack.Count < 1)
				return;

			base.receiveLeftClick(x, y, playSound);

			if (Game1.activeClickableMenu == null)
				return;

			_isNavigatingWithKeyboard = false;
			var state = _stack.Peek();
			var buttons = MenuButtons[(int) state];
			var button = buttons.FindIndex(bounds => bounds.Contains(x, y));
			if (button != -1)
			{
				ClickButton(true);
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
			
			var state = _stack.Peek();

			switch (state)
			{
				case State.Root:
					// Navigate bundle buttons
					var button = -1;
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
						_whenToPulseTimer = timeToPulse;
					}
					_hoveredButton = button;

					if (Game1.options.doesInputListContain(Game1.options.useToolButton, key) 
					    || Game1.options.doesInputListContain(Game1.options.actionButton, key))
					{
						_isNavigatingWithKeyboard = true;
						ClickButton(true);
					}
					break;
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

			if (Game1.getOldMouseX() != Game1.getMouseX() || Game1.getOldMouseY() != Game1.getMouseY())
				_isNavigatingWithKeyboard = false;

			if (_animationTimer > 0)
				_animationTimer -= time.ElapsedGameTime.Milliseconds;

			var state = _stack.Peek();
			switch (state)
			{
				case State.Root:
					if (_animationTimer <= 0 && Game1.random.NextDouble() > animChance)
					{
						_animationTimer = animDuration;
					}
					break;
			}
		}

		public override void draw(SpriteBatch b)
		{
			if (_stack.Count < 1)
				return;

			const int scale = 4;
			const float blackoutOpacity = 0.75f;

			var state = _stack.Peek();
			var view = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea();
			
			// Icon pulse
			if (_pulseTimer > 0)
				_pulseTimer -= Game1.currentGameTime.ElapsedGameTime.Milliseconds;
			if (_whenToPulseTimer >= 0)
			{
				_whenToPulseTimer -= Game1.currentGameTime.ElapsedGameTime.Milliseconds;
				if (_whenToPulseTimer <= 0)
				{
					_whenToPulseTimer = timeToPulse;
					_pulseTimer = pulseTime;
				}
			}
			var scalePulse = 1f / (Math.Max(300f, Math.Abs(_pulseTimer % 1000 - 500)) / 500f);

			// Screen blackout
			b.Draw(
				Game1.fadeToBlackRect,
				Game1.graphics.GraphicsDevice.Viewport.Bounds,
				Color.Black * blackoutOpacity);

			// Menu backgrounds:
			// TODO: CONTENT: Draw Ema menu background - Shrine

			// Midground - Ema frame
			var source = MenuMidgroundSourceRect;
			//source.X *= _season; // TODO: CONTENT: Re-enable Ema menu spritesheet variants when they're ready
			b.Draw(
				_texture,
				new Rectangle(
					xPositionOnScreen - source.Width / 2 * scale,
					yPositionOnScreen - source.Height / 2 * scale,
					source.Width * scale,
					source.Height * scale),
				source,
				Color.White,
				0f,
				Vector2.Zero,
				SpriteEffects.None,
				0.99f);

			// TODO: CONTENT: Draw Ema menu foreground - Plants
			
			// Menu elements
			switch (state)
			{
				case State.Root:
					if (_hoveredButton > -1 && _isNavigatingWithKeyboard)
					{
						// Draw pointer at hovered Ema buttons
						var button = MenuButtons[(int) state][_hoveredButton];
						b.Draw(
							Game1.mouseCursors,
							new Vector2(
								(button.X + button.Width) * scale,
								(button.Y + button.Height / 2) * scale),
							new Rectangle(0, 16, 16, 16),
							Color.White,
							0f,
							Vector2.Zero,
							scale * scalePulse,
							SpriteEffects.None,
							1f);
					}

					for (var i = 0; i < Data.BundlesThisSeason.Count; ++i)
					{
						var completed
							= Data.BundlesThisSeason[i].All(kv
								=> kv.Value.TrueForAll(v => v));

						// Draw question mark over unfinished bundles
						if (!completed)
						{
							const int x = 330;
							const int y = 358;
							const int w = 7;
							const int h = 13;
							const int frames = 6;
							
							var button = MenuButtons[(int) state][i];
							b.Draw(
								Game1.mouseCursors,
								new Vector2(
									button.Center.X - w / 2,
									button.Center.Y - h / 2),
								new Rectangle((int)(x + w * _animationTimer % (frames * 300)), y, w, h),
								Color.White,
								0f,
								Vector2.Zero,
								scale,
								SpriteEffects.None,
								0.99f + 1f / 10000f);
							continue;
						}
						
						// Draw art over finished bundles
						source = BundleIconSourceRect;
						source.X *= _season;
						source.Y *= i;
						b.Draw(
							_texture,
							MenuButtons[(int) state][i],
							source,
							Color.White,
							0f,
							Vector2.Zero,
							SpriteEffects.None,
							0.99f + 1f / 10000f);
					}

					break;

				case State.Bundle:

					break;
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
