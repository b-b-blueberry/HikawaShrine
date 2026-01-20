using Hikawa.Data;
using Hikawa.Objects.Items;
using StardewValley.Menus;
using System;
using System.Linq;

namespace Hikawa.Objects.Menus
{
    /// <summary>
    /// Handles <see cref="Items.BugFurniture"/> interactions to add and remove bugs.
    /// </summary>
    public class BugFurnitureMenu : IClickableMenu
    {
        /// <summary>List of bug slot buttons.</summary>
        public ClickableTextureComponent[] BugSlotButtons;
        /// <summary>List of bug buttons in the slot popup.</summary>
        public ClickableTextureComponent[] BugButtons;
        /// <summary>List of <see cref="BugButtons"/> indexes where the bug can be added to the current bug slot.</summary>
        public bool[] BugsAllowed;
        /// <summary>Current bug slot index.</summary>
        public int BugSlotIndex;
        /// <summary>Furniture instance being targeted.</summary>
        public BugFurniture BugFurniture;

        /// <summary>Number of columns used to display bug buttons in the slot popup.</summary>
        const int BugButtonsColumns = 3;
        /// <summary>Scale applied to menu layout and draw behaviours.</summary>
        const int MenuScale = Game1.pixelZoom;
        /// <summary>Scaled offset used for <see cref="IClickableMenu.drawTextureBox"/>.</summary>
        const int Border = 3 * MenuScale;
        /// <summary>Unscaled tile size used for buttons.</summary>
        const int TileSize = Game1.smallestTileSize;

        public const string OpenSound = "openBox";
        public const string SelectSound = "smallSelect";
        public const string DeselectSound = "dialogueCharacter";

        public BugFurnitureMenu(BugFurniture furniture)
            : base()
        {
            this.BugFurniture = furniture;
            this.BugButtons = ModEntry.BugsData.Value.BugData
                .Reverse() // buttons are added from bottom to top
                .Where(pair => ModEntry.SaveData.BugCollection.ContainsKey(pair.Key)) // caught bugs only
                .Select((pair, i) => new ClickableTextureComponent(
                    bounds: new Rectangle(0, 0, TileSize * MenuScale, TileSize * MenuScale),
                    texture: Game1.content.Load<Texture2D>(pair.Value.TextureId),
                    sourceRect: pair.Value.MenuTextureRegion,
                    scale: MenuScale,
                    drawShadow: false)
            { name = pair.Key }).ToArray();
            this.BugSlotButtons = furniture.Definition.BugSlots
                .Select(slot => new ClickableTextureComponent(
                    bounds: new Rectangle(0, 0, TileSize * MenuScale, TileSize * MenuScale),
                    texture: ModEntry.Sprites,
                    sourceRect: new Rectangle(192 + TileSize * (int)Enum.Parse(typeof(BugSlotType), slot.Type), 448, TileSize, TileSize),
                    scale: MenuScale,
                    drawShadow: false)).ToArray();
            this.BugsAllowed = new bool[this.BugButtons.Length];
            this.BugSlotIndex = -1;

            this.RepositionComponents();
        }

        public void RepositionComponents()
        {
            // menu positioned relative to furniture in world
            var position = this.BugFurniture.getLocalPosition(Game1.viewport)
                // align to right side
                + new Vector2(this.BugFurniture.sourceRect.Value.Size.X, 0) * Game1.pixelZoom
                // added spacing
                + new Vector2(6, -6) * Game1.pixelZoom
                ;

            this.xPositionOnScreen = (int)position.X;
            this.yPositionOnScreen = (int)position.Y;

            // dummy values, not used
            this.width = TileSize;
            this.height = TileSize;

            // prevent bug button popup from going offscreen
            var h = Border * 4 + TileSize * MenuScale * (1 + ((this.BugButtons.Length - 1) / BugButtonsColumns));
            this.yPositionOnScreen += Math.Max(0, h - this.yPositionOnScreen);

            // bug slots arranged in row
            for (var i = 0; i < this.BugSlotButtons.Length; ++i)
                this.BugSlotButtons[i].bounds.Location = new(this.xPositionOnScreen + TileSize * i * MenuScale, this.yPositionOnScreen);

            // bug buttons arranged in grid above slots
            for (var i = 0; i < this.BugButtons.Length; ++i)
                this.BugButtons[i].bounds.Location = new(this.xPositionOnScreen + TileSize * MenuScale * (i % BugButtonsColumns), this.yPositionOnScreen - Border * 2 - TileSize * MenuScale * (1 + (i / BugButtonsColumns)));
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);

            this.RepositionComponents();
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            // select bug slot
            for (var slotIndex = 0; slotIndex < this.BugSlotButtons.Length; ++slotIndex)
            {
                if (this.BugSlotButtons[slotIndex].containsPoint(x, y))
                {
                    if (this.BugSlotIndex == slotIndex)
                    {
                        // select same bug slot, close
                        this.BugSlotIndex = -1;
                        Game1.playSound(DeselectSound);
                    }
                    else
                    {
                        // select new bug slot, change
                        this.BugSlotIndex = slotIndex;
                        var bugSlot = this.BugFurniture.Definition.BugSlots[this.BugSlotIndex].Type;
                        for (var bugIndex = 0; bugIndex < this.BugButtons.Length; ++bugIndex)
                            this.BugsAllowed[bugIndex] = ModEntry.BugsData.Value.BugData[this.BugButtons[bugIndex].name].BugSlots.Contains(bugSlot);
                        Game1.playSound(SelectSound);
                    }
                    return;
                }
            }

            // select bug with bug buttons popup open
            if (this.BugSlotIndex >= 0)
            {
                for (var i = 0; i < this.BugButtons.Length; ++i)
                {
                    if (this.BugButtons[i].containsPoint(x, y))
                    {
                        string bugId = this.BugButtons[i].name;
                        if (this.BugFurniture.Bugs[this.BugSlotIndex] is not null && this.BugFurniture.Bugs[this.BugSlotIndex].BugId == bugId)
                        {
                            // selected same bug, remove
                            this.BugFurniture.RemoveBug(this.BugSlotIndex);
                            Game1.playSound(DeselectSound);
                        }
                        else
                        {
                            // selected different bug, add or replace
                            if (this.BugFurniture.TryAddBug(this.BugSlotIndex, bugId))
                            {
                                Game1.playSound(SelectSound);
                                this.BugSlotIndex = -1;
                            }
                            else
                            {
                                Game1.playSound(DeselectSound);
                            }
                        }
                        return;
                    }
                }

                // close bug buttons popup
                Game1.playSound(DeselectSound);
                this.BugSlotIndex = -1;
            }
            else
            {
                // close menu
                this.exitThisMenu();
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            base.receiveRightClick(x, y, playSound);

            // close bug buttons popup if open, otherwise close menu
            if (this.BugSlotIndex >= 0)
            {
                Game1.playSound(DeselectSound);
                this.BugSlotIndex = -1;
            }
            else
            {
                this.exitThisMenu();
            }
        }

        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);

            foreach (var button in this.BugSlotButtons)
                button.tryHover(x, y);

            // update bug buttons as if unhovered while bugs popup is closed
            if (this.BugSlotIndex < 0)
                x = y = -9999;

            foreach (var button in this.BugButtons)
                button.tryHover(x, y);
        }

        public override void draw(SpriteBatch b)
        {
            base.draw(b);

            // bug buttons
            if (this.BugSlotIndex >= 0)
            {
                var slot = this.BugSlotButtons[this.BugSlotIndex];
                drawTextureBox(b: b, x: this.BugButtons[0].bounds.X - Border, y: this.BugButtons[^1].bounds.Y - Border, width: BugButtonsColumns * TileSize * MenuScale + Border * 2, height: this.BugButtons.Length / BugButtonsColumns * TileSize * MenuScale + Border * 2, color: Color.White);
                for (var i = 0; i < this.BugButtons.Length; ++i)
                    this.BugButtons[i].draw(b, c: this.BugsAllowed[i] ? Color.White : Color.Black * 0.35f, layerDepth: 1f);
            }

            // bug slots
            drawTextureBox(b: b, x: this.BugSlotButtons[0].bounds.X - Border, y: this.BugSlotButtons[^1].bounds.Y - Border, width: this.BugSlotButtons.Length * TileSize * MenuScale + Border * 2, height: TileSize * MenuScale + Border * 2, color: Color.White);
            for (var i = 0; i < this.BugSlotButtons.Length; ++i)
                this.BugSlotButtons[i].draw(b, c: this.BugSlotIndex < 0 || this.BugSlotIndex == i ? Color.White : Color.Black * 0.35f, layerDepth: 1f);

            // bugs in slots
            for (var i = 0; i < this.BugFurniture.Bugs.Length; ++i)
                this.BugFurniture.Bugs[i]?.DrawInMenu(b, this.BugSlotButtons[i].bounds.Location.ToVector2(), Vector2.Zero, MenuScale);

            this.drawMouse(b);
        }
    }
}
