using StardewModdingAPI.Events;
using StardewValley.TokenizableStrings;
using System;
using System.IO;

namespace Hikawa
{
	internal static class AssetManager
    {
        internal static readonly string RootAssetDir = Path.Combine("Mods", "blueberry", "Hikawa");

        internal static readonly string DataAssetName = Path.Combine(RootAssetDir, "Data", "Data");
        internal static readonly string BowsDataAssetName = Path.Combine(RootAssetDir, "Data", "Bows");
        internal static readonly string BugsDataAssetName = Path.Combine(RootAssetDir, "Data", "Bugs");
        internal static readonly string CrowTradeRulesDataAssetName = Path.Combine(RootAssetDir, "Data", "CrowTradeRules");
        internal static readonly string DecorSpawnsDataAssetName = Path.Combine(RootAssetDir, "Data", "DecorSpawns");
        internal static readonly string KiteDataAssetName = Path.Combine(RootAssetDir, "Data", "Kites");
        internal static readonly string ShrineTreesDataAssetName = Path.Combine(RootAssetDir, "Data", "ShrineTrees");
        internal static readonly string ShrubsDataAssetName = Path.Combine(RootAssetDir, "Data", "Shrubs");
        internal static readonly string VolleyballDataAssetName = Path.Combine(RootAssetDir, "Data", "Volleyball");
        internal static readonly string WeddingsDataAssetName = Path.Combine(RootAssetDir, "Data", "Weddings");

        internal static readonly string StringsAssetName = Path.Combine(RootAssetDir, "Strings", "Strings");

        internal static readonly string EventSpritesAssetName = Path.Combine(RootAssetDir, "Locations", "Events");
        internal static readonly string HouseSpritesAssetName = Path.Combine(RootAssetDir, "Locations", "House");
        internal static readonly string IndoorsSpritesAssetName = Path.Combine(RootAssetDir, "Locations", "Indoors");
        internal static readonly string OutdoorsSpritesAssetName = Path.Combine(RootAssetDir, "Locations", "Outdoors");

        internal static readonly string ExtraSpritesAssetName = Path.Combine(RootAssetDir, "Sprites", "Extras");
        internal static readonly string CrowSpritesAssetName = Path.Combine(RootAssetDir, "Sprites", "Crows");
        internal static readonly string CatSpritesAssetName = Path.Combine(RootAssetDir, "Sprites", "Cats");
        internal static readonly string LightSpritesAssetName = Path.Combine(RootAssetDir, "Sprites", "Lights");

        internal static readonly string ItalicsFontAssetName = Path.Combine(RootAssetDir, "Fonts", "Italics");

		internal static readonly Rectangle ExtraSpritesFeathersArea = new Rectangle(x: 176, y: 0, width: 48, height: 16);
        internal static readonly Rectangle ExtraSpritesVolleyballPlayerTagArea = new Rectangle(x: 112, y: 16, width: 16, height: 16);
        internal static readonly Rectangle ExtraSpritesVolleyballAimpointArea = new Rectangle(x: 176, y: 16, width: 16, height: 16);
        internal static readonly Rectangle ExtraSpritesVolleyballVersusArea = new Rectangle(x: 288, y: 48, width: 32, height: 32);

		internal static void TryEdit(object sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Weddings"))
            {
                e.Edit(asset =>
                {
                    var data = asset.GetData<StardewValley.GameData.Weddings.WeddingData>();
                    var weddingData = Game1.content.Load<Hikawa.Data.WeddingData>(WeddingsDataAssetName);
                    var group = Utils.GetWeddingResponse(ModEntry.ModData.ContentPrefix + "_Wedding_Attendees_");
                    var location = Utils.GetWeddingResponse(ModEntry.ModData.ContentPrefix + "_Wedding_Location_");
                    var officiant = Utils.GetWeddingResponse(ModEntry.ModData.ContentPrefix + "_Wedding_Officiant_");
                    var decoration = Utils.GetWeddingResponse(ModEntry.ModData.ContentPrefix + "_Wedding_Decorations_");

                    if (officiant is null || location is null || group is null)
                        return;

                    // apply wedding attendee replacements
                    if (weddingData.AttendeeGroups.TryGetValue(group, out var attendees))
                    {
                        if (attendees.Attendees is not null)
                        {
                            data.Attendees = attendees.Attendees;
                        }
                    }

                    // apply wedding attendee tile offsets
                    if (weddingData.Locations.TryGetValue(location, out var locationData) is true && locationData.WeddingOffset != default)
                    {
                        foreach (var pair in data.Attendees)
                        {
                            var split = ArgUtility.SplitBySpace(pair.Value.Setup);
                            if (ArgUtility.TryGetPoint(split, 1, out var tile, out var error))
                                pair.Value.Setup = ApplyOffset(tile, split, 1);
                        }
                    }

                    // apply wedding script replacements
                    if (weddingData.EventScript is string script)
                    {
                        // remove all spouse variants
                        data.EventScript.Clear();
                        // substitute script tokens
                        script = TokenParser.ParseText(script, null, ParseWeddingToken);
                        // it's 12pm and 25c and i am GOING to parse these inner tokens and i do NOT care how
                        script = TokenParser.ParseText(script, null, ParseWeddingToken);
                        // add populated default event
                        data.EventScript.Add("default", script);
                    }

                    string ApplyOffset(Point point, string[] split, int index)
                    {
                        split[index] = $"{point.X + locationData.WeddingOffset.X}";
                        split[index + 1] = $"{point.Y + locationData.WeddingOffset.Y}";
                        return string.Join(' ', split);
                    }

                    bool ParseWeddingToken(string[] query, out string replacement, Random random, Farmer player)
                    {
                        replacement = null;
                        if (ArgUtility.Get(query, 0)?.ToLower() is string token)
                        {
                            if (token == $"{ModEntry.ModData.ContentPrefix}_Wedding_Offset".ToLower())
                            {
                                // offset coordinates
                                if (ArgUtility.TryGetPoint(query, 1, out var tile, out var error))
                                    replacement = ApplyOffset(tile, query[1..], 0);
                            }
                            else if (token == $"{ModEntry.ModData.ContentPrefix}_Wedding_Location".ToLower())
                            {
                                // location
                                replacement = location;
                            }
                            else if (token == $"{ModEntry.ModData.ContentPrefix}_Wedding_Officiant".ToLower())
                            {
                                // officiant
                                replacement = officiant;
                            }
                            else if (token == $"{ModEntry.ModData.ContentPrefix}_Wedding_OfficiantScript".ToLower())
                            {
                                // officiant event script
                                if (weddingData.Officiants.TryGetValue(officiant, out var officiantData))
                                    replacement = officiantData.EventScript;
                            }
                            else if (token == $"{ModEntry.ModData.ContentPrefix}_Wedding_DecorationScript".ToLower())
                            {
                                // decoration event script
                                if (weddingData.Decorations.TryGetValue(decoration, out var decorationData))
                                    replacement = decorationData.EventScript;
                            }
                        }
                        return replacement is not null;
                    }
                },
                (AssetEditPriority)int.MaxValue - 1);
            }
        }
    }
}
