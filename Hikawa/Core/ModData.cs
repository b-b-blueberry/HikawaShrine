using System.Collections.Generic;
using Hikawa.GameObjects.Menus;

namespace Hikawa
{
	class ModData
	{
		public enum Chapter
		{
			None,
			Stock,
			Plant,
			Mist,
			Doors,
			Vortex,
			Tower,
			End
		}
		public enum Progress
		{
			None,
			Started,
			Stage1,
			Stage2,
			Stage3,
			Complete
		}

		public Dictionary<Chapter, Progress> Story = new Dictionary<Chapter, Progress>
		{
			{ Chapter.Stock, Progress.None },
			{ Chapter.Plant, Progress.None },
			{ Chapter.Mist, Progress.None },
			{ Chapter.Doors, Progress.None },
			{ Chapter.Vortex, Progress.None },
			{ Chapter.Tower, Progress.None },
			{ Chapter.End, Progress.None }
		};

		public bool Interlude { get; set; }
		public ModEntry.Buffs LastShrineBuffId { get; set; }
		public bool AwaitingShrineBuff { get; set; }
		public int ShrineBuffCooldown { get; set; }
		public int BananaBunch { get; set; }
		public int BananaRepublic { get; set; }
		public bool HasCheckedBundlesThisSeason { get; set; }
		public int SpecialGiftSentToNpc { get; set; }
		public List<EmaMenu.Bundle> BundlesThisSeason { get; set; }
	}
}
