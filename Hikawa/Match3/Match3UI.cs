using StardewValley.GameData;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hikawa.Match3
{
	public class TokenParticle
	{
		public TokenData Data;
		public Vector2 DrawPixel;
		public Vector2 Motion;
		public float Rotation;
		public float DrawRotation;
		public Color TextColor;
		public int Counter;
		public float Ratio;
		public float Limit;
		public float LifespanRate;

		public TokenParticle Set(Token token, float ratio = 1, Vector2? drawPixel = null, Vector2? motion = null, float? rotation = null, float? lifespanRate = null, int counter = -1, Color? color = null)
		{
			this.Data = token.TypeData;
			this.DrawPixel = drawPixel ?? token.DrawPixel;
			this.Motion = motion ?? Vector2.Zero;
			this.Rotation = rotation ?? 0;
			this.DrawRotation = 0;
			this.LifespanRate = lifespanRate ?? 1;
			this.Counter = counter;
			this.TextColor = color ?? Color.White;
			this.Limit = this.Ratio = ratio;
			return this;
		}

		public bool IsReady => this.Ratio > 0;

		public bool Update(int ms, float fade)
		{
			this.Ratio -= fade * this.LifespanRate;
			this.DrawPixel += this.Motion * ms;
			this.DrawRotation += this.Rotation * ms;
			return this.IsReady;
		}
	}

	public class TokenParticlePool
	{
		protected List<TokenParticle> _items;

		public List<TokenParticle> Items { get => this._items; }

		public TokenParticlePool(int size)
		{
			this._items = new List<TokenParticle>(size);
		}

		public TokenParticle Get()
		{
			// Add new item if below capacity
			if (this._items.Count < this._items.Capacity)
				this._items.Add(new());
			// Recycle best available item if at capacity
			return this._items.MinBy(item => item.Ratio);
		}

		public void Return(TokenParticle obj)
		{
			obj.Ratio = 0;
		}
	}

	public class Match3UI
	{
		// Game state
		/// <summary>
		/// Instance of match game.
		/// </summary>
		public Match3Game Game;
		/// <summary>
		/// Whether active game is currently paused.
		/// </summary>
		public bool IsPaused;
		/// <summary>
		/// Absolute position of actual mouse cursor on screen.
		/// </summary>
		public Vector2 RealCursor;
		/// <summary>
		/// Position of cursor.
		/// </summary>
		public Vector2 CursorPixel;
		/// <summary>
		/// Position of cursor.
		/// </summary>
		public Point? CursorToken;
		/// <summary>
		/// Position of cursor on mouse pressed.
		/// </summary>
		public Point? ActiveToken;
		/// <summary>
		/// Position of cursor on mouse released.
		/// </summary>
		public Point? TargetToken;

		// UI dimensions
		public Vector2 Position;
		public Vector2 Size;
		public Vector2 Margin;

		protected TokenParticlePool _tokenParticles;
		protected TokenParticlePool _matchParticles;

		// UI state
		public bool IsMute;
		public long DisplayScore;
		public MusicContext MusicContext;

		protected float _shakeScale;
		protected Point _shakeAmount;
		protected float _tokenEffectsTimer;

		public float TimeScale => 1f;
		public int Ms;

		// World state
		public string StoryId;

		// Match3 data
		public AudioData AudioData => this.Game.Data.AudioData;
		public UIData MenuData => this.Game.Data.UIData;
		public GameData GameData => this.Game.Data.GameData;
		public Dictionary<string, StageData> StageData => this.Game.Data.StageData;
		public Dictionary<string, TokenData> TokenData => this.Game.Data.TokenData;
		public Dictionary<string, CharacterData> CharacterData => this.Game.Data.CharacterData;
		public Dictionary<string, EnemyData> EnemyData => this.Game.Data.EnemyData;
		public WorldData WorldData => this.Game.Data.WorldData;

		public delegate void StageEnded(string stage, bool won);
		public event StageEnded OnStageEnded;

        public Match3UI(Match3Game game, string storyId)
		{
			this.Game = game;
			this.StoryId = storyId;

			this.DisplayScore = this.Game.TotalScore;
			this._tokenParticles = new TokenParticlePool(size: this.MenuData.ParticleCount);
			this._matchParticles = new TokenParticlePool(size: this.MenuData.ParticleCount);

			this.MusicContext = MusicContext.MiniGame;

			this.Game.OnStageStateChanged += this.OnStageStateChanged;

			this.SetupUI();
			this.SetupActors();
			this.SetupTokens();

			this.PlayMusic(this.AudioData.IntroMusic);
		}

		public void SetupActors()
		{
			foreach (CharacterData data in this.CharacterData.Values)
			{
				data.Texture = Game1.content.Load<Texture2D>(data.TextureId);
			}
			foreach (EnemyData data in this.EnemyData.Values)
			{
				data.Texture = Game1.content.Load<Texture2D>(data.TextureId);
			}
		}

		/// <summary>
		/// Initialises tokens for Stardew Valley menu.
		/// </summary>
		public void SetupTokens()
		{
			Point size = this.Game.Stage.Data.GameSize;

			if (this.Game.Tokens is not null)
			{
				for (int x = 0; x < size.X; ++x)
				{
					for (int y = 0; y < size.Y; ++y)
					{
						if (this.Game.Tokens[x][y] is Token token)
						{
							token.DrawPixel = this.GetPixelAtToken(x: x, y: y, isCentred: true);
                            token.IdleTimer = 100 + this.Game.Random.Next(10) * 300;
						}
					}
				}
			}
		}

		/// <summary>
		/// Sets menu values from metadata.
		/// </summary>
		public void SetupUI()
		{
			this.Ms = 0;

			this._tokenParticles.Items.Clear();
			this._matchParticles.Items.Clear();

			this.MenuData.MenuTexture = Game1.content.Load<Texture2D>(this.MenuData.MenuTextureId);
			this.MenuData.CursorTexture = Game1.content.Load<Texture2D>(this.MenuData.CursorTextureId);
			foreach (TokenData data in this.TokenData.Values)
			{
				data.Texture = Game1.content.Load<Texture2D>(data.TextureId);
			}
		}

		/// <summary>
		/// Sets position and dimensions for menu and its components.
		/// </summary>
		public void UpdateComponents(Rectangle? bounds = null)
		{
			bounds ??= new(location: this.Position.ToPoint(), size: this.Size.ToPoint());

			float scale = this.MenuData.Scale;

			this.Position = bounds.Value.Location.ToVector2();
			this.Size = bounds.Value.Size.ToVector2();
			this.Margin = new(x: 8, y: 8);

			if (this.Game.Character is Character chara && chara.Data is not null)
			{
				Vector2 size = chara.Data.FrameTextureRegion.Size.ToVector2();
				chara.DrawPixel
					// bottom-left of board
					= new Vector2(x: 0, y: bounds.Value.Height)
					// accounting for margin
					+ new Vector2(x: 0, y: this.Margin.Y) * scale * 2
					// aligning portrait with bottom of board
					+ new Vector2(x: -size.X, y: -size.Y) * scale
					// aligning frame with board frame
					+ new Vector2(x: 0, y: size.Y - chara.PortraitSize.Y - this.Margin.Y) * scale / 2
					;
			}
			if (this.Game.Stage.Enemy is Enemy enemy && enemy.Data is not null)
			{
				Vector2 size = enemy.Data.FrameTextureRegion.Size.ToVector2();
				enemy.DrawPixel = new Vector2(x: bounds.Value.Width, y: bounds.Value.Height)
					+ new Vector2(x: this.Margin.X, y: this.Margin.Y) * scale * 2
					+ new Vector2(x: 0, y: -size.Y) * scale;
				enemy.DrawPixel
					// bottom-right of board
					= new Vector2(x: bounds.Value.Width, y: bounds.Value.Height)
					// accounting for margin
					+ new Vector2(x: this.Margin.X, y: this.Margin.Y) * scale * 2
					// aligning portrait with bottom of board
					+ new Vector2(x: 0, y: -size.Y) * scale
					// aligning frame with board frame
					+ new Vector2(x: 0, y: size.Y - enemy.PortraitSize.Y - this.Margin.Y) * scale / 2
					;
			}
			if (this.Game.Stage.DialogueCharacter is Character dchara && dchara.Data is not null)
			{
				// duplicate enemy position
				Vector2 size = dchara.Data.FrameTextureRegion.Size.ToVector2();
				dchara.DrawPixel = new Vector2(x: bounds.Value.Width, y: bounds.Value.Height)
					// accounting for margin
					+ new Vector2(x: this.Margin.X, y: this.Margin.Y) * scale * 2
					// aligning portrait with bottom of board
					+ new Vector2(x: 0, y: -size.Y) * scale
					// aligning frame with board frame
					+ new Vector2(x: 0, y: size.Y - dchara.PortraitSize.Y - this.Margin.Y) * scale / 2
					;
			}
		}

		public void PlaySound(string id)
		{
			if (!this.IsMute)
			{
				Game1.playSound(id);
			}
		}

		public void PlayMusic(string id)
		{
			Game1.changeMusicTrack(
				newTrackName: id,
				track_interruptable: false,
				music_context: this.MusicContext);
		}

		public void ClearPlayerContextualState()
		{
			this.ActiveToken = this.TargetToken = this.CursorToken = null;
		}

		/// <summary>
		/// Converts screen pixel coordinates to token coordinates on game board.
		/// </summary>
		public Point? GetTokenAtPixel(int x, int y)
		{
			Point? point = null;
			if (x > this.Position.X
				&& y > this.Position.Y
				&& x < this.Position.X + this.Size.X
				&& y < this.Position.Y + this.Size.Y)
			{
				Point tokenSize = this.MenuData.TokenSize;
				point = new(
					x: (int)((x - this.Position.X) / tokenSize.X / this.MenuData.Scale),
					y: (int)((y - this.Position.Y) / tokenSize.Y / this.MenuData.Scale));
			}
			return point;
		}

		/// <summary>
		/// Converts token coordinates to screen pixel coordinates.
		/// </summary>
		public Vector2 GetPixelAtToken(int x, int y, bool isCentred)
		{
			float scale = this.MenuData.Scale;
			Point pixel = Point.Zero;
			Point tokenSize = this.MenuData.TokenSize;
			Vector2 point = new Vector2(
				x: pixel.X + x * tokenSize.X * scale,
				y: pixel.Y + y * tokenSize.Y * scale);
			if (isCentred)
			{
				point += new Vector2(
					x: tokenSize.X * scale / 2,
					y: tokenSize.Y * scale / 2);
			}
			return point;
		}

		/// <summary>
		/// Determines match direction from cursor endpoint relative to startpoint.
		/// </summary>
		public MatchDirection? GetDirection(Point from, Point to)
		{
			MatchDirection? direction;
			Point difference = from - to;
			if (difference == Point.Zero)
			{
				direction = null;
			}
			else if (Math.Abs(difference.X) > Math.Abs(difference.Y))
			{
				// Prioritise X-axis
				if (from.X > to.X)
				{
					direction = MatchDirection.Left;
				}
				else if (from.X < to.X)
				{
					direction = MatchDirection.Right;
				}
				else if (from.Y > to.Y)
				{
					direction = MatchDirection.Up;
				}
				else
				{
					direction = MatchDirection.Down;
				}
			}
			else
			{
				// Prioritise Y-axis
				if (from.Y > to.Y)
				{
					direction = MatchDirection.Up;
				}
				else if (from.Y < to.Y)
				{
					direction = MatchDirection.Down;
				}
				else if (from.X > to.X)
				{
					direction = MatchDirection.Left;
				}
				else
				{
					direction = MatchDirection.Right;
				}
			}
			return direction;
		}

		/// <summary>
		/// Assigns active token from token at cursor position, if any.
		/// </summary>
		public void SetActiveToken(int x, int y)
		{
			Point? active = this.GetTokenAtPixel(x: x, y: y);
			if (active is not null && this.Game.Tokens[active.Value.X][active.Value.Y] is Token token && token.Ready())
				this.ActiveToken = active;
		}

		/// <summary>
		/// Attempts to match token with other in cursor direction,
		/// swapping both tokens and completing match if successful.
		/// </summary>
		public bool TryMatchTokens(Point a, Point b, MatchEffect format)
		{
			//Console.WriteLine($"{this.Game.TokenAsString(a)} x {this.Game.TokenAsString(b)}");

			// Swap tokens and check for matches
			this.Game.SwapTokens(a: a, b: b);
			List<Point> matches = this.Game.CheckMatches(position: a)
				.Concat(this.Game.CheckMatches(position: b))
				.Distinct()
				.ToList()
				;
			if (matches.Any())
			{
				this.Game.Tokens[a.X][a.Y].State = this.Game.Tokens[b.X][b.Y].State = TokenState.Motion;

				// Swap tokens visibly immediately since the active-cursor swap preview already did the transition for us
				Token tokenA = this.Game.Tokens[a.X][a.Y];
				Token tokenB = this.Game.Tokens[b.X][b.Y];
				Vector2 drawPixel = tokenA.DrawPixel;
				tokenA.DrawPixel = tokenB.DrawPixel;
				tokenB.DrawPixel = drawPixel;

				// Match and clear if matches were found
                this.OnMatchesMade(a: a, b: b, matches: matches);
				return true;
			}
			else
			{
				// Reverse swap if no matches were found
				this.Game.SwapTokens(a: a, b: b);
                this.PlaySound(this.AudioData.SwapSound);
			}
			return false;
		}

		/// <summary>
		/// Various behaviours in response to a successful token match.
		/// </summary>
		public void OnMatchesMade(Point? a, Point? b, List<Point> matches)
		{
			Console.WriteLine($"Matches: {matches.Count} {string.Join(' ', matches.Select(p => $"({this.Game.Tokens[p.X][p.Y]?.Type ?? "null"} {p.X} {p.Y})"))}");

            // Count up matched tokens of each token type
            Dictionary<string, int> matchTypes = this.TokenData.Keys
                .ToDictionary((string type) => type, (string type) => matches
                    .Count((Point point) => type == this.Game.Tokens[point.X][point.Y]?.Type));

			// Count up matched tokens of each match group
            Dictionary<string, int> matchGroups = this.TokenData.Keys
				.ToDictionary((string type) => type, (string type) => matches
					.Count((Point point) => type == this.Game.Tokens[point.X][point.Y]?.TypeData.MatchGroup));

			bool isPowerMatch = false;
			bool isSuperPowerMatch = false;
			bool isUpgrade = false;
			bool isSuperUpgrade = false;

			/*if (a is not null)
			{
				this._matchParticles.Get().Set(
					token: this.Game.Tokens[a.Value.X][a.Value.Y],
					ratio: 2,
					counter: matches.Count,
					lifespanRate: 1.5f);
			}*/
			{
				// Make a combo particle for each type of token collected
				Log.D($"Group: {string.Join(' ', matchGroups.Where(pair => pair.Value > 0).Select(pair => $"{pair.Key}_{pair.Value}"))}");
				Dictionary<string, bool> created = matchGroups.ToDictionary(pair => pair.Key, pair => false);
				foreach (Point match in matches)
				{
					Token token = this.Game.Tokens[match.X][match.Y];
					if (!created[token.TypeData.MatchGroup] && matchGroups[token.TypeData.MatchGroup] > this.Game.Stage.Data.Match)
					{
                        // Set effects for token matched
                        isPowerMatch |= token.TypeData.MatchEffect is MatchEffect.Radial;
                        isSuperPowerMatch |= token.TypeData.MatchEffect is MatchEffect.Linear or MatchEffect.Global;

                        Log.D($"  + particle: {token.TypeData.MatchGroup}_{matchGroups[token.TypeData.MatchGroup]}");
						created[token.TypeData.MatchGroup] = true;
						var typeMatches = matches
							.Select(point => this.Game.Tokens[point.X][point.Y])
							.Where(other => token.TypeData.MatchGroup == other?.TypeData.MatchGroup);
						Vector2 drawPixel = new Vector2(
							x: typeMatches.Average(token => token.DrawPixel.X),
							y: typeMatches.Average(token => token.DrawPixel.Y));
						this._matchParticles.Get().Set(
							token: token,
							ratio: 2,
							counter: matchGroups[token.TypeData.MatchGroup],
							lifespanRate: 1.5f,
							drawPixel: drawPixel,
							color: token.TypeData.ExplodeColour);
					}
				}
			}

            Log.D($"  + initial: [{matches.Count}] {string.Join(' ', matches.Select(p => $"({this.Game.Tokens[p.X][p.Y]?.Type ?? "null"} {p.X} {p.Y})"))}");

            // Check for additional matches from token match effects
            List<Point> totalMatches = [];
            {
                Point size = this.Game.Stage.Data.GameSize;
                bool[][] visited = new bool[size.X][], additionalMatches = new bool[size.X][];
                for (int x = 0; x < size.X; ++x)
                {
                    visited[x] = new bool[size.Y];
                    additionalMatches[x] = new bool[size.Y];
                }

				// get additional matches
                foreach (Point match in matches.ToList())
                    this.Game.GetAdditionalMatchesForToken(match, in visited, in additionalMatches);

                // Combine initial and additional matches, removing duplicates
                // (we can't exclude initial matches from available tokens above since they need checking for additional matches too)
                for (int x = 0; x < size.X; ++x)
                    for (int y = 0; y < size.Y; ++y)
						if (additionalMatches[x][y])
                            totalMatches.Add(new(x, y));

				// i'm so done with amtching fucking tokens
				totalMatches = totalMatches.Concat(matches).Distinct().ToList();

				// Behaviours on additional matches found
				if (totalMatches.Count > matches.Count)
                {
                    this._tokenEffectsTimer = 750;
					var except = totalMatches.Except(matches);

                    Log.D($"  + additional: [{except.Count()}] " + string.Join(' ', except.Select(p => $"({this.Game.Tokens[p.X][p.Y]?.Type ?? "null"} {p.X} {p.Y})")));
                }
            }

            Log.D($"  + total: [{totalMatches.Count}] {string.Join(' ', totalMatches.Select(p => $"({this.Game.Tokens[p.X][p.Y]?.Type ?? "null"} {p.X} {p.Y})"))}");

			// Substitute token upgrades into matches
			if (!this.Game.Stage.Data.NoTokenUpgrades)
			{
				foreach (Point? point in new[] { a, b })
				{
					if (point is not null
						&& this.Game.Tokens[point.Value.X][point.Value.Y] is Token token
						&& matchGroups[token.Type] > this.Game.Stage.Data.Match
						&& token.TypeData.TokenUpgrade is string type)
					{
						// Use super upgrade for big matches
						if (matchGroups[token.Type] > this.Game.Stage.Data.Match + 1
							&& this.TokenData[type].TokenUpgrade is string superType)
						{
							isSuperUpgrade = true;
							type = superType;
						}
						else
                        {
                            isUpgrade = true;
                        }

						// Replace token
						token.Set(state: TokenState.Motion, type: type, data: this.TokenData[type]);

						// Update draw pixel for match swap
						token.DrawPixel = this.GetPixelAtToken(x: point.Value.X, y: point.Value.Y, isCentred: true);

                        // Prevent initial tokens from being matched again
                        totalMatches.Remove(point.Value);
					}
				}
			}

			foreach (Point match in totalMatches)
			{
				int x = match.X;
				int y = match.Y;

				Token token = this.Game.Tokens[x][y];

				// Create particles from matched tokens
				token.DrawPixel = this.GetPixelAtToken(x: x, y: y, isCentred: true);
				this._tokenParticles.Get().Set(token: token);
				if (token.TypeData.ExplodeScale > 0)
				{
					this._matchParticles.Get().Set(
						token: token,
						ratio: 2,
						rotation: 0,
						lifespanRate: 2f);
				}
			}

            int[] additionalY = new int[this.Game.Stage.Data.GameSize.X];
			foreach (Point match in totalMatches)
			{
				int x = match.X;
				int y = match.Y;

				Token token = this.Game.Tokens[x][y];

                // Affect tokens above matched tokens
				// Move higher tokens downwards to replace matched token
				for (; y > 0; --y)
				{
					this.Game.Tokens[x][y] = this.Game.Tokens[x][y - 1];
					this.Game.Tokens[x][y].State = TokenState.Motion;
				}

                // Recycle matched token as random new token
                string type = this.Game.GetRandomTokenType();
                token.Set(state: TokenState.Motion, type: type, data: this.TokenData[type]);
				token.DrawPixel = this.GetPixelAtToken(x: x, y: --additionalY[x], isCentred: true);
				this.Game.Tokens[x][y] = token;
			}

			// Console.WriteLine($"\t{string.Join(' ', tokenCounts.Where(pair => pair.Value > 0).Select(pair => $"{pair.Value}x{pair.Key}"))}");

			// Award power for tokens matched
			int power = matchTypes.Sum((pair) => this.TokenData[pair.Key].IsPowerToken
				? this.GameData.PowerPerToken[Math.Min(pair.Value, this.GameData.PowerPerToken.Length - 1)]
				: 0);
			this.Game.Power = Math.Min(this.GameData.PowerMax, this.Game.Power + power);

			// Award score for tokens matched
			int score = matchTypes.Values.Sum((int count) => this.GameData.ScorePerToken[Math.Min(count, this.GameData.ScorePerToken.Length - 1)]);
			this.Game.Stage.Score += score;

            // Play sounds
            if (isSuperUpgrade)
                this.PlaySound(this.AudioData.SuperUpgradeSound);
            if (isUpgrade)
                this.PlaySound(this.AudioData.UpgradeSound);
            if (isSuperPowerMatch)
                this.PlaySound(this.AudioData.SuperPowerMatchSound);
            if (isPowerMatch)
                this.PlaySound(this.AudioData.PowerMatchSound);
            if (!isSuperPowerMatch && !isPowerMatch)
                this.PlaySound(this.AudioData.MatchSound);
		}

		public void UpdateCursor(Point pixel)
		{
			this.RealCursor = pixel.ToVector2();
			Point? cursor = this.GetTokenAtPixel(x: pixel.X, y: pixel.Y);
			this.CursorPixel = pixel.ToVector2() - this.Position;
			if (this.ActiveToken is null)
            {
                // Mark token under cursor for hover and swap behaviours
                this.CursorToken = cursor;
            }
            else
			{
				float scale = this.MenuData.Scale;
				Point tokenSize = this.MenuData.TokenSize;

                // Limit movement of cursor with active token
				Vector2 activePixel = this.GetPixelAtToken(x: this.ActiveToken.Value.X, y: this.ActiveToken.Value.Y, isCentred: true);
				Vector2 pixelDistance = Utils.Vector.Abs(this.CursorPixel - activePixel);
				bool isFreeMotion = this.CursorToken is null && (pixelDistance.X + pixelDistance.Y) / 2 < (tokenSize.X + tokenSize.Y) / 2 * scale;
				Vector2 cursorPixel = new Vector2(
					x: (!isFreeMotion || pixelDistance.X < tokenSize.X) && pixelDistance.X < pixelDistance.Y ? activePixel.X : this.CursorPixel.X,
					y: (!isFreeMotion || pixelDistance.Y < tokenSize.Y) && pixelDistance.Y < pixelDistance.X ? activePixel.Y : this.CursorPixel.Y);

                // Limit movement to axis from active to cursor
                if (this.CursorToken is not null && this.CursorToken != this.ActiveToken)
	                cursorPixel = activePixel + pixelDistance * Vector2.Normalize(this.CursorToken.Value.ToVector2() - this.ActiveToken.Value.ToVector2());

                // Limit distance to adjacent token
                cursorPixel.X = Math.Clamp(value: cursorPixel.X, min: activePixel.X - tokenSize.X * scale, max: activePixel.X + tokenSize.X * scale);
				cursorPixel.Y = Math.Clamp(value: cursorPixel.Y, min: activePixel.Y - tokenSize.Y * scale, max: activePixel.Y + tokenSize.Y * scale);

				this.CursorPixel = cursorPixel;

				// Prevent non-adjacent tokens being set as cursor with active token
				if (cursor is not null)
				{
					Vector2 tokenDistance = Utils.Vector.Abs(cursor.Value.ToVector2() - this.ActiveToken.Value.ToVector2());
					if (tokenDistance.X + tokenDistance.Y < 2)
					{
						this.CursorToken = cursor;
					}
				}
			}
		}

		public void UpdateTokens(int ms)
		{
			// Game tokens
			if (this.Game.Tokens is not null)
			{
				for (int x = 0; x < this.Game.Stage.Data.GameSize.X; ++x)
				{
					for (int y = 0; y < this.Game.Stage.Data.GameSize.Y; ++y)
					{
						if (this.Game.Tokens[x][y] is Token token)
						{
							Point point = new(x: x, y: y);

							// Timers
							token.IdleTimer = Math.Max(0, token.IdleTimer - ms);
							if (token.IdleTimer > 0)
								continue;

							// Scale
							{
								// get
								float scale = this.MenuData.TokenScaleDefault;
								if (this.Game.IsPaused)
									scale = this.MenuData.TokenScaleDefault;
								else if (point == this.ActiveToken)
									scale = this.MenuData.TokenScaleActive;
								else if (point == this.CursorToken)
									scale = this.MenuData.TokenScaleHovered;
								// set
								if (Math.Abs(scale - token.Scale) < 0.1f)
									token.Scale = scale;
								else
									token.Scale += this.MenuData.TokenScaleRate * ms * (scale > token.Scale ? 1 : -1);
							}

							// Position
							if (token.State is TokenState.Motion)
							{
								// Move token towards target position
								Vector2 targetPixel = this.GetPixelAtToken(x: x, y: y, isCentred: true);
								if (token.DrawPixel.Y >= targetPixel.Y)
								{
									token.DrawPixel = targetPixel;

									// Wait for game unpaused before continuing to match tokens
									if (!this.Game.IsPaused)
									{
										this.PlaySound(this.AudioData.LandSound);

										// Stop token motion
										token.State = TokenState.Idle;
										token.Acceleration = 0;

										// Check chained matches appearing on tokens moved to previous match positions
										List<Point> matches = this.Game.CheckMatches(position: new(x: x, y: y));
										if (matches.Count > 0)
										{
											// Match and clear if matches were found
											this.OnMatchesMade(a: point, b: null, matches: matches);
										}
									}
								}
								else
								{
									// Accelerate token towards top speed
									token.Acceleration = Math.Min(
										val1: this.MenuData.TokenMotionMax * this.MenuData.Scale,
										val2: this.MenuData.TokenMotionRate * this.MenuData.Scale + token.Acceleration);
									token.DrawPixel += Utils.Vector.MotionTo(origin: token.DrawPixel, target: targetPixel)
										* token.Acceleration * ms;
								}
							}
						}
					}
				}
			}
		}

		public void Shake(float scale, Point amount)
		{
			this._shakeScale = scale;
			this._shakeAmount = amount;
		}

		public void OnMoveMade()
		{
			++this.Game.Stage.Moves;

			if (this.Game.Character.State is CharacterState.Hurt)
				this.Game.Character.State = CharacterState.Idle;

			this.TryDoEnemyTurn();
		}

		public void TryUsePlayerPower()
		{
			if (this.Game.Power >= this.GameData.PowerMax)
			{
				this.PlaySound("warrior");

				this.Game.Power = 0;
			}
			else if (this.Game.Power >= this.GameData.PowerMax / 2)
			{
				this.PlaySound("powerup");

				this.Game.Power -= this.GameData.PowerMax / 2;
			}
			else
			{
				this.PlaySound("cancel");
			}
		}

		public void AttackPlayer(int damage)
		{
			this.Game.Life -= damage;
			this.Game.Power = Math.Min(this.GameData.PowerMax, this.Game.Power + damage / 2);

			this.Shake(scale: 4f, amount: new(x: 2, y: 2));
			if (this.Game.Character is Character chara && chara.Data is not null)
			{
				chara.State = CharacterState.Hurt;
			}
			if (this.Game.Stage?.Enemy is Enemy enemy && enemy.Data is not null)
			{
				enemy.AttackRotation = (float)(this.Game.Random.NextDouble() * Math.PI * 2);
				enemy.AttackDrawTimer = enemy.Data.AttackDuration;
				enemy.AttackDrawPixel = new Vector2(
					x: -this.Size.X / 4 + this.Game.Random.Next((int)this.Size.X / 2),
					y: -this.Size.Y / 4 + this.Game.Random.Next((int)this.Size.Y / 2));
			}
		}

		public void TryDoEnemyTurn()
		{
			if (this.Game.Stage.Enemy is Enemy enemy
				&& enemy.Data is not null)
			{
				if (this.Game.Stage.Moves % enemy.Data.AttackRate == 0)
				{
					enemy.State = EnemyState.Attack;
					if (enemy.Power >= enemy.Data.PowerMax)
					{
						this.PlaySound("serpent");

						enemy.Power = 0;
					}
					else
					{
						this.PlaySound("ow");

						this.AttackPlayer(damage: enemy.Data.AttackValue);

						enemy.Power = Math.Min(enemy.Data.PowerMax, enemy.Data.AttackValue);
					}
				}
				else
				{
					enemy.State = EnemyState.Idle;
				}
			}
		}

		public void SetupStage(string stageId, bool reset, StageState state)
		{
			this.Game.SetUpGame(stage: stageId, resetTokens: reset, state: state);
			this.SetupTokens();
		}

		public void ChangeStage(string stageId)
		{
			// Reset player interactions
			this.ClearPlayerContextualState();

			// Reset tokens
			this.SetupStage(stageId: stageId, reset: false, state: StageState.Start);
		}

		public void OnStageStateChanged(StageState previous, StageState next)
		{
			if (previous is StageState.Start && next is StageState.Active)
			{
				this.PlayMusic(id: this.Game.Stage.Data.Music);
			}
			else if (next is StageState.End)
			{
				if (this.Game.Stage.IsWon)
				{
					// Win celebration
					Game1.MusicDuckTimer = this.Game.Stage.Data.EndDelay;
					this.PlaySound(id: this.Game.Stage.Data.WinMusic);
				}
				else
				{
					// Lose commiseration
					this.Shake(scale: 4f, amount: new(x: 2, y: 2));
				}

				this.OnStageEnded?.Invoke(stage: this.Game.Stage.Id, won: this.Game.Stage.IsWon);
			}
		}

		public void OnActionStart(int x, int y)
		{
			if (this.Game.IsPaused)
			{
				// Advance stage dialogue
				if (this.Game.Stage is Stage stage && stage.HasRemainingDialogue() && stage.Time > this.MenuData.DialogueIgnoreInputTime)
				{
					stage.AdvanceOrEndDialogue();
				}

				// Ignore other interactions when game is not active
				return;
			}

			// Use player power
			Vector2 relative = new Vector2(x: x, y: y) - this.Position;
			if (this.Game.Character.ContainsCursor(x: (int)relative.X, y: (int)relative.Y, scale: this.MenuData.Scale))
			{
				this.TryUsePlayerPower();
				return;
			}

			// Attempt to fetch active token
			this.SetActiveToken(x: x, y: y);
			if (this.ActiveToken is not null)
				this.PlaySound(this.AudioData.SelectSound);
		}

		public void OnActionUpdate(int x, int y)
		{
			if (this.Game.IsPaused)
				return;

			if (this.ActiveToken is not null && this.CursorToken is not null)
			{
				MatchDirection? direction = this.GetDirection(from: this.ActiveToken.Value, to: this.CursorToken.Value);
				if (direction is not null)
				{
					this.TargetToken = this.Game.GetAdjacentToken(point: this.ActiveToken.Value, direction: direction.Value);
				}
				else
				{
					this.TargetToken = null;
				}
			}
		}

		public void OnActionEnd(int x, int y)
		{
			// Attempt to match tokens on release
			if (this.ActiveToken is not null && this.TargetToken is not null && this.ActiveToken.Value != this.TargetToken.Value)
			{
				this.CursorToken = this.TargetToken;

				// Match tokens
				bool isMoveMade = this.TryMatchTokens(
					a: this.ActiveToken.Value,
					b: this.TargetToken.Value,
					format: MatchEffect.Standard);

				if (isMoveMade)
				{
					this.OnMoveMade();
				}
			}

			// Clear active tokens on release
			this.CursorToken = this.ActiveToken = this.TargetToken = null;
		}

		public bool OnTick(GameTime time)
		{
			int ms = (int)(time.ElapsedGameTime.Milliseconds * this.TimeScale);
			this.Ms += ms;

			Stage stage = this.Game.Stage;
			Point size = this.Game.Stage.Data.GameSize;

			// Score
            this.DisplayScore = (long)Math.Min(this.Game.TotalScore + stage.Score, this.DisplayScore + this.MenuData.ScoreTickRate * ms);

			// Character
			if (this.Game.Character is Character chara && chara.Data is not null)
			{
				Vector2 pixel = this.CursorPixel;
				bool isHovered = !this.Game.IsPaused
					&& pixel.X > chara.DrawPixel.X
					&& pixel.Y > chara.DrawPixel.Y
					&& pixel.X < chara.DrawPixel.X + chara.PortraitSize.X * this.MenuData.Scale
					&& pixel.Y < chara.DrawPixel.Y + chara.PortraitSize.Y * this.MenuData.Scale;

                CharacterState idle = this.Game.Stage.Moves / 5 % 1 == 0 ? CharacterState.Idle : CharacterState.Idle2;

				// Cycle between idle frames
				chara.PortraitTime += ms;
				if (chara.State is CharacterState.Idle or CharacterState.Idle2 or CharacterState.Danger)
				{
                    if (this.Game.Life < this.GameData.InitialLife / 5 // low life
						|| (stage.Data.TimeGoal > 0 && stage.Time > stage.Data.TimeGoal / 4 * 5) // low time
						|| (this.Game.GetAllTokens(isBlock: true).Count >= size.X * size.Y / 5)) // low tokens
                    {
                        chara.State = CharacterState.Danger;
                    }
                    else if (isHovered)
					{
						chara.State = CharacterState.Hover;
					}
					else
					{
						chara.State = idle;
					}
				}
				else if (chara.State is CharacterState.Hover && !isHovered)
				{
					chara.State = idle;
				}
			}

			// Shake
			this._shakeScale = Math.Max(0, this._shakeScale - ms / 100f);

			// Animations
			this._tokenEffectsTimer = Math.Max(0, this._tokenEffectsTimer - ms);

            // Particles
            {
                // update particles
                float fade = this.MenuData.ParticleFadeRate * ms;
				foreach (TokenParticle particle in this._tokenParticles.Items)
					if (particle.IsReady && !particle.Update(ms: ms, fade: fade))
						this._tokenParticles.Return(particle);
				foreach (TokenParticle particle in this._matchParticles.Items)
					if (particle.IsReady && !particle.Update(ms: ms, fade: fade))
						this._matchParticles.Return(particle);
			}

			// Cursor
			this.UpdateCursor(pixel: Game1.getMousePosition(ui_scale: true));

            // Tokens
			if (this._tokenEffectsTimer <= 0)
			this.UpdateTokens(ms: ms);

            // Game
			if (!this.Game.OnTick(ms: ms))
			{
				string stageId = this.Game.Stage.Id;
                if (this.WorldData.Stories.TryGetValue(this.StoryId, out StoryData storyData)
					&& storyData.Stages.TryGetValue(stageId, out StoryStageData stageData)
					&& stageData.NextStage is not null)
					this.ChangeStage(stageData.NextStage);
				else
					return false;
            }

			return true;
		}

		public void Draw(SpriteBatch b)
		{
			float scale = this.MenuData.Scale;
			Vector2 shake = new Vector2(
				x: this.Game.Random.Next(-this._shakeAmount.X, this._shakeAmount.X),
				y: this.Game.Random.Next(-this._shakeAmount.Y, this._shakeAmount.Y))
				* this._shakeScale * scale / 2;
			Vector2 position = this.Position + shake;
			Point size = this.Game.Stage.Data.GameSize;
			Point tokenSize = this.MenuData.TokenSize;
			Rectangle board = new(
				x: (int)(position.X - this.Margin.X * scale),
				y: (int)(position.Y - this.Margin.Y * scale),
				width: (int)((this.Margin.X * 2 * scale) + size.X * tokenSize.X * scale),
				height: (int)((this.Margin.Y * 2 * scale) + size.Y * tokenSize.Y * scale));
			Vector2 meterPosition = position
				+ new Vector2(x: this.Size.X / 2, y: this.Size.Y)
				+ new Vector2(x: 0, y: 11) * scale;
			Vector2 textSize;
			string text;
			Stage stage = this.Game.Stage;
			bool isDialogue = stage?.State is StageState.Start && stage.HasRemainingDialogue();

			// Game board
			{
				for (int x = 0; x < this.Game.Stage.Data.GameSize.X; ++x)
					for (int y = 0; y < this.Game.Stage.Data.GameSize.Y; ++y)
						if ((x + y) % 2 == 0)
							b.Draw(Game1.staminaRect, new Rectangle((int)(position.X + x * tokenSize.X * scale), (int)(position.Y + y * tokenSize.Y * scale), (int)(tokenSize.X * scale), (int)(tokenSize.Y * scale)), Color.MediumVioletRed * 0.1f);
			}

			// Game particles
			foreach (TokenParticle particle in this._tokenParticles.Items)
			{
				if (particle.IsReady)
				{
					b.Draw(
						texture: particle.Data.Texture,
						position: position + particle.DrawPixel,
						sourceRectangle: particle.Data.TextureRegion,
						color: Color.White * particle.Ratio,
						rotation: 0,
						origin: tokenSize.ToVector2() / 2,
						scale: scale * particle.Ratio,
						effects: SpriteEffects.None,
						layerDepth: 1);
				}
			}

			void drawToken(Point coords)
			{
				Token token = this.Game.Tokens[coords.X][coords.Y];
				if (token is null)
					return;
				bool isActive = this.ActiveToken == coords;
				Vector2 draw = token.DrawPixel;
				if (isActive)
				{
					// active token
					draw = this.CursorPixel;
                }
				else if (this.ActiveToken is not null && this.CursorToken == coords && token.Ready())
				{
					// swap token with active set
					Token activeToken = this.Game.Tokens[this.ActiveToken.Value.X][this.ActiveToken.Value.Y];
					draw -= (this.CursorPixel - activeToken.DrawPixel);
                }

				bool isPower = token.TypeData.IsPowerToken;
				bool isSuperPower = token.TypeData.MatchEffect is MatchEffect.Linear;

                float r = (float)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 666f);
                float sin = 1f + 0.5f * MathF.Sin(r);

				float alpha = Math.Clamp((draw.Y + tokenSize.Y) / tokenSize.Y, 0, 1);

                Rectangle star = new Rectangle(128, 0, 32, 32);
                Vector2 origin = star.Size.ToVector2() / 2;

				// back particles
                /*if (isPower || isSuperPower)
                {
                    b.Draw(
                        texture: this.MenuData.MenuTexture,
                        position: position + draw,
                        sourceRectangle: star,
                        color: token.TypeData.ExplodeColour * (0.25f * sin) * alpha,
                        rotation: r,
                        origin: origin,
                        scale: scale * (isSuperPower ? 0.6f : 0.5f) + sin * 0.5f,
                        effects: SpriteEffects.None,
                        layerDepth: 1);
                }*/

				// token
				b.Draw(
					texture: token.TypeData.Texture,
					position: position + draw,
					sourceRectangle: token.TypeData.TextureRegion,
					color: (isSuperPower ? Color.Lerp(token.TypeData.ExplodeColour, Color.White, sin / 3 * 4) : Color.White) * alpha,
					rotation: 0,
					origin: tokenSize.ToVector2() / 2,
					scale: (isSuperPower ? scale + 0.25f * sin : scale) * token.Scale,
					effects: SpriteEffects.None,
					layerDepth: 1);

				// front particles
                if (isPower || isSuperPower)
                {
					// hehe stoloe ur smoke code
                    int interval = 1600 + 256 * 6666 % 200;
                    Vector2[] offsets = [new(-8, 4), new(1, 6), new(8, 6)];
                    for (int i = 0; i < offsets.Length; ++i)
                    {
                        b.Draw(
                            texture: this.MenuData.MenuTexture,
                            position: position + draw
                                + offsets[i] * scale
                                + new Vector2(0f, (float)((0f - Game1.currentGameTime.TotalGameTime.TotalMilliseconds + interval * i) % 2000f) * 0.03f),
                            sourceRectangle: star,
                            color: (isSuperPower ? token.TypeData.ExplodeColour : Color.Gold)
                                * alpha
                                * 0.75f
                                * (1f - (float)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + interval * i) % 2000f) / 2000f),
                            rotation: (float)((0f - Game1.currentGameTime.TotalGameTime.TotalMilliseconds) % 2000f)
                                * 0.001f,
                            origin: origin,
                            scale: scale / 10,
                            effects: SpriteEffects.None,
                            layerDepth: 1);
                    }
                }
			}

			// Game tokens
			if (this.Game.Tokens is not null)
			{
				// Game tokens drawn in order
				for (int x = 0; x < size.X; ++x)
				{
					for (int y = 0; y < size.Y; ++y)
					{
						Point point = new Point(x: x, y: y);
						if (this.Game.Tokens[x][y] is not null && point != this.ActiveToken)
						{
							drawToken(coords: point);
						}
					}
				}
				// Active token drawn above others
				if (this.ActiveToken is not null)
				{
					drawToken(coords: this.ActiveToken.Value);
				}
			}

			// Stage name
			text = this.Game.Stage.Data.DisplayName;
			textSize = Game1.dialogueFont.MeasureString(text);
			b.DrawString(
				spriteFont: Game1.dialogueFont,
				text: text,
				position: this.Position
					+ new Vector2(x: (this.Size.X - textSize.X) / 2, y: 0)
					+ new Vector2(x: 0, y: -22) * scale,
				color: Color.White);

			// Score
			{
				bool isScoreMeterVisible = this.Game.Stage.Data.ScoreGoal > 0;
				Vector2 scorePosition = this.Position
					+ new Vector2(x: 0, y: this.Size.Y)
					+ new Vector2(x: 0, y: 10) * scale;
				Rectangle scoreRegion = new Rectangle(
					x: (int)(scorePosition.X),
					y: (int)(scorePosition.Y),
					width: (int)(size.X * tokenSize.X * scale),
					height: (int)(5 * scale));

				// bar
				if (isScoreMeterVisible)
				{
					// back
					b.Draw(
						texture: Game1.fadeToBlackRect,
						destinationRectangle: scoreRegion,
						color: Color.Plum * 0.3f);

					float ratio = Math.Clamp((float)(this.DisplayScore - this.Game.TotalScore) / stage.Data.ScoreGoal, 0, 1);
					int width = (int)(ratio * scoreRegion.Width);
					int offset = 0;

					// fill
					scoreRegion.Width = width;
					scoreRegion.X += offset;
					b.Draw(
						texture: Game1.fadeToBlackRect,
						destinationRectangle: scoreRegion,
						color: Color.Plum);
				}

				// text
				text = $"{this.DisplayScore}";
				textSize = Game1.dialogueFont.MeasureString(text);
				b.DrawString(
					spriteFont: Game1.dialogueFont,
					text: text,
					position: scorePosition
						+ new Vector2(x: (this.Size.X - textSize.X) / 2, y: -textSize.Y / 4),
					color: Color.White);
			}

			// Enemy character and enemy meters
			if (this.Game.Stage.Enemy is Enemy enemy && enemy.Data is not null && !isDialogue)
			{
				Vector2 enemyPosition = position + enemy.DrawPixel;
				enemy.Draw(
					b: b,
					position: enemyPosition,
					scale: scale);
				b.Draw(
					texture: enemy.Data.Texture,
					position: enemyPosition + enemy.Data.TextureRegions[(int)enemy.State].Size.ToVector2() / 2 * scale,
					sourceRectangle: enemy.Data.FrameTextureRegion,
					color: Color.White,
					rotation: 0,
					origin: enemy.Data.FrameTextureRegion.Size.ToVector2() / 2,
					scale: scale,
					effects: SpriteEffects.None,
					layerDepth: 1);

				if (false)
				{	// PRIMITIVE
					// Enemy life meter
					text = $"{enemy.Life}<";
					textSize = Game1.dialogueFont.MeasureString(text);
					b.DrawString(
						spriteFont: Game1.dialogueFont,
						text: text,
						position: meterPosition
							+ new Vector2(x: this.Size.X / 2 - textSize.X, y: 0)
							+ new Vector2(x: 64, y: 0) * scale,
						color: Color.White);

					// Enemy power meter
					text = $"{enemy.Power}=";
					textSize = Game1.dialogueFont.MeasureString(text);
					b.DrawString(
						spriteFont: Game1.dialogueFont,
						text: text,
						position: meterPosition
							+ new Vector2(x: this.Size.X / 2 - textSize.X, y: 0)
							+ new Vector2(x: 32, y: 0) * scale,
						color: Color.White);
				}
				else
				{	// GRAPHICAL
					bool isLifeMeterVisible = this.Game.Stage.Enemy?.Data is not null;
					bool isPowerMeterVisible = !this.Game.Stage.Data.NoTokenUpgrades;
					Vector2 lifePosition = position
						+ new Vector2(x: this.Size.X, y: 0)
						+ this.MenuData.EnemyMeterOffset * scale;
					Vector2 powerPosition = lifePosition
						+ new Vector2(x: this.MenuData.EnemyLifeTextureRegion.Width * scale, y: 0);
					Rectangle lifeRegion = new Rectangle(
						location: (lifePosition + this.MenuData.EnemyLifeFillRegion.Location.ToVector2() * scale).ToPoint(),
						size: (this.MenuData.EnemyLifeFillRegion.Size.ToVector2() * scale).ToPoint());
					Rectangle powerRegion = new Rectangle(
						location: (powerPosition + this.MenuData.EnemyPowerFillRegion.Location.ToVector2() * scale).ToPoint(),
						size: (this.MenuData.EnemyPowerFillRegion.Size.ToVector2() * scale).ToPoint());
					// Back
					if (isLifeMeterVisible)
					{
						b.Draw(
							texture: Game1.fadeToBlackRect,
							destinationRectangle: lifeRegion,
							color: Color.Red * 0.3f);
					}
					if (isPowerMeterVisible)
					{
						b.Draw(
							texture: Game1.fadeToBlackRect,
							destinationRectangle: powerRegion,
							color: Color.Blue * 0.3f);
					}
					// Fill
					// life
					float ratio = (float)enemy.Life / enemy.Data.LifeMax;
					int height = (int)(ratio * lifeRegion.Height);
					int offset = lifeRegion.Height - height;
					if (isLifeMeterVisible)
					{
						lifeRegion.Height = height;
						lifeRegion.Y += offset;
						b.Draw(
							texture: Game1.fadeToBlackRect,
							destinationRectangle: lifeRegion,
							color: Color.Red);
						b.Draw(
							texture: this.MenuData.MenuTexture,
							position: lifePosition,
							sourceRectangle: this.MenuData.EnemyLifeTextureRegion,
							color: Color.White,
							rotation: 0,
							origin: Vector2.Zero,
							scale: scale,
							effects: SpriteEffects.None,
							layerDepth: 1);
					}
					// power
					if (isPowerMeterVisible)
					{
						ratio = (float)enemy.Power / enemy.Data.PowerMax;
						height = (int)(ratio * powerRegion.Height);
						offset = powerRegion.Height - height;
						Rectangle powerRegion2 = powerRegion;
						powerRegion2.Height = height;
						powerRegion2.Y += offset;
						b.Draw(
							texture: Game1.fadeToBlackRect,
							destinationRectangle: powerRegion2,
							color: Color.Blue);
						b.Draw(
							texture: this.MenuData.MenuTexture,
							position: powerPosition,
							sourceRectangle: this.MenuData.EnemyPowerTextureRegion,
							color: Color.White,
							rotation: 0,
							origin: Vector2.Zero,
							scale: scale,
							effects: SpriteEffects.None,
							layerDepth: 1);
					}
				}

				// Enemy attacks
				float attackRatio = (float)enemy.AttackDrawTimer / enemy.Data.AttackDuration;
				b.Draw(
					texture: enemy.Data.Texture,
					position: position + this.Size / 2 + enemy.AttackDrawPixel,
					sourceRectangle: enemy.Data.AttackTextureRegion,
					color: Color.White * attackRatio,
					rotation: enemy.AttackRotation,
					origin: enemy.Data.AttackTextureRegion.Size.ToVector2() / 2,
					scale: scale * enemy.Data.AttackScale * attackRatio,
					effects: SpriteEffects.None,
					layerDepth: 1);
			}

			// Player character
			if (this.Game.Character is Character chara && chara.Data is not null)
			{
				Vector2 charaPosition = position + chara.DrawPixel;
				chara.Draw(
					b: b,
					position: charaPosition,
					scale: scale);
				b.Draw(
					texture: chara.Data.Texture,
					position: charaPosition + chara.PortraitSize / 2 * scale,
					sourceRectangle: chara.Data.FrameTextureRegion,
					color: Color.White,
					rotation: 0,
					origin: chara.Data.FrameTextureRegion.Size.ToVector2() / 2,
					scale: scale,
					effects: SpriteEffects.None,
					layerDepth: 1);
			}

			// Player meters
			if (false)
			{	// PRIMITIVE
				// Player life meter
				text = $"<{this.Game.Life}";
				textSize = Game1.dialogueFont.MeasureString(text);
				b.DrawString(
					spriteFont: Game1.dialogueFont,
					text: text,
					position: meterPosition
						+ new Vector2(x: -this.Size.X / 2, y: 0)
						+ new Vector2(x: -64, y: 0) * scale,
					color: Color.White);

				// Player power meter
				text = $"={this.Game.Power}";
				textSize = Game1.dialogueFont.MeasureString(text);
				b.DrawString(
					spriteFont: Game1.dialogueFont,
					text: text,
					position: meterPosition
						+ new Vector2(x: -this.Size.X / 2, y: 0)
						+ new Vector2(x: -32, y: 0) * scale,
					color: Color.White);
			}
			else
			{   // GRAPHICAL
				bool isLifeMeterVisible = this.Game.Stage.Enemy?.Data is not null;
				bool isPowerMeterVisible = !this.Game.Stage.Data.NoSpecialPowers && !this.Game.Stage.Data.NoTokenUpgrades;
				Vector2 lifePosition = position
					+ this.MenuData.CharacterMeterOffset * scale;
				Vector2 powerPosition = lifePosition
					- new Vector2(x: this.MenuData.CharacterPowerTextureRegion.Width * scale, y: 0);
				Rectangle lifeRegion = new Rectangle(
					location: (lifePosition + this.MenuData.CharacterLifeFillRegion.Location.ToVector2() * scale).ToPoint(),
					size: (this.MenuData.CharacterLifeFillRegion.Size.ToVector2() * scale).ToPoint());
				Rectangle powerRegion = new Rectangle(
					location: (powerPosition + this.MenuData.CharacterPowerFillRegion.Location.ToVector2() * scale).ToPoint(),
					size: (this.MenuData.CharacterPowerFillRegion.Size.ToVector2() * scale).ToPoint());
				// Back
				if (isLifeMeterVisible)
				{
					b.Draw(
						texture: Game1.fadeToBlackRect,
						destinationRectangle: lifeRegion,
						color: Color.Red * 0.3f);
				}
				if (isPowerMeterVisible)
				{
					b.Draw(
						texture: Game1.fadeToBlackRect,
						destinationRectangle: powerRegion,
						color: Color.Blue * 0.3f);
				}
				// Fill
				// life
				float ratio = (float)this.Game.Life / this.GameData.LifeMax;
				int height = (int)(ratio * lifeRegion.Height);
				int offset = lifeRegion.Height - height;
				if (isLifeMeterVisible)
				{
					lifeRegion.Height = height;
					lifeRegion.Y += offset;
					b.Draw(
						texture: Game1.fadeToBlackRect,
						destinationRectangle: lifeRegion,
						color: Color.Red);
					b.Draw(
						texture: this.MenuData.MenuTexture,
						position: lifePosition,
						sourceRectangle: this.MenuData.CharacterLifeTextureRegion,
						color: Color.White,
						rotation: 0,
						origin: Vector2.Zero,
						scale: scale,
						effects: SpriteEffects.None,
						layerDepth: 1);
				}
				// power
				if (isPowerMeterVisible)
				{
					ratio = (float)this.Game.Power / this.GameData.PowerMax;
					height = (int)(ratio * powerRegion.Height);
					offset = powerRegion.Height - height;
					Rectangle powerRegion2 = powerRegion;
					powerRegion2.Height = height;
					powerRegion2.Y += offset;
					b.Draw(
						texture: Game1.fadeToBlackRect,
						destinationRectangle: powerRegion2,
						color: Color.Blue);
					b.Draw(
						texture: this.MenuData.MenuTexture,
						position: powerPosition,
						sourceRectangle: this.MenuData.CharacterPowerTextureRegion,
						color: Color.White,
						rotation: 0,
						origin: Vector2.Zero,
						scale: scale,
						effects: SpriteEffects.None,
						layerDepth: 1);
					// Icons
					if (ratio >= 0.5f)
					{
						b.Draw(
							texture: this.MenuData.MenuTexture,
							position: powerPosition
								+ new Vector2(x: 0, y: (this.MenuData.CharacterPowerTextureRegion.Height - this.MenuData.CharacterPowerMinorTextureRegion.Height) * scale / 2),
							sourceRectangle: this.MenuData.CharacterPowerMinorTextureRegion,
							color: Color.White,
							rotation: 0,
							origin: Vector2.Zero,
							scale: scale,
							effects: SpriteEffects.None,
							layerDepth: 1);
					}
					if (ratio >= 1f)
					{
						b.Draw(
							texture: this.MenuData.MenuTexture,
							position: powerPosition,
							sourceRectangle: this.MenuData.CharacterPowerMajorTextureRegion,
							color: Color.White,
							rotation: 0,
							origin: Vector2.Zero,
							scale: scale,
							effects: SpriteEffects.None,
							layerDepth: 1);
					}
				}
			}

			// Match particles
			foreach (TokenParticle particle in this._matchParticles.Items)
			{
				if (particle.IsReady)
				{
					if (particle.Counter > 0)
					{
						// Match counter (e.g. 3x)
						int i = Math.Clamp(particle.Counter, min: 0, max: 9);
						float particleScale = ((particle.Limit - particle.Ratio) * scale + i * 0.25f) * particle.Ratio / 2;
						if (particle.Counter > this.Game.Stage.Data.Match)
						{
							b.Draw(
								texture: this.MenuData.MenuTexture,
								position: position
									+ particle.DrawPixel
									+ new Vector2(x: -this.MenuData.DigitRectangles[i].Width, y: 0) * particleScale / 2,
								sourceRectangle: this.MenuData.DigitRectangles[i],
								color: particle.TextColor * particle.Ratio,
								rotation: particle.DrawRotation,
								origin: this.MenuData.DigitRectangles[i].Size.ToVector2() / 2,
								scale: particleScale,
								effects: SpriteEffects.None,
								layerDepth: 1);
							b.Draw(
								texture: this.MenuData.MenuTexture,
								position: position
									+ particle.DrawPixel
									+ new Vector2(x: -this.MenuData.DigitRectangles[i].Width, y: 0) * particleScale / 2
									+ new Vector2(x: this.MenuData.DigitRectangles[i].Width, y: 0) * particleScale
									,
								sourceRectangle: this.MenuData.DigitRectangles[^1],
								color: particle.TextColor * particle.Ratio,
								rotation: particle.DrawRotation,
								origin: this.MenuData.DigitRectangles[i].Size.ToVector2() / 2,
								scale: particleScale,
								effects: SpriteEffects.None,
								layerDepth: 1);
						}
					}
					else
					{
						// Explode
						b.Draw(
							texture: particle.Data.Texture,
							position: position
								+ particle.DrawPixel,
							sourceRectangle: particle.Data.ExplodeTextureRegion,
							color: particle.Data.ExplodeColour * particle.Ratio,
							rotation: particle.DrawRotation,
							origin: particle.Data.ExplodeTextureRegion.Size.ToVector2() / 2,
							scale: (particle.Limit - particle.Ratio * 1.5f) * scale,
							effects: SpriteEffects.None,
							layerDepth: 1);
					}
				}
			}

			// Stage overlay
			if (stage?.State is not StageState.Active)
			{
				float textScale = 1;

				if (isDialogue)
				{
					// overlay
					b.Draw(
						texture: Game1.fadeToBlackRect,
						destinationRectangle: board,
						color: Color.Black * 0.75f);

					// Dialogue

					// dialogue character
					if (this.Game.Stage.DialogueCharacter is Character dchara && dchara.Data is not null)
					{
						Vector2 charaPosition = position + dchara.DrawPixel;
						dchara.Draw(
							b: b,
							position: charaPosition,
							scale: scale,
							flip: true);
						b.Draw(
							texture: dchara.Data.Texture,
							position: charaPosition + dchara.PortraitSize / 2 * scale,
							sourceRectangle: dchara.Data.FrameTextureRegion,
							color: Color.White,
							rotation: 0,
							origin: dchara.Data.FrameTextureRegion.Size.ToVector2() / 2,
							scale: scale,
							effects: SpriteEffects.FlipHorizontally,
							layerDepth: 1);
					}
					// position and size
					SpriteFont font = Game1.dialogueFont;
					Vector2 dialoguePadding = new Vector2(x: 4, y: 2) * scale;
					Vector2 dialogueMargin = new Vector2(x: 4, y: 2) * scale;
					Vector2 dialogueOffset = new Vector2(x: 0, y: 8) * scale;
					Point dialogueSize = new(
						x: (int)(board.Width - dialogueMargin.X * 2),
						y: (int)(32 * scale - dialogueMargin.Y * 2));
					int textWidth = dialogueSize.X - (int)(dialoguePadding.X * 2) - (int)(8 * scale);
					// text
					text = stage.DialogueText[..stage.DialogueTextIndex];
					text = Game1.parseText(
						text: text,
						whichFont: font,
						width: textWidth);
					textSize = font.MeasureString(Game1.parseText(
						text: stage.DialogueText, // use size of full text
						whichFont: font,
						width: textWidth));
					dialogueSize.Y = Math.Max(dialogueSize.Y, (int)(textSize.Y + 6 * scale)); // correct for sdv texturebox
					// bounds
					Rectangle dialogueBounds = new(
						x: (int)(position.X - dialogueOffset.X - dialogueMargin.X),
						y: (int)(position.Y - dialogueOffset.Y - dialogueMargin.Y * 2 - dialogueSize.Y + board.Height),
						width: dialogueSize.X,
						height: dialogueSize.Y);
					// dialogue box
					IClickableMenu.drawTextureBox(
						b: b,
						x: dialogueBounds.X,
						y: dialogueBounds.Y,
						width: dialogueBounds.Width,
						height: dialogueBounds.Height,
						color: Color.White);
					// dialogue text
					b.DrawString(
						spriteFont: font,
						text: text,
						position: dialogueBounds.Location.ToVector2() + dialogueMargin + dialoguePadding,
						color: Game1.textColor,
						rotation: 0,
						origin: Vector2.Zero,
						scale: textScale,
						effects: SpriteEffects.None,
						layerDepth: 1);
					// advance or end dialogue prompt
					if (stage.Time > this.MenuData.DialogueIgnoreInputTime && stage.DialogueTextIndex >= stage.DialogueText.Length)
					{
						Rectangle region;
						Vector2 promptPosition = new Vector2(x: dialogueBounds.Right, y: dialogueBounds.Bottom);
						float offset = (float)Math.Cos(this.Ms * Math.PI / 512d) * scale; //(float)(Math.Sin(Game1.currentGameTime.TotalGameTime.Milliseconds / 3000f % 30f) * 16 * scale);
						if (stage.DialogueIndex < stage.Data.Dialogue.Length - 1)
						{
							promptPosition.X += offset;
							region = this.MenuData.DialogueAdvanceTextureRegion;
						}
						else
						{
							promptPosition.Y += offset;
							region = this.MenuData.DialogueEndTextureRegion;
						}
						b.Draw(
							texture: this.MenuData.MenuTexture,
							position: promptPosition,
							sourceRectangle: region,
							color: Color.White,
							rotation: 0,
							origin: region.Size.ToVector2() / 2,
							scale: scale,
							effects: SpriteEffects.None,
							layerDepth: 1);
					}
				}
				else
				{
					// overlay
					b.Draw(
						texture: Game1.fadeToBlackRect,
						destinationRectangle: board,
						color: Color.Black * 0.75f);

					// Non-dialogue overlays

					if (stage.State is StageState.Start)
					{
						long ms = (stage.Data.StartDelay - stage.Time);
						int sec = (int)Math.Floor(ms / 1000f);
						textScale = 2 + (ms / 1000f) % 1;
						text = sec > 0 ? $"{sec}" : "PLAY!";
					}
					else if (stage.State is StageState.End)
					{
						text = stage.IsWon ? "CLEAR!" : "FAIL..";
					}
					else if (stage.State is StageState.Pause)
					{
						text = "PAUSE";
					}
					textSize = Game1.dialogueFont.MeasureString(text);
					b.DrawString(
						spriteFont: Game1.dialogueFont,
						text: text,
						position: position
							+ this.Size / 2,
						color: Color.White,
						rotation: 0,
						origin: textSize / 2,
						scale: textScale,
						effects: SpriteEffects.None,
						layerDepth: 1);
				}
			}

			// Cursor
			{
				b.Draw(
					texture: this.MenuData.CursorTexture,
					position: this.RealCursor + shake / 2,
					sourceRectangle: this.MenuData.CursorTextureRegion,
					color: Color.White,
					rotation: 0,
					origin: Vector2.Zero,
					scale: this.MenuData.CursorScale,
					effects: SpriteEffects.None,
					layerDepth: 1);
			}
		}
	}
}
