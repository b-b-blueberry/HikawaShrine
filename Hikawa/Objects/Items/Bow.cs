using Hikawa.Data;
using Hikawa.Objects.Items.Data;
using Hikawa.Objects.Projectiles;
using Netcode;
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
        private readonly IReflectedField<NetEvent0> _finishEvent;

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

            this._aimMethod = ModEntry.Instance.Helper.Reflection.GetMethod(this, "updateAimPos");
            this._finishEvent = ModEntry.Instance.Helper.Reflection.GetField<NetEvent0>(this, "finishEvent");
        }

        public static BowsDataEntry GetData(string itemId)
        {
            if (ItemRegistry.GetData(itemId) is ParsedItemData itemData && itemData.RawData is BowsDataEntry bowData)
            {
                return bowData;
            }

            return null;
        }

        public static BowsDataEntry GetData(Item item)
        {
            return Bow.GetData(item?.ItemId);
        }

        public bool HasArrow(Farmer player)
        {
            if (Bow.GetData(this) is BowsDataEntry bowData)
            {
                return player.Items.ContainsId(bowData.FireObject);
            }

            return false;
        }

        /// <param name="chargeRatio">Charge ratio from 0 (empty) to 1 (full).</param>
        /// <returns>Whether the <see cref="Bow"/> is charged and ready to fire. May not be fully charged if <see cref="BowsDataEntry.MinimumDrawTime"/> is defined.</returns>
        public bool CanRelease(float chargeRatio)
        {
            if (Bow.GetData(this) is BowsDataEntry bowData && bowData.MinimumDrawTime.HasValue)
            {
                return chargeRatio >= bowData.MinimumDrawTime.Value / this.GetRequiredChargeTime();
            }

            return chargeRatio >= 1;
        }

        public float GetTotalChargeTime()
        {
            return (float)(Game1.currentGameTime.TotalGameTime.TotalSeconds - this.pullStartTime);
        }

        public float SpeedMultiplier(Farmer player) => 1 + player.buffs.WeaponSpeedMultiplier;

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

        public override bool CanBeLostOnDeath()
        {
            if (Bow.GetData(this) is BowsDataEntry bowData)
            {
                return bowData.CanBeLostOnDeath;
            }

            return base.CanBeLostOnDeath();
        }

        public override bool CanAutoFire()
        {
            return this.HasArrow(Game1.player) && this.GetAutoFireRate() > 0;
        }

        public override float GetAutoFireRate()
        {
            if (Bow.GetData(this) is BowsDataEntry bowData)
            {
                return bowData.FireRate / this.SpeedMultiplier(this.getLastFarmerToUse());
            }

            return 0;
        }

        public override bool beginUsing(GameLocation location, int x, int y, Farmer who)
        {
            if (base.beginUsing(location, x, y, who))
            {
                // Only play sound on first draw, not on autofire
                if (Bow.GetData(this) is BowsDataEntry bowData && bowData.DrawSound is not null)
                {
                    who.playNearbySoundAll(bowData.DrawSound);
                }

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
            if (Bow.GetData(this) is BowsDataEntry bowData)
            {
                // magical bows don't require arrows
                bool hasArrow = bowData.IsMagical || this.HasArrow(who);
                if (hasArrow)
                {
                    this._aimMethod.Invoke();
                    float chargeRatio = this.GetSlingshotChargeTime();
                    if (this.CanRelease(chargeRatio))
                    {
                        // consume arrow and fire
                        if (!bowData.IsMagical)
                        {
                            who.removeFirstOfThisItemFromInventory(bowData.FireObject);
                        }
                        location.projectiles.Add(BowProjectile.Create(
                            player: who,
                            location: location,
                            bow: this,
                            bowData: bowData,
                            chargeRatio: chargeRatio));
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
            this.lastUser = who;
            this._finishEvent.GetValue().Poll();

            if (who.IsLocalPlayer && who.UsingTool && who.CurrentTool == this)
            {
                who.usingSlingshot = true;

                // Shake on long pulls
                if (!this.CanAutoFire() && this.GetTotalChargeTime() > this.GetRequiredChargeTime() * 2f)
                    who.jitterStrength = 0.5f;

                this._aimMethod.Invoke();
                int mouseX = this.aimPos.X;
                int mouseY = this.aimPos.Y;
                this.mouseDragAmount++;

                if (!Game1.options.useLegacySlingshotFiring)
                {
                    Vector2 shoot_origin = this.GetShootOrigin(who);
                    Vector2 aim_offset = this.AdjustForHeight(new Vector2(mouseX, mouseY)) - shoot_origin;
                    if (Math.Abs(aim_offset.X) > Math.Abs(aim_offset.Y))
                    {
                        if (aim_offset.X < 0f)
                        {
                            who.faceDirection(Game1.left);
                        }
                        if (aim_offset.X > 0f)
                        {
                            who.faceDirection(Game1.right);
                        }
                    }
                    else
                    {
                        if (aim_offset.Y < 0f)
                        {
                            who.faceDirection(Game1.up);
                        }
                        if (aim_offset.Y > 0f)
                        {
                            who.faceDirection(Game1.down);
                        }
                    }
                }
                else
                {
                    who.faceGeneralDirection(new Vector2(mouseX, mouseY), 0, opposite: true);
                }

                // Legacy and auto-fire behaviours as default
                #region Default
                if (!Game1.options.useLegacySlingshotFiring)
                {
                    if (this.canPlaySound && this.GetSlingshotChargeTime() >= 1f)
                    {
                        this.canPlaySound = false;
                    }
                }
                else if (this.canPlaySound && (Math.Abs(mouseX - this.lastClickX) > 8 || Math.Abs(mouseY - this.lastClickY) > 8) && this.mouseDragAmount > 4)
                {
                    this.canPlaySound = false;
                }
                if (!this.CanAutoFire())
                {
                    this.lastClickX = mouseX;
                    this.lastClickY = mouseY;
                }
                if (Game1.options.useLegacySlingshotFiring)
                {
                    Game1.mouseCursor = Game1.cursor_none;
                }
                if (this.CanAutoFire())
                {
                    bool first_fire = false;
                    if (this.GetBackArmDistance(who) >= 20 && this.nextAutoFire < 0f)
                    {
                        this.nextAutoFire = 0f;
                        first_fire = true;
                    }
                    if (this.nextAutoFire > 0f || first_fire)
                    {
                        this.nextAutoFire -= (float)time.ElapsedGameTime.TotalSeconds;
                        if (this.nextAutoFire <= 0f)
                        {
                            this.PerformFire(who.currentLocation, who);
                            this.nextAutoFire = this.GetAutoFireRate();
                        }
                    }
                }
                int offset = ((who.FacingDirection == 3 || who.FacingDirection == 1) ? 1 : ((who.FacingDirection == 0) ? 2 : 0));
                who.FarmerSprite.setCurrentFrame(42 + offset);
                #endregion

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
            if (player.UsingTool && player.CurrentTool is Bow bow && Bow.GetData(bow) is BowsDataEntry bowData)
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

                        Vector2 pull = new Vector2(backArmDistance, 0) * flip;

                        // arbitrary farmer sprite adjustment
                        Vector2 bowArmOffset = new Vector2(-0.5f + 1 * flip, -2);
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
                        // angle between origin (O) and target (T)
                        float a = (float)Utils.Vector.RadiansBetween(O, T);
                        // distance from origin (O) to bow (P)
                        float d = bowSize.X / 2 + 4 * scaledPixelZoom;
                        // position of bow (P)
                        Vector2 P = O + new Vector2(
                            x: MathF.Cos(a) * d,
                            y: MathF.Sin(a) * d);

                        // bowstrings
                        foreach (Vector2 v in bowFrame.Bowstrings)
                        {
                            // angle from boworigin to bowstring
                            float aV = MathF.PI / 2;
                            // distance from origin (O) to bowstring (PV)
                            float dxV = v.X * scaledPixelZoom;
                            // distance between bow (P) and bowstring (PV)
                            float dyV = v.Y * scaledPixelZoom;
                            Vector2 PV = P + new Vector2(
                                x: MathF.Cos(a) * dxV + MathF.Cos(a + aV) * dyV,
                                y: MathF.Sin(a) * dxV + MathF.Sin(a + aV) * dyV);
                            // O1 -> PV
                            Utility.drawLineWithScreenCoordinates((int)O1.X, (int)O1.Y, (int)PV.X, (int)PV.Y, b, bowstringColour, GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot, true) + pinch);
                        }

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
                        // arbitrary farmer sprite adjustment
                        Vector2 pullArmOffset = new Vector2(4, -32 - backArmDistance / 2);
                        // arbitrary farmer sprite adjustment
                        Vector2 bowArmOffset = new Vector2(-2, 1) * scaledPixelZoom;

                        // origin (O)
                        Vector2 O = position;
                        // origin (O) adjusted for pulling arm (O1)
                        Vector2 O1 = O + new Vector2(16, -28 - backArmDistance / 2);
                        // position of bow (P)
                        Vector2 P = O + new Vector2(11 - frontArmRotation * 2.5f, -4) * scaledPixelZoom;

                        // pulling arm
                        b.Draw(playerTexture, O + pullArmOffset, new Rectangle(148, 244, 4, 4), Color.White, 0f, Vector2.Zero, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Arms));

                        // bowstrings
                        foreach (Vector2 v in bowFrame.Bowstrings)
                        {
                            Vector2 PV = P + v * scaledPixelZoom;
                            Utility.drawLineWithScreenCoordinates((int)O1.X, (int)O1.Y, (int)PV.X, (int)PV.Y, b, bowstringColour, GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot));
                        }

                        // bow arm
                        b.Draw(playerTexture, P + bowArmOffset, new Rectangle(168, 239, 5, 3), Color.White, 0, Vector2.Zero, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot, true) + pinch);

                        // bow
                        b.Draw(bowTexture, P, bowSource, Color.White, 0, bowOrigin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot) + pinch);
                    }
                    break;
                }
                return true;
            }
            return false;
        }
    }
}
