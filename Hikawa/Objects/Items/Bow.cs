using Hikawa.Data;
using Hikawa.Objects.Items.Data;
using Hikawa.Objects.Projectiles;
using Microsoft.Xna.Framework.Media;
using StardewModdingAPI;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Tools;
using System;
using System.Xml.Serialization;
using static StardewValley.FarmerRenderer;

namespace Hikawa.Objects.Items
{
    [XmlType($"{ModConsts.SpaceCoreXmlPrefix}{nameof(Bow)}")] // SpaceCore serialisation signature
    public class Bow : Slingshot
    {
        private readonly IReflectedMethod _aimMethod;

        public override string TypeDefinitionId => BowItemDataDefinition.TypeDefinitionId;

        public Bow() : this(id: null) {}

        public Bow(string id)
        {
            this.ItemId = id;

            var data = ItemRegistry.GetDataOrErrorItem(this.TypeDefinitionId + this.ItemId);

            this.Name = data.InternalName;
            this.InitialParentTileIndex = data.SpriteIndex;
            this.CurrentParentTileIndex = data.SpriteIndex;
            this.IndexOfMenuItemView = data.SpriteIndex;
            this.AttachmentSlotsCount = 0;
            this.PlayUseSounds = false;

            if (ItemRegistry.GetData(this.ItemId) is ParsedItemData itemData && itemData.RawData is BowsDataEntry bowData)
            {
                this.PlayUseSounds = !bowData.IsMagical;
            }

            this._aimMethod = ModEntry.Instance.Helper.Reflection.GetMethod(this, "updateAimPos");
        }

        protected override Item GetOneNew()
        {
            return new Bow(this.ItemId);
        }

        protected override void GetOneCopyFrom(Item source)
        {
            base.GetOneCopyFrom(source);

            if (source is Bow bow)
            {
                this.AttachmentSlotsCount = bow.AttachmentSlotsCount;
            }
        }

        public override bool beginUsing(GameLocation location, int x, int y, Farmer who)
        {
            if (base.beginUsing(location, x, y, who))
            {
                // Prevent slingshot draw behaviours
                who.usingSlingshot = false;
                who.UsingTool = true;
                return true;
            }

            return false;
        }

        public override void endUsing(GameLocation location, Farmer who)
        {
            base.endUsing(location, who);

            this.finish();
        }

        public override void PerformFire(GameLocation location, Farmer who)
        {
            if (ItemRegistry.GetData(this.ItemId) is ParsedItemData itemData && itemData.RawData is BowsDataEntry bowData)
            {
                who.playNearbySoundAll(bowData.FireSound);

                this._aimMethod.Invoke();
                int backArmDistance = this.GetBackArmDistance(who);
                if (backArmDistance > 4 && !this.canPlaySound)
                {
                    location.projectiles.Add(BowProjectile.Create(
                        who: who,
                        location: location,
                        bow: this,
                        bowData: bowData,
                        charge: backArmDistance));
                }
            }

            this.canPlaySound = true;
        }

        public override void tickUpdate(GameTime time, Farmer who)
        {
            if (who.UsingTool)
            {
                // terrible things
                who.usingSlingshot = true;
                base.tickUpdate(time, who);
                who.usingSlingshot = false;
            }
        }

        public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            this.AdjustMenuDrawForRecipes(ref transparency, ref scaleSize);

            var data = ItemRegistry.GetDataOrErrorItem(this.QualifiedItemId);
            var origin = data.GetSourceRect().Size.ToVector2() / 2;
            spriteBatch.Draw(
                texture: data.GetTexture(),
                position: location + origin * Game1.pixelZoom,
                sourceRectangle: data.GetSourceRect(),
                color: color * transparency,
                rotation: 0,
                origin: origin,
                scale: Game1.pixelZoom * scaleSize,
                effects: SpriteEffects.None,
                layerDepth: layerDepth);

            this.DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color);
        }

        public override void draw(SpriteBatch b)
        {
            base.draw(b);
        }

        public static void TryDrawWhenUsing(SpriteBatch b, Texture2D playerTexture, Farmer player, Vector2 position, int direction, float layerDepth, float scaledPixelZoom)
        {
            if (player.UsingTool && player.CurrentTool is Bow bow && ItemRegistry.GetData(bow.ItemId) is ParsedItemData itemData && itemData.RawData is BowsDataEntry bowData)
            {
                // From base game:
                int backArmDistance = bow.GetBackArmDistance(player);
                Vector2 target = bow.AdjustForHeight(Utility.PointToVector2(bow.aimPos.Value));
                Vector2 from = bow.GetShootOrigin(player);
                Vector2 motion = target - from;
                float frontArmRotation = MathF.Atan2(motion.Y, motion.X) + MathF.PI;
                if (!Game1.options.useLegacySlingshotFiring)
                {
                    frontArmRotation -= MathF.PI;
                    if (frontArmRotation < 0)
                        frontArmRotation += MathF.PI * 2;
                }

                // Bow behaviour:
                Texture2D bowTexture = Game1.content.Load<Texture2D>(bowData.Texture);
                Rectangle bowSource = bowData.SourceRect;
                Vector2 bowOrigin = bowSource.Size.ToVector2() / 2;
                Color bowstringColour = Utility.StringToColor(bowData.BowstringColour) ?? Color.White;
                Vector2 bowSize = new Vector2(16, 16) * Game1.pixelZoom;
                int distance = 8;
                float degrees = (bowSize.Y - distance) / 2;

                switch (direction)
                {
                    case Game1.up:
                    {
                        // arm
                        b.Draw(playerTexture, position + new Vector2(4 + frontArmRotation * 8, -Game1.tileSize * 3 / 4 + 4), new Rectangle(173, 238, 9, 14), Color.White, 0f, new Vector2(4, 11), scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.SlingshotUp));
                        // bow
                        b.Draw(bowTexture, position + new Vector2(4 + frontArmRotation * 8, -Game1.tileSize * 3 / 4 + 4), bowSource, Color.White, 0f, bowOrigin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.SlingshotUp));
                    }                       
                    break;

                    case Game1.right:
                    {
                        // pulling arm
                        b.Draw(playerTexture, position + new Vector2(52 - backArmDistance, -Game1.tileSize / 2), new Rectangle(147, 237, 10, 4), Color.White, 0f, new Vector2(8, 3), scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot));
                        // bow arm
                        b.Draw(playerTexture, position + new Vector2(36, -24), new Rectangle(147, 237, 10, 4), Color.White, frontArmRotation, new Vector2(0, 3), scaledPixelZoom, SpriteEffects.FlipVertically, GetLayerDepth(layerDepth, FarmerSpriteLayers.SlingshotUp));

                        // inverse of back arm to keep front arm in place
                        Vector2 pullback = new Vector2(backArmDistance, 0);
                        // point where bowstring joins hand
                        Vector2 hand = position - pullback + new Vector2(14, -9) * scaledPixelZoom;
                        Vector2 centre = Vector2.Normalize(motion) * distance * scaledPixelZoom + pullback;
                        for (int i = 0; i < 2; ++i)
                        {
                            // How to rotate 2d vector???????
                            // https://stackoverflow.com/a/28730480
                            // Johan Larsson - Feb 25, 2015
                            double radians = (-degrees + i * degrees * 2) * Math.PI / 180;
                            var ca = Math.Cos(radians);
                            var sa = Math.Sin(radians);
                            Vector2 v = hand + new Vector2(
                                x: (float)(ca * centre.X - sa * centre.Y),
                                y: (float)(sa * centre.X + ca * centre.Y));
                            Utility.drawLineWithScreenCoordinates((int)hand.X, (int)hand.Y, (int)v.X, (int)v.Y, b, bowstringColour);
                        }

                        // bow
                        b.Draw(bowTexture, hand + centre, bowSource, Color.White, frontArmRotation, bowOrigin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot));
                    }
                    break;

                    case Game1.left:
                    {
                        // pulling arm
                        b.Draw(playerTexture, position + new Vector2(40 + backArmDistance, -Game1.tileSize / 2), new Rectangle(147, 237, 10, 4), Color.White, 0f, new Vector2(9, 4), scaledPixelZoom, SpriteEffects.FlipHorizontally, GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot));
                        // bow arm
                        b.Draw(playerTexture, position + new Vector2(20, -28), new Rectangle(147, 237, 10, 4), Color.White, frontArmRotation + MathF.PI, new Vector2(8, 3), scaledPixelZoom, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, GetLayerDepth(layerDepth, FarmerSpriteLayers.SlingshotUp));

                        Vector2 pullback = new Vector2(-backArmDistance, 0);
                        Vector2 hand = position - pullback + new Vector2(2, -9) * scaledPixelZoom;

                        // beau
                        //{
                        //    var source = new Rectangle(306, 320, 16, 16);
                        //    b.Draw(Game1.mouseCursors, hand + pullback + Vector2.Normalize(motion) * distance * scaledPixelZoom, source, Color.White * 0.75f, frontArmRotation, source.Size.ToVector2() / 2, 4f, SpriteEffects.None, 1);
                        //}
                        // clocque
                        //{
                        //    var source = new Rectangle(228, 465, 37, 37);
                        //    b.Draw(Game1.mouseCursors, hand, source, Color.White * 0.5f, 0, source.Size.ToVector2() / 2, 4f, SpriteEffects.None, 1);
                        //}

                        Vector2 centre = Vector2.Normalize(motion) * distance * scaledPixelZoom + pullback;
                        for (int i = 0; i < 2; ++i)
                        {
                            double radians = (-degrees + i * degrees * 2) * Math.PI / 180;
                            var ca = Math.Cos(radians);
                            var sa = Math.Sin(radians);
                            Vector2 v = hand + new Vector2(
                                x: (float)(ca * centre.X - sa * centre.Y),
                                y: (float)(sa * centre.X + ca * centre.Y));
                            Utility.drawLineWithScreenCoordinates((int)hand.X, (int)hand.Y, (int)v.X, (int)v.Y, b, bowstringColour);
                        }

                        // bow
                        b.Draw(bowTexture, hand + centre, bowSource, Color.White, frontArmRotation, bowOrigin, scaledPixelZoom, SpriteEffects.FlipVertically, GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot));
                    }
                    break;

                    case Game1.down:
                    {
                        // back arm
                        b.Draw(playerTexture, position + new Vector2(4, -Game1.tileSize / 2 - backArmDistance / 2), new Rectangle(148, 244, 4, 4), Color.White, 0f, Vector2.Zero, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Arms));

                        // TODO
                        Utility.drawLineWithScreenCoordinates(
                            (int)(position.X + 16),
                            (int)(position.Y - 28 - backArmDistance / 2),
                            (int)(position.X + 44 - frontArmRotation * 10),
                            (int)(position.Y - Game1.tileSize / 4 - 8),
                            b,
                            bowstringColour);
                        Utility.drawLineWithScreenCoordinates(
                            (int)(position.X + 16),
                            (int)(position.Y - 28 - backArmDistance / 2),
                            (int)(position.X + 56 - frontArmRotation * 10),
                            (int)(position.Y - Game1.tileSize / 4 - 8),
                            b,
                            bowstringColour);

                        // front arm
                        b.Draw(playerTexture, position + new Vector2(44 - frontArmRotation * 10, -Game1.tileSize / 4), new Rectangle(167, 235, 7, 9), Color.White, 0f, new Vector2(3, 5), scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot, true));

                        // bow
                        b.Draw(bowTexture, position + new Vector2(44 - frontArmRotation * 10, -Game1.tileSize / 4), bowSource, Color.White, 0, bowOrigin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot));
                    }
                    break;
                }
            }
        }
    }
}
