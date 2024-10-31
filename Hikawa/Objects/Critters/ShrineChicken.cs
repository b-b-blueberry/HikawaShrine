using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Pathfinding;
using System;
using System.Linq;

namespace Hikawa.Objects.Critters
{
	public class ShrineChicken : Monster
	{
		private Farmer _angryAtFarmer;
		public Farmer AngryAtFarmer
		{
			get
			{
				return this._angryAtFarmer;
			}
			set
			{
				bool calm = value is null;
				this.Speed = calm ? 2 : 6;
				this.Slipperiness = calm ? 5 : 10;
				this.Sprite.interval = calm ? 175 : 100;
				this.jitteriness.Value = calm ? 0 : 0.25d;
				this.DamageToFarmer = calm ? 0 : 3 + value.CombatLevel + value.maxHealth / 50;
				this.IsDetermined = !calm;
				this.ResetAnimation();
				this._angryAtFarmer = value;
			}
		}
		public ICue SoundCue;
		public bool IsBrown;
		public bool IsDetermined;
		public bool IsAnimating => this.Sprite?.CurrentFrame > 16;
		public bool CanAnimate => this.AngryAtFarmer is null && this.controller is null && !this.IsAnimating && this.timeBeforeAIMovementAgain <= 0;
		public string Colour => this.IsBrown ? "Brown" : "White";

		public ShrineChicken(GameLocation where, Vector2 position, bool isBrown = false)
		{
			this.Name = ModConsts.ContentPrefix + "Chicken";
			this.displayName = Game1.content.LoadString($"Strings/FarmAnimals:DisplayType_Chicken_{this.Colour}");
			this.currentLocation = where;
			this.DefaultPosition = this.Position = position;
			this.IsBrown = isBrown;
			this.reloadSprite();

			this.Breather = false;
			this.HideShadow = true;
			this.IsWalkingTowardPlayer = false;
			this.willDestroyObjectsUnderfoot = false;
			this.collidesWithOtherCharacters.Value = false;
			this.farmerPassesThrough = true;

			this.mineMonster.Value = false;
			this.isHardModeMonster.Value = false;
			//this.durationOfRandomMovements.Value = 1000;
			this.MaxHealth = this.Health =  this.resilience.Value = 99999;

			this.AngryAtFarmer = null;
		}

		public void ReturnHome()
		{
			// Reset angry state on player lost
			this.AngryAtFarmer = null;
			this.Speed = 4;
			this.controller = new PathFindController(
				c: this,
				location: this.currentLocation,
				finalFacingDirection: -1,
				endPoint: Utility.Vector2ToPoint(this.DefaultPosition / Game1.tileSize));
			this.controller.endBehaviorFunction = (Character chicken, GameLocation where) =>
			{
				chicken.Speed = 2;
				chicken.controller = null;
				chicken.Halt();
				//chicken.timeBeforeAIMovementAgain = 3300;
			};
		}

		public void TryPlaySound(double chance = 1, bool pitched = false)
		{
			if (chance == 1 ||
				(Game1.random.NextDouble() <= chance
					&& (this.SoundCue == null || !this.SoundCue.IsPlaying)
					&& Game1.currentLocation == this.currentLocation
					&& Utility.isOnScreen(positionNonTile: this.Position, acceptableDistanceFromScreen: Game1.tileSize)))
			{
				Game1.playSound(
					cueName: "cluck",
					pitch: pitched ? (byte)Game1.random.Next(1, 24) * 100 + Game1.random.Next(-100, 100) : 0,
					cue: out this.SoundCue);
			}
		}
		
		public void TryAnimate(GameTime time, double chance = 1)
		{
			if (!this.IsAnimating && this.timeBeforeAIMovementAgain <= 0 && (chance == 1 ||  Game1.random.NextDouble() <= chance))
			{
				// From: StardewValley.FarmAnimal.cs
				this.Sprite.loop = true;
				int interval = Game1.random.Next(70, 100);
				this.Sprite.setCurrentAnimation(new ()
				{
					new (frame: 24, milliseconds: interval),
					new (frame: 25, milliseconds: interval),
					new (frame: 26, milliseconds: interval),
					new (frame: 27, milliseconds: interval, secondaryArm: false, flip: false, frameBehavior: (Farmer who) => {
						/*if (Utility.isOnScreen(positionNonTile: this.Position, acceptableDistanceFromScreen: Game1.tileSize))
						{
							Game1.playSound("sandyStep");
						}*/
						if (Game1.random.NextDouble() < 0.3)
						{
							this.timeBeforeAIMovementAgain = 1100;
							this.ResetAnimation();
							this.Sprite.AnimateDown(gameTime: time);
						}
					})
				});
			}
		}

		public void ResetAnimation()
		{
			this.Sprite.StopAnimation();
			this.Sprite.ClearAnimation();
			this.Sprite.CurrentAnimation = null;
			this.Sprite.currentFrame = 0;
			this.Sprite.UpdateSourceRect();
			this.faceDirection(Game1.down);
			this.Sprite.loop = false;
		}

		public override void reloadSprite(bool onlyAppearance = false)
		{
			this.Sprite = new AnimatedSprite(
				textureName: $"Animals/{this.Colour} Chicken",
				currentFrame: 0,
				spriteWidth: 16,
				spriteHeight: 16);
		}

		public override void shedChunks(int number, float scale)
		{
			// From: StardewValley.Monsters.Monster.cs
			int size = AssetManager.ExtraSpritesFeathersArea.Height;
			Game1.createRadialDebris(
				location: this.currentLocation,
				texture: AssetManager.ExtraSpritesAssetName,
				sourcerectangle: new Rectangle(
					x: AssetManager.ExtraSpritesFeathersArea.X
						+ Game1.random.Next(AssetManager.ExtraSpritesFeathersArea.Width / size)
						+ (this.IsBrown ? AssetManager.ExtraSpritesFeathersArea.Width : 0),
					y: AssetManager.ExtraSpritesFeathersArea.Y,
					width: size,
					height: size),
				sizeOfSourceRectSquares: size,
				xPosition: this.GetBoundingBox().Center.X,
				yPosition: this.GetBoundingBox().Center.Y,
				numberOfChunks: number,
				groundLevelTile: this.TilePoint.Y,
				color: Color.White,
				scale: Game1.pixelZoom * scale);
		}

		public override void InitializeForLocation(GameLocation location)
		{
			if (this.initializedForLocation)
			{
				return;
			}
			this.initializedForLocation = true;
		}

		protected override Farmer findPlayer()
		{
			return this.AngryAtFarmer ?? Game1.player;
		}

		public override bool OverlapsFarmerForDamage(Farmer who)
		{
			return this.AngryAtFarmer == who && base.OverlapsFarmerForDamage(who);
		}

		public override bool withinPlayerThreshold(int threshold)
		{
			Vector2 playerTile = this.AngryAtFarmer?.Tile ?? Vector2.Zero;
			return playerTile != Vector2.Zero
				&& Math.Abs(this.Tile.X - playerTile.X) <= threshold
				&& Math.Abs(this.Tile.Y - playerTile.Y) <= threshold;
		}

		public override void onDealContactDamage(Farmer who)
		{
			this.shedChunks(Game1.random.NextDouble() < 0.5 ? 2 : 3);
		}

		public override int takeDamage(int damage, int xTrajectory, int yTrajectory, bool isBomb, double addedPrecision, Farmer who)
		{
			this.controller = null;
			this.setInvincibleCountdown(1000);
			this.currentLocation.playSound("hitEnemy");
			this.setTrajectory(xVelocity: xTrajectory / 2, yVelocity: yTrajectory / 2);
			this.shedChunks(Game1.random.NextDouble() < 0.7 ? 4 : 5);

			if (this.AngryAtFarmer is null)
			{
				// Character reactions
				foreach (NPC npc in this.currentLocation.characters.Where(c =>
					c.CanSocialize
					&& Utility.tileWithinRadiusOfPlayer(xTile: c.TilePoint.X, yTile: c.TilePoint.Y, tileRadius: 12, f: who)))
				{
					npc.showTextAboveHead(ModEntry.I18n.Get($"chicken.fight.{Game1.random.Next(1, 7)}", new { farmer = who.displayName }));
					npc.faceTowardFarmerForPeriod(milliseconds: 4000, radius: 8, faceAway: false, who: who);
				}

				// Alert this chicken and all others
				foreach (ShrineChicken chicken in this.currentLocation.characters.Where(c => c is ShrineChicken).Cast<ShrineChicken>())
				{
					chicken.AngryAtFarmer = who;
				}

				// angy
				this.doEmote(whichEmote: Character.angryEmote);
			}

			if (Game1.random.NextDouble() < 0.002)
			{
				// Eggs
				Game1.createItemDebris(
					item: new StardewValley.Object(itemId: this.IsBrown ? "180" : "176", initialStack: 1),
					pixelOrigin: this.Position,
					direction: -1,
					location: this.currentLocation);

				return 1;
			}

			return 0;
		}

		public override void behaviorAtGameTick(GameTime time)
		{
			base.behaviorAtGameTick(time);

			if (this.AngryAtFarmer is Farmer farmer)
			{
				if (farmer == Utility.isThereAFarmerWithinDistance(tileLocation: this.Tile, tilesAway: 8, location: this.currentLocation))
				{
					// Lose determination when safely in range
					if (this.IsDetermined && Game1.random.NextDouble() < 0.1)
					{
						this.IsDetermined = false;
					}

					// From: StardewValley.Monsters.DustSprite.cs
					if (this.yJumpOffset == 0)
					{
						// Jump if not jumping
						this.jumpWithoutSound();
						this.yJumpVelocity = Game1.random.Next(50, 70) / 10f;

						this.TryPlaySound(chance: 0.2, pitched: true);

						// Chance to jump higher
						if (Game1.random.NextDouble() < 0.2)
						{
							this.yJumpVelocity *= 1.5f;
							Rectangle source = new (0, 320, 64, 64);
							TemporaryAnimatedSprite puff = new (
								textureName: "TileSheets/animations",
								sourceRect: source,
								animationInterval: 50f,
								animationLength: 8,
								numberOfLoops: 0,
								position: this.getStandingPosition() + new Vector2(x: source.Width * -0.25f, y: 0),
								flicker: false,
								flipped: false)
							{
								scale = 0.5f,
								alpha = 0.95f,
								alphaFade = 0.01f
							};
							Game1.Multiplayer.broadcastSprites(location: this.currentLocation, sprites: puff);
						}

						// Look to farmer
						this.faceGeneralDirection(target: farmer.getStandingPosition());
					}
					else
					{
						// Throw feathers if jumping
						if (Game1.random.NextDouble() < 0.03)
						{
							this.TryPlaySound(pitched: true);
							this.shedChunks(Game1.random.NextDouble() > 0.25 ? 1 : 2);
						}

						// Move to farmer
						// From: StardewValley.Monsters.DustSprite.cs
						Vector2 v = Utility.getAwayFromPlayerTrajectory(monsterBox: this.GetBoundingBox(), who: farmer);
						this.xVelocity += -v.X / 150f + ((Game1.random.NextDouble() < 0.01) ? (Game1.random.Next(-50, 50) / 10f) : 0);
						if (Math.Abs(this.xVelocity) > 5f)
						{
							this.xVelocity = Math.Sign(this.xVelocity) * 5;
						}
						this.yVelocity += -v.Y / 150f + ((Game1.random.NextDouble() < 0.01) ? (Game1.random.Next(-50, 50) / 10f) : 0);
						if (Math.Abs(this.yVelocity) > 5f)
						{
							this.yVelocity = Math.Sign(this.yVelocity) * 5;
						}
					}
				}
				else if (!this.IsDetermined)
				{
					// Return to normal if farmer lost
					this.ReturnHome();
				}
			}
			else if (this.CanAnimate)
			{
				this.TryPlaySound(chance: 0.001);
				this.TryAnimate(time: time, chance: 0.005);
			}
		}

		public override void updateMovement(GameLocation location, GameTime time)
		{
			if (this.AngryAtFarmer is Farmer farmer)
			{
				// Usual monster seek behaviour if farmer found
				base.updateMovement(location: location, time: time);
			}
			else if (this.CanAnimate && Game1.random.NextDouble() < 0.001)
			{
				// Idle pathing if no farmer
				for (int i = 0; i < 25 && this.controller is null; ++i)
				{
					Vector2 tile = this.DefaultPosition / Game1.tileSize + new Vector2(x: Game1.random.Next(-6, 6), y: Game1.random.Next(-5, 5));
					if (location.isTilePassable(tile) && tile != this.Tile)
					{
						this.controller = new PathFindController(
							c: this,
							location: location,
							finalFacingDirection: -1,
							endPoint: Utility.Vector2ToPoint(tile));
						this.controller.endBehaviorFunction = (Character chicken, GameLocation where) =>
						{
							chicken.controller = null;
							//chicken.timeBeforeAIMovementAgain = 3300;
						};
					}
				}
			}
		}

		protected override void updateAnimation(GameTime time)
		{
		}

		public override void draw(SpriteBatch b)
		{
			// From: StardewValley.Monsters.FarmAnimal.cs
			Vector2 drawPosition = this.Position - new Vector2(0, 24);

			this.Sprite.drawShadow(
				b: b,
				screenPosition: Game1.GlobalToLocal(Game1.viewport, drawPosition),
				scale: Game1.pixelZoom + this.yJumpOffset / 30f);

			this.Sprite.draw(
				b: b,
				screenPosition: Utility.snapDrawPosition(Game1.GlobalToLocal(Game1.viewport, drawPosition + new Vector2(0, this.yJumpOffset))),
				layerDepth: ((this.GetBoundingBox().Center.Y + 4) + this.Position.X / 20000f) / 10000f,
				xOffset: 0,
				yOffset: 0,
				c: !this.isInvincible() || this.invincibleCountdown / 25 % 2 == 0 ? Color.White : new Color(r: 255, g: 175, b: 175),
				flip: false,
				scale: Game1.pixelZoom);

			// From: StardewValley.Character.cs
			if (this.IsEmoting)
			{
				Vector2 emotePosition = this.getLocalPosition(Game1.viewport);
				emotePosition.Y += this.yJumpOffset - this.Sprite.SpriteHeight * Game1.pixelZoom - 16;
				b.Draw(
					texture: Game1.emoteSpriteSheet,
					position: emotePosition,
					sourceRectangle: new Rectangle(
						x: this.CurrentEmoteIndex * 16 % Game1.emoteSpriteSheet.Width,
						y: this.CurrentEmoteIndex * 16 / Game1.emoteSpriteSheet.Width * 16,
						width: 16,
						height: 16),
					color: Color.White,
					rotation: 0f,
					origin: Vector2.Zero,
					scale: Game1.pixelZoom,
					effects: SpriteEffects.None,
					layerDepth: this.getStandingPosition().Y / 10000f);
			}
		}
	}
}
