using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;

namespace Hikawa.Modules
{
    internal class MultipleDialogueQuestion : DialogueBox
    {
        private readonly List<Response> CustomResponses;

        public MultipleDialogueQuestion(List<string> dialogues, List<Response> responses)
            : base(dialogues)
        {
            CustomResponses = responses;
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);
            if (dialogues.Count <= 1)
            {
                isQuestion = true;
                responses = CustomResponses.ToArray();
                ModEntry.Instance.Helper.Reflection.GetMethod(this, "setUpQuestions").Invoke();
            }
        }
    }
}