using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Menus;

namespace Hikawa.Volleyball
{
	internal class VolleyballClientDialogueBox : ConfirmationDialog
	{
		public const string CheckID = ModConsts.ContentPrefix + "VolleyballCheck";

		public VolleyballClientDialogueBox(behavior onConfirm, behavior onCancel) : base(
				  message: ModEntry.I18n.Get("ui.volleyball.client.wait"),
				  onConfirm: onConfirm,
				  onCancel: onCancel)
		{
			this.okButton.visible = false;
			this.cancelButton.visible = false;
			this.exitFunction = delegate
			{
				this.closeDialog(Game1.player);
			};
		}

		public override void update(GameTime time)
		{
			base.update(time);

			//Game1.player.team.SetLocalReady(VolleyballClientDialogueBox.CheckID, ready: true);
			//if (Game1.player.team.IsReady(VolleyballClientDialogueBox.CheckID))
			{
				this.confirm();
			}
		}
	}
}
