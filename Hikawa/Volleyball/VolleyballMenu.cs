using StardewValley.Menus;
using System.Collections.Generic;
using System.Linq;

namespace Hikawa.Volleyball
{
	public class VolleyballMenu : IClickableMenu
	{
		public enum Team
		{
			None,
			Left,
			Right
		}

		public class PlayerButtonEntry
		{
			public Team Team;
			public ClickableComponent Clickable;

            public PlayerButtonEntry(Team team, ClickableComponent clickable)
            {
                this.Team = team;
                this.Clickable = clickable;
            }

            public void SetTeam(Team team)
            {
                this.Team = team;
            }

            public void SetRegion(int x, int y, int? width = null, int? height = null)
            {
                this.Clickable.bounds = new Rectangle(x: x, y: y, width: width ?? this.Clickable.bounds.Width, height: height ?? this.Clickable.bounds.Height);
            }
		}

		// Constants
		public static readonly Point Dimensions = new Point(x: 800, y: 600);
        public const bool IsCloseButtonVisible = true;

        // Components
        public Rectangle MenuArea;
        public Rectangle ContentArea;
        public Rectangle PlayerArea;
		public readonly ClickableTextureComponent StartButton;
        public readonly Dictionary<Character, PlayerButtonEntry> PlayerButtons;

        // State
        public VolleyballRules Rules;
        public Character HoveredPlayer;
        public bool IsReadyToStart => this.PlayerButtons.Values.All((PlayerButtonEntry entry) => entry.Team is not Team.None);

        public VolleyballMenu(IEnumerable<Character> players, int scoreGoal, bool isDoublesAllowed = false) : base(
            x: Game1.uiViewport.Width / 2 - (VolleyballMenu.Dimensions.X + IClickableMenu.borderWidth * 2) / 2,
            y: Game1.uiViewport.Height / 2 - (VolleyballMenu.Dimensions.Y + IClickableMenu.borderWidth * 2) / 2,
            width: VolleyballMenu.Dimensions.X + IClickableMenu.borderWidth * 2,
            height: VolleyballMenu.Dimensions.Y + IClickableMenu.borderWidth * 2,
            showUpperRightCloseButton: VolleyballMenu.IsCloseButtonVisible)
        {
            // Menu fields
            if (VolleyballMenu.IsCloseButtonVisible)
				this.initializeUpperRightCloseButton();

            // VolleyballPicker fields
            this.Rules = new VolleyballRules(
				players: players,
				scoreGoal: scoreGoal,
				isDoubles: isDoublesAllowed);

            // Clickable components
            {
                const int scale = Game1.pixelZoom;
                Rectangle source;

                // start-button
                source = AssetManager.ExtraSpritesVolleyballArea;
				this.StartButton = new ClickableTextureComponent(
                    bounds: new(
                        x: 0,
                        y: 0,
                        width: source.Width * scale,
                        height: source.Height * scale),
                    texture: ModEntry.Sprites,
                    sourceRect: source,
                    scale: scale,
                    drawShadow: true);

                // player-buttons
                this.PlayerButtons = new();
                this.PlayerButtons = players.ToDictionary(player => player, player => new PlayerButtonEntry(
                    team: Team.None, clickable: new ClickableComponent(bounds: Rectangle.Empty, name: player.Name)));
            }

            // Positions
            Rectangle bounds = Game1.graphics.GraphicsDevice.Viewport.Bounds;
			this.gameWindowSizeChanged(oldBounds: bounds, newBounds: bounds);
        }

        public override void populateClickableComponentList()
        {
            // Menu component registry
            base.populateClickableComponentList();

            // VolleyballPicker components
            int ID = 1000;
            IEnumerable<ClickableComponent> components = new[]
            {
				this.StartButton
            }.Concat(this.PlayerButtons.Values.Select(pair => pair.Clickable));
            foreach (ClickableComponent c in components)
            {
                c.myID = ID++;
            };
			this.allClickableComponents.AddRange(components);
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            // Menu position updates
            base.gameWindowSizeChanged(oldBounds, newBounds);

            Point padding = new(x: 64, y: 48);
			this.MenuArea = new(
				x: this.xPositionOnScreen,
				y: this.yPositionOnScreen,
				width: this.width,
				height: this.height);
			this.ContentArea = new(
				x: this.MenuArea.X + padding.X,
				y: this.MenuArea.Y + padding.Y,
				width: this.MenuArea.Width - padding.X * 2,
				height: this.MenuArea.Height - padding.Y * 2);

            // Component updates

            this.PlayerArea = new Rectangle(
				x: this.ContentArea.X + this.ContentArea.Width / 6,
				y: this.ContentArea.Y + this.ContentArea.Height / 6,
				width: this.ContentArea.Width / 3 * 2,
				height: this.ContentArea.Height / 3 * this.PlayerButtons.Count / 2);

            // Close-button
            this.upperRightCloseButton.setPosition(Utility.PointToVector2(
                this.MenuArea.Location
                + new Point(x: this.MenuArea.Width - this.upperRightCloseButton.bounds.Width, 0)));

            // Start-button
			this.StartButton.setPosition(
			    new Vector2(
                    x: this.PlayerArea.Center.X,
                    y: this.PlayerArea.Bottom + (this.ContentArea.Bottom - this.PlayerArea.Bottom) / 2)
                - this.StartButton.bounds.Size.ToVector2() / 2);

            // Player-buttons
            int i = 0;
            foreach ((Character character, PlayerButtonEntry entry) in this.PlayerButtons)
			{
                Point size = new Point(x: this.PlayerArea.Width, y: this.PlayerArea.Height / this.PlayerButtons.Count);
				Point point = this.PlayerArea.Location + new Point(x: 0, y: size.Y * i);
                entry.SetRegion(x: point.X, y: point.Y, width: size.X, height: size.Y);
                ++i;
            }
		}

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            // Upper-right close button behaviours
            base.receiveLeftClick(x: x, y: y, playSound: playSound);

            // VolleyballPicker button behaviours

            // Start-button
            if (this.StartButton.containsPoint(x, y))
            {
                if (this.IsReadyToStart)
                {
                    Game1.playSound("bigDeSelect");
					this.exitThisMenu();
                }
                else
                {
                    Game1.playSound("cancel");
                }
            }

            // Player-buttons
            if (this.HoveredPlayer is not null)
            {
                PlayerButtonEntry entry = this.PlayerButtons[this.HoveredPlayer];
				Team team = entry.Team is Team.None
                    ? (x < entry.Clickable.bounds.Center.X ? Team.Left : Team.Right)
                    : Team.None;
                entry.SetTeam(team);
			}
        }

		public override void performHoverAction(int x, int y)
		{
			base.performHoverAction(x, y);

            // Player-buttons
            (Character player, PlayerButtonEntry entry) = this.PlayerButtons
                .FirstOrDefault(pair => pair.Value.Clickable.containsPoint(x: x, y: y));
            this.HoveredPlayer = player;
		}

		public override void update(GameTime time)
        {
            base.update(time);
        }

        public override void draw(SpriteBatch b, int red = -1, int green = -1, int blue = -1)
		{
			const float scale = Game1.pixelZoom;

			// Blackout
			b.Draw(
                texture: Game1.fadeToBlackRect,
                destinationRectangle: Game1.graphics.GraphicsDevice.Viewport.Bounds,
                color: Color.Black * 0.6f);

			// Menu card
			Rectangle cardArea = new(x: this.MenuArea.X - 16, y: this.MenuArea.Y - 80, width: this.MenuArea.Width + 32, height: this.MenuArea.Height + 96);
			Game1.drawDialogueBox(
                x: cardArea.X,
                y: cardArea.Y,
                width: cardArea.Width,
                height: cardArea.Height,
                speaker: false,
                drawOnlyBox: true,
                message: null,
                objectDialogueWithPortrait: false,
                ignoreTitleSafe: true,
                r: red,
                g: green,
                b: blue);

			// test
			b.Draw(
				texture: Game1.fadeToBlackRect,
				destinationRectangle: this.MenuArea,
				color: Color.Red * 0f);
			b.Draw(
				texture: Game1.fadeToBlackRect,
				destinationRectangle: this.ContentArea,
				color: Color.Green * 0.3f);
			b.Draw(
				texture: Game1.fadeToBlackRect,
				destinationRectangle: this.PlayerArea,
				color: Color.Orange * 0.3f);

			// Start-button
			this.StartButton.draw(
                b: b,
                c: this.IsReadyToStart ? Color.White : Color.Plum * 0.5f,
                layerDepth: 1f);

            // Player-buttons
            foreach ((Character player, PlayerButtonEntry entry) in this.PlayerButtons)
			{
                Rectangle bounds = entry.Clickable.bounds;
				Rectangle source = player is NPC npc
                    ? VolleyballHUD.GetNPCPortraitSourceArea(npc: npc)
                    : new(x: 0, y: 0, width: 16, height: 16);
				Vector2 position = bounds.Location.ToVector2()
                    + new Vector2(
                        x: bounds.Size.X / 4 * (entry.Team is Team.Left ? 1 : entry.Team is Team.Right ? 3 : 2),
                        y: bounds.Size.Y / 2)
                    - source.Size.ToVector2() / 2 * scale;

				b.Draw(
					texture: Game1.fadeToBlackRect,
					destinationRectangle: bounds,
					color: Color.BlueViolet * 0.3f);

				// Player icons
				if (player is Farmer farmer)
				{
                    // farmer
					farmer.FarmerRenderer.drawMiniPortrat(
						b: b,
						position: position,
						layerDepth: 1f,
						scale: scale,
						facingDirection: Game1.down,
					who: farmer);
				}
                else
				{
					// character
                    b.Draw(
                        texture: player.Sprite.spriteTexture,
                        position: position,
                        sourceRectangle: source,
                        color: Color.White,
                        rotation: 0f,
                        origin: Vector2.Zero,
                        scale: scale,
                        effects: SpriteEffects.None,
                        layerDepth: 1f);
				}

				// Left-right arrows to assign player to a team
				if (player == this.HoveredPlayer)
				{
					source = new(x: 448, y: 96, width: 32, height: 32);
					for (int side = 0; side < 2; ++side)
					{
						bool isLeftArrow = side == 0;
						bool isArrowShown = entry.Team == Team.None
                            || (isLeftArrow && entry.Team == Team.Right)
                            || (!isLeftArrow && entry.Team == Team.Left);
						if (isArrowShown)
						{
							int offset = isLeftArrow ? (entry.Team is Team.None ? 1 : 2) : (entry.Team is Team.None ? 2 : 1);
							b.Draw(
								texture: Game1.mouseCursors,
								sourceRectangle: source,
								position: new Vector2(
									x: bounds.X + bounds.Width / 3 * offset,
									y: bounds.Y + bounds.Height / 2),
							color: Color.White,
							rotation: 0f,
								origin: Utility.PointToVector2(source.Size) / 2 * new Vector2(x: isLeftArrow ? 1 : -1, y: 1),
								scale: 1f,
								effects: isLeftArrow ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
								layerDepth: 1f);
						}
					}
				}
			}

            // Close-button
            this.upperRightCloseButton.draw(b: b);

			// Cursor
			if (!Game1.options.hardwareCursor)
				this.drawMouse(b);
        }

        public override void draw(SpriteBatch b)
        {
			this.draw(b: b, red: -1, green: -1, blue: -1);
        }
    }
}
