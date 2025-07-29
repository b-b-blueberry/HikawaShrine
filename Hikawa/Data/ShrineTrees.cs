using System.Collections.Generic;

namespace Hikawa.Data
{
    public record class ShrineTreesDataAsset
    {
        public Dictionary<string, ShrineTreeData> ShrineTrees;
    }

    public record class ShrineTreeData
    {
        /// <summary>
        /// Custom source area of tree texture used to draw entire tree.
        /// </summary>
        public Rectangle TextureRegion;
        public Vector2 TextureOrigin;
        public bool HasShadow;
        public bool CanShake;
        /// <summary>
        /// Area relative to unscaled sprite that can spawn petals.
        /// </summary>
        public Rectangle? LeafRegion;
    }
}
