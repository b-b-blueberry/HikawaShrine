using Hikawa.Objects.Decor;
using StardewValley.Tools;
using System.Xml.Serialization;
using static StardewValley.FarmerSprite;

namespace Hikawa.Objects.Items
{
	[XmlType($"{ModConsts.SpaceCoreXmlPrefix}{nameof(ShrubTool)}")] // SpaceCore serialisation signature
	public class ShrubTool : Tool
	{
		public float StaminaCost => 2f;

		public ShrubTool()
			: base()
		{
			this.Name = ModEntry.ModData.ItemShrubTool;
			this.InstantUse = false;
		}

		protected override Item GetOneNew()
		{
			return new ShrubTool();
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
		}

		public override void DoFunction(GameLocation location, int x, int y, int power, Farmer who)
		{
			base.DoFunction(location, x, y, power, who);

			if (this.TrimShrub(who: who, where: location, x: x, y: y))
			{
				who.Stamina -= this.StaminaCost;
			}

			//Shears.playSnip(who);
			Farmer.canMoveNow(this.lastUser);
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
				new(frames[1], ms, secondaryArm: false, flip: flip, Shears.playSnip),
				new(frames[2], ms, secondaryArm: false, flip: flip),
				new(frames[3], ms, secondaryArm: false, flip: flip, Farmer.useTool)
			];
			who.FarmerSprite.animateOnce(animation: animation);
			who.FarmerSprite.PauseForSingleAnimation = true;
			who.UsingTool = true;
			who.canReleaseTool = false;
		}

		public bool TrimShrub(Farmer who, GameLocation where, int x, int y)
		{
			Point tile = Vector2.Floor(new Vector2(x, y) / Game1.tileSize).ToPoint();
			if (where.getLargeTerrainFeatureAt(tile.X, tile.Y) is Shrub shrub)
			{
				shrub.Trim();
				return true;
			}
			return false;
		}
	}
}
