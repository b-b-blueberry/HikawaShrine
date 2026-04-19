using System;
using System.Collections.Generic;

namespace Hikawa.Match3
{
	public class Stage
	{
		public string Id;
		public StageData Data;
		public StageState State
		{
			get => this._state;
			set
			{
				this.DialogueIndex = 0;
				this.OnStateChanged?.Invoke(stage: this, current: this._state, next: value);
				this._state = value;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		public Enemy Enemy;
		/// <summary>
		/// Number of successful player moves made this stage.
		/// </summary>
		public int Moves;
		/// <summary>
        /// Number of player powers used this stage.
        /// </summary>
        public int Powers;
        /// <summary>
        /// Number of player super powers used this stage.
        /// </summary>
        public int SuperPowers;
        /// <summary>
        /// Number of matches made this stage.
        /// </summary>
        public int Matches;
        /// <summary>
        /// Number of power matches made this stage.
        /// </summary>
        public int PowerMatches;
        /// <summary>
        /// Number of super power matches made this stage.
        /// </summary>
        public int SuperPowerMatches;
        /// <summary>
		/// Timer of active stage, excluding time paused.
		/// </summary>
		public long Time;
		/// <summary>
		/// Score gained in active stage, excluding score from other stages.
		/// </summary>
		public long Score;
		/// <summary>
		/// Whether player has passed win conditions.
		/// </summary>
		public bool IsWon;
		public int DialogueIndex;
		public int DialogueTextIndex;
		public string DialogueText;
		public Character DialogueCharacter;

		public delegate void StateChanged(Stage stage, StageState current, StageState next);
		public event StateChanged OnStateChanged;

		protected StageState _state;

		protected readonly Dictionary<string, CharacterData> _characterData;

		public Stage(Dictionary<string, CharacterData> characterData)
		{
			this._characterData = characterData;
		}

		public Stage Set(string name, StageData data, StageState state)
		{
			// Always set state last throughout, ensures correct values on state changed listeners

			this.Time = 0;
			this.Score = 0;

			this.Id = name;
			this.Data = data;
			this.State = state;

			this.DialogueText = string.Empty;
			this.AdvanceOrEndDialogue(initial: true);

			return this;
		}

		public bool OnTick(int ms)
		{
			if (this.State is StageState.Pause)
				return true;

			this.Time += ms;

			// Enemy
			if (this.Enemy is Enemy enemy && enemy.Data is not null)
			{
				if (enemy.State is EnemyState.Attack)
				{
					enemy.AttackDrawTimer -= ms;
					if (enemy.AttackDrawTimer <= 0)
					{
						enemy.AttackDrawTimer = 0;
						enemy.State = EnemyState.Idle;
					}
				}
			}

			bool hasDialogue = this.HasRemainingDialogue();

			// Dialogue
			if (hasDialogue)
			{
				if (this.DialogueTextIndex < this.DialogueText.Length)
				{
					++this.DialogueTextIndex;
				}
			}

			// State
			if (this.State is StageState.Start)
			{
				if (this.Time >= this.Data.StartDelay && !hasDialogue)
				{
					this.Time = 0;
					this.State = StageState.Active;
				}
			}
			else if (this.State is StageState.Active)
			{
				if (this.IsReadyToEnd())
				{
					this.IsWon = this.CheckIfWon();
					this.State = StageState.End;
					this.Time = 0;
				}
			}
			else if (this.State is StageState.End)
			{
				if (this.Time >= this.Data.EndDelay)
				{
					return false;
				}
			}
			return true;
		}

		public bool IsReadyToEnd()
		{
			return this.State is StageState.Active && (this.CheckIfWon() || this.CheckIfLost());
		}

		public bool CheckIfWon()
		{
			bool isOverScore = this.Data.ScoreGoal > 0 && this.Score >= this.Data.ScoreGoal;
			return isOverScore;
		}

		public bool CheckIfLost()
		{
			bool isOverTime = this.Data.TimeGoal > 0 && this.Time >= this.Data.TimeGoal;
			bool isUnderScore = this.Data.ScoreGoal <= 0 || this.Score < this.Data.ScoreGoal;
			return isOverTime && isUnderScore;
		}

		public bool HasDialogue()
		{
			return this.Data.Dialogue?.Length > 0;
		}

		public bool HasRemainingDialogue()
		{
			return this.DialogueIndex < this.Data.Dialogue?.Length;
		}

		public void AdvanceOrEndDialogue(bool initial = false)
		{
			this.DialogueTextIndex = 0;

			if (!initial)
				++this.DialogueIndex;
			if (this.HasRemainingDialogue())
			{
				string[] data = this.Data.Dialogue[this.DialogueIndex].Split(':', 3, StringSplitOptions.TrimEntries);
				string name = data[0];
				string text = data[2];
				CharacterState state = (CharacterState)int.Parse(data[1]);

				this.DialogueText = text;
				this.DialogueCharacter.Set(
					name: name,
					data: this._characterData[name],
					state: state);
			}
			else
			{
				this.Time = 0;
				this.DialogueText = string.Empty;
			}
		}
	}
}
