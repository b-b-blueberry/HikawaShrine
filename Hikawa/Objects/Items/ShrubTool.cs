using Hikawa.Objects.Items.Data;
using StardewValley.Tools;
using System.Xml.Serialization;
using static StardewValley.FarmerSprite;

namespace Hikawa.Objects.Items
{
	[XmlType($"{ModConsts.SpaceCoreXmlPrefix}{nameof(ShrubTool)}")] // SpaceCore serialisation signature
	public class ShrubTool : Tool
	{
        public override string TypeDefinitionId => ShrubToolItemDataDefinition.TypeDefinitionId;
        
		public ShrubTool() : this(null)
		{
		}

		public ShrubTool(string id)
        {
            this.ItemId = id;

            var data = ItemRegistry.GetDataOrErrorItem(this.TypeDefinitionId + this.ItemId);

            this.Name = data.InternalName;
            this.InitialParentTileIndex = data.SpriteIndex;
            this.CurrentParentTileIndex = data.SpriteIndex;
            this.IndexOfMenuItemView = data.SpriteIndex;
            this.AttachmentSlotsCount = 0;
            this.PlayUseSounds = false;
        }

        protected override Item GetOneNew()
        {
            return new ShrubTool(ModEntry.ShrubsData.Value.ShrubToolData.ItemId);
        }

        public override bool beginUsing(GameLocation location, int x, int y, Farmer who)
		{
			x = (int)who.GetToolLocation().X;
			y = (int)who.GetToolLocation().Y;
			this.PlayAnimation(who);
			return true;
		}

		public override void endUsing(GameLocation location, Farmer who)
		{
			base.endUsing(location, who);

            Farmer.canMoveNow(this.lastUser);
        }

		public override void DoFunction(GameLocation location, int x, int y, int power, Farmer who)
		{
			base.DoFunction(location, x, y, power, who);

			var tile = Vector2.Floor(new Vector2(x, y) / Game1.tileSize);

			location.performToolAction(this, (int)tile.X, (int)tile.Y);
            if (location.terrainFeatures.TryGetValue(tile, out var terrainFeature))
            {
                terrainFeature.performToolAction(this, 0, tile);
            }

            Shears.playSnip(who);
        }

		public void PlayAnimation(Farmer who)
		{
			who.Halt();
			who.FarmerSprite.oldFrame = who.FarmerSprite.CurrentFrame;

			int ms = 200;
			bool flip = who.FacingDirection == Game1.left;
			int[] frames = who.FacingDirection switch
			{
				Game1.up => [82, 83, 82, 83],
				Game1.down => [78, 79, 78, 79],
				_ => [80, 81, 80, 81],
			};
			AnimationFrame[] animation = [
				new(frames[0], ms, secondaryArm: false, flip: flip),
				new(frames[1], ms, secondaryArm: false, flip: flip, Farmer.useTool),
				new(frames[2], ms, secondaryArm: false, flip: flip),
				new(frames[3], ms, secondaryArm: false, flip: flip, (farmer) => this.endUsing(who.currentLocation, who))
			];
			who.animateInFacingDirection(Game1.currentGameTime);
			who.FarmerSprite.animateOnce(animation: animation);
			who.FarmerSprite.PauseForSingleAnimation = true;
			who.UsingTool = true;
			who.canReleaseTool = false;
		}
	}
}
