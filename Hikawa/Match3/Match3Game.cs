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
		/// Time in milliseconds to skip token update behaviours.
		/// </summary>
        public float IdleTimer;

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

			this.IdleTimer = 0;
		}

		/// <summary>
		/// Checks whether this token can match with another.
		/// </summary>
		public bool Matches(Token other, bool visual = false)
		{
			return other is not null
				&& (visual || (this.Ready() && other.Ready()))
				&& (this.Type == other.Type || this.TypeData.MatchGroup == other.TypeData.MatchGroup)
				&& !(this.TypeData.IsBlock || other.TypeData.IsBlock);
		}

		public bool Ready()
		{
			return this.State is TokenState.Idle && this.IdleTimer <= 0;
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

		/// <summary>
		/// Constructor for a prepared game.
		/// Populates game board with random tokens.
		/// </summary>
		public Match3Game(Match3Data data, Random random, string stage)
		{
			this.Data = data;
			this.Random = random;

			this.SetUpGame(stage: stage ?? this.Data.GameData.InitialStage);
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
			stage ??= this.Stage.Id;
			if (this.Stage is null)
			{
				this.Stage = new(characterData: this.Data.CharacterData);
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
			Point size = this.Stage.Data.GameSize;
			this.TokenIDs = this.Data.TokenData.Keys.Where(this.Stage.Data.Tokens.Contains).ToArray();
			if (this.Tokens is null || resetTokens || size.X * size.Y != this.Tokens.SelectMany(t => t).Count())
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
						if (y >= min && tokens[x][(y - min)..(y - 1)].All((Token other) => token.Matches(other)))
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

			return matches;
				}

        public List<Point> GetAllTokens(Token match)
				{
            Point size = this.Stage.Data.GameSize;
            List<Point> tokens = [];
            for (int x = 0; x < size.X; ++x)
					{
                for (int y = 0; y < size.Y; ++y)
						{
                    if (match is null || match.Matches(this.Tokens[x][y]))
							{
                        tokens.Add(new(x: x, y: y));
							}
						}
					}
            return tokens;
        }

        public List<Point> GetAllTokens(string type)
        {
            Point size = this.Stage.Data.GameSize;
            List<Point> tokens = [];
            for (int x = 0; x < size.X; ++x)
            {
                for (int y = 0; y < size.Y; ++y)
                {
                    if (this.Tokens[x][y] is Token token && token.Type == type)
                    {
                        tokens.Add(new(x, y));
                    }
                }
            }
            return tokens;
        }

        public List<Point> GetAllTokens(bool isBlock)
        {
            Point size = this.Stage.Data.GameSize;
            List<Point> tokens = [];
            for (int x = 0; x < size.X; ++x)
            {
                for (int y = 0; y < size.Y; ++y)
                {
                    if (this.Tokens[x][y] is Token token && token.TypeData.IsBlock == isBlock)
                    {
                        tokens.Add(new(x, y));
                    }
                }
            }
            return tokens;
        }

        public List<Point> GetAdjacentTokens(Point point, int radius, bool onlyMatches)
        {
            Token token = this.Tokens[point.X][point.Y];
            Point size = this.Stage.Data.GameSize;
            List<Point> tokens = [];
            for (int x = 0; x < size.X; ++x)
            {
                for (int y = 0; y < size.Y; ++y)
                {
                    if (this.Tokens[x][y] is Token other // linear
                        && ((point.X == x && point.Y - y <= radius)
                            || (point.Y == y && point.X - x <= radius))
                        && (!onlyMatches || token is null || token.Matches(other)))
                    {
                        tokens.Add(new(x: x, y: y));
                    }
                }
            }
            return tokens;
        }

        public List<Point> GetSurroundingTokens(Point point, int radius, bool onlyMatches)
        {
            // literally never fails btw
            Token token = this.Tokens[point.X][point.Y];
            Point size = this.Stage.Data.GameSize;
            List<Point> tokens = [];
            for (int x = 0; x < size.X; ++x)
            {
                for (int y = 0; y < size.Y; ++y)
                {
                    if (this.Tokens[x][y] is Token other // circular
                        && Vector2.Distance(point.ToVector2(), new Vector2(x, y)) <= radius
                        && (!onlyMatches || token is null || token.Matches(other)))
                    {
                        tokens.Add(new(x: x, y: y));
                    }
                }
            }
            return tokens;
        }

        public List<Point> GetAdjacentTokensVisually(Point point, int radius, bool onlyMatches)
						{
            Token token = this.Tokens[point.X][point.Y];
            Point size = this.Stage.Data.GameSize;
            Point tokenSize = this.Data.UIData.TokenSize;
			float scale = this.Data.UIData.Scale;
            List<Point> tokens = [];
            for (int x = 0; x < size.X; ++x)
								{
                for (int y = 0; y < size.Y; ++y)
            {
                    if (this.Tokens[x][y] is Token other // linear
                        && (((Math.Abs(token.DrawPixel.X - other.DrawPixel.X) <= tokenSize.X * scale / 2) && (token.DrawPixel.Y - other.DrawPixel.Y <= tokenSize.Y * scale / 2 * radius))
							|| ((Math.Abs(token.DrawPixel.Y - other.DrawPixel.Y) <= tokenSize.Y * scale / 2) && (token.DrawPixel.X - other.DrawPixel.X <= tokenSize.X * scale / 2 * radius)))
                        && (!onlyMatches || token is null || token.Matches(other, visual: true)))
                {
                    tokens.Add(new(x: x, y: y));
						}
					}
            }
            return tokens;
				}

        public List<Point> GetSurroundingTokensVisually(Point point, int radius, bool onlyMatches)
				{
			// literally never fails btw
			Token token = this.Tokens[point.X][point.Y];
            Point size = this.Stage.Data.GameSize;
            Vector2 tokenSize = this.Data.UIData.TokenSize.ToVector2();
            float scale = this.Data.UIData.Scale;
            List<Point> tokens = [];
			for (int x = 0; x < size.X; ++x)
					{
				for (int y = 0; y < size.Y; ++y)
						{
					if (this.Tokens[x][y] is Token other // circular
                        //&& token.DrawPixel - other.DrawPixel is Vector2 distance
                        //                  && Math.Abs(distance.X) <= (tokenSize.X * scale * radius) && Math.Abs(distance.Y) <= (tokenSize.Y * scale * radius)
                        && Vector2.Distance(token.DrawPixel, other.DrawPixel) <= (tokenSize.X + tokenSize.Y) / 2 * scale * radius
                        && (!onlyMatches || token is null || token.Matches(other, visual: true)))
							{
						tokens.Add(new(x: x, y: y));
							}
						}
					}
			return tokens;
		}

		public Point? GetAdjacentToken(Point point, MatchDirection direction)
		{
			Point size = this.Stage.Data.GameSize;
			Point? other = null;
			switch (direction)
			{
				case MatchDirection.Up:
					if (point.Y > 0)
					{
						other = new(x: point.X, y: point.Y - 1);
					}
					break;
				case MatchDirection.Down:
					if (point.Y < size.Y - 1)
					{
						other = new(x: point.X, y: point.Y + 1);
					}
					break;
				case MatchDirection.Left:
					if (point.X > 0)
					{
						other = new(x: point.X - 1, y: point.Y);
					}
					break;
				case MatchDirection.Right:
					if (point.X < size.X - 1)
					{
						other = new(x: point.X + 1, y: point.Y);
					}
					break;
			}
			return other;
		}

		public void GetAdditionalMatchesForToken(Point point, in bool[][] visited, in bool[][] matches)
        {
            if (!visited[point.X][point.Y] && this.Tokens[point.X][point.Y] is Token token)
            {
				visited[point.X][point.Y] = true;

                int radius = token.TypeData.MatchEffectRadius;
                if (radius <= 0)
                    radius = Math.Max(this.Stage.Data.GameSize.X, this.Stage.Data.GameSize.Y);

				List<Point> nextMatches = [];
                switch (token.TypeData.MatchEffect)
                {
                    case MatchEffect.Radial:
                        nextMatches = this.GetSurroundingTokens(point, radius, onlyMatches: false);
                        break;
                    case MatchEffect.Linear:
                        nextMatches = this.GetAdjacentTokens(point, radius, onlyMatches: false);
                        break;
                    case MatchEffect.Global:
                        nextMatches = this.GetAllTokens(match: token);
                        break;
                    case MatchEffect.Standard:
                    default:
                        break;
                }

				// Flag matches before continuing
				foreach (Point next in nextMatches)
					matches[next.X][next.Y] = true;
				
				// Recursively get additional matches for each additional match
				foreach (Point next in nextMatches)
					this.GetAdditionalMatchesForToken(next, in visited, in matches);
                }
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
					this.Stage.State = StageState.End;
					this.Stage.Time = 0;
				}
				else if (this.Stage.Enemy is Enemy enemy && enemy.Data is not null && enemy.Life <= 0)
				{
					this.Stage.State = StageState.End;
					this.Stage.Time = 0;
				}
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
