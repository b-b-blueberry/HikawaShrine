using StardewValley.Tools;
using System;
using System.Text;

namespace Hikawa.Volleyball
{
	internal class VolleyballItem : MeleeWeapon
	{
		public VolleyballItem() : base()
		{
			this.Name = ModEntry.ModData.ItemVolleyball;
			this.InitialParentTileIndex = this.CurrentParentTileIndex = 0;
		}

		protected override void initNetFields()
		{
			base.initNetFields();
		}

		protected override Item GetOneNew()
		{
			return new VolleyballItem();
		}

		protected override string loadDisplayName()
		{
			return "";
		}

		protected override string loadDescription()
		{
			return "";
		}

		public override string getCategoryName()
		{
			return "";
		}

		public override bool isScythe()
		{
			return false;
		}

		public override float defaultKnockBackForThisType(int type)
		{
			return 0;
		}

		public override Rectangle getAreaOfEffect(int x, int y, int facingDirection, ref Vector2 tileLocation1, ref Vector2 tileLocation2, Rectangle wielderBoundingBox, int indexInCurrentAnimation)
		{
			return Rectangle.Empty;
		}

		public override void setFarmerAnimating(Farmer who)
		{
			if (who is null || who.CurrentTool != this)
			{
				return;
			}
			if (who.IsLocalPlayer)
			{
				who.TemporaryPassableTiles.Clear();
				who.currentLocation.lastTouchActionLocation = Vector2.Zero;
				who.currentLocation.playSound(audioName: "clubswipe", position: who.Tile);
				who.FarmerSprite.PauseForSingleAnimation = false;
				who.FarmerSprite.StopAnimation();
				who.EndEmoteAnimation();
				who.lastClick = Vector2.Zero;
			}

			// Animate in direction of cursor
			// mathemagicians look away now
			Point cursor = Game1.getMousePosition(ui_scale: false);
			double angle = Math.PI + Utils.Vector.RadiansBetween(va: Game1.player.getStandingPosition(), vb: new Vector2(x: Game1.viewport.X + cursor.X, y: Game1.viewport.Y + cursor.Y));
			int faces = 4;
			int facingDirection = (-1 + (int)((angle + Math.PI / faces) / Math.PI * 2)) % faces;
			facingDirection = facingDirection >= 0 ? facingDirection : facingDirection + faces;
			who.faceDirection(facingDirection);
			((FarmerSprite)who.Sprite).animateOnce(
				whichAnimation: (who.yJumpOffset < -1 ? (int[])[176, 168, 160, 184] : (int[])[248, 240, 232, 256])[who.FacingDirection],
				animationInterval: 60,
				numberOfFrames: 6);
		}

		public override void DoDamage(GameLocation location, int x, int y, int facingDirection, int power, Farmer who)
		{
		}

		public override void leftClick(Farmer who)
		{
			if (!who.UsingTool)
			{
				who.CanMove = who.yJumpOffset < -1;
				who.UsingTool = true;
				who.canReleaseTool = true;
				this.setFarmerAnimating(who);
			}
		}

		protected override void doAnimateSpecialMove()
		{
			if (this.lastUser is Farmer who && who.CurrentTool == this && !who.UsingTool && who.yJumpOffset >= 0)
			{
				Game1.player.jump(jumpVelocity: 8f);
			}
		}

		public override void tickUpdate(GameTime time, Farmer who)
		{
		}

		public override void draw(SpriteBatch b)
		{
		}

		public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
		{
		}

		public override void drawTooltip(SpriteBatch spriteBatch, ref int x, ref int y, SpriteFont font, float alpha, StringBuilder overrideText)
		{
		}

		public override void drawAttachments(SpriteBatch b, int x, int y)
		{
		}

		public override void DrawIconBar(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color)
		{
		}

		public override void DrawMenuIcons(SpriteBatch sb, Vector2 location, float scale_size, float transparency, float layer_depth, StackDrawType drawStackNumber, Color color)
		{
		}

		public override void drawDuringUse(int frameOfFarmerAnimation, int facingDirection, SpriteBatch spriteBatch, Vector2 playerPosition, Farmer f)
		{
		}
	}
}
