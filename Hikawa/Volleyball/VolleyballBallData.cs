namespace Hikawa.Volleyball
{
    public class VolleyballBallData
    {
        /// <summary>Asset name for volleyball overworld sprite.</summary>
        public string TextureId;
        /// <summary>Region in sprite asset to draw.</summary>
        public Rectangle SourceArea;
        /// <summary>Sound played on hit.</summary>
        public string SmallHitSound;
        /// <summary>Sound played on spike.</summary>
        public string HitSound;
        /// <summary>Sound played on serve.</summary>
        public string HeavyHitSound;
        /// <summary>Modifier to movement values.</summary>
        public float Weight;
        /// <summary>Whether to use loose rotation behaviour.</summary>
        public bool IsFloaty;
    }
}
