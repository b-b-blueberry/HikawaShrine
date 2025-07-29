using Hikawa.Data;
using Hikawa.Objects.Critters;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hikawa.Objects.Menus
{
	public class BugCollectionMenu : IClickableMenu
	{
		public readonly int Scale = 4;
		public Point BorderSize => new(4, 4);
		public Point ClickableAreaSize => new(96, 48);
		public Point ClickableAreaOffset => new(-0, -2);
		public Rectangle SceneBackgroundSource => new(0, 464, 192, 128);
		public Rectangle CardAreaSource => new(192, 464, 112, 160);

		public Dictionary<string, ClickableTextureComponent> BugClickables;
		public HashSet<string> BugCollection;
		public ShrineBug BugPreview;

		private Rectangle _sceneArea;
		private Rectangle _displayArea;
		private Rectangle _clickableArea;
		private Rectangle _cardArea;
		private Rectangle _textArea;

		public bool IsCloseButtonAllowed => !Game1.options.SnappyMenus;

		public string BugClicked
		{
			get => this._bugClicked;
			set
			{
				this._bugClicked = value;
				if (value is not null)
					this.BugPreview?.Init(
						bugId: value,
						definition: ModEntry.BugsData.Value.BugData.TryGetValue(value, out var definition)
							? definition
							: ModEntry.BugsData.Value.BugData.Values.FirstOrDefault());
			}
		}

		private string _bugClicked;

		public BugCollectionMenu()
			: base()
		{
			// FX
			Game1.playSound("openBox");

			// Components
			this.BugPreview = new();
			this.BugCollection = ModEntry.BugsData.Value.BugData.Keys
				.Where(ModEntry.SaveData.BugCollection.ContainsKey).ToHashSet();
			this.BugClicked = this.BugCollection
				.FirstOrDefault();
			this.BugClickables = ModEntry.BugsData.Value.BugData
				.ToDictionary(
					pair => pair.Key,
					pair => new ClickableTextureComponent(
						name: pair.Key,
						bounds: new(
							x: 0,
							y: 0,
							width: pair.Value.MenuTextureRegion.Width * this.Scale,
							height: pair.Value.MenuTextureRegion.Height * this.Scale),
						texture: Game1.temporaryContent.Load<Texture2D>(pair.Value.TextureId),
						sourceRect: pair.Value.MenuTextureRegion,
						scale: this.Scale,
						label: null,
						hoverText: null,
						drawShadow: false)
					{
						visible = this.BugCollection.Contains(pair.Key)
					});
			
			this.initializeUpperRightCloseButton();
			this.gameWindowSizeChanged(oldBounds: Rectangle.Empty, newBounds: Game1.graphics.GraphicsDevice.Viewport.Bounds);
			this.populateClickableComponentList();
			this.UpdateComponentNavigation();
		}

		public void UpdateComponentNavigation()
		{
			List<ClickableComponent> components = [..this.BugClickables.Values];
			for (int i = 0; i < components.Count; i++)
			{
				ClickableComponent c = components[i];
				c.upNeighborID = c.downNeighborID = c.rightNeighborID = c.leftNeighborID = -99998;
				c.myID = 1000 + i;
			}
			/*
			foreach (ClickableComponent itemT in inventory.GetBorder(InventoryMenu.BorderSide.Top))
			{
				itemT.upNeighborID = -99998;
			}
			foreach (ClickableComponent itemR in inventory.GetBorder(InventoryMenu.BorderSide.Right))
			{
				itemR.rightNeighborID = 4857;
				itemR.rightNeighborImmutable = true;
			}
			*/
			this.populateClickableComponentList();
			if (Game1.options.SnappyMenus)
				this.snapToDefaultClickableComponent();
		}

		public void UpdateComponentLayout(Rectangle bounds)
		{
			Point centre = bounds.Center;
			if (Context.IsSplitScreen)
			{
				// Centre the menu in splitscreen
				centre.X = centre.X / 3 * 2;
			}

			Point offset = new(0, 0);
			Point cardOffset = new(-8, -4);

			// Menu visible area
			Point displaySize = new(
				x: (this.SceneBackgroundSource.Width + this.BorderSize.X * 2 + cardOffset.X + this.CardAreaSource.Width) * this.Scale,
				y: (this.SceneBackgroundSource.Height + this.BorderSize.Y * 2) * this.Scale
			);
			this._displayArea = new(
				x: centre.X - displaySize.X / 2 + offset.X * this.Scale,
				y: centre.Y - displaySize.Y / 2 + offset.Y * this.Scale,
				width: displaySize.X,
				height: displaySize.Y
			);

			// Info area
			this._sceneArea = new(
				x: this._displayArea.Center.X - (this.SceneBackgroundSource.Width + cardOffset.X + this.CardAreaSource.Width) / 2 * this.Scale,
				y: this._displayArea.Top + (this.BorderSize.Y * this.Scale),
				width: this.SceneBackgroundSource.Width * this.Scale,
				height: this.SceneBackgroundSource.Height * this.Scale
			);

			// Text area
			Point textOffset = new(-2, 22);
			Point textMargin = new(12, 0);
			Point textSize = new(118, 144);

			this._cardArea = new(
				x: this._sceneArea.Right + cardOffset.X * this.Scale,
				y: this._displayArea.Center.Y - this.CardAreaSource.Height / 2 * this.Scale,
				width: this.CardAreaSource.Width * this.Scale,
				height: this.CardAreaSource.Height * this.Scale
			);
			this._textArea = new(
				x: this._cardArea.Left + (textOffset.X + textMargin.X) * this.Scale,
				y: this._cardArea.Top + (textOffset.Y + textMargin.Y) * this.Scale,
				width: (textSize.X - textOffset.X - textMargin.X * 2) * this.Scale,
				height: (textSize.Y - textOffset.Y - textMargin.Y * 2) * this.Scale
			);

			// Clickables
			this._clickableArea = new(
				x: this._sceneArea.Center.X - (this.ClickableAreaSize.X / 2 + this.ClickableAreaOffset.X) * this.Scale,
				y: this._sceneArea.Center.Y - (this.ClickableAreaSize.Y / 2 + this.ClickableAreaOffset.Y) * this.Scale,
				width: this.ClickableAreaSize.X * this.Scale,
				height: this.ClickableAreaSize.Y * this.Scale
			);
			if (this.BugClickables?.Any() == true)
			{
				var items = this.BugClickables.Values;
				Rectangle r = this._clickableArea;
				int w = items.Count / 3;
				int h = items.Count / w;
				int i = 0;
				foreach (var c in items)
				{
					int row = i / w;
					int col = i % w;
					c.bounds.X = r.X + r.Width / (w - 1) * col - c.bounds.Width / 2;
					c.bounds.Y = r.Y + r.Height / (h - 1) * row - c.bounds.Height / 2;
					++i;
				}
			}

			// Close button
			this.upperRightCloseButton.setPosition(
				x: this._displayArea.Right + (this.BorderSize.X - 16) * this.Scale,
				y: this._displayArea.Top + (this.BorderSize.Y + 6) * this.Scale
			);
		}

		protected override void cleanupBeforeExit()
		{
			Game1.playSound("doorCreakReverse");

			base.cleanupBeforeExit();
		}

		public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
		{
			newBounds.Offset(offsetX: -newBounds.X, offsetY: -newBounds.Y);

			this.xPositionOnScreen = newBounds.X;
			this.yPositionOnScreen = newBounds.Y;

			base.gameWindowSizeChanged(oldBounds: oldBounds, newBounds: newBounds);

			this.UpdateComponentLayout(bounds: newBounds);
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			foreach ((string bugId, ClickableTextureComponent c) in this.BugClickables)
			{
				if (c.containsPoint(x, y))
				{
					this.BugClicked = bugId;
					Game1.playSound("newRecipe");
				}
			}

			if (this.IsCloseButtonAllowed && this.upperRightCloseButton.containsPoint(x, y))
				this.exitThisMenu();
		}

		public override void performHoverAction(int x, int y)
		{
			if (this.IsCloseButtonAllowed)
				this.upperRightCloseButton.tryHover(x, y, maxScaleIncrease: 0.5f);
			
			foreach ((string bugId, ClickableTextureComponent c) in this.BugClickables)
			{
				bool isClicked = bugId == this.BugClicked;
				c.tryHover(
					x: isClicked ? c.bounds.X : x,
					y: isClicked ? c.bounds.Y : y,
					maxScaleIncrease: isClicked ? 1f : 0.5f);
			}
		}

		public override void update(GameTime time)
		{
			this.BugPreview?.update(time, Game1.currentLocation);

			base.update(time);
		}

		public override void draw(SpriteBatch b)
		{
			Rectangle screen = Game1.graphics.GraphicsDevice.Viewport.Bounds;
			screen.Offset(
				offsetX: -screen.X,
				offsetY: -screen.Y);

			Color blackoutColor = new Color(25, 0, 10);

			// Blackout
			b.Draw(
				texture: Game1.fadeToBlackRect,
				destinationRectangle: screen,
				color: blackoutColor * 0.5f);

			// Scene
			b.Draw(
				texture: ModEntry.Sprites,
				sourceRectangle: this.SceneBackgroundSource,
				destinationRectangle: this._sceneArea,
				color: Color.White);

			// Text
			{
				SpriteFont mainFont = LocalizedContentManager.CurrentLanguageLatin ? ModEntry.Handwriting.Value : Game1.smallFont;
				float mainFontScale = LocalizedContentManager.CurrentLanguageLatin ? 0.95f : 1f;
				SpriteFont font;
				string text;
				float textScale;
				float textRotation;
				Vector2 textSize;
				Vector2 textPosition;
				Color textColor = Game1.textColor;
				Color dividerColor = new Color(189, 69, 19) * 0.3f;
				Color dotColor = new Color(189, 69, 19) * 0.1f;
				Color shadowColor = Color.SaddleBrown * 0.333f;

				// Card
				/*
				b.Draw(
					texture: Game1.fadeToBlackRect,
					destinationRectangle: this._cardArea,
					color: Color.Wheat);
				*/
				b.Draw(
					texture: ModEntry.Sprites,
					sourceRectangle: this.CardAreaSource,
					destinationRectangle: this._cardArea,
					color: Color.White);
				
				if (this.BugClicked is string bugId && ModEntry.BugsData.Value.BugData.TryGetValue(bugId, out BugData bug))
				{
					// Name
					font = mainFont;
					text = Game1.parseText(bug.DisplayName, font, this._textArea.Width);
					textPosition = new(x: this._textArea.X, y: this._textArea.Y);
					textSize = font.MeasureString(text);
					textScale = mainFontScale;
					textRotation = MathF.PI * 0f;
					b.DrawString(
						spriteFont: font,
						text: text,
						position: textPosition,
						color: textColor,
						rotation: textRotation,
						origin: Vector2.Zero,
						scale: textScale,
						effects: SpriteEffects.None,
						layerDepth: 1f);

					// Bug
					this.BugPreview?.DrawAt(b, new Vector2(this._textArea.Right, this._textArea.Top)
						+ new Vector2(-20, -8) * Game1.pixelZoom);

					// divider
					int lineY = (int)(textPosition.Y + textSize.Y * textScale);
					Utility.drawLineWithScreenCoordinates(
						x1: this._textArea.Left - 2 * Game1.pixelZoom,
						y1: lineY,
						x2: this._textArea.Right,
						y2: lineY,
						b: b,
						color1: dividerColor,
						thickness: 1);

					// Scientific name
					bool isItalics = true;
					font = isItalics ? ModEntry.Italics.Value : Game1.smallFont;
					text = Game1.parseText(bug.ScientificName, font, this._textArea.Width);
					textPosition.Y += textSize.Y * textScale;
					textSize = font.MeasureString(text);
					textScale = isItalics ? 2f : 1f;
					b.DrawString(
						spriteFont: font,
						text: text,
						position: textPosition + (isItalics ? new Vector2(-2, -2) : new Vector2(0, 2) * Game1.pixelZoom),
						color: textColor,
						rotation: textRotation,
						origin: Vector2.Zero,
						scale: textScale,
						effects: SpriteEffects.None,
						layerDepth: 1f);

					textPosition.Y += 0 * Game1.pixelZoom;

					// Description
					font = mainFont;
					text = Game1.parseText(bug.Description, font, this._textArea.Width);
					textPosition.Y += font.LineSpacing * 2;
					textSize = font.MeasureString(text);
					textScale = mainFontScale;
					b.DrawString(
						spriteFont: font,
						text: text,
						position: textPosition,
						color: textColor,
						rotation: textRotation,
						origin: Vector2.Zero,
						scale: textScale,
						effects: SpriteEffects.None,
						layerDepth: 1f);

					// Page markings
					int lineOffset = -4 * Game1.pixelZoom;
					int lineMargin = 2 * Game1.pixelZoom;
					int lineDiff = 2 * Game1.pixelZoom;
					font = mainFont;
					textScale = mainFontScale * 1f;
					textRotation = MathF.PI * 0f;
					Vector2 linePosition = /*new Vector2(x: this._textArea.X, y: this._textArea.Y)
						+ new Vector2(-2, 5) * Game1.pixelZoom;*/
						textPosition;
					/*
					StringBuilder sb = new();
					for (int j = 0; j < 8; ++j)
						sb.Append(".".PadRight(4));
					*/
					for (int i = 0; i < 12; ++i)
					{
						linePosition.Y += font.LineSpacing * mainFontScale;
						
						Utility.drawLineWithScreenCoordinates(
							x1: (int)(lineOffset + this._textArea.Left + lineMargin),
							y1: (int)linePosition.Y,
							x2: (int)(lineOffset + this._textArea.Right - lineMargin + lineDiff),
							y2: (int)linePosition.Y,
							b: b,
							color1: dotColor,
							thickness: 1);
						
						/*
						b.DrawString(
							spriteFont: font,
							text: sb.ToString(),
							position: textPosition,
							color: dotColor,
							rotation: textRotation,
							origin: Vector2.Zero,
							scale: textScale,
							effects: SpriteEffects.None,
							layerDepth: 1f);
						*/
					}

					// Datemark
					if (ModEntry.SaveData.BugCollection?.TryGetValue(bugId, out int daysPlayed) == true)
					{
						WorldDate date = WorldDate.ForDaysPlayed(daysPlayed);
						font = mainFont;
						text = Utility.getDateStringFor(date.DayOfMonth, date.SeasonIndex, date.Year);
						text = SDate.From(date).ToLocaleString();
						textSize = font.MeasureString(text);
						textPosition.X = this._textArea.Right - textSize.X * textScale - 4 * Game1.pixelZoom;
						textPosition.Y = this._textArea.Bottom - textSize.Y * textScale - 1 * Game1.pixelZoom;
						textScale = mainFontScale;
						textRotation = MathF.PI * 0.015f;
						b.DrawString(
							spriteFont: font,
							text: text,
							position: textPosition,
							color: textColor,
							rotation: textRotation,
							origin: Vector2.Zero,
							scale: textScale,
							effects: SpriteEffects.None,
							layerDepth: 1f);
					}
				}
			}

			// Components
			foreach ((string bugId, ClickableTextureComponent c) in this.BugClickables)
				
					c.draw(b);

			this.upperRightCloseButton?.draw(b);
			this.drawMouse(b);

			// Test
			if (true)
			if (false)
			{
				float a = 0.2f;
				b.Draw(
					texture: Game1.fadeToBlackRect,
					destinationRectangle: this._displayArea,
					color: Color.Yellow * a);
				b.Draw(
					texture: Game1.fadeToBlackRect,
					destinationRectangle: this._sceneArea,
					color: Color.Red * a);
				b.Draw(
					texture: Game1.fadeToBlackRect,
					destinationRectangle: this._clickableArea,
					color: Color.Blue * a);
				b.Draw(
					texture: Game1.fadeToBlackRect,
					destinationRectangle: this._cardArea,
					color: Color.Orange * a);
				b.Draw(
					texture: Game1.fadeToBlackRect,
					destinationRectangle: this._textArea,
					color: Color.Purple * a);
			}
		}

		public override bool IsAutomaticSnapValid(int direction, ClickableComponent a, ClickableComponent b)
		{
			return base.IsAutomaticSnapValid(direction, a, b);
		}

		public override void snapToDefaultClickableComponent()
		{
			this.currentlySnappedComponent = this.getComponentWithID(0);
			this.snapCursorToCurrentSnappedComponent();
		}
	}
}
