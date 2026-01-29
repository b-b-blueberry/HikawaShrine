using StardewValley.Objects.Trinkets;

namespace Hikawa.Objects.Trinkets
{
    public class CrowTrinketEffect : TrinketEffect
    {
        public CrowTrinketEffect(Trinket trinket)
            : base(trinket)
        {
        }

        public override void Apply(Farmer farmer)
        {
            this.Companion = new CrowCompanion();
            if (Game1.gameMode == Game1.playingGameMode)
            {
                farmer.AddCompanion(this.Companion);
            }
        }
    }
}
