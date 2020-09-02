using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;

using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Hikawa
{
	public static class MiniSit
	{
		private static IModHelper Helper => ModEntry.Instance.Helper;
		
		private const string ActionName = "MiniSit";
		private static readonly int[] PlayerSittingFrames = {62, 117, 54, 117};

		private static Vector2 _playerLastStandingLocation;
		private static string[] _playerLastSittingProperties;
		
		internal static bool IsPlayerSittingDown;

		public static void Setup()
		{
			Helper.Events.Player.Warped += PlayerOnWarped;
			Helper.Events.GameLoop.DayStarted += GameLoopOnDayStarted;
			Helper.Events.Input.ButtonPressed += InputOnButtonPressed;
		}

		private static void PlayerOnWarped(object sender, WarpedEventArgs e) { IsPlayerSittingDown = false; }

		private static void GameLoopOnDayStarted(object sender, DayStartedEventArgs e) { IsPlayerSittingDown = false; }

		private static void InputOnButtonPressed(object sender, ButtonPressedEventArgs e)
		{
			if (!IsPlayerSittingDown)
			{
				if (!e.Button.IsActionButton() && !e.Button.IsUseToolButton())
					return;
				var position = e.Cursor.GrabTile;
				var property = ModEntry.GetTileAction(position);
				if (property == null || property[0] != ActionName)
					return;

				_playerLastSittingProperties = property;
				var tileCoordinates = new Vector2((float)Math.Floor(position.X), (float)Math.Floor(position.Y));
				var direction = property.Length > 1 ? int.Parse(property[1]) : 2;
				SitDownStart(tileCoordinates, direction);
			}
			else
			{
				Helper.Input.Suppress(e.Button);
				SitDownEnd();
			}
		}

		public static bool CheckToHidePlayerShadow()
		{
			return IsPlayerSittingDown || Game1.player.isRidingHorse();
		}
		
		/// <summary>
		/// Lock the player into a sitting-down animation facing a given direction until they press any key.
		/// </summary>
		/// <param name="position">Target position in world coordinates to sit at.</param>
		/// <param name="direction">Value for direction to face, follows standard SDV rules of clockwise-from-zero.</param>
		private static void SitDownStart(Vector2 position, int direction) {
			
			Game1.playSound("breathin");
			
			Game1.player.mount = null;
			_playerLastStandingLocation = Game1.player.getTileLocation();
			IsPlayerSittingDown = true;

			// TODO: METHOD: Add check for front layer objects to opt-out of the 64f+16f player offset
			Game1.player.yOffset = 0f;
			if (direction != 0)
			{
				const int yOffsetTiles = 1;
				position.Y += yOffsetTiles;
				Game1.player.yOffset = yOffsetTiles * 64f + 16f;
			}
			Game1.player.faceDirection(direction);
			Game1.player.completelyStopAnimatingOrDoingAction();
			Game1.player.setTileLocation(position);

			var animFrames = new FarmerSprite.AnimationFrame[1];
			animFrames[0] = new FarmerSprite.AnimationFrame(
				PlayerSittingFrames[direction], 999999, false, direction == 3);
			Game1.player.FarmerSprite.animateOnce(animFrames);
			Game1.player.CanMove = false;
		}

		/// <summary>
		/// Remove restrictions from the player after sitting, and teleport them to their last standing position.
		/// </summary>
		private static void SitDownEnd() {
			Game1.playSound("breathout");
			
			Game1.player.yOffset = 0f;
			Game1.player.completelyStopAnimatingOrDoingAction();
			//Game1.player.faceDirection((Game1.player.FacingDirection + 2) % 4);
			Game1.player.setTileLocation(_playerLastStandingLocation);
			Game1.player.CanMove = true;
			
			Game1.player.mount = null;
			_playerLastStandingLocation = Vector2.Zero;
			IsPlayerSittingDown = false;

			// Leave a buttprint in Winter on seats that would have one
			if (Game1.currentSeason != "winter" || !_playerLastSittingProperties.Any(p => p == "butt"))
				return;

			var position = new Vector2(
				Game1.player.lastPosition.X,
				Game1.player.lastPosition.Y - 32 - 64);
			var id = 87008
			         + (int)Math.Floor(position.Y / 64)
			         * Game1.currentLocation.Map.DisplayWidth / 64 
			         + (int)Math.Floor(position.X / 64);

			if (Game1.currentLocation.getTemporarySpriteByID(id) != null)
				return;

			var assetKey = Helper.Content.GetActualAssetKey(
				Path.Combine(ModConsts.SpritesPath, $"{ModConsts.ExtraSpritesFile}.png"));
			var direction = Game1.player.FacingDirection;
			var layer = (Game1.player.getStandingY() - 64f) / 10000f - 1f / 1000f;
			var multiplayer = Helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();
			multiplayer.broadcastSprites(
				Game1.currentLocation,
				new TemporaryAnimatedSprite(
					assetKey, 
					new Rectangle(64, direction % 2 == 0 ? 0 : 16, 16, 16), 
					9999, 
					1,
					9999, 
					position, 
					false, 
					direction == 3, 
					layer, 
					0f, 
					Color.White,
					4f, 
					0f, 
					direction == 0 ? 0f : (float)Math.PI, 
					0f) 
				{
					id = id, 
					holdLastFrame = true, 
					verticalFlipped = direction == 0
				});
		}
	}
}
