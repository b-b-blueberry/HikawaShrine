using System.Collections.Generic;

namespace Hikawa.Match3
{
	/// <summary>
	/// Enum life state for each token.
	/// </summary>
	public enum TokenState
	{
		/// <summary>
		/// State of tokens available to be matched with others.
		/// </summary>
		Idle,
		/// <summary>
		/// State of tokens moving draw position. Cannot be matched.
		/// </summary>
		Motion
	}

	/// <summary>
	/// Enum type of format used when matching tokens.
	/// </summary>
	public enum MatchFormat
	{
		/// <summary>
		/// Match format for multiple tokens across either X or Y axes.
		/// </summary>
		Standard,
		/// <summary>
		/// Match format for tokens in a circle.
		/// </summary>
		Power,
		/// <summary>
		/// Match format for tokens in any position.
		/// </summary>
		Global
	}

	/// <summary>
	/// Enum type of direction used when matching tokens.
	/// </summary>
	public enum MatchDirection
	{
		Up,
		Down,
		Left,
		Right
	}

	public enum CharacterState
	{
		Idle,
		Idle2,
		Hover,
		Hurt,
		Power,
		Win
	}

	public enum EnemyState
	{
		Idle,
		Attack,
		Pain
	}

	public enum StageState
	{
		Start,
		Active,
		Pause,
		End
	}

	/// <summary>
	/// Class for Match3 menu metadata.
	/// </summary>
	public class UIData
	{
		/// <summary>
		/// Dimensions of each token when drawn.
		/// </summary>
		public Point TokenSize;
		/// <summary>
		/// Base scale for menu render.
		/// </summary>
		public float Scale;
		/// <summary>
		/// 
		/// </summary>
		public float ScoreTickRate;
		/// <summary>
		/// 
		/// </summary>
		public int ParticleCount;
		/// <summary>
		/// 
		/// </summary>
		public float ParticleFadeRate;
		/// <summary>
		/// Scale of default token.
		/// </summary>
		public float TokenScaleDefault;
		/// <summary>
		/// Scale of active token.
		/// </summary>
		public float TokenScaleActive;
		/// <summary>
		/// Scale of hovered token.
		/// </summary>
		public float TokenScaleHovered;
		/// <summary>
		/// Rate of change for token scale between token states.
		/// </summary>
		public float TokenScaleRate;
		/// <summary>
		/// Rate of change for token motion when moving position.
		/// </summary>
		public float TokenMotionRate;
		/// <summary>
		/// Max rate of token motion when moving position.
		/// </summary>
		public float TokenMotionMax;
		/// <summary>
		/// Audio cue played on token idle after motion.
		/// </summary>
		public string LandSound;
		/// <summary>
		/// 
		/// </summary>
        public string SelectSound;
        /// <summary>
        /// 
        /// </summary>
		public string SwapSound;
		/// <summary>
		/// Audio cue played on token match 3 of a kind.
		/// </summary>
		public string MatchSmallSound;
		/// <summary>
		/// Audio cue played on token match 4 of a kind.
		/// </summary>
		public string MatchMediumSound;
		/// <summary>
		/// Audio cue played on token match 5 or more.
		/// </summary>
		public string MatchLargeSound;
		/// <summary>
		/// 
		/// </summary>
		public string IntroMusic;
		/// <summary>
		/// 
		/// </summary>
		public string PauseMusic;
		/// <summary>
		/// Base scale for cursor render.
		/// </summary>
		public float CursorScale;
		/// <summary>
		/// Asset key of texture for cursor render.
		/// </summary>
		public string CursorTextureId;
		/// <summary>
		/// Area in texture asset for cursor render.
		/// </summary>
		public Rectangle CursorTextureRegion;
		/// <summary>
		/// Texture instance for cursor render.
		/// </summary>
		public Texture2D CursorTexture;
		public string MenuTextureId;
		public Texture2D MenuTexture;
		public int DialogueIgnoreInputTime;
		public Rectangle DialogueAdvanceTextureRegion;
		public Rectangle DialogueEndTextureRegion;
		public Vector2 CharacterMeterOffset;
		public Vector2 EnemyMeterOffset;
		public Rectangle CharacterLifeFillRegion;
		public Rectangle CharacterPowerFillRegion;
		public Rectangle EnemyLifeFillRegion;
		public Rectangle EnemyPowerFillRegion;
		public Rectangle CharacterLifeTextureRegion;
		public Rectangle CharacterPowerTextureRegion;
		public Rectangle CharacterPowerMinorTextureRegion;
		public Rectangle CharacterPowerMajorTextureRegion;
		public Rectangle EnemyLifeTextureRegion;
		public Rectangle EnemyPowerTextureRegion;
		public List<Rectangle> DigitRectangles;
	}

	public class GameData
	{
		/// <summary>
		/// 
		/// </summary>
		public int InitialLife;
		/// <summary>
		/// 
		/// </summary>
		public int LifeMax;
		/// <summary>
		/// 
		/// </summary>
		public int PowerMax;
		/// <summary>
		/// 
		/// </summary>
		public int[] ScorePerToken;
		/// <summary>
		/// 
		/// </summary>
		public int[] PowerPerToken;
		/// <summary>
		/// ID of initial stage.
		/// </summary>
		public string InitialStage;
	}

	/// <summary>
	/// Class for stage metadata.
	/// </summary>
	public class StageData
	{
		public string DisplayName;
		/// <summary>
		/// Number of tokens needed for a match.
		/// </summary>
		public int Match;
		/// <summary>
		/// Dimensions of game board.
		/// </summary>
		public Point GameSize;
		/// <summary>
		/// Array of token IDs used.
		/// </summary>
		public string[] Tokens;
		/// <summary>
		/// Array of token IDs added to available list after board created.
		/// </summary>
		public string[] AddedTokens;
		/// <summary>
		/// Number of standard tokens between added tokens created.
		/// </summary>
		public int AddedTokenRate;
		/// <summary>
		/// Optional name of character actor used.
		/// </summary>
		public string Character;
		/// <summary>
		/// Optional name of enemy actor used.
		/// </summary>
		public string Enemy;
		public string Music;
		public string WinMusic;
		public int ScoreGoal;
		public int TimeGoal;
		public int StartDelay;
		public int EndDelay;
		public string[] Dialogue;
		public bool NoTokenUpgrades;
		public bool ResetTokens;
		public string NextStage;
	}

	/// <summary>
	/// Class for token metadata shared between token types.
	/// </summary>
	public class TokenData
	{
		/// <summary>
		/// Character representing token in console.
		/// </summary>
		public char Char;
		/// <summary>
		/// Asset key of texture for token render.
		/// </summary>
		public string TextureId;
		/// <summary>
		/// Area in texture asset for token render.
		/// </summary>
		public Rectangle TextureRegion;
		/// <summary>
		/// Texture instance for token render.
		/// </summary>
		public Texture2D Texture;
		/// <summary>
		/// 
		/// </summary>
		public Rectangle ExplodeTextureRegion;
		/// <summary>
		/// 
		/// </summary>
		public Color ExplodeColour;
		/// <summary>
		/// 
		/// </summary>
		public float ExplodeScale;
		/// <summary>
		/// Token type this token is upgraded to in a power match.
		/// </summary>
		public string TokenUpgrade;
		/// <summary>
		/// Whether token contributes to power meter when matched.
		/// </summary>
		public bool IsPowerToken;
		/// <summary>
		/// Whether token is blocked from matching.
		/// </summary>
		public bool IsBlock;
	}

	public class ActorData
	{
		public string TextureId;
		public List<Rectangle> TextureRegions;
		public Rectangle FrameTextureRegion;
		public Texture2D Texture;
	}

	public class CharacterData : ActorData
	{
		/// <summary>
		/// ID of token affinity for this character.
		/// </summary>
		public string MatchToken;
		/// <summary>
		/// ID of token used when matching minor power.
		/// </summary>
		public string MinorToken;
		/// <summary>
		/// ID of token used when matching major power.
		/// </summary>
		public string MajorToken;
	}

	public class EnemyData : ActorData
	{
		public int LifeMax;
		public int LifeInitial;
		public int PowerMax;
		public int PowerInitial;
		public int AttackRate;
		public int AttackValue;
		public int AttackDuration;
		public float AttackScale;
		public Rectangle AttackTextureRegion;
	}

	/// <summary>
	/// Class for complete game metadata.
	/// </summary>
	public class Match3Data
	{
		/// <summary>
		/// Model of Match3 menu metadata.
		/// </summary>
		public UIData UIData;
		/// <summary>
		/// 
		/// </summary>
		public GameData GameData;
		/// <summary>
		/// Model of game type metadata.
		/// </summary>
		public Dictionary<string, StageData> StageData;
		/// <summary>
		/// Map of token types to their metadata.
		/// </summary>
		public Dictionary<string, TokenData> TokenData;
		/// <summary>
		/// 
		/// </summary>
		public Dictionary<string, CharacterData> CharacterData;
		/// <summary>
		/// 
		/// </summary>
		public Dictionary<string, EnemyData> EnemyData;
	}
}
