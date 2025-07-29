using Hikawa.Data;
using StardewValley.TerrainFeatures;
using System;
using System.Xml.Serialization;

namespace Hikawa.Objects.Decor
{
	[XmlType($"{ModConsts.SpaceCoreXmlPrefix}{nameof(Shrub)}")] // SpaceCore serialisation signature
	public class Shrub : LargeTerrainFeature
	{
		[XmlIgnore]
		public string ShrubId
		{
			get => this._shrubId;
			set
			{
				this._shrubId = value;
				if (ModEntry.ShrubsData.Value.Shrubs.TryGetValue(value, out ShrubDataEntry data))
				{
					this.Data = data;
				}
			}
		}
		[XmlIgnore]
		public int Size
		{
			get => this._size;
			set
			{
				this._size = Math.Clamp(value: value, min: 0, max: this.Data.MaxSize);
			}
		}
		[XmlIgnore]
		public ShrubDataEntry Data;

		private string _shrubId;
		private int _size;

		public Shrub()
			: base(needsTick: true)
		{
		}

		public Shrub(GameLocation where, Vector2 tile, string id, int size = 0)
			: this()
		{
			this.Location = where;
			this.Tile = tile;
			this.ShrubId = id;
			this.Size = size;
		}

		public override bool seasonUpdate(bool onLoad)
		{
			if (Game1.season is Season.Spring)
				++this.Size;

			return base.seasonUpdate(onLoad);
		}

		public bool Trim()
		{
			int previous = this.Size;
			--this.Size;
			return previous != this.Size;
		}
	}
}
