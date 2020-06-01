using System.Collections.Generic;

namespace Hikawa
{
	public interface IJsonAssetsApi
	{
		void LoadAssets(string path);
		int GetObjectId(string name);
		int GetFruitTreeId(string name);
		int GetHatId(string name);
		List<string> GetAllHatsFromContentPack(string cp);
	}
}
