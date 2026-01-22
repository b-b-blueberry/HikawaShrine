namespace Hikawa.Volleyball
{
    public class VolleyballCharacterData
    {
        /// <summary>Asset name for volleyball overworld sprite.</summary>
        public string TextureId;
        /// <summary>Reaction speed and thinking rate.</summary>
        public float Responsiveness;
        /// <summary>Velocity used when jumping.</summary>
        public float Jump;
        /// <summary>Modifier to movement values (velocity, gravity, deceleration).</summary>
        public float Weight;
        /// <summary>Modifier to movement values (velocity, acceleration).</summary>
        public float Speed;
        /// <summary>Modifier to strike values.</summary>
        public float Power;
    }
}
