using System.Collections.Generic;

namespace Hikawa
{
	public interface IJsonAssetsApi
	{
		void LoadAssets(string path);

		int GetObjectId(string name);
		int GetCropId(string name);
		int GetFruitTreeId(string name);
		int GetBigCraftableId(string name);
		int GetHatId(string name);
		int GetWeaponId(string name);
		int GetClothingId(string name);

		List<string> GetAllHatsFromContentPack(string cp);
	}
}
