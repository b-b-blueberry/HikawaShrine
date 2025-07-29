using Hikawa.Data;
using Hikawa.Objects.Critters;
using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Hikawa.Objects.Items
{
	public enum BugSlot
	{
		Ground,
		Perch,
		Flying
	}

	[XmlType($"{ModConsts.SpaceCoreXmlPrefix}{nameof(BugFurniture)}")] // SpaceCore serialisation signature
	public class BugFurniture : Furniture
	{
		[XmlIgnore]
		public BugFurnitureDataEntry Definition;

		[XmlIgnore]
		public List<ShrineBug> Bugs;

		public List<string> BugIds;

		public BugFurniture() : base()
		{
			this.Bugs ??= [];
		}

		public BugFurniture(ParsedItemData data)
			: base()
		{
			this.Definition = data.RawData as BugFurnitureDataEntry;
		}

		public bool CanAddBug(string bugId)
		{
			var definition = ItemRegistry.GetData(this.ItemId).RawData as BugFurnitureDataEntry;
			BugSlot slot = ModEntry.BugsData.Value.BugData[bugId].BugSlot;
			int initial = definition.BugSlots.Count(c => c == slot);
			int used = this.Bugs.Count(bug => bug.Definition.BugSlot == slot);
			return initial - used > 0 && !this.Bugs.Any(bug => bug.BugId == bugId);
		}

		public bool TryAddBug(string bugId)
		{
			if (this.CanAddBug(bugId))
			{
				ShrineBug bug = new();
				bug.Init(bugId, ModEntry.BugsData.Value.BugData[bugId]);
				this.Bugs.Add(bug);
				this.BugIds.Add(bugId);
				return true;
			}
			return false;
		}

		public void RemoveBug(string bugId)
		{
			this.Bugs.RemoveWhere(bug => bug.BugId == bugId);
			this.BugIds.Remove(bugId);
		}

		public override void updateWhenCurrentLocation(GameTime time)
		{
			foreach (var bug in this.Bugs)
				bug?.update(time, this.Location);

			base.updateWhenCurrentLocation(time);
		}

		// override draw
	}
}
