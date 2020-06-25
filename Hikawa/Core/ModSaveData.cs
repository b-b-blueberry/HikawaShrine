namespace Hikawa
{
	class ModSaveData
	{
		public bool AtlantisInterlude { get; set; }
		public ModEntry.Buffs LastShrineBuffId { get; set; }
		public bool AwaitingShrineBuff { get; set; }
		public int ShrineBuffCooldown { get; set; }
		public int StoryStock { get; set; }
		public int StoryPlant { get; set; }
		public int StoryMist { get; set; }
		public int StoryDoors { get; set; }
		public int StoryGap { get; set; }
		public int StoryTower { get; set; }
		public int BananaBunch { get; set; }
		public int BananaRepublic { get; set; }
	}
}
