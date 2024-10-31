using System.Collections.Generic;
using System.Linq;
using Hikawa.Objects.Locations;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Objects.Trinkets;

namespace Hikawa.Objects.Menus
{
	public class CrowTradeMenu : MenuWithInventory
	{
		public static readonly Point InventoryOffset = new(16, 132);

		public readonly int Scale = 4;
		public readonly Point BorderSize = new(4, 4);
		public readonly Rectangle SceneBackgroundSource = new(0, 0, 200, 120);
		public readonly Rectangle ItemSlotSource = new(0, 0, 16, 16);

		public Rectangle _infoArea;
		public Rectangle _inventoryArea;
		public Rectangle _displayArea;

		public ClickableTextureComponent ItemSlot;
		public Texture2D CrowTradeMenuTexture;

		private readonly Point _borderScaled;

		public bool IsCloseButtonAllowed => !Game1.options.SnappyMenus;

		public CrowTradeMenu(Shrine shrine) : base(
			highlighterMethod: CrowTradeMenu.HighlightItems,
			okButton: false,
			trashCan: false,
			inventoryXOffset: InventoryOffset.X,
			inventoryYOffset: InventoryOffset.Y)
		{
			this._borderScaled = new(x: this.BorderSize.X * this.Scale, y: this.BorderSize.Y * this.Scale);

			this.initializeUpperRightCloseButton();
			this.CrowTradeMenuTexture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\FieldOfficeDonationMenu");
			this.ItemSlot = new ClickableTextureComponent(
				name: null,
				label: null,
				hoverText: null,
				texture: Game1.birdsSpriteSheet,
				sourceRect: new(0, 0, 16, 16),
				scale: Game1.pixelZoom,
				bounds: Rectangle.Empty)
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

		public void DonateItem()
		{
			Game1.playSound("grassyStep");
			this.ItemSlot.item = ItemRegistry.Create(this.heldItem.QualifiedItemId);
			this.heldItem = this.heldItem.ConsumeStack(1);
		}

		public void UndonateItem()
		{
			Game1.playSound("pickUpItem");
			this.heldItem = this.ItemSlot.item;
			this.ItemSlot.item = null;
		}

		public static bool HighlightItems(Item i)
		{
			return i is Object o && !o.bigCraftable.Value && !o.questItem.Value && o is not (Wallpaper or Trinket);
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

			int yOffset = -8 * this.Scale;

			// Menu visible area
			Point displaySize = new(
				x: (this.SceneBackgroundSource.Width + this.BorderSize.X) * this.Scale,
				y: this.SceneBackgroundSource.Height * this.Scale + this.inventory.height);
			this._displayArea = new(
				x: centre.X - displaySize.X / 2,
				y: centre.Y - displaySize.Y / 2 + yOffset,
				width: displaySize.X,
				height: displaySize.Y
			);

			// Info area
			this._infoArea = new(
				x: this._displayArea.X,
				y: this._displayArea.Y,
				width: this.SceneBackgroundSource.Width * this.Scale,
				height: this.SceneBackgroundSource.Height * this.Scale);

			// Inventory area
			this._inventoryArea = new(
				x: centre.X - this.inventory.width / 2,
				y: this._infoArea.Bottom + this._borderScaled.Y / 7 * 13,
				width: Game1.tileSize * 12,
				height: Game1.tileSize * this.inventory.rows);

			// Item slot
			this.ItemSlot.bounds = new Rectangle(
				x: this._infoArea.Left + this._infoArea.Width / 7 * 4,
				y: this._infoArea.Top + this._infoArea.Height / 3 - this.ItemSlotSource.Width * this.Scale / 2,
				width: this.ItemSlotSource.Width * this.Scale,
				height: this.ItemSlotSource.Height * this.Scale);

			// Close button
			this.upperRightCloseButton.setPosition(
				x: this._infoArea.X + this._infoArea.Width - 2 * this.Scale,
				y: this._infoArea.Y - this.BorderSize.Y * this.Scale);

			// Evidently this was good enough for the base game
			this.inventory = new(
				xPosition: this._inventoryArea.X,
				yPosition: this._inventoryArea.Y,
				playerInventory: false,
				actualInventory: null,
				highlightMethod: this.inventory.highlightMethod)
			{
				width = this._inventoryArea.Width,
				height = this._inventoryArea.Height
			};
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			base.receiveLeftClick(x, y, playSound);

			if (this.ItemSlot.containsPoint(x, y))
			{
				if (this.heldItem is not null && this.ItemSlot.item is null)
				{
					this.DonateItem();
				}
				else if (this.heldItem is null && this.ItemSlot.item is not null)
				{
					this.UndonateItem();
				}
			}
			else if (this.IsCloseButtonAllowed && this.upperRightCloseButton.containsPoint(x, y))
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
			this.ItemSlot.tryHover(x, y, maxScaleIncrease: scale);

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
		}

		public override void draw(SpriteBatch b)
		{
			Rectangle screen = Game1.graphics.GraphicsDevice.Viewport.Bounds;
			screen.Offset(
				offsetX: -screen.X,
				offsetY: -screen.Y);

			// Blackout
			b.Draw(
				texture: Game1.fadeToBlackRect,
				destinationRectangle: screen,
				color: Color.Black * 0.5f);

			b.Draw(
				texture: Game1.fadeToBlackRect,
				destinationRectangle: this._infoArea,
				color: Color.Red * 0.5f);

			b.Draw(
				texture: Game1.fadeToBlackRect,
				destinationRectangle: this._inventoryArea,
				color: Color.Blue * 0.5f);

			this.okButton?.draw(b);
			this.inventory?.draw(b, 0, 0, 0);

			this.ItemSlot.draw(b);
			this.ItemSlot.item?.drawInMenu(
				spriteBatch: b,
				location: Utility.PointToVector2(this.ItemSlot.bounds.Location),
				scaleSize: 1f);

			this.drawMouse(b);
			this.heldItem?.drawInMenu(
				spriteBatch: b,
				location: new Vector2(Game1.getOldMouseX() + 16, Game1.getOldMouseY() + 16),
				scaleSize: 1f);
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
