using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hikawa.Match3
{
	/// <summary>
	/// Class for tokens in play on board.
	/// </summary>
	public class Token
	{
		public TokenState State;
		public string Type;
		public TokenData TypeData;

		/// <summary>
		/// Current additional scale for token render.
		/// </summary>
		public float Scale;
		/// <summary>
		/// Current rate of draw pixel change per tick.
		/// </summary>
		public float Acceleration;
		/// <summary>
		/// 
		/// </summary>
		public Vector2 DrawPixel;

		public Token(TokenState state, string type, TokenData data)
		{
			this.Set(state: state, type: type, data: data);
		}

		/// <summary>
		/// Updates token state and type.
		/// </summary>
		public void Set(TokenState state, string type, TokenData data)
		{
			this.State = state;
			this.Type = type;
			this.TypeData = data;
		}

		/// <summary>
		/// Checks whether this token can match with another.
		/// </summary>
		public bool Matches(Token other)
		{
			return other is not null
				&& this.State is TokenState.Idle
				&& this.State == other.State
				&& (this.Type == other.Type || this.TypeData.TokenUpgrade == other.Type || this.Type == other.TypeData.TokenUpgrade)
				&& !(this.TypeData.IsBlock || other.TypeData.IsBlock);
		}
	}

	/// <summary>
	/// Match3 main game class.
	/// </summary>
	public class Match3Game
	{
		/// <summary>
		/// Random number gen for active game.
		/// </summary>
		public Random Random;
		/// <summary>
		/// Model of complete game data.
		/// </summary>
		public Match3Data Data;
		/// <summary>
		/// Active game board for tokens.
		/// </summary>
		public Token[][] Tokens;
		/// <summary>
		/// Array of token IDs used in active game.
		/// </summary>
		public string[] TokenIDs;
		/// <summary>
		/// 
		/// </summary>
		public int AddedTokenCountdown;
		/// <summary>
		/// 
		/// </summary>
		public Stage Stage;
		/// <summary>
		/// Timer of active game, excluding time paused.
		/// </summary>
		public long TotalTime;
		/// <summary>
		/// 
		/// </summary>
		public long TotalScore;
		/// <summary>
		/// Player health meter value.
		/// </summary>
		public int Life;
		/// <summary>
		/// Player power meter value.
		/// </summary>
		public int Power;
		/// <summary>
		/// 
		/// </summary>
		public Character Character;

		public bool IsPaused => this.Stage?.State is not StageState.Active;

		public delegate void TokensCreated();
		public delegate void DamageTaken(int value);
		public event TokensCreated OnTokensCreated;
		public event DamageTaken OnDamageTaken;
		public event Stage.StateChanged OnStageStateChanged;

		/// <summary>
		/// Constructor for a prepared game.
		/// Populates game board with random tokens.
		/// </summary>
		public Match3Game(Match3Data data, Random random)
		{
			this.Data = data;
			this.Random = random;

			this.SetUpGame(stage: this.Data.GameData.InitialStage);
		}

		/// <summary>
		/// Populates game values for given game type data.
		/// </summary>
		public void SetUpGame(string stage = null, bool resetTokens = false, StageState state = StageState.Start)
		{
			// Player stats
			this.TotalScore += this.Stage?.Score ?? 0;
			if (this.Life <= 0)
			{
				this.Life = this.Data.GameData.InitialLife;
			}

			// Stage
			stage ??= this.Stage.Name;
			if (this.Stage is null)
			{
				this.Stage = new(characterData: this.Data.CharacterData);
				this.Stage.OnStateChanged += (StageState previous, StageState next) => this.OnStageStateChanged?.Invoke(previous: previous, next: next);
			}
			this.Stage.DialogueCharacter ??= new();
			this.Stage.Set(name: stage, data: this.Data.StageData[stage], state: state);

			// Character
			this.Character ??= new();
			if (this.Stage.Data.Character is string chara)
			{
				this.Character.Set(
					name: chara,
					data: this.Data.CharacterData[chara],
					state: CharacterState.Idle);
			}
			else
			{
				this.Character.Reset();
			}

			// Enemy
			this.Stage.Enemy ??= new();
			if (this.Stage.Data.Enemy is string enemy)
			{
				this.Stage.Enemy.Set(
					name: enemy,
					data: this.Data.EnemyData[enemy],
					state: EnemyState.Idle);
			}
			else
			{
				this.Stage.Enemy.Reset();
			}

			// Tokens
			this.TokenIDs = this.Data.TokenData.Keys.Where(this.Stage.Data.Tokens.Contains).ToArray();
			if (this.Tokens is null || this.Stage.Data.ResetTokens || resetTokens)
			{
				this.Tokens = this.CreateBoardWithTokens();
				this.OnTokensCreated?.Invoke();
			}
		}

		/// <summary>
		/// Creates a game board of the given size, populated with random tokens.
		/// </summary>
		public Token[][] CreateBoardWithTokens()
		{
			Point size = this.Stage.Data.GameSize;
			Token[][] tokens = new Token[size.X][];
			int min = this.Stage.Data.Match - 1;
			for (int x = 0; x < size.X; ++x)
			{
				tokens[x] = new Token[size.Y];
				for (int y = 0; y < size.Y; ++y)
				{
					while (tokens[x][y] is null)
					{
						Token token = this.CreateToken();
						if (x >= min && tokens[(x - min)..(x - 1)].All((Token[] row) => token.Matches(row[y])))
							continue;
						if (y >= min && tokens[x][(y - min)..(y - 1)].All(token.Matches))
							continue;
						tokens[x][y] = token;
					}
				}
			}
			return tokens;
		}

		/// <summary>
		/// Creates a game token of a given type, or random type if none is provided.
		/// </summary>
		public Token CreateToken(string type = null)
		{
			type ??= this.GetRandomTokenType();
			TokenState state = TokenState.Idle;
			return new Token(state: state, type: type, data: this.Data.TokenData[type]);
		}

		/// <summary>
		/// Swaps a pair of tokens on game board.
		/// </summary>
		public void SwapTokens(Point a, Point b)
		{
			(this.Tokens[b.X][b.Y], this.Tokens[a.X][a.Y]) = (this.Tokens[a.X][a.Y], this.Tokens[b.X][b.Y]);
		}

		/// <summary>
		/// Finds all tokens matching the given token and format.
		/// </summary>
		public List<Point> CheckMatches(Point position)
		{
			Point size = this.Stage.Data.GameSize;
			Token token = this.Tokens[position.X][position.Y];
			if (token is null)
				return [];

			// Tokens match adjacently, power tokens match radially
			MatchFormat format = token.TypeData.IsPowerToken ? MatchFormat.Power : MatchFormat.Standard;

			// ABSOLUTELY FOOLPROOF BTW ALWAYS WORKS
			List<Point> getAdjacentMatches(int include, int length, int min, Func<int, Token> tokenise, Func<int, int, Point> matchise)
			{
				int start = -1;
				List<Point> matches = [];

				// Traverse row/column
				for (int i = 0; i < length; ++i)
				{
					// Continue until tokens no longer match or until end of row/column
					Token other = tokenise(i);
					if (token.Matches(other))
					{
						if (i < length - 1)
							continue;
						else
							++i;
					}
						
					// Check if match range covered minimum count
					if (i - start > min)
					{
						// Check if match range included token
						if (start <= include && include <= i)
						{
							// Save matches and break
							for (int j = start + 1; j < i; ++j)
								matches.Add(matchise(start, j));
							break;
						}
					}
					else
					{
						// Reset match range and continue
						start = i;
					}
				}
				return matches;
			}

			List<Point> matches = [];
			switch (format)
			{
				case MatchFormat.Standard:
				{
					List<Point> horizontal = getAdjacentMatches(
						include: position.X,
						length: size.X,
						min: this.Stage.Data.Match,
						tokenise: (int x) => this.Tokens[x][position.Y],
						matchise: (int start, int x) => new(x: x, y: position.Y));
					List<Point> vertical = getAdjacentMatches(
						include: position.Y,
						length: size.Y,
						min: this.Stage.Data.Match,
						tokenise: (int y) => this.Tokens[position.X][y],
						matchise: (int start, int y) => new(x: position.X, y: y));
					matches.AddRange(horizontal);
					matches.AddRange(vertical);
					break;
				}
				case MatchFormat.Power:
				{
					int radius = 3;
					List<Point> radial = [];
					for (int x = position.X - radius; x < position.X + radius; ++x)
					{
						for (int y = position.Y - radius; y < position.Y + radius; ++y)
						{
							if (x >= 0
								&& y >= 0
								&& x < size.X
								&& y < size.Y
								&& this.Tokens[x][y] is Token other
								&& token.Matches(other))
							{
								radial.Add(new(x: x, y: y));
							}
						}
					}
					if (radial.Count >= this.Stage.Data.Match)
					{
						// Match tokens
						matches.AddRange(radial);

						// Additionally match the lowest block on the board
						for (int x = size.X - 1; x > 0 + radius; --x)
						{
							for (int y = size.Y - 1; y > 0; --y)
							{
								if (this.Tokens[x][y] is Token other
									&& other.TypeData.IsBlock)
								{
									matches.Add(new(x: x, y: y));
								}
							}
						}
					}
					break;
				}
				case MatchFormat.Global:
				{
					for (int x = 0; x < size.X; ++x)
					{
						for (int y = 0; y < size.Y; ++y)
						{
							if (token.Matches(this.Tokens[x][y]))
							{
								matches.Add(new(x: x, y: y));
							}
						}
					}
					break;
				}
			}

			return matches;
		}

		public Point? GetAdjacentToken(Point position, MatchDirection direction)
		{
			Point size = this.Stage.Data.GameSize;
			Point? other = null;
			switch (direction)
			{
				case MatchDirection.Up:
					if (position.Y > 0)
					{
						other = new(x: position.X, y: position.Y - 1);
					}
					break;
				case MatchDirection.Down:
					if (position.Y < size.Y - 1)
					{
						other = new(x: position.X, y: position.Y + 1);
					}
					break;
				case MatchDirection.Left:
					if (position.X > 0)
					{
						other = new(x: position.X - 1, y: position.Y);
					}
					break;
				case MatchDirection.Right:
					if (position.X < size.X - 1)
					{
						other = new(x: position.X + 1, y: position.Y);
					}
					break;
			}
			return other;
		}

		public string GetRandomTokenType()
		{
			string[] ids = this.TokenIDs;
			if (this.Stage.Time > 0
				&& this.Stage.Data.AddedTokens?.Length > 0
				&& this.Stage.Data.AddedTokenRate > 0)
			{
				this.AddedTokenCountdown = ++this.AddedTokenCountdown % this.Stage.Data.AddedTokenRate;
				if (this.AddedTokenCountdown == 0)
					ids = this.Stage.Data.AddedTokens;
			}
			int i = (int)Math.Floor(this.Random.NextDouble() * ids.Length);
			return ids[i];
		}

		public bool OnTick(int ms)
		{
			if (this.Stage.State is not StageState.End)
			{
				if (this.Life <= 0)
				{
					this.Stage.Time = 0;
					this.Stage.State = StageState.End;
					this.Character.State = CharacterState.Hurt;
				}
				else if (this.Stage.Enemy is Enemy enemy && enemy.Data is not null && enemy.Life <= 0)
				{
					this.Stage.Time = 0;
					this.Stage.State = StageState.End;
					this.Character.State = CharacterState.Win;
				}
			}
			else if (this.Character is Character chara && chara.Data is not null && chara.State is not CharacterState.Win or CharacterState.Hurt)
			{
				this.Character.State = this.Stage.IsWon ? CharacterState.Win : CharacterState.Hurt;
			}

			if (!this.Stage.OnTick(ms: ms))
			{
				return false;
			}
			return true;
		}

		public string TokenAsString(Point p) => (p.X >= 0 && p.Y >= 0 && p.X < this.Stage.Data.GameSize.X && p.Y < this.Stage.Data.GameSize.Y) ? $"{this.Tokens[p.X][p.Y]?.TypeData?.Char ?? ' '}{p.X}-{p.Y}" : "BAD";

		public string Print()
		{
			Point size = this.Stage.Data.GameSize;
			StringBuilder s = new StringBuilder();
			const int width = 2;

			s.AppendLine();
			s.AppendLine();

			s.Append("   ");
			for (int x = 0; x < size.X * width; ++x)
			{
				s.Append(x % 2 == 1 ? ' ' : (x % 5 == 0) ? '│' : '•');
			}

			s.AppendLine();
			s.Append("  ");
			s.Append('╔');
			s.Append('═', size.X * width);
			s.Append('╗');

			for (int y = 0; y < size.Y; ++y)
			{
				s.AppendLine();
				s.Append((y % 5 == 0) ? '—' : '•');
				s.Append(' ');
				s.Append('║');
				for (int x = 0; x < size.X; ++x)
				{
					s.Append(this.Tokens[x][y]?.TypeData?.Char ?? ' ', width);
				}
				s.Append('║');
				s.Append(' ');
				s.Append((y % 5 == 0) ? '—' : '•');
			}

			s.AppendLine();
			s.Append("  ");
			s.Append('╚');
			s.Append('═', size.X * width);
			s.Append('╝');

			s.AppendLine();
			s.Append("   ");
			for (int x = 0; x < size.X * width; ++x)
			{
				s.Append(x % 2 == 1 ? ' ' : (x % 5 == 0) ? '│' : '•');
			}

			s.AppendLine();

			return s.ToString();
		}
	}
}
