using Hjson;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;

namespace ThaiLanguageLibrary.Common.UI
{
    public class TranslateTool_ModlistMenu(UIState previousState) : UIState
    {


        UIAutoScaleTextTextPanel<LocalizedText> backbutton;

        
        public override void OnInitialize()
        {
            UIAutoScaleTextTextPanel<string> t = new(Language.GetText("Mods.ThaiLanguageLibrary.UI.Modlist").Value)
            {
                Width = new StyleDimension(-10f, 1f / 3f),
                Height = { Pixels = 60 },
                HAlign = 0.5f,
                VAlign = 0.15f,
            };
            UIPanel panel = new();  
            panel.Width.Set(500, 0);          
            panel.Height.Set(450, 0);
            panel.HAlign = 0.5f;
            panel.VAlign = 0.6f;
            var scrollbar = new UIScrollbar();
            scrollbar.SetView(100f, 1000f);
            scrollbar.Height.Set(0f, 1f);
            scrollbar.HAlign = 1f;
            var grid = new UIGrid();
            grid.Width.Set(0, 1f);
            grid.Height.Set(0, 1f);
            grid.ListPadding = 5f;
            grid.SetScrollbar(scrollbar);
            foreach (Mod mod in ModLoader.Mods) {
                if (mod.Name == "ModLoader")
                {
                    continue;
                }
                var item = new UIPanel();
                    
                item.Width.Set(100f, 0);
                item.Height.Set(100f, 0);
                item.WithFadedMouseOver();
                Asset<Texture2D> iconTexture;
                if (mod.FileExists("icon.png"))
                {
                    using var s = mod.GetFileStream("icon.png");
                    iconTexture = Main.Assets.CreateUntracked<Texture2D>(s, ".png");

                }
                else
                {
                    iconTexture = ModContent.Request<Texture2D>("Asset/Temp-icon.png");
                }
                if (iconTexture.Width() == 80 && iconTexture.Height() == 80)
                {
                    var image = new UIImage(iconTexture);
                    item.Append(image);
                }
                item.OnMouseOver += (s, e) => {t.SetText(Language.GetText("Mods.ThaiLanguageLibrary.UI.Modlist").Value +"\n"+ mod.Name);};
                item.OnMouseOut += (s, e) => { t.SetText(Language.GetText("Mods.ThaiLanguageLibrary.UI.Modlist").Value); };
                item.OnLeftClick += (s, e) =>
                {
                    ProcessStartInfo startInfo = new()
                    {
                        Arguments = Utility.ExtractLocalization(mod),
                        FileName = "explorer.exe"
                    };
                    SoundEngine.PlaySound(SoundID.MenuOpen);
                    Process.Start(startInfo);
                };
                grid.Add(item);
            }
            panel.Append(grid);
            backbutton = new UIAutoScaleTextTextPanel<LocalizedText>(Language.GetText("UI.Back"))
            {
                Width = new StyleDimension(-10f, 1f / 3f),
                Height = { Pixels = 40 },
                HAlign = 0.5f,
                VAlign = 0.95f,
                Top = { Pixels = -20 }
            }.WithFadedMouseOver();

            backbutton.OnLeftClick += OnClick;
            Append(t);
            Append(panel);
            Append(backbutton);
        }

        private void OnClick(UIMouseEvent evt, UIElement listeningElement)
        {
            if (listeningElement == backbutton)
            {
                SoundEngine.PlaySound(SoundID.MenuClose);
                Main.MenuUI.GoBack();
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            if (ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
            }
        }
    }
}
