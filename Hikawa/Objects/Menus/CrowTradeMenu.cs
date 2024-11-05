using System;
using System.Collections.Generic;
using System.Linq;
using Hikawa.Objects.Locations;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Objects.Trinkets;

/**
cs Game1.activeClickableMenu = new StardewValley.Menus.FieldOfficeMenu(Game1.getLocationFromName(`IslandFieldOffice`) as StardewValley.Locations.IslandFieldOffice);
*/
namespace Hikawa.Objects.Menus
{
	public class CrowTradeMenu : MenuWithInventory
	{
		public static readonly Point InventoryOffset = new(16, 132);

		public readonly int Scale = 4;
		public readonly Point BorderSize = new(4, 4);
		public readonly Point ItemSlotSize = new(18, 18);
		public readonly Rectangle SceneBackgroundSource = new(0, 336, 192, 128);

		public Shrine Shrine;

		public ClickableComponent ItemSlot;
		public ClickableComponent ItemInventorySlot;

		public bool IsAnimating;
		public Vector2 AnimStartPosition;
		public Vector2 AnimEndPosition;
		public double AnimStartTime;
		public double AnimEndTime;

		private Rectangle _sceneArea;
		private Rectangle _inventoryArea;
		private Rectangle _displayArea;

		public bool IsCloseButtonAllowed => !Game1.options.SnappyMenus;
		public double TotalMs => Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
		public double AnimTime => this.AnimEndTime - this.AnimStartTime;
		public double AnimRatio => this.IsAnimating ? (this.TotalMs - this.AnimStartTime) / (this.AnimEndTime - this.AnimStartTime) : 0;

		public CrowTradeMenu(Shrine shrine) : base(
			highlighterMethod: CrowTradeMenu.HighlightItems,
			okButton: false,
			trashCan: false,
			inventoryXOffset: CrowTradeMenu.InventoryOffset.X,
			inventoryYOffset: CrowTradeMenu.InventoryOffset.Y)
		{
			this.Shrine = shrine;

			// Components
			this.ItemSlot = new ClickableComponent(
				bounds: Rectangle.Empty,
				name: null,
				label: null)
			{
				item = this.Shrine.CrowTradeItem.Value
			};
			this.initializeUpperRightCloseButton();
			this.gameWindowSizeChanged(oldBounds: Rectangle.Empty, newBounds: Game1.graphics.GraphicsDevice.Viewport.Bounds);
			this.populateClickableComponentList();
			this.UpdateComponentNavigation();
		}

		public void UpdateComponentNavigation()
		{
			List<ClickableComponent> components = [this.ItemSlot];
			for (int i = 0; i < components.Count; i++)
			{
				ClickableComponent c = components[i];
				c.upNeighborID = c.downNeighborID = c.rightNeighborID = c.leftNeighborID = -99998;
				c.myID = 1000 + i;
			}
			foreach (ClickableComponent itemT in inventory.GetBorder(InventoryMenu.BorderSide.Top))
			{
				itemT.upNeighborID = -99998;
			}
			foreach (ClickableComponent itemR in inventory.GetBorder(InventoryMenu.BorderSide.Right))
			{
				itemR.rightNeighborID = 4857;
				itemR.rightNeighborImmutable = true;
			}
			this.populateClickableComponentList();
			if (Game1.options.SnappyMenus)
			{
				this.snapToDefaultClickableComponent();
			}
			if (this.trashCan is not null && this.okButton is not null)
			{
				this.trashCan.leftNeighborID = this.okButton.leftNeighborID = 11;
			}
		}

		public void UpdateComponentLayout(Rectangle bounds)
		{
			Point centre = bounds.Center;
			if (Context.IsSplitScreen)
			{
				// Centre the menu in splitscreen
				centre.X = centre.X / 3 * 2;
			}

			Point offset = new(0, -11);
			int inventorySpacing = -6;
			Point inventorySize = new(x: 12, y: 3);
			Point scaledInventorySize = new(
				x: inventorySize.X * ((Game1.smallestTileSize + inventory.horizontalGap) * this.Scale),
				y: inventorySize.Y * (int)((Game1.smallestTileSize + inventory.verticalGap + 0.5f) * this.Scale));

			// Menu visible area
			Point displaySize = new(
				x: (this.SceneBackgroundSource.Width + this.BorderSize.X * 2) * this.Scale,
				y: (this.SceneBackgroundSource.Height + this.BorderSize.Y * 2 + inventorySpacing) * this.Scale + scaledInventorySize.Y);
			this._displayArea = new(
				x: centre.X - displaySize.X / 2 + offset.X * this.Scale,
				y: centre.Y - displaySize.Y / 2 + offset.Y * this.Scale,
				width: displaySize.X,
				height: displaySize.Y
			);

			// Info area
			this._sceneArea = new(
				x: this._displayArea.Center.X - this.SceneBackgroundSource.Width / 2 * this.Scale,
				y: this._displayArea.Top + (this.BorderSize.Y * this.Scale),
				width: this.SceneBackgroundSource.Width * this.Scale,
				height: this.SceneBackgroundSource.Height * this.Scale);

			// Inventory area
			this._inventoryArea = new(
				x: this._displayArea.Center.X - scaledInventorySize.X / 2,
				y: this._displayArea.Bottom - scaledInventorySize.Y,
				width: scaledInventorySize.X,
				height: scaledInventorySize.Y);

			// Item slot
			this.ItemSlot.bounds = new Rectangle(
				x: this._sceneArea.Center.X - this.ItemSlotSize.X * this.Scale / 2,
				y: this._sceneArea.Center.Y - this.ItemSlotSize.Y * this.Scale / 2,
				width: this.ItemSlotSize.X * this.Scale,
				height: this.ItemSlotSize.Y * this.Scale);

			// Close button
			this.upperRightCloseButton.setPosition(
				x: this._displayArea.Right + (this.BorderSize.X - 16) * this.Scale,
				y: this._displayArea.Top + (this.BorderSize.Y + 16) * this.Scale);

			// Evidently this was good enough for the base game
			this.inventory = new(
				xPosition: this._inventoryArea.X,
				yPosition: this._inventoryArea.Y,
				playerInventory: false,
				actualInventory: null,
				highlightMethod: this.inventory.highlightMethod,
				rows: inventorySize.Y)
			{
				width = this._inventoryArea.Width,
				height = this._inventoryArea.Height
			};
		}

		public void DonateItem(Item item)
		{
			int index = Game1.player.Items.IndexOf(item);
			this.ItemInventorySlot = this.inventory.inventory.Find(c => c.myID == index);
			this.ItemSlot.item = item.getOne();
			if (item.ConsumeStack(1) is null)
				Utility.removeItemFromInventory(index, this.inventory.actualInventory);
		}

		public void UndonateItem()
		{
			if (this.ItemSlot.item is not null)
			{
				Game1.player.addItemToInventory(this.ItemSlot.item);
				this.ItemSlot.item = null;
			}
		}

		public void SetShrineItem()
		{
			this.Shrine.CrowTradeItem.Set(this.ItemSlot.item);
			this.Shrine.IsCrowTradeUsedToday = this.Shrine.CrowTradeItem.Value is not null;
		}

		public static bool HighlightItems(Item i)
		{
			return i is StardewValley.Object o && !o.bigCraftable.Value && !o.questItem.Value && o is not (Wallpaper or Trinket or Furniture);
		}

		protected override void cleanupBeforeExit()
		{
			this.SetShrineItem();

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
			if (this.IsAnimating)
				return;

			// Item interactions
			if (this.inventory.getItemAt(x, y) is Item item && this.inventory.highlightMethod(item))
			{
				this.UndonateItem();
				this.DonateItem(item);

				this.AnimStartPosition = this.ItemInventorySlot.bounds.Location.ToVector2();
				this.AnimEndPosition = Utility.PointToVector2(this.ItemSlot.bounds.Location);
				this.AnimStartTime = this.TotalMs;
				this.AnimEndTime = this.AnimStartTime + 200 + Math.Abs(this.AnimEndPosition.X - this.AnimStartPosition.X) / this.Scale;
				this.IsAnimating = true;
				Game1.playSound("throwDownITem");
				DelayedAction.playSoundAfterDelay(
					soundName: Game1.random.NextDouble() < 0.3 ? "leafrustle" : "grassyStep",
					delay: (int)this.AnimTime);
			}
			else if (this.ItemSlot.containsPoint(x, y))
			{
				Game1.playSound("pickUpItem");
				this.UndonateItem();
			}

			// Close menu
			if (this.IsCloseButtonAllowed && this.upperRightCloseButton.containsPoint(x, y))
			{
				this.exitThisMenu();
			}
		}

		public override void performHoverAction(int x, int y)
		{
			this.hoverText = "";
			this.hoveredItem = null;

			this.inventory?.hover(x, y, null);

			const float scale = 0.25f;

			if (this.IsCloseButtonAllowed)
			{
				// Hover close button
				this.upperRightCloseButton.tryHover(x, y, maxScaleIncrease: scale * 2);
			}
			if (this.ItemSlot.containsPoint(x, y) && this.ItemSlot.item is not null)
			{
				// Hover item slot
				this.hoveredItem = this.ItemSlot.item;
			}
			else if (this.inventory.getInventoryPositionOfClick(x, y) is int index && this.inventory.actualInventory.ElementAtOrDefault(index) is Item item)
			{
				// Hover inventory item
				this.hoveredItem = item;
			}
		}

		public override void update(GameTime time)
		{
			base.update(time);

			if (this.IsAnimating && this.TotalMs > this.AnimEndTime)
				this.IsAnimating = false;
		}

		public override void draw(SpriteBatch b)
		{
			Rectangle screen = Game1.graphics.GraphicsDevice.Viewport.Bounds;
			screen.Offset(
				offsetX: -screen.X,
				offsetY: -screen.Y);

			float ratio = (float)this.AnimRatio;
			float circular = Utils.SharpCircularFromRatio(ratio);

			Color blackoutColor = new Color(25, 0, 10);
			Color cardColor = new(150, 105, 115);
			Color frameColor = new(175, 88, 33);
			Color slotsColor = new(75, 15, 25);

			// Blackout
			b.Draw(
				texture: Game1.fadeToBlackRect,
				destinationRectangle: screen,
				color: blackoutColor * 0.25f);

			// Card
			Point inventoryOffset = new(x: 0 * this.Scale, y: 1 * this.Scale);
			Point inventoryPadding = new(x: 18 * this.Scale, y: 22 * this.Scale);
			this.DrawInventoryCard(spriteBatch: b, area: new(
				x: this._inventoryArea.X - inventoryOffset.X - inventoryPadding.X,
				y: this._inventoryArea.Y - inventoryOffset.Y - inventoryPadding.Y - 32,
				width: this._inventoryArea.Width + inventoryPadding.X * 2,
				height: this._inventoryArea.Height + inventoryPadding.Y * 2),
				c: frameColor,
				bg: cardColor);

			// Scene
			b.Draw(
				texture: ModEntry.Sprites,
				sourceRectangle: this.SceneBackgroundSource,
				destinationRectangle: this._sceneArea,
				color: Color.White);

			// Text
			if (false)
			{
				SpriteFont font = Game1.dialogueFont;
				string text = Game1.parseText("Leave something in the nest?", font, int.MaxValue);
				Vector2 textSize = font.MeasureString(text);
				Vector2 textPosition = new(x: this._sceneArea.Center.X - textSize.X / 2, y: this._displayArea.Bottom);
				Utility.drawTextWithColoredShadow(
					b: b,
					text: text,
					font: font,
					position: textPosition,
					color: Game1.textColor,
					shadowColor: Color.SaddleBrown * 0.333f);
			}

			// Components
			this.upperRightCloseButton?.draw(b);
			this.okButton?.draw(b);
			this.trashCan?.draw(b);
			this.inventory?.draw(b, slotsColor.R, slotsColor.G, slotsColor.B);
			this.ItemSlot.item?.drawInMenu(
				spriteBatch: b,
				location: this.IsAnimating
					? Vector2.Lerp(this.AnimStartPosition, this.AnimEndPosition, ratio) // direct motion
						+ new Vector2(x: (this.AnimStartPosition.X - this.AnimEndPosition.X) / 2 * circular, y: 0) // added motion
					: Utility.PointToVector2(this.ItemSlot.bounds.Location),
				scaleSize: 1f + 0.75f * circular);
			this.drawMouse(b);

			// Test
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
					destinationRectangle: this._inventoryArea,
					color: Color.Blue * a);
				b.Draw(
					texture: Game1.fadeToBlackRect,
					destinationRectangle: this.ItemSlot.bounds,
					color: Color.Green * a);
			}
		}

		public void DrawInventoryCard(SpriteBatch spriteBatch, Rectangle area, Color c, Color bg)
		{
			int x = area.X;
			int y = area.Y;
			int width = area.Width;
			int height = area.Height;
			Texture2D texture = Game1.uncoloredMenuTexture;
			Rectangle source = new(64, 128, 64, 64);
			spriteBatch.Draw(texture: texture, destinationRectangle: new Rectangle(x + source.Width / 2, y + source.Height / 6 * 10, width - source.Width, height - source.Height * 2), sourceRectangle: source, color: bg);
			source.Y = 0;
			source.X = 0;
			spriteBatch.Draw(texture, new Vector2(x, y + source.Height), source, c);
			source.X = 192;
			spriteBatch.Draw(texture, new Vector2(x + width - source.Width, y + source.Height), source, c);
			source.Y = 192;
			spriteBatch.Draw(texture, new Vector2(x + width - source.Width, y + height - source.Height), source, c);
			source.X = 0;
			spriteBatch.Draw(texture, new Vector2(x, y + height - source.Height), source, c);
			source.X = 128;
			source.Y = 0;
			spriteBatch.Draw(texture, new Rectangle(x + source.Width, y + source.Height, width - source.Width * 2, source.Height), source, c);
			source.Y = 192;
			spriteBatch.Draw(texture, new Rectangle(x + source.Width, y + height - source.Height, width - source.Width * 2, source.Height), source, c);
			source.Y = 128;
			source.X = 0;
			spriteBatch.Draw(texture, new Rectangle(x, y + source.Height * 2, source.Width, height - source.Height * 3), source, c);
			source.X = 192;
			spriteBatch.Draw(texture, new Rectangle(x + width - source.Width, y + source.Height * 2, source.Width, height - source.Height * 3), source, c);
		}

		public override bool IsAutomaticSnapValid(int direction, ClickableComponent a, ClickableComponent b)
		{
			if (b.myID == 5948 && b.myID != 4857)
			{
				return false;
			}
			return base.IsAutomaticSnapValid(direction, a, b);
		}

		public override void snapToDefaultClickableComponent()
		{
			this.currentlySnappedComponent = this.getComponentWithID(0);
			this.snapCursorToCurrentSnappedComponent();
		}
	}
}
