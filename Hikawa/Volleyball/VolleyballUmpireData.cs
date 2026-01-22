namespace Hikawa.Volleyball
{
    public struct VolleyballUmpireData(string name, string displayName, string textureName, Rectangle sourceRectangle, int initialFrame, int portraitFrame, bool isBreathing)
    {
        public string Name = name;
        public string DisplayName = displayName;
        public string TextureName = textureName;
        public Rectangle SourceRectangle = sourceRectangle;
        public int InitialFrame = initialFrame;
        public int PortraitFrame = portraitFrame;
        public bool IsBreathing = isBreathing;
    }
}
