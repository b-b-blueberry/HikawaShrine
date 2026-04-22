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
	/// Enum type of additional effect on token matched.
	/// </summary>
	public enum MatchEffect
	{
		/// <summary>
		/// No added effects.
		/// </summary>
		Standard,
		/// <summary>
		/// Surrounding tokens will be matched.
		/// </summary>
		Radial,
		/// <summary>
		/// Adjacent tokens will be matched.
		/// </summary>
		Linear,
		/// <summary>
		/// All tokens will be matched.
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
		Win,
		Danger
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
    /// Class for Match3 audio metadata.
    /// </summary>
    public class AudioData
    {
        /// <summary>Token motion ended.</summary>
        public string LandSound;
        /// <summary>Token set as active.</summary>
        public string SelectSound;
        /// <summary>Played on swap without match.</summary>
        public string SwapSound;
        /// <summary>Played on any token matched.</summary>
        public string MatchSound;
        /// <summary>Power token matched.</summary>
		public string PowerMatchSound;
        /// <summary>Super power token matched.</summary>
		public string SuperPowerMatchSound;
        /// <summary>Token upgraded.</summary>
		public string UpgradeSound;
        /// <summary>Token super upgraded.</summary>
		public string SuperUpgradeSound;
        /// <summary>Main menu and submenus.</summary>
        public string MainMenuMusic;
        /// <summary>Stage intro cutscene and countdown.</summary>
        public string IntroMusic;
        /// <summary>Stage in progress paused.</summary>
        public string PauseMusic;
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
		public bool NoSpecialPowers;
		public bool NoTokenUpgrades;
		public string IntroCutsceneId;
		public string OutroCutsceneId;
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
		/// Token can match with any other tokens with an equal <see cref="MatchGroup"/> value.
		/// </summary>
		public string MatchGroup;
        /// <summary>
		/// Additional matches made when matched.
		/// </summary>
		public MatchEffect MatchEffect;
        /// <summary>
        /// Radius of additional matches. Defaults to largest possible size.
        /// </summary>
        public int MatchEffectRadius;
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

	public class WorldData
	{
		public Dictionary<string, StoryData> Stories;
    }

    public class StoryData
    {
        /// <summary>
        /// Cue ID of music played in story map.
        /// </summary>
        public string Music;
        /// <summary>
        /// Asset name of texture used for world sprites.
        /// </summary>
        public string TextureId;
        /// <summary>
        /// ID of cutscene played on story selected if no progress is found.
        /// </summary>
        public string CutsceneId;
        /// <summary>
        /// Duration in ms for each frame in <see cref="BackgroundTextureRegion"/>.
        /// </summary>
		public int BackgroundFrameTime;
        /// <summary>
        /// Area in <see cref="StoryData.Texture"/> used to draw world background for story in <see cref="Match3StoryMenu"/>.
        /// </summary>
        public List<Rectangle> BackgroundTextureRegion;
        /// <summary>
        /// Area in <see cref="StoryData.Texture"/> used to draw stage marker in <see cref="Match3StoryMenu"/>.
        /// </summary>
        public Rectangle StageTextureRegion;
        /// <summary>
        /// Area in <see cref="StoryData.Texture"/> used to draw stage marker in <see cref="Match3StoryMenu"/>.
        /// </summary>
        public Rectangle StageCompleteTextureRegion;
        /// <summary>
        /// Map of stage IDs to their respective data used for world progress.
        /// </summary>
        public Dictionary<string, StoryStageData> Stages;
		/// <summary>
		/// Asset instance loaded from <see cref="TextureId"/>.
		/// </summary>
		public Texture2D Texture;
    }

    public class StoryStageData
    {
		/// <summary>
		/// Stage ID that needs completing before this stage is available in the world.
		/// </summary>
        public List<string> UnlockedBy;
		/// <summary>
		/// Stage ID played immediately after this stage is completed, without returning to the world.
		/// </summary>
        public string NextStage;
        /// <summary>
        /// Unscaled pixel offset relative to the centre of the world.
        /// </summary>
        public Point Position;
    }

	public class CutsceneData
    {
        /// <summary>
        /// Cue ID of music played in cutscene.
        /// </summary>
        public string Music;
        /// <summary>
        /// Asset name of texture used for cutscene sprites.
        /// </summary>
		public string TextureId;
        /// <summary>
        /// List of on-screen elements for this cutscene.
		/// Outer lists are shown in sequence, iterated on player input.
		/// Inner lists are shown altogether.
        /// </summary>
		public List<List<CutsceneItemData>> Items;
        /// <summary>
        /// Asset instance loaded from <see cref="TextureId"/>.
        /// </summary>
        public Texture2D Texture;
    }

    public class CutsceneItemData
    {
        /// <summary>
        /// Display text.
        /// </summary>
        public string Text;
		public string TextColor;
        /// <summary>
        /// 
        /// </summary>
        public Rectangle TextureRegion;
        /// <summary>
        /// Unscaled pixel offset relative to the centre of the world.
        /// </summary>
        public Point Position;
    }

	/// <summary>
	/// Class for complete game metadata.
	/// </summary>
	public class Match3Data
	{
        /// <summary>
        /// Model of Match3 audio metadata.
        /// </summary>
        public AudioData AudioData;
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
        /// <summary>
        /// 
        /// </summary>
        public WorldData WorldData;
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, CutsceneData> CutsceneData;
	}
}
