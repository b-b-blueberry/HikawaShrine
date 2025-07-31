using Hikawa.Data;
using Hikawa.Objects.Items.Data;
using Hikawa.Objects.Projectiles;
using StardewModdingAPI;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Tools;
using System;
using System.Linq;
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

        public bool HasArrow(Farmer player)
        {
            if (ItemRegistry.GetData(this.ItemId) is ParsedItemData itemData && itemData.RawData is BowsDataEntry bowData)
            {
                return player.Items.ContainsId(bowData.FireObject);
            }

            return false;
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

        public override bool CanAutoFire()
        {
            return this.HasArrow(Game1.player) && this.GetAutoFireRate() > 0;
        }

        public override float GetAutoFireRate()
        {
            if (ItemRegistry.GetData(this.ItemId) is ParsedItemData itemData && itemData.RawData is BowsDataEntry bowData)
            {
                return bowData.FireRate;
            }

            return 0;
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
                // magical bows don't require arrows
                bool hasArrow = bowData.IsMagical || this.HasArrow(who);
                if (hasArrow)
                {
                    this._aimMethod.Invoke();
                    int backArmDistance = this.GetBackArmDistance(who);
                    if (backArmDistance > 4 && !this.canPlaySound)
                    {
                        // consume arrow and fire
                        if (!bowData.IsMagical)
                        {
                            who.removeFirstOfThisItemFromInventory(bowData.FireObject);
                        }
                        location.projectiles.Add(BowProjectile.Create(
                            who: who,
                            location: location,
                            bow: this,
                            bowData: bowData,
                            charge: backArmDistance));
                        who.playNearbySoundAll(bowData.FireSound);
                    }
                }
                else
                {
                    Game1.showRedMessage(Game1.content.LoadString("Strings/StringsFromCSFiles:Slingshot.cs.14254"));
                }
            }

            this.canPlaySound = true;
        }

        public override void tickUpdate(GameTime time, Farmer who)
        {
            if (who.CurrentTool == this && who.UsingTool)
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

        public static bool TryGetMaxDrawTime(Slingshot slingshot, ref float result)
        {
            if (slingshot is Bow bow && ItemRegistry.GetData(bow.ItemId) is ParsedItemData itemData && itemData.RawData is BowsDataEntry bowData && bowData.DrawTime.HasValue)
            {
                result = bowData.DrawTime.Value;
                return true;
            }
            return false;
        }

        public static bool TryDrawWhenUsing(SpriteBatch b, Texture2D playerTexture, Farmer player, Vector2 position, int direction, float layerDepth, float scaledPixelZoom)
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
                BowFrame bowFrame = bowData.HeldFrames[direction];
                Texture2D bowTexture = Game1.content.Load<Texture2D>(bowData.Texture);
                Rectangle bowSource = bowFrame.SourceRect;
                Vector2 bowOrigin = bowSource.Size.ToVector2() / 2;
                Color bowstringColour = Utility.StringToColor(bowData.BowstringColour) ?? Color.White;
                Vector2 bowSize = bowSource.Size.ToVector2() * scaledPixelZoom;

                bool hasArrow = bowData.IsMagical || player.Items.ContainsId(bowData.FireObject);
                ParsedItemData arrowData = ItemRegistry.GetData(bowData.FireObject);
                Texture2D arrowTexture = arrowData?.GetTexture();
                Rectangle arrowSource = arrowData?.GetSourceRect() ?? Rectangle.Empty;
                Vector2 arrowOrigin = arrowSource.Size.ToVector2() / 2;
                float pinch = 0.00001f;

                switch (direction)
                {
                    case Game1.up:
                    {
                        // arm
                        b.Draw(playerTexture, position + new Vector2(frontArmRotation * 8 - 32, -36), new Rectangle(103, 240, 7, 5), Color.White, MathF.PI * 0.5f, new Vector2(4, 11), scaledPixelZoom, SpriteEffects.FlipVertically, GetLayerDepth(layerDepth, FarmerSpriteLayers.SlingshotUp));
                        // bow
                        b.Draw(bowTexture, position + new Vector2(4 + frontArmRotation * 8, -56), bowSource, Color.White, 0f, bowOrigin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth - pinch, FarmerSpriteLayers.SlingshotUp));
                    }                       
                    break;

                    case Game1.right:
                    case Game1.left:
                    {
                        bool isFlip = direction == Game1.right;
                        int flip = isFlip ? 1 : -1;
                        SpriteEffects spriteFlipH = flip > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                        SpriteEffects spriteFlipHV = flip > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically;

                        // distance from origin (O) to bow (P)
                        float d = bowSize.X / 2 + 4 * scaledPixelZoom;
                        // distance from origin (O) to top bowstring (P1)
                        float dx1 = bowSize.X - 8 * scaledPixelZoom;
                        // distance from origin (O) to bottom bowstring (P2)
                        float dx2 = bowSize.X - 8 * scaledPixelZoom;
                        // distance between bow (P) and top (P1) bowstring
                        float dy1 = bowSize.Y / 2;
                        // distance between top (P) and bottom (P2) bowstring
                        float dy2 = bowSize.Y / 2;

                        Vector2 pull = new Vector2(backArmDistance, 0) * flip;

                        // arbitrary farmer sprite adjustment
                        Vector2 bowArmOffset = new Vector2(-0.5f - 1 * flip, -2);
                        // arbitrary farmer sprite adjustment
                        Vector2 pullArmOffset = new Vector2(0.5f + 7 * flip, -1);

                        // origin (O) adjusted for bow arm position
                        Vector2 O = Game1.GlobalToLocal(from) + bowArmOffset * scaledPixelZoom;
                        // origin (O) adjusted for pulling arm (O1)
                        Vector2 O1 = O - pull + pullArmOffset * scaledPixelZoom;
                        // target (T) of aim point at mouse cursor
                        Vector2 T = Game1.GlobalToLocal(target);

                        // tiltookilikak (2025)
                        // https://discord.com/channels/137344473976799233/156109690059751424/1400346902297575597
                        float a = (float)Utils.Vector.RadiansBetween(O, T);
                        float a1 = a + MathF.PI * 0.5f;
                        float a2 = a + MathF.PI * 1.5f;
                        Vector2 P = O + new Vector2(
                            x: MathF.Cos(a) * d,
                            y: MathF.Sin(a) * d);
                        Vector2 P1 = O + new Vector2(
                            x: MathF.Cos(a) * dx1 + MathF.Cos(a1) * dy1,
                            y: MathF.Sin(a) * dx1 + MathF.Sin(a1) * dy1);
                        Vector2 P2 = O + new Vector2(
                            x: MathF.Cos(a) * dx2 + MathF.Cos(a2) * dy2,
                            y: MathF.Sin(a) * dx2 + MathF.Sin(a2) * dy2);
                        Utility.drawLineWithScreenCoordinates((int)O1.X, (int)O1.Y, (int)P1.X, (int)P1.Y, b, bowstringColour, GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot, true) - pinch);
                        Utility.drawLineWithScreenCoordinates((int)O1.X, (int)O1.Y, (int)P2.X, (int)P2.Y, b, bowstringColour, GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot, true) - pinch);

                        // pulling arm
                        b.Draw(playerTexture, O1, new Rectangle(147, 237, 10, 4), Color.White, 0, new Vector2(isFlip ? 10 : 0, 2), scaledPixelZoom, spriteFlipH, GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot));
                        // bow arm
                        b.Draw(playerTexture, P, new Rectangle(147, 237, 10, 4), Color.White, a + MathF.PI, new Vector2(0, 2), scaledPixelZoom, spriteFlipHV, GetLayerDepth(layerDepth, FarmerSpriteLayers.SlingshotUp));

                        // bow
                        b.Draw(bowTexture, P, bowSource, Color.White, a, bowOrigin, scaledPixelZoom, spriteFlipHV, GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot));
                        // arrow
                        if (hasArrow && arrowTexture is not null)
                        {
                            b.Draw(arrowTexture, P, arrowSource, Color.White, a, new Vector2(arrowOrigin.X - pull.X / scaledPixelZoom * -flip, arrowOrigin.Y), scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth + pinch, FarmerSpriteLayers.Slingshot));
                        }
                    }
                    break;

                    case Game1.down:
                    {
                        // back arm
                        b.Draw(playerTexture, position + new Vector2(4, -Game1.tileSize / 2 - backArmDistance / 2), new Rectangle(148, 244, 4, 4), Color.White, 0f, Vector2.Zero, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Arms));

                        // bowstring
                        Vector2 centre = position + new Vector2(11 - frontArmRotation * 2.5f, -4) * scaledPixelZoom;
                        Utility.drawLineWithScreenCoordinates(
                            (int)(position.X + 16),
                            (int)(position.Y - 28 - backArmDistance / 2),
                            (int)(centre.X),
                            (int)(centre.Y - bowSize.Y / 2),
                            b,
                            bowstringColour,
                            GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot));
                        Utility.drawLineWithScreenCoordinates(
                            (int)(position.X + 16),
                            (int)(position.Y - 28 - backArmDistance / 2),
                            (int)(centre.X),
                            (int)(centre.Y + bowSize.Y / 2),
                            b,
                            bowstringColour,
                            GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot));

                        // front arm
                        b.Draw(playerTexture, centre + new Vector2(-2, 1) * scaledPixelZoom, new Rectangle(168, 239, 5, 3), Color.White, 0f, Vector2.Zero, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot, true) + pinch);

                        // bow
                        b.Draw(bowTexture, centre, bowSource, Color.White, 0, bowOrigin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot) + pinch);
                    }
                    break;
                }
                return true;
            }
            return false;
        }
    }
}
