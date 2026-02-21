using Hikawa.Objects.Locations;
using StardewModdingAPI.Utilities;
using StardewValley.Delegates;
using StardewValley.Extensions;
using StardewValley.Menus;
using StardewValley.Triggers;
using System;
using System.Text.RegularExpressions;

namespace Hikawa.Modules
{
	public static class DialogueEffects
	{
		public class DialogueEffectsState
		{
			public bool IsDialogue;

			public bool IsWave;
			public float WaveScale;
			public float WaveRate;

			public bool IsShake;
			public float ShakeScale;
			public Vector2 ShakeOffset;
		}

		public static PerScreen<DialogueEffectsState> State { get; internal set; }

		private static DialogueEffectsState _state => DialogueEffects.State.Value;

		public static void Init()
		{
			TriggerActionManager.RegisterAction(
				name: ModEntry.ModData.TriggerDialogueEffects,
				action: delegate(string[] args, TriggerActionContext context, out string error)
				{
					error = null;
					try
					{
						DialogueEffects.SetFromTriggerAction(args);
					}
					catch (Exception e)
					{
						error = e.ToString();
					}
					return error is null;
				});
		}

		public static void SetFromTriggerAction(string[] args)
		{
			_state.IsWave = false;
			_state.IsShake = false;

			if (args.Length < 2)
				return;
			else if (args[1] == "wave")
			{
				_state.IsWave = true;
				_state.WaveScale = args.Length > 2 && float.TryParse(args[2], out float scale) ? scale : 4f;
				_state.WaveRate = args.Length > 3 && float.TryParse(args[3], out float rate) ? rate : 1f;
			}
			else if (args[1] == "shake")
			{
				_state.IsShake = true;
				_state.ShakeScale = args.Length > 2 && float.TryParse(args[2], out float scale) ? scale : 4f;
				_state.ShakeOffset = Vector2.Zero;
			}
			else if (args[1] == "sound" && args.Length > 2)
			{
				Game1.playSound(string.Join(string.Empty, args[2..]));
			}
			else if (args[1] == "exit")
			{
				if (Game1.activeClickableMenu is DialogueBox db)
				{
					db.closeDialogue();
				}
			}
			else if (args[1] == "shop")
			{
				if (Game1.activeClickableMenu is DialogueBox db)
				{
					if (Game1.currentLocation is Shrine shrine && shrine.GetShopPerson() is NPC npc)
					{
						Game1.afterDialogues += () => Utility.TryOpenShopMenu(ModEntry.ModData.ShopShrineRei, npc.Name);
					}
					db.closeDialogue();
				}
			}
            else if (args[1] == "dialogueAnswered")
            {
                for (int i = 2; i < args.Length; i += 2)
                    if (ArgUtility.TryGet(args, i, out string key, out string error) && ArgUtility.TryGetBool(args, i + 1, out bool value, out error))
                        Game1.player.dialogueQuestionsAnswered.Toggle(key, value);
            }
            else if (args[1] == "forgetDialogueAnswersStartingWith")
            {
                if (ArgUtility.TryGet(args, 2, out string prefix, out string error))
                    Game1.player.dialogueQuestionsAnswered.RemoveWhere(s => s.StartsWith(prefix));
            }
		}

		public static void SetFromDialogue(Dialogue d)
		{
			// TODO: Fix states reset on new dialogues loaded arbitrarily for unrelated characters

			_state.IsWave = false;
			_state.IsShake = false;

			if (d is not null && d.currentDialogueIndex < d.dialogues.Count)
			{
				DialogueLine line = d.dialogues[d.currentDialogueIndex];
				RegexOptions o = RegexOptions.IgnoreCase;
				Regex r;

				r = new(pattern: @"^\(wave.*\)", options: o);
				if (r.Match(line.Text) is Match wave && wave.Success)
				{
					line.Text = r.Replace(line.Text, string.Empty);
					string raw = Regex.Replace(
						input: wave.Value,
						pattern: @"[^\d\.\s]",
						replacement: string.Empty,
						options: o);
					if (ArgUtility.SplitBySpace(raw) is string[] values)
					{
						_state.IsWave = true;
						_state.WaveScale = values.Length > 0 && float.TryParse(values[0], out float scale) ? scale : 4f;
						_state.WaveRate = values.Length > 1 && float.TryParse(values[1], out float rate) ? rate : 1f;
					}
				}

				r = new Regex(pattern: @"^\(shake.*\)", options: o);
				if (r.Match(line.Text) is Match shake && shake.Success)
				{
					line.Text = r.Replace(line.Text, string.Empty);
					string raw = Regex.Replace(
						input: shake.Value,
						pattern: @"[^\d\.\s]",
						replacement: string.Empty,
						options: o);
					if (ArgUtility.SplitBySpace(raw) is string[] values)
					{
						_state.IsShake = true;
						_state.ShakeScale = values.Length > 0 && float.TryParse(values[0], out float scale) ? scale : 4f;
						_state.ShakeOffset = Vector2.Zero;
					}
				}
			}
		}

		public static void Update(uint ticks)
		{
			if (_state.IsShake && ticks % 3 == 0)
			{
				_state.ShakeOffset = new Vector2(_state.ShakeScale)
					* new Vector2((float)Game1.random.NextDouble(), (float)Game1.random.NextDouble());
			}
		}

		public static void ApplyStringOffset(ref Vector2 v, string text)
		{
			if (_state.IsDialogue && _state.IsShake)
				v += _state.ShakeOffset;
		}

		public static void ApplyCharOffset(ref Vector2 v, int i)
		{
			if (_state.IsDialogue && _state.IsWave)
				v += new Vector2(0f, _state.WaveScale
					* MathF.Cos((float)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 200f * _state.WaveRate + i)));
		}
	}
}
