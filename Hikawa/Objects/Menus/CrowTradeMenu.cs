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
		public readonly Rectangle SceneBackgroundSource = new(0, 336, 144, 80);

		public Rectangle _sceneArea;
		public Rectangle _inventoryArea;
		public Rectangle _displayArea;

		public ClickableComponent ItemSlot;
		public ClickableComponent ItemInventorySlot;
		public Texture2D CrowTradeMenuTexture;

		public bool IsAnimating;
		public Vector2 AnimStartPosition;
		public Vector2 AnimEndPosition;
		public double AnimStartTime;
		public double AnimEndTime;

		private readonly Point _borderScaled;

		public bool IsCloseButtonAllowed => !Game1.options.SnappyMenus;
		public double AnimTime => Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
		public double AnimRatio => this.IsAnimating ? (this.AnimTime - this.AnimStartTime) / (this.AnimEndTime - this.AnimStartTime) : 0;

		public CrowTradeMenu(Shrine shrine) : base(
			highlighterMethod: CrowTradeMenu.HighlightItems,
			okButton: false,
			trashCan: false,
			inventoryXOffset: CrowTradeMenu.InventoryOffset.X,
			inventoryYOffset: CrowTradeMenu.InventoryOffset.Y)
		{
			this._borderScaled = new(x: this.BorderSize.X * this.Scale, y: this.BorderSize.Y * this.Scale);

			this.initializeUpperRightCloseButton();
			this.CrowTradeMenuTexture = ModEntry.Sprites;
			this.ItemSlot = new ClickableComponent(
				bounds: Rectangle.Empty,
				name: null,
				label: null)
			{
				item = shrine.CrowTradeItem
			};

			// Position components
			this.gameWindowSizeChanged(oldBounds: Rectangle.Empty, newBounds: Game1.graphics.GraphicsDevice.Viewport.Bounds);

			// Clickable navigation
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

		public void DonateItem(Item item)
		{
			Game1.playSound("grassyStep");
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
				Game1.playSound("pickUpItem");
				Game1.player.addItemToInventory(this.ItemSlot.item);
				this.ItemSlot.item = null;
			}
		}

		public static bool HighlightItems(Item i)
		{
			return i is StardewValley.Object o && !o.bigCraftable.Value && !o.questItem.Value && o is not (Wallpaper or Trinket or Furniture);
		}

		public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
		{
			newBounds.Offset(offsetX: -newBounds.X, offsetY: -newBounds.Y);

			this.xPositionOnScreen = newBounds.X;
			this.yPositionOnScreen = newBounds.Y;

			base.gameWindowSizeChanged(oldBounds: oldBounds, newBounds: newBounds);

			Point centre = newBounds.Center;
			if (Context.IsSplitScreen)
			{
				// Centre the menu in splitscreen
				centre.X = centre.X / 3 * 2;
			}

			int yOffset = -8;
			int inventorySpacing = 2;
			Point inventorySize = new(x: 6, y: 6);
			Point scaledInventorySize = new(
				x: inventorySize.X * ((Game1.smallestTileSize + inventory.horizontalGap) * this.Scale),
				y: inventorySize.Y * (int)((Game1.smallestTileSize + inventory.verticalGap + 0.5f) * this.Scale));

			// Menu visible area
			Point displaySize = new(
				x: (this.SceneBackgroundSource.Width + this.BorderSize.X * 2 + inventorySpacing) * this.Scale + scaledInventorySize.X,
				y: Math.Max(scaledInventorySize.Y, this.SceneBackgroundSource.Height * this.Scale) + (this.BorderSize.Y * 2 * this.Scale));
			this._displayArea = new(
				x: centre.X - displaySize.X / 2,
				y: centre.Y - displaySize.Y / 2 + yOffset * this.Scale,
				width: displaySize.X,
				height: displaySize.Y
			);

			// Info area
			this._sceneArea = new(
				x: this._displayArea.Right - this.SceneBackgroundSource.Width * this.Scale - this._borderScaled.X,
				y: this._displayArea.Center.Y - (this.SceneBackgroundSource.Height * this.Scale / 2),
				width: this.SceneBackgroundSource.Width * this.Scale,
				height: this.SceneBackgroundSource.Height * this.Scale);

			// Inventory area
			this._inventoryArea = new(
				x: this._displayArea.Left + this.BorderSize.Y * this.Scale,
				y: this._displayArea.Center.Y - scaledInventorySize.Y / 2,
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
				x: this._displayArea.Right - this.BorderSize.X * this.Scale,
				y: this._displayArea.Y - this.BorderSize.Y * this.Scale);

			// Evidently this was good enough for the base game
			this.inventory = new(
				xPosition: this._inventoryArea.X,
				yPosition: this._inventoryArea.Y,
				playerInventory: false,
				actualInventory: null,
				highlightMethod: this.inventory.highlightMethod,
				rows: inventorySize.X)
			{
				width = this._inventoryArea.Width,
				height = this._inventoryArea.Height
			};
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
				this.AnimStartTime = this.AnimTime;
				this.AnimEndTime = this.AnimStartTime + 300 + Math.Abs(this.AnimEndPosition.X - this.AnimStartPosition.X) / this.Scale;
				this.IsAnimating = true;
			}
			else if (this.ItemSlot.containsPoint(x, y))
			{
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

			if (this.IsAnimating && this.AnimTime > this.AnimEndTime)
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

			// Blackout
			b.Draw(
				texture: Game1.fadeToBlackRect,
				destinationRectangle: screen,
				color: Color.Black * 0.5f);

			// Card
			Point cardOffset = new(0 * this.Scale, -6 * this.Scale);
			Point cardPadding = new(12 * this.Scale, 28 * this.Scale);
			Game1.drawDialogueBox(
				x: this._displayArea.X - cardOffset.X - cardPadding.X,
				y: this._displayArea.Y - cardOffset.Y - cardPadding.Y - 32,
				width: this._displayArea.Width + cardPadding.X * 2,
				height: this._displayArea.Height + cardPadding.Y * 2,
				speaker: false,
				drawOnlyBox: true,
				message: null,
				objectDialogueWithPortrait: false,
				ignoreTitleSafe: false,
				r: 100,
				g: 50,
				b: 0);
			
			// Scene
			b.Draw(
				texture: this.CrowTradeMenuTexture,
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
			this.inventory?.draw(b, 150, 75, 25);
			this.ItemSlot.item?.drawInMenu(
				spriteBatch: b,
				location: this.IsAnimating
					? Vector2.Lerp(this.AnimStartPosition, this.AnimEndPosition, ratio) + new Vector2(x: 0, (this.AnimStartPosition.Y - this.AnimEndPosition.Y) / 2 * circular)
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
					color: Color.Black * a);
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
