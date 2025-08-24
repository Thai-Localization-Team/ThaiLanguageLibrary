using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;
using ThaiLanguageLibrary.Common.UI;

namespace ThaiLanguageLibrary.Common.Config
{
    [DisplayName("Config")]
    internal class Config : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Header("AssetLoader")]

        [DefaultValue(false)]
        [ReloadRequired]
        public bool ExternalAsset;

        [Header("TranslateTool")]

        [SeparatePage]
        public ExportAsJsonButton ExportAsJson { get; set; } = new();

		[SeparatePage]
		public GenerateTempleButton GenerateTemple { get; set; } = new();

		[SeparatePage]
        public UpdateExternalAssetButton UpdateExternalAsset { get; set; } = new();

		public class GenerateTempleButton()
		{
			[CustomModConfigItem(typeof(GenerateTempleProcess))]
			public int Button;
		}
		public class GenerateTempleProcess : FloatElement
		{
			public override void Draw(SpriteBatch spriteBatch)
			{
				var mod = ModContent.GetInstance<ThaiLanguageLibrary>();
				var Temple = Directory.CreateDirectory(Path.Combine(ThaiLanguageLibrary.Export, "Temple Language Pack_" + Guid.NewGuid().ToString()));
                Directory.CreateDirectory(Path.Combine(Temple.FullName, "Content", "Localization"));
				using Stream stream = mod.GetFileStream("Asset/Temple Language Pack/pack.json");
				using StreamReader streamReader = new(stream);
				string fileText = streamReader.ReadToEnd();
				File.WriteAllText(Path.Combine(Temple.FullName, "pack.json"), fileText);
				ProcessStartInfo startInfo = new()
				{
					Arguments = Path.Combine(Temple.FullName, "pack.json"),
					FileName = "notepad.exe"
				};
				SoundEngine.PlaySound(SoundID.MenuOpen);
				Process.Start(startInfo);
				Main.MenuUI.GoBack();
			}
		}
		public class ExportAsJsonButton()
        {
            [CustomModConfigItem(typeof(ExportAsJsonProcess))]
            public int Button;
        }

        public class ExportAsJsonProcess : FloatElement
        {
            public override void Draw(SpriteBatch spriteBatch)
            {
                Main.MenuUI.SetState(new TranslateTool_ModlistMenu(Main.MenuUI.CurrentState));
                SoundEngine.PlaySound(SoundID.MenuOpen);
                Main.menuMode = 888;
            }
        }

        public class UpdateExternalAssetButton()
        {
            [CustomModConfigItem(typeof(UpdateExternalAssetProcess))]
            public int Button;
        }

        public class UpdateExternalAssetProcess : FloatElement
        {
            public override void Draw(SpriteBatch spriteBatch)
            {
                var mod = ModContent.GetInstance<ThaiLanguageLibrary>(); 
                foreach (String file in mod.GetFileNames())
                {
                    string extension = Path.GetExtension(file);
                    if (extension != ".json" && extension != ".csv")
                    {
                        continue;
                    }
                    if (file.StartsWith("Asset/Localization/th-TH."))
                    {
                        using Stream stream = mod.GetFileStream(file);
                        using StreamReader streamReader = new(stream);
                        string fileText = streamReader.ReadToEnd();
                        File.WriteAllText(Path.Combine(ThaiLanguageLibrary.Asset, file.Split("/")[2]), fileText);
                    }
                }
                ProcessStartInfo startInfo = new()
                {
                    Arguments = ThaiLanguageLibrary.Asset,
                    FileName = "explorer.exe"
                };
                SoundEngine.PlaySound(SoundID.MenuOpen);
                Process.Start(startInfo);
				Main.MenuUI.GoBack();
			}
        }
    }
}
