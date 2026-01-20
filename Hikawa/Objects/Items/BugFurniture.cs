using Hikawa.Data;
using Hikawa.Objects.Critters;
using Hikawa.Objects.Items.Data;
using Hikawa.Objects.Menus;
using Netcode;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;
using System.Linq;
using System.Xml.Serialization;

namespace Hikawa.Objects.Items
{
	[XmlType($"{ModConsts.SpaceCoreXmlPrefix}{nameof(BugFurniture)}")] // SpaceCore serialisation signature
	public class BugFurniture : Furniture
    {
        /// <summary>Whether the owner is currently using the <see cref="BugFurnitureMenu"/>.</summary>
        [XmlIgnore]
        public NetBool IsInUse = new();

        /// <summary>Bug furniture data definition.</summary>
        [XmlIgnore]
        public BugFurnitureDataEntry Definition;

        /// <summary>Bug instances for each slot.</summary>
        [XmlIgnore]
		public ShrineBug[] Bugs;

        /// <summary>Bug IDs for each slot. Saves current bugs.</summary>
        public NetArray<string> BugIds = new();

        public override string TypeDefinitionId => BugFurnitureItemDataDefinition.TypeDefinitionId;

		public BugFurniture() : base()
		{
		}

		public BugFurniture(ParsedItemData data)
			: this()
        {
            // BugFurniture
            this.Definition = data.RawData as BugFurnitureDataEntry;
            this.BugIds ??= new string[this.Definition.BugSlots.Length];
            this.Bugs = new ShrineBug[this.BugIds.Length];
            for (int i = 0; i < this.BugIds.Length; ++i)
                this.TryAddBug(i, this.BugIds[i]);

            // Item
            this.ItemId = data.ItemId;
            this.Name = data.InternalName;
            this.Type = ModEntry.BugsData.Value.BugFurnitureData.Type;
            this.furniture_type.Value = Furniture.getTypeNumberFromName(ModEntry.BugsData.Value.BugFurnitureData.FurnitureType);
            this.SpecialVariable = 388859; // allow front texture draw

            // update bounding box and sprite size
            this.updateRotation();
        }

        protected override void initNetFields()
        {
            base.initNetFields();

            this.NetFields.AddField(this.IsInUse);
        }

        public bool CanAddBug(int slot, string bugId)
		{
            if (bugId is null || !ModEntry.BugsData.Value.BugData.ContainsKey(bugId))
                return false;

			string slotType = this.Definition.BugSlots[slot].Type;
			string[] allowedSlotTypes = ModEntry.BugsData.Value.BugData[bugId].BugSlots;
			return allowedSlotTypes.Contains(slotType);
		}

		public bool TryAddBug(int slot, string bugId)
		{
			if (this.CanAddBug(slot, bugId))
			{
				this.BugIds[slot] = bugId;
				if (this.Bugs[slot] is null)
					this.Bugs[slot] = new ShrineBug(bugId);
				else
					this.Bugs[slot].Init(bugId);
                this.Bugs[slot].flip = this.Definition.BugSlots[slot].Flip;
				return true;
			}
			return false;
		}

		public void RemoveBug(int slot)
		{
			this.Bugs[slot] = null;
			this.BugIds[slot] = null;
		}

        public override bool checkForAction(Farmer who, bool justCheckingForActivity = false)
        {
            if (this.Location is null)
                return false;

            if (justCheckingForActivity)
                return true;

            if (who.UniqueMultiplayerID != this.owner.Value)
                return false;

            if (ModEntry.SaveData.BugCollection.Any())
            {
                Game1.playSound(BugFurnitureMenu.OpenSound);
                BugFurnitureMenu menu = new(furniture: this);
                Game1.activeClickableMenu = menu;
            }
            else
            {
                Game1.drawDialogueNoTyping(ModEntry.I18n.Get("bugs.furniture.none"));
            }

            return true;
        }

        public override bool canBeRemoved(Farmer who)
        {
            return base.canBeRemoved(who) && !this.IsInUse.Value;
        }

        public override void performRemoveAction()
        {
            base.performRemoveAction();

            // return bugs to owner or something
        }

        public override void updateRotation()
        {
            base.updateRotation();

            // you drove me to this
            var data = ItemRegistry.GetDataOrErrorItem(this.ItemId);
            this.defaultSourceRect.Value = this.sourceRect.Value = data.GetSourceRect();
            this.defaultBoundingBox.Value = this.boundingBox.Value = new Rectangle((int)this.TileLocation.X * Game1.tileSize, (int)this.TileLocation.Y * Game1.tileSize, this.Definition.CollisionSize.X * Game1.tileSize, this.Definition.CollisionSize.Y * Game1.tileSize);

            // align bounding box to bottom of sprite when drawn
            this.drawPosition.Value = this.boundingBox.Value.Location.ToVector2() + new Vector2(0, this.Definition.CollisionSize.Y - this.Definition.SpriteSize.Y) * Game1.tileSize;
        }

        public override void updateWhenCurrentLocation(GameTime time)
		{
			foreach (var bug in this.Bugs)
				bug?.update(time, this.Location);

			base.updateWhenCurrentLocation(time);
		}

        public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1)
        {
            base.draw(spriteBatch, x, y, alpha);

            if (this.isTemporarilyInvisible || !Furniture.isDrawingLocationFurniture)
                return;

            Vector2 position = Game1.GlobalToLocal(Game1.viewport, drawPosition.Value + ((shakeTimer > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero));

			// draw bugs at slot offsets
            for (var i = 0; i < this.Bugs.Length; ++i)
				this.Bugs[i]?.DrawInWorld(
                    b: spriteBatch,
                    position: position + this.Definition.BugSlots[i].Position * Game1.pixelZoom,
                    origin: new Vector2(0.5f),
                    scale: Game1.pixelZoom,
                    layerDepth: (boundingBox.Bottom - 3 - i) / 10000f);
        }
    }
}
