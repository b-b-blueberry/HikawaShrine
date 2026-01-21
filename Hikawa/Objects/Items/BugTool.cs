using Hikawa.Data;
using Hikawa.Objects.Critters;
using Hikawa.Objects.Items.Data;
using StardewValley.ItemTypeDefinitions;
using System;
using System.Text;
using System.Xml.Serialization;
using static StardewValley.FarmerSprite;
using Object = StardewValley.Object;

namespace Hikawa.Objects.Items
{
	[XmlType($"{ModConsts.SpaceCoreXmlPrefix}{nameof(BugTool)}")] // SpaceCore serialisation signature
	public class BugTool : Object
	{
		public override string TypeDefinitionId => BugToolItemDataDefinition.TypeDefinitionId;

		[XmlIgnore]
		public string BugId
		{
			get
			{
				return this._bugId;
			}
			set
			{
				this._bugId = value;
				this.GetContextTags().Remove(this.BugData?.ContextTag);
				ParsedItemData data = ItemRegistry.GetDataOrErrorItem(this.QualifiedItemId);
				if (ModEntry.BugsData.Value.BugData.TryGetValue(this._bugId, out BugData bugData))
				{
					this.BugData = bugData;
					this.BugTexture = Game1.temporaryContent.Load<Texture2D>(bugData.TextureId);
					this.SourceRect = bugData.ToolTextureRegion;
					this.GetContextTags().Add(bugData.ContextTag);
				}
				else
				{
					this.SourceRect = data.GetSourceRect();
					this.BugTexture = null;
					this.BugData = null;
				}
				this.getDescription();
			}
		}

		[XmlIgnore]
		public BugData BugData;
		[XmlIgnore]
		public Texture2D BugTexture;
		[XmlIgnore]
		public Rectangle SourceRect;

		public bool HasBug => this.BugId is not null;

		private string _bugId;

		[XmlIgnore]
		private float _iconShakeScale;
		[XmlIgnore]
		private Farmer lastUser;

		public BugTool()
			: base()
		{
			this.Name = ModEntry.BugsData.Value.BugToolData.Name;
			this.Type = ModEntry.BugsData.Value.BugToolData.Type;

			ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(this.QualifiedItemId);
			this.SourceRect = itemData.GetSourceRect();
		}

		protected override Item GetOneNew()
		{
			return new BugTool();
		}

		public override bool canBeGivenAsGift()
		{
			return this.HasBug;
		}

		public override bool IsHeldOverHead()
		{
			return false;
		}

		public override bool isPlaceable()
		{
			return true;
		}

		public override bool canBePlacedHere(GameLocation l, Vector2 tile, CollisionMask collisionMask = CollisionMask.All, bool showError = false)
		{
			return true;
		}

		public override bool canBeDropped()
		{
			return false;
		}

		public override bool CanBeLostOnDeath()
		{
			return false;
		}

		public override bool canBeShipped()
		{
			return false;
		}

		public override bool canBeTrashed()
		{
			return false;
		}

		public override void drawWhenHeld(SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f)
		{
			// Duplicate base behaviour plus variable source rect
			ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(this.QualifiedItemId);
			Texture2D texture = itemData.GetTexture();
			float layerDepth = MathF.Max(0f, (f.StandingPixel.Y + 3) / 10000f);

			spriteBatch.Draw(
				texture: texture,
				position: objectPosition,
				sourceRectangle: this.SourceRect,
				color: Color.White,
				rotation: 0,
				origin: Vector2.Zero,
				scale: Game1.pixelZoom,
				effects: SpriteEffects.None,
				layerDepth: layerDepth);
		}

		public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
		{
			// Hide from mouse cursor when held in world
			if (layerDepth == 0.999f)
				return;

			// Duplicate base behaviour plus shake and variable source rect
			this.AdjustMenuDrawForRecipes(ref transparency, ref scaleSize);
			ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(this.QualifiedItemId);
			Texture2D texture = itemData.GetTexture();
			Vector2 size = this.SourceRect.Size.ToVector2();

			// Shake icon in menu when bug caught
			if (this._iconShakeScale > 0)
			{
				Point range = (new Vector2(-1.5f, 1.5f) * Game1.pixelZoom).ToPoint();
				location += new Vector2(Game1.random.Next(range.X, range.Y), Game1.random.Next(range.X, range.Y)) * this._iconShakeScale;
				this._iconShakeScale -= 0.025f;
			}

			spriteBatch.Draw(
				texture: texture,
				position: location + size / 2 * Game1.pixelZoom,
				sourceRectangle: this.SourceRect,
				color: color * transparency,
				rotation: 0,
				origin: size / 2,
				scale: Game1.pixelZoom * scaleSize,
				effects: SpriteEffects.None,
				layerDepth: layerDepth);
			this.DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color);
		}

		public override Point getExtraSpaceNeededForTooltipSpecialIcons(SpriteFont font, int minWidth, int horizontalBuffer, int startingHeight, StringBuilder descriptionText, string boldTitleText, int moneyAmountToDisplayAtBottom)
		{
			Point dimensions = base.getExtraSpaceNeededForTooltipSpecialIcons(
				font: font,
				minWidth: minWidth,
				horizontalBuffer: horizontalBuffer,
				startingHeight: startingHeight,
				descriptionText: descriptionText,
				boldTitleText: boldTitleText,
				moneyAmountToDisplayAtBottom: moneyAmountToDisplayAtBottom);

			if (this.BugData?.DisplayName is string text && this.BugTexture is not null)
			{
				Vector2 margin = new Vector2(4);
				Vector2 textSize = font.MeasureString(text) / Game1.pixelZoom;
				Vector2 spriteSize = this.BugData.MenuTextureRegion.Size.ToVector2();
				dimensions.X = (int)Math.Max(minWidth, (margin.X + spriteSize.X * 2 + textSize.X) * Game1.pixelZoom);
				dimensions.Y = (int)(startingHeight + (margin.Y + spriteSize.Y) * Game1.pixelZoom);
			}

			return dimensions;
		}

		public override void drawTooltip(SpriteBatch spriteBatch, ref int x, ref int y, SpriteFont font, float alpha, StringBuilder overrideText)
		{
			base.drawTooltip(
				spriteBatch: spriteBatch,
				x: ref x,
				y: ref y,
				font: font,
				alpha: alpha,
				overrideText: overrideText);

			if (this.BugTexture is not null)
			{
				string text = this.BugData.DisplayName;
				Color colour = Game1.textColor;
				Vector2 margin = new Vector2(4);
				Vector2 position = new Vector2(x: x, y: y);
				Vector2 textSize = font.MeasureString(text) / Game1.pixelZoom;
				Vector2 spriteSize = this.BugData.MenuTextureRegion.Size.ToVector2();

				position += margin * Game1.pixelZoom;
				position += spriteSize / 2 * Game1.pixelZoom;

				spriteBatch.Draw(texture: this.BugTexture,
					position: position,
					sourceRectangle: this.BugData.MenuTextureRegion,
					color: Color.White,
					rotation: 0,
					origin: spriteSize / 2,
					scale: Game1.pixelZoom,
					effects: SpriteEffects.None,
					layerDepth: 1);

				position += new Vector2(spriteSize.X, (textSize.Y - spriteSize.Y) / 2) * Game1.pixelZoom;

				Utility.drawTextWithShadow(
					b: spriteBatch,
					text: text,
					font: font,
					position: position,
					color: colour * alpha);
			}
		}

		public override void drawPlacementBounds(SpriteBatch spriteBatch, GameLocation location)
		{
			// 🐛
		}

		public override bool placementAction(GameLocation location, int x, int y, Farmer who = null)
		{
			who ??= Game1.player;
			bool isOk = !Game1.eventUp
				&& !Game1.isFestival()
				&& !Game1.fadeToBlack
				&& !who.swimming.Value
				&& !who.bathingClothes.Value
				&& !who.onBridge.Value
				&& who.canMove
				&& who.currentLocation is not null;
			if (!isOk || this.isTemporarilyInvisible)
				return false;

			Point global = who.GetToolLocation().ToPoint();
			this.DoFunction(location: who.currentLocation, x: global.X, y: global.Y, who: who);
			return false;
		}

		public bool DoFunction(GameLocation location, int x, int y, Farmer who)
		{
			this.lastUser = who;
			Point global = (who.GetGrabTile() * Game1.tileSize).ToPoint();
			this.CatchBug(who: who, where: location, x: global.X, y: global.Y);
			this.PlayAnimation(who);
			who.playNearbySoundAll("daggerswipe");
			return true;
		}

		public static void DoEndFunction(Farmer who)
		{
			who.completelyStopAnimatingOrDoingAction();
			who.UsingTool = false;
			who.canReleaseTool = true;
		}

		public void PlayAnimation(Farmer who)
		{
			who.Halt();
			who.FarmerSprite.oldFrame = who.FarmerSprite.CurrentFrame;

			int ms = 200;
			bool flip = who.FacingDirection == Game1.left;
			int[] frames = who.FacingDirection switch
            {
				Game1.up => [36, 38, 36, 38], // something animation
                Game1.down => [111, 112, 111, 112],
				_ => [111, 112, 111, 112], // Haley jar animation
            };
			AnimationFrame[] animation = [
				new(frames[0], ms, secondaryArm: false, flip: flip),
				new(frames[1], ms, secondaryArm: false, flip: flip),
				new(frames[2], ms, secondaryArm: false, flip: flip),
				new(frames[3], ms, secondaryArm: false, flip: flip, BugTool.DoEndFunction)
			];
			who.FarmerSprite.animateOnce(animation: animation);
			who.FarmerSprite.PauseForSingleAnimation = true;
			who.UsingTool = true;
			who.canReleaseTool = false;
		}

		public void CatchBug(Farmer who, GameLocation where, int x, int y)
		{
			if (/*!this.HasBug
				&& */where.critters.Find(c => c is ShrineBug && c.getBoundingBox(xOffset: 0, yOffset: 0).Contains(x, y)) is ShrineBug bug)
			{
				// Play effects
				var bounds = bug.getBoundingBox(0, 0);
				bounds.Inflate(bounds.Width, bounds.Height);
				where.TemporarySprites.Add(new TemporaryAnimatedSprite(
					rowInAnimationTexture: 5,
					position: bounds.Center.ToVector2()
						//- new Vector2(16, 24) * Game1.pixelZoom / 2
						,
					color: Color.White,
					animationLength: 8,
					animationInterval: 100)
				{
					alphaFade = 0.02f,
					drawAboveAlwaysFront = true
				});

				this._iconShakeScale = 1f;

				// Do behaviours
				this.BugId = bug.BugId;
				where.critters.Remove(bug);
				BugTool.CatchBug(bug.BugId);
            }
		}

		public void ReleaseBug()
		{
			this.BugId = null;
		}

		public static void CatchBug(string bugId, int count = 1)
        {
            if (!ModEntry.SaveData.BugCollection.ContainsKey(bugId))
            {
                ModEntry.SaveData.BugCollection[bugId] = new()
                {
                    DaysPlayed = WorldDate.GetDaysPlayed(
                        year: Game1.year,
                        season: Game1.season,
                        dayOfMonth: Game1.dayOfMonth)
                };
            }
            ModEntry.SaveData.BugCollection[bugId].Count += count;
        }
	}
}
