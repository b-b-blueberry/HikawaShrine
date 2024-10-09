using StardewValley;

namespace Hikawa.Objects.Locations
{
	public class Summer : GameLocation
	{
		public override bool HasLocationOverrideDialogue(NPC character)
		{
			return false;
		}

		public override string GetLocationOverrideDialogue(NPC character)
		{
			return base.GetLocationOverrideDialogue(character);
		}

		public override bool answerDialogue(Response answer)
		{
			return base.answerDialogue(answer);
		}

	}
}
