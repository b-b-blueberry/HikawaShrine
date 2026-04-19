using StardewValley.Menus;
using System.Collections.Generic;

namespace Hikawa.Match3
{
	/// <summary>
	/// Class used to interface with <see cref="Match3UI"/> in Stardew Valley.
	/// </summary>
	public class Match3SDVMenu : IClickableMenu
	{
		// Match3 instances
		public Match3UI UI;

		// Menu components
		public readonly List<ClickableComponent> Match3MenuComponents = []; // Required on PopulateClickableComponentList

		protected ClickableTextureComponent _shuffleButton;
		protected ClickableTextureComponent _helpButton;
		protected ClickableTextureComponent _muteButton;

		protected bool _isExiting;

		public Match3SDVMenu(Match3UI ui)
			: base()
		{
			this.UI = ui;

			this.UI.Game.OnTokensCreated += this.OnTokensCreated;
            this.UI.Game.OnStageChanged += this.OnStageChanged;
            this.UI.Game.Stage.OnStateChanged += this.OnStageStateChanged;

			this.SetupMenu();
			this.UpdateMenuComponents();

			this.OnStageChanged(stage: null, next: this.UI.Game.Stage.Id);
		}

		public void SetupMenu()
		{
			this.initializeUpperRightCloseButton();

			float scale = this.UI.MenuData.Scale;
			Rectangle source;

			source = new(x: 240, y: 192, width: 16, height: 16);
			this._helpButton = new(
				name: "helpButton",
				bounds: new(-1, -1, (int)(source.Width * scale), (int)(source.Height * scale)),
				label: null,
				hoverText: null,
				texture: Game1.mouseCursors,
				sourceRect: source,
				scale: scale,
				drawShadow: true);

			source = new(x: 366, y: 373, width: 16, height: 16);
			this._shuffleButton = new(
				name: "shuffleButton",
				bounds: new(-1, -1, (int)(source.Width * scale), (int)(source.Height * scale)),
				label: null,
				hoverText: null,
				texture: Game1.mouseCursors,
				sourceRect: source,
				scale: scale,
				drawShadow: true);

			source = new(x: 128, y: 384, width: 9, height: 9);
			this._muteButton = new(
				name: "muteButton",
				bounds: new(-1, -1, (int)(source.Width * scale), (int)(source.Height * scale)),
				label: null,
				hoverText: null,
				texture: Game1.mouseCursors,
				sourceRect: source,
				scale: scale,
				drawShadow: true);

			this.Match3MenuComponents.AddRange([this._helpButton, this._shuffleButton, this._muteButton]);
		}

		public void UpdateMenuComponents()
		{
			Rectangle view = Game1.graphics.GraphicsDevice.Viewport.Bounds;
			Point gameSize = this.UI.Game.Stage.Data.GameSize;
			Point tokenSize = this.UI.MenuData.TokenSize;
			float scale = this.UI.MenuData.Scale;

			this.width = (int)(gameSize.X * tokenSize.X * scale);
			this.height = (int)(gameSize.Y * tokenSize.Y * scale);

			this.xPositionOnScreen = view.Center.X - this.width / 2;
			this.yPositionOnScreen = view.Center.Y - this.height / 2;

			Rectangle bounds = new(x: this.xPositionOnScreen, y: this.yPositionOnScreen, width: this.width, height: this.height);

			int x = bounds.Right + (int)(64 * scale);
			this.upperRightCloseButton.bounds.Location = new(x + (int)(1 * scale), bounds.Top);
			this._helpButton.bounds.Location = new(x, this.upperRightCloseButton.bounds.Bottom + (int)(3 * scale));
			this._shuffleButton.bounds.Location = new(x, this._helpButton.bounds.Bottom + (int)(4 * scale));
			this._muteButton.bounds.Location = new(x + (int)(3 * scale), this._shuffleButton.bounds.Bottom + (int)(4 * scale));

			this.UI.UpdateComponents(bounds: bounds);
		}

		public void OnTokensCreated()
		{
			this.UpdateMenuComponents();
		}

        public void OnStageChanged(Stage stage, string next)
        {
			var data = this.UI.Game.Data;
            var menu = this._childMenu;

            // Play outro cutscene for current scene
            if (stage?.Data?.OutroCutsceneId is not null)
            {
                if (menu is Match3SDVMenu)
                {
                    menu.exitFunction += PlayOutro;
                }
                else
                {
                    PlayOutro();
                }
            }
            else
            {
                TryPlayIntro();
            }

            void PlayOutro()
            {
                var cutscene = new Match3CutsceneMenu(data, stage.Data.OutroCutsceneId);
                this.SetChildMenu(cutscene);
                cutscene.exitFunction += () =>
                {
                    TryPlayIntro();
                };
            }

            void TryPlayIntro()
            {
                // Play intro cutscene for next scene
                if (next is not null && data.StageData.TryGetValue(next, out StageData nextStageData) && nextStageData.IntroCutsceneId is not null)
                {
                    var cutscene = new Match3CutsceneMenu(data, nextStageData.IntroCutsceneId);
                    this.SetChildMenu(cutscene);
                    cutscene.exitFunction += () =>
                    {
                        this.SetChildMenu(menu);
                    };
                }
            }
        }

        public void OnStageStateChanged(Stage stage, StageState current, StageState next)
        {
			if (current is StageState.Active && next is StageState.End)
            {
                // Update total stats from stage stats
                ModEntry.SaveData.Match3.TotalTime += stage.Time;
                ModEntry.SaveData.Match3.TotalScore += stage.Score;
                ModEntry.SaveData.Match3.TotalMoves += stage.Moves;
                ModEntry.SaveData.Match3.TotalPowers += stage.Powers;
                ModEntry.SaveData.Match3.TotalSuperPowers += stage.SuperPowers;
                ModEntry.SaveData.Match3.TotalMatches += stage.Matches;
                ModEntry.SaveData.Match3.TotalPowerMatches += stage.PowerMatches;
                ModEntry.SaveData.Match3.TotalSuperPowerMatches += stage.SuperPowerMatches;
            }
        }

		public bool TryPressButton(int x, int y)
		{
			if (this._helpButton.containsPoint(x: x, y: y))
			{
				Match3.PlaySound("dwop");
			}
			else if (this._shuffleButton.containsPoint(x: x, y: y))
			{
				this.UI.Game.Stage.Score = 0;
				this.UI.SetupStage(stageId: this.UI.Game.Stage.Id, reset: true, state: this.UI.Game.Stage.State);
				this.UI.Shake(scale: 4f, amount: new(x: 2, y: 2));
                Match3.PlaySound("throwDownITem");
			}
			else if (this._muteButton.containsPoint(x: x, y: y))
			{
				this.UI.IsMute = !this.UI.IsMute;
                Match3.PlaySound("smallSelect");
				this.SetMuteButtonSprite(this.UI.IsMute);
			}
			else
			{
				return false;
			}
			return true;
		}

		public void TryHover(int x, int y)
		{
			const float scaleTo = 0.5f;

			this._helpButton.tryHover(x: x, y: y, maxScaleIncrease: scaleTo);
			this._shuffleButton.tryHover(x: x, y: y, maxScaleIncrease: scaleTo);
			this._muteButton.tryHover(x: x, y: y, maxScaleIncrease: scaleTo);
		}

		public void SetMuteButtonSprite(bool mute)
		{
			this._muteButton.sourceRect = mute ? new(x: 137, y: 384, width: 9, height: 9) : new(x: 128, y: 384, width: 9, height: 9);
		}

		public void DrawMenuComponents(SpriteBatch b)
		{
			if (this.shouldDrawCloseButton())
				this.upperRightCloseButton?.draw(b: b);
			this._helpButton.draw(b: b);
			this._shuffleButton.draw(b: b);
			this._muteButton.draw(b: b);
		}

		protected override void cleanupBeforeExit()
		{
			base.cleanupBeforeExit();

			Match3.StopMusic();
		}

		public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
		{
			base.gameWindowSizeChanged(oldBounds, newBounds);

			this.UpdateMenuComponents();
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			base.receiveLeftClick(x, y, playSound);

			if (this.TryPressButton(x: x, y: y))
				return;

			this.UI.OnActionStart(x: x, y: y);
		}

		public override void leftClickHeld(int x, int y)
		{
			base.leftClickHeld(x, y);

			this.UI.OnActionUpdate(x: x, y: y);
		}

		public override void releaseLeftClick(int x, int y)
		{
			base.releaseLeftClick(x, y);

			this.UI.OnActionEnd(x: x, y: y);
		}

		public override void performHoverAction(int x, int y)
		{
			base.performHoverAction(x, y);

			this.TryHover(x: x, y: y);
		}

		public override void update(GameTime time)
		{
			base.update(time);

            if (this._childMenu is not null)
                return;

            if (!this._isExiting)
                this._isExiting = !this.UI.OnTick(time: time);

            if (this._isExiting && this._childMenu is null)
			{
				this.exitThisMenuNoSound();
			}
		}

		public override void draw(SpriteBatch b)
		{
			base.draw(b: b);

			if (this._childMenu is not null)
				return;

			void drawScreenOverlay() => b.Draw(
				texture: Game1.fadeToBlackRect,
				destinationRectangle: Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea,
				color: new Color(10, 3, 5, 232));

			if (this.UI.Game.IsPaused)
			{
				this.DrawMenuComponents(b: b);
				drawScreenOverlay();
				this.UI.Draw(b: b);
			}
			else
			{
				drawScreenOverlay();
				this.DrawMenuComponents(b: b);
				this.UI.Draw(b: b);
			}
		}
	}
}
