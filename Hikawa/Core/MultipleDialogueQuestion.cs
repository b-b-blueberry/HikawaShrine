using System.Collections.Generic;

using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Hikawa
{
	internal class MultipleDialogueQuestion : DialogueBox
	{
		private readonly IModHelper _helper;
		
		private readonly List<Response> _responses;

		public MultipleDialogueQuestion(IModHelper helper, List<string> dialogues, List<Response> responses)
			: base(dialogues)
		{
			_helper = helper;
			_responses = responses;
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			base.receiveLeftClick(x, y, playSound);
			var dialogues = _helper.Reflection.GetField<List<string>>(this, "dialogues").GetValue();
			if (dialogues.Count <= 1)
			{
				_helper.Reflection.GetField<bool>(this, "isQuestion").SetValue(true);
				_helper.Reflection.GetField<List<Response>>(this, "responses").SetValue(_responses);
				_helper.Reflection.GetMethod(this, "setUpQuestions").Invoke();
			}
		}
	}
}