using Netcode;
using StardewModdingAPI;
using StardewValley.Menus;
using System;
using System.Linq;

namespace Hikawa.Volleyball
{
    public class VolleyballLocation : GameLocation
    {
		public static readonly Vector2 UmpirePosition = new(x: 33, y: 26.5f);
		public static readonly Vector2 NetSize = new(x: Game1.tileSize / 16f, y: Game1.tileSize * 1.5f);
        public static readonly Rectangle PlayArea = new(x: 23, y: 29, width: 17, height: 11);
		public static Vector2 PlayAreaCentre => (Utility.PointToVector2(VolleyballLocation.PlayArea.Center) + new Vector2(x: 0.5f, y: 0)) * Game1.tileSize;

		public Volleyball Volleyball;
		public VolleyballUmpireData UmpireData;

        public NetCollection<Character> Players;
        public NetInt ScoreL;
        public NetInt ScoreR;
        public NetInt ScoreGoal;

        public VolleyballLocation() : base(mapPath: "Maps/" + ModEntry.ModData.MapVolleyball, name: "Temp/" + ModEntry.ModData.MapVolleyball)
        {
			// ???
        }

        protected override void initNetFields()
        {
            base.initNetFields();

			this.Players = new();
            this.ScoreL = new(0);
            this.ScoreR = new(0);
            this.ScoreGoal = new(0);

			this.NetFields
                .AddField(this.Players, nameof(this.Players))
                .AddField(this.ScoreL, nameof(this.ScoreL))
                .AddField(this.ScoreR, nameof(this.ScoreR))
				.AddField(this.ScoreGoal, nameof(this.ScoreGoal));
		}

        public static VolleyballLocation MakeTemp()
        {
            VolleyballLocation location = new();
            Game1._locationLookup.TryAdd(key: location.Name, value: location);
            return location;
        }

		public Character GetPlayer(string name)
		{
			return this.Players.FirstOrDefault(c => c.Name == name);
		}

		public VolleyballUmpireData GetUmpireData(string name)
		{
			if (name == ModEntry.ModData.NpcCat)
			{
				Point size = new(x: 32, y: 32);
				return new VolleyballUmpireData(
					name: name + ModEntry.ModData.NpcVolleyballSuffix,
					displayName: NPC.GetDisplayName(name),
					textureName: AssetManager.CatSpritesAssetName,
					sourceRectangle: new(x: 0, y: size.Y * 4, width: size.X, height: size.Y),
					initialFrame: 16,
					portraitFrame: 5,
					isBreathing: false);
			}
			return new VolleyballUmpireData(
				name: name + ModEntry.ModData.NpcVolleyballSuffix,
				displayName: NPC.GetDisplayName(name),
				textureName: $"Characters/{name}",
				sourceRectangle: new Rectangle(x: 0, y: 0, width: 16, height: 32),
				initialFrame: 0,
				portraitFrame: 0,
				isBreathing: true);
		}

		public void HandleInput(SButton button)
		{
			Farmer who = Game1.player;
			if (Game1.activeClickableMenu is null && who.TemporaryItem is VolleyballItem item && who.freezePause <= 0 && Game1.fadeToBlackAlpha <= 0)
			{
				if (button.IsUseToolButton())
				{
					item.leftClick(who: who);
				}
				else if (button.IsActionButton() && who.yJumpOffset >= 0 && !who.UsingTool)
				{
					item.animateSpecialMove(who: who);
				}
			}
		}

        public void SetUpLocation(VolleyballRules rules, string umpireName, bool skipMenu)
        {
            // Components
            VolleyballHUD.Init();

            // Warp to temp location
            LocationRequest locationRequest = Game1.getLocationRequest(locationName: this.Name);
            locationRequest.OnWarp += () =>
			{
				// Umpire
				this.UmpireData = this.GetUmpireData(umpireName);

				if (Context.IsMainPlayer)
				{
					// Umpire
					AnimatedSprite sprite = new(
						textureName: this.UmpireData.TextureName,
						currentFrame: this.UmpireData.InitialFrame,
						spriteWidth: this.UmpireData.SourceRectangle.Width,
						spriteHeight: this.UmpireData.SourceRectangle.Height);
					NPC umpire = new(
						sprite: sprite,
						position: VolleyballLocation.UmpirePosition * Game1.tileSize,
						facingDir: Game1.down,
						name: this.UmpireData.Name)
					{
						Breather = this.UmpireData.IsBreathing,
						displayName = this.UmpireData.DisplayName
					};
					umpire.Sprite.CurrentFrame = this.UmpireData.InitialFrame;
					//umpire.syncedPortraitPath.Value = $"Portraits/{umpireName}";
					this.addCharacter(character: umpire);

					if (skipMenu)
					{
						this.SetUpGame(rules: rules);
					}
					else
					{
						// Build rules from VolleyballPicker
						VolleyballMenu menu = null;
						Character[] characters = [
							Game1.player,
							VolleyballNPC.MakeFor(ModEntry.ModData.NpcRei)
						]; // TODO: DEBUG: REMOVE THIS
						menu = new(rules)
						{
							exitFunction = () => this.SetUpGame(rules: menu.Rules)
						};
						Game1.pauseThenDoFunction(pauseTime: 750, function: () =>
						{
							Game1.activeClickableMenu = menu;
						});
					}
				}
                else
                {
                    // Open client block
                    Game1.activeClickableMenu = new VolleyballClientDialogueBox(
                        onConfirm: (Farmer who) =>
                        {
							// TODO: MAKE THIS WORK
							// MENU HAS NO CHARACTERS FOR CLIENTS
							this.SetUpGame(rules: rules);
						},
                        onCancel: (Farmer who) =>
                        {
                            // ???
                        });
                }
            };
            Game1.warpFarmer(
                locationRequest: locationRequest,
                tileX: 0,
                tileY: 0,
                facingDirectionAfterWarp: Game1.player.FacingDirection);
        }

		public void SetUpGame(VolleyballRules rules)
		{
			// Set game state
			Game1.displayHUD = false;
			Game1.player.TemporaryItem = new VolleyballItem();

			if (Context.IsMainPlayer)
			{
				// Game rules
				this.ScoreGoal.Set(rules.ScoreGoal);

				// Volleyball
				this.Volleyball = new(this, rules);
				this.Volleyball.TouchPlayerEvent.onEvent += this.OnTouchPlayer;
				this.Volleyball.TouchGroundEvent.onEvent += this.OnTouchGround;

				// Game players
				this.Players.Clear();
				Character[] players = rules.Players.ToArray();
				foreach (Character player in players)
				{
					this.Players.Add(player);
					if (player is VolleyballNPC npc)
					{
						npc.Volleyball = this.Volleyball;
					}
				}

				// Start game
				this.StartRound();
			}
		}

		public void ResetPlayerPositions()
		{
			for (int i = 0; i < this.Players.Count; ++i)
			{
				if (this.Players[i] is not null)
				{
					if (this.Players[i] is Farmer farmer)
					{
						farmer.completelyStopAnimatingOrDoingAction();
					}

					bool isLeftTeam = i < this.Players.Count / 2; // Whether player is on left side of play area
					int xFlipPerTeam = isLeftTeam ? -1 : 1; // Side of centre per player
					int xOffsetPerPlayer = Game1.tileSize * 2;
					this.Players[i].Position = VolleyballLocation.PlayAreaCentre
						+ new Vector2(x: (i % 2 + 1) * xOffsetPerPlayer * xFlipPerTeam, y: 0);
				}
			}
		}

        public void StartRound()
		{
			this.ResetPlayerPositions();

			int round = this.ScoreL.Value + this.ScoreR.Value;
			Character characterToServe = this.Players[round % 2 == 0 ? 0 : this.Players.Count / 2];
			Vector2 position = characterToServe.getStandingPosition();

			this.Volleyball.Start(
				position: new Vector3(
					x: position.X,
					y: position.Y,
					z: characterToServe.Sprite.SpriteHeight * Game1.pixelZoom),
				character: characterToServe,
				isLeftSidePlayerStarting: position.X < VolleyballLocation.PlayAreaCentre.X);
		}

        public void OnTouchPlayer()
        {
            Character character = this.GetPlayer(this.Volleyball.LastHitBy.Value);
			// ???
        }

        public void OnTouchGround(Vector2 position)
		{
            bool isLandingOnLeftSide = position.X < VolleyballLocation.PlayAreaCentre.X;
            bool isLastHitByLeftSide = this.GetPlayer(this.Volleyball.LastHitBy.Value).Position.X < VolleyballLocation.PlayAreaCentre.X;
			bool isFumbled = isLastHitByLeftSide == isLandingOnLeftSide;
            bool isInBounds = VolleyballLocation.PlayArea.Contains(position / Game1.tileSize);
            bool isLeftSideWin = isLastHitByLeftSide == (!isFumbled && isInBounds);

			// Add score
            ++(isLeftSideWin ? this.ScoreL : this.ScoreR).Value;
			bool isGameWin = this.ScoreL.Value >= this.ScoreGoal.Value || this.ScoreR.Value >= this.ScoreGoal.Value;

			// Show message
			Character leftPlayer = this.Players[0];
			Character rightPlayer = this.Players[this.Players.Count / 2];
			string winner = isLeftSideWin ? leftPlayer.displayName : rightPlayer.displayName;
            string loser = isLeftSideWin ? rightPlayer.displayName : leftPlayer.displayName;
            string messageKey = !isInBounds
				? "ui.volleyball.score.out"
				: isFumbled
					? "ui.volleyball.score.drop"
					: "ui.volleyball.score.score";
			object tokens = new { winner = winner, loser = loser, team = this.Players.Count <= 2 ? null : ModEntry.I18n.Get("ui.volleyball.score.team") };
			string message = ModEntry.I18n.Get(messageKey, tokens);

			void endFunc()
			{
				if (Game1.fadeToBlackAlpha <= 0)
				{
					// Hide message
					if (Game1.activeClickableMenu is DialogueBox db)
					{
						db.closeDialogue();
					}
					Game1.exitActiveMenu();

					// End or continue ongoing game
					if (isGameWin)
					{
						this.OnGameEnd();
					}
					else
					{
						this.OnRoundEnd();
					}
				}
			}

			int endDelay;
			if (isGameWin)
			{
				const int addedDelay = 1500;
				endDelay = 7000 + addedDelay;

				NPC umpire = this.getCharacterFromName(name: this.UmpireData.Name);
				umpire.shake(500);
				this.playSound("whistle");

				DelayedAction.functionAfterDelay(
					func: () => umpire.shakeTimer = 0,
					delay: 500);

				DelayedAction.functionAfterDelay(func: () =>
				{
					string winMessage = ModEntry.I18n.Get("ui.volleyball.score.win", tokens);
					umpire.CurrentDialogue.Clear();
					umpire.CurrentDialogue.Push(new Dialogue(
						speaker: umpire,
						translationKey: null,
						dialogueText: $"${this.UmpireData.PortraitFrame}{message} {winMessage}"));
					Game1.drawDialogue(speaker: umpire);
					Game1.activeClickableMenu.exitFunction = () => endFunc();
				}, delay: addedDelay);
			}
			else
			{
				endDelay = 3000;
				Game1.activeClickableMenu = new DialogueBox(dialogue: message);
			}
			Game1.player.completelyStopAnimatingOrDoingAction();
			Game1.player.freezePause = endDelay + 500;
            DelayedAction.functionAfterDelay(func: endFunc, delay: endDelay);
        }

		public void OnRoundEnd()
		{
			// Replace ball
			TemporaryAnimatedSprite puff = new(
				textureName: "TileSheets/animations",
				sourceRect: new Rectangle(0, 320, 64, 64),
				animationInterval: 50f,
				animationLength: 8,
				numberOfLoops: 0,
				position: this.Volleyball.Position.Value + Utility.PointToVector2(this.Volleyball.BallData.SourceArea.Size) * -this.Volleyball.Scale / 2,
				flicker: false,
				flipped: false)
			{
				scale = 1f,
				alpha = 0.95f,
				alphaFade = 0.01f
			};
			Game1.Multiplayer.broadcastSprites(location: this, sprites: new[] { puff });

			// Continue game
			this.StartRound();
		}

		public void OnGameEnd()
		{
			// Return to previous location
			LocationRequest locationRequest = Game1.getLocationRequest(locationName: "FarmHouse"); // DEBUG: REMOVE THIS
			Game1.warpFarmer(
				locationRequest: locationRequest,
				tileX: 5,
				tileY: 5,
				facingDirectionAfterWarp: Game1.player.FacingDirection);
			locationRequest.OnWarp += () =>
			{
				// Reset game state
				Game1.displayHUD = true;
				Game1.player.TemporaryItem = null;
			};
		}

		public override void UpdateWhenCurrentLocation(GameTime time)
        {
            base.UpdateWhenCurrentLocation(time: time);

			// Player state
			Game1.player.canOnlyWalk = true; // Prevent player interactions
			Game1.player.running = true; // Override walk animation frames

			// Host updates only
			if (Context.IsMainPlayer && Game1.activeClickableMenu is not GameMenu)
			{
				// Players
				foreach (Character character in this.Players)
				{
					if (character is not Farmer)
					{
						character.update(time: time, location: this);
					}
				}

				// Volleyball
				this.Volleyball?.Update(time: time);
			}
        }

		protected override void drawCharacters(SpriteBatch b)
		{
			base.drawCharacters(b);

			// Players
			foreach (Character character in this.Players)
			{
				if (character is not Farmer)
				{
					character.draw(b: b);
				}
			}
		}

		protected override void drawFarmers(SpriteBatch b)
		{
			if (this.Volleyball is not null)
			{
				// Shadow
				float scale = Math.Clamp(value: this.Volleyball.Scale - this.Volleyball.zPosition.Value / 60f, min: 1.5f, max: 6.5f);
				b.Draw(
					texture: Game1.shadowTexture,
					position: Game1.GlobalToLocal(
						viewport: Game1.viewport,
						globalPosition: this.Volleyball.Position.Value
							//- new Vector2(x: this.SourceArea.Width / 2, y: this.SourceArea.Height) * this.Scale
							+ new Vector2(-scale)
					),
					sourceRectangle: Game1.shadowTexture.Bounds,
					color: Color.White * 0.75f,
					rotation: 0,
					origin: Utility.PointToVector2(Game1.shadowTexture.Bounds.Center),
					scale: scale,
					effects: SpriteEffects.None,
					layerDepth: (this.Volleyball.Position.Value.Y - 1f) / 10000f);

			}
			if (this.Volleyball?.IsInPlay.Value is not null and true && Game1.activeClickableMenu is null)
			{
				// Player aimpoints
				Vector2 viewpoint = new Vector2(x: Game1.viewport.X, y: Game1.viewport.Y);
				Vector2 player = Game1.player.getStandingPosition();
                Vector2 cursor = Utility.PointToVector2(Game1.getMousePosition(ui_scale: false));
				Vector2 distance = Vector2.Clamp(value1: Utils.Vector.Abs(vector: player - viewpoint - cursor), min: Vector2.Zero, max: new Vector2(Game1.tileSize * 3));

				Rectangle source = AssetManager.ExtraSpritesVolleyballAimpointArea;
				source.X += Math.Clamp(this.Players.IndexOf(Game1.player) * source.Width, min: 0, max: 4);
				b.Draw(
					texture: ModEntry.Sprites,
					sourceRectangle: source,
					position: cursor,
					color: Color.White,
					rotation: (float)Utils.Vector.RadiansBetween(cursor + viewpoint, player),
					origin: Utility.PointToVector2(source.Size) / 2,
					scale: Game1.pixelZoom + (distance.X + distance.Y) / Game1.tileSize / 3,
					effects: SpriteEffects.None,
					layerDepth: 1f);
			}
            
			base.drawFarmers(b);
		}

		public override void drawAboveAlwaysFrontLayer(SpriteBatch b)
        {
            base.drawAboveAlwaysFrontLayer(b);

            if (false && ModEntry.Config.DebugMode && this.Volleyball is not null)
			{
				// test - collision box
				Rectangle bounds = Game1.player.GetBoundingBox();
				b.Draw(
					texture: Game1.fadeToBlackRect,
					destinationRectangle: new Rectangle(
						x: bounds.X - Game1.viewport.X,
						y: bounds.Y - Game1.viewport.Y,
						width: bounds.Width,
						height: bounds.Height),
					color: Color.Red * 0.5f);

				// test - adjusted collision area
				for (int i = 0; i < this.Players.Count; ++i)
				{
					if (this.Players[i] is Character player)
                    {
                        Rectangle collisionArea = Game1.GlobalToLocal(Game1.viewport, this.Volleyball.GetCharacterCollisionArea(character: player));
                        collisionArea.Y += player.yJumpOffset * 2;
                        b.Draw(
                            texture: Game1.fadeToBlackRect,
                            destinationRectangle: collisionArea,
                            color: Color.Blue * ((this.Volleyball.LastHitBy.Value == player.Name && this.Volleyball.TravelTime.Value < this.Volleyball.TravelTimeBeforeHit) ? 0.2f : 0.5f));
                    }
                }

				// test: ball collision area
				b.Draw(
					texture: ModEntry.Sprites,
					destinationRectangle: new Rectangle(
						x: (int)this.Volleyball.Position.X - Game1.viewport.X,
						y: (int)this.Volleyball.Position.Y - Game1.viewport.Y,
						width: this.Volleyball.CollisionSize,
						height: this.Volleyball.CollisionSize),
					sourceRectangle: this.Volleyball.BallData.SourceArea,
					color: Color.Green * 0.6f,
					rotation: 0,
					origin: this.Volleyball.BallData.SourceArea.Size.ToVector2() / 2,
					effects: SpriteEffects.None,
					layerDepth: 1f);

			} // DEBUG: REMOVE THIS

			for (int i = 0; i < this.Players.Count; ++i)
            {
                if (this.Players[i] is not null)
				{
					// Player draw behaviours
					if (this.Players[i] is not Farmer)
					{
						this.Players[i].drawAboveAlwaysFrontLayer(b: b);
					}

					// Player tags
					Rectangle source = AssetManager.ExtraSpritesVolleyballPlayerTagArea;
                    source.X += i * source.Width;
					b.Draw(
						texture: ModEntry.Sprites,
						sourceRectangle: source,
						position: this.Players[i].getLocalPosition(viewport: Game1.viewport)
                            + new Vector2(x: 0, y: this.Players[i].yJumpOffset) * 1
							+ new Vector2(x: this.Players[i].Sprite.SpriteWidth / 2, y: -this.Players[i].Sprite.SpriteHeight) * Game1.pixelZoom
							+ new Vector2(x: 0, y: -4) * Game1.pixelZoom,
						color: Color.White,
						rotation: 0,
						origin: Utility.PointToVector2(source.Size) / 2,
						scale: Game1.pixelZoom,
						effects: SpriteEffects.None,
						layerDepth: 1f);
                }
			}

			// Volleyball
			this.Volleyball?.Draw(b: b);

			// HUD
			VolleyballHUD.Draw(
                b: b,
                characters: /*this.Players.ToArray()*/ new Character[] {
					Game1.player,
					Game1.getCharacterFromName(ModEntry.ModData.NpcRei)/*,
					Game1.getCharacterFromName(ModEntry.ModData.NpcAmi),
					Game1.getCharacterFromName(ModEntry.ModData.NpcGramps)*/
				}, // DEBUG: REMOVE THIS
                scoreL: this.ScoreL.Value,
                scoreR: this.ScoreR.Value);
		}
    }
}
