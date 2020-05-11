﻿using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;

namespace Hikawa.GameObjects
{
	public class HikawaShrine : GameLocation
	{
		[XmlIgnore]
		private readonly NetObjectList<FarmAnimal> _shrineAnimals = new NetObjectList<FarmAnimal>();

		public HikawaShrine() {}

		public HikawaShrine(string map, string name)
			: base(map, name)
		{
			var multiplayer = ModEntry.Instance.Helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();
			_shrineAnimals.Add(new FarmAnimal("White Chicken", multiplayer.getNewID(), -1) {age = { 99 }});
			_shrineAnimals.Add(new FarmAnimal("White Chicken", multiplayer.getNewID(), -1) {age = { 99 }});
			_shrineAnimals[0].Position = new Vector2(41f, 18f) * 64f;
			_shrineAnimals[1].Position = new Vector2(43f, 18f) * 64f;
		}

		protected override void initNetFields()
		{
			base.initNetFields();
			base.NetFields.AddFields(_shrineAnimals);
		}

		public override void UpdateWhenCurrentLocation(GameTime time)
		{
			base.UpdateWhenCurrentLocation(time);
			foreach (var animal in _shrineAnimals)
			{
				animal.updateWhenCurrentLocation(time, this);
			}
		}

		public override void draw(SpriteBatch spriteBatch)
		{
			base.draw(spriteBatch);
			foreach (var animal in _shrineAnimals)
			{
				animal.draw(spriteBatch);
			}
		}
	}
}