using System.Collections.Generic;
using xTile.Dimensions;

namespace Hikawa
{
	internal class ModConsts
	{
		/* Mod data */
		// Directories
		internal const string TilesheetPrefix = "z_hikawa";
		internal const string SpritesDirectory = "Sprites";
		internal const string ExtraSpritesFile = TilesheetPrefix + "_extras";
		internal const string ArcadeSpritesFile = TilesheetPrefix + "_arcade";


		/* Game objects */
		// NPCs
		internal const string ReiNpcId = "Rei";
		internal const string AmiNpcId = "Ami";
		internal const string UsaNpcId = "Usagi";
		// Maps
		internal const string MapPrefix = "Hikawa";
		internal const string ShrineMapId = MapPrefix + "Shrine";
		internal const string TownSnippetId = MapPrefix + "Town";
		internal const string TownJojaSnippetId = TownSnippetId + "Joja";
		internal const string CorridorMapId = MapPrefix + "Corridor";
		internal const string NegativeMapId = MapPrefix + "Negative";
		// Objects
		internal const string ArcadeMinigameId = "Hikawa_LightGun";
		internal const string ArcadeObjectName = "Sailor V Arcade System";
		// Coordinates
		internal static readonly Location ArcadeMachinePosition = new Location(40, 16);
		internal static readonly List<Location> CrowTilePositions = new List<Location>
		{
			new Location()
		};
		// Events and story
		internal enum Progress
		{
			None,
			Started,
			Complete
		}

		internal const string SaveDataKey = "Hikawa";
	}
}
