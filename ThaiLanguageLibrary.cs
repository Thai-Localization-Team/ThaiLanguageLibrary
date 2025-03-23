using Terraria.ModLoader;
using Terraria.Localization;
using Terraria.GameContent;
using System.Reflection;
using System;
using Terraria;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ReLogic.Graphics;
using ReLogic.Content;
using System.Text;
using Newtonsoft.Json.Linq;
using CsvHelper;
using System.Text.RegularExpressions;
using Terraria.ModLoader.Core;

namespace ThaiLanguageLibrary
{
    public partial class ThaiLanguageLibrary : Mod
    {
        private Asset<DynamicSpriteFont> ItemStack;
        private Asset<DynamicSpriteFont> MouseText;
        private Asset<DynamicSpriteFont> DeathText;
        private Asset<DynamicSpriteFont>[] CombatText = new Asset<DynamicSpriteFont>[2];
        private bool firstLoad = false;
		public static readonly string MainDir = Path.Combine(Main.SavePath, nameof(ThaiLanguageLibrary));
        public static readonly string Asset = Path.Combine(MainDir, "Asset");
        public static readonly string AssetMods = Path.Combine(Asset, "Mods");

        public static readonly string Export = Path.Combine(MainDir, "Export");

        public static DirectoryInfo AssetDirectory;
        public static DirectoryInfo AssetModsDirectory;

        internal static Dictionary<string, string> ModdedKeys;
        private readonly static string[] IncompatibleModNames = {
            "ChineseLocalization",
            "ExtraLanguage",
			"MoreLocales"
		};
        private bool MoreLocales = false;


		public override void Load()
        {
            foreach (Mod mod in ModLoader.Mods)
            {
                if (IncompatibleModNames.Contains(mod.Name))
                {
                    if (mod.Name == "MoreLocales")
                    {
						MoreLocales = true;

					}
                    else {
						throw new Exception($"Incompatible mod detected: {mod.Name}. Please unload it first before enabling this mod!");
					}
                        
                }
            }

            Directory.CreateDirectory(MainDir);
            AssetDirectory = Directory.CreateDirectory(Asset);
            AssetModsDirectory = Directory.CreateDirectory(AssetMods);

            foreach (String file in GetFileNames())
            {
                string extension = Path.GetExtension(file);
                if (extension != ".json" && extension != ".csv")
                {
                    continue;
                }
                if (file.StartsWith("Asset/Localization/th-TH."))
                {
                    if (!File.Exists(Path.Combine(Asset, file.Split("/")[2])))
                    {
                        using Stream stream = GetFileStream(file);
                        using StreamReader streamReader = new(stream);
                        string fileText = streamReader.ReadToEnd();
                        File.WriteAllText(Path.Combine(Asset, file.Split("/")[2]), fileText);
					}
                }
            }
            var fileMod = "Asset/Mod/th-TH_Mods.ThaiLanguageLibrary.json";
            if (!File.Exists(Path.Combine(AssetMods, fileMod.Split("/")[2])))
            {
                using Stream stream = GetFileStream(fileMod);
                using StreamReader streamReader = new(stream);
                string fileText = streamReader.ReadToEnd();
                File.WriteAllText(Path.Combine(AssetMods, fileMod.Split("/")[2]), fileText);
				firstLoad = true;
			}
            if (!MoreLocales) {
				ItemStack = FontAssets.ItemStack;
				MouseText = FontAssets.MouseText;
				DeathText = FontAssets.DeathText;
				CombatText = FontAssets.CombatText;
				FontAssets.ItemStack = Assets.Request<DynamicSpriteFont>("Asset/Fonts/Item_Stack", AssetRequestMode.ImmediateLoad);
				FontAssets.MouseText = Assets.Request<DynamicSpriteFont>("Asset/Fonts/Mouse_Text", AssetRequestMode.ImmediateLoad);
				FontAssets.DeathText = Assets.Request<DynamicSpriteFont>("Asset/Fonts/Death_Text", AssetRequestMode.ImmediateLoad);
				FontAssets.CombatText[0] = Assets.Request<DynamicSpriteFont>("Asset/Fonts/Combat_Text", AssetRequestMode.ImmediateLoad);
				FontAssets.CombatText[1] = Assets.Request<DynamicSpriteFont>("Asset/Fonts/Combat_Crit", AssetRequestMode.ImmediateLoad);
			}
			ModdedKeys = [];

            var namedCulturesFieldInfo = typeof(GameCulture).GetField("_NamedCultures", BindingFlags.Static | BindingFlags.NonPublic);
            var namedCultures = (Dictionary<GameCulture.CultureName, GameCulture>)namedCulturesFieldInfo.GetValue(null);



            // When the culture doesn't exist, it will be returned English culture instead.
            if (!MoreLocales) {
				var culture = GameCulture.FromName("th-TH");
				if (culture.LegacyId == 1)
				{
					culture = new GameCulture("th-TH", 10);
				}
				else
				{
					Logger.Debug($"ภาษาไทย (Thai) already exists, skipping");
				}
				namedCultures.Add((GameCulture.CultureName)10, culture);
				namedCulturesFieldInfo.SetValue(null, namedCultures);
			}

            Logger.Info("Loaded Thai Language Support!");
            LoadHooks();
            base.Load();
        }

        public override void Unload()
        {
            UnloadHooks();

            var dict = new Dictionary<GameCulture.CultureName, GameCulture>();
            for (int i = 0; i < 10; i++)
            {
                dict.Add((GameCulture.CultureName)i, GameCulture.FromLegacyId(i));
            }

            var namedCulturesFieldInfo = typeof(GameCulture).GetField("_NamedCultures", BindingFlags.Static | BindingFlags.NonPublic);
            namedCulturesFieldInfo.SetValue(null, dict);

            if (!MoreLocales)
            {
				FontAssets.ItemStack = ItemStack;
				FontAssets.DeathText = DeathText;
				FontAssets.MouseText = MouseText;
				FontAssets.CombatText = CombatText;
			}
            ModdedKeys = null;
            base.Unload();
        }

        public override void PostSetupContent()
        {
			if (firstLoad)
			{
				
				LanguageManager.Instance.SetLanguage("th-TH");
				Main.QueueMainThreadAction(Main.AssetSourceController.Refresh);
			}
        }

        public static void AddTranslations(Mod mod, List<string> files)
        {
            foreach (string file in files)
            {
                using Stream stream = mod.GetFileStream(file);
                using StreamReader streamReader = new(stream, Encoding.UTF8);
                string translationFileContents = streamReader.ReadToEnd();
                (string culture, string prefix) = GetCultureAndPrefixFromPath(file);
                if (Path.GetExtension(file) == ".json")
                {
                    JObject json = JObject.Parse(translationFileContents);
                    foreach (JToken item3 in json.SelectTokens("$..*"))
                    {
                        if (!item3.HasValues && (!(item3 is JObject jObject) || jObject.Count != 0))
                        {
                            string text3 = "";
                            JToken item = item3;
                            for (JToken parent = item3.Parent; parent != null; parent = parent.Parent)
                            {
                                string text4 = ((parent is JProperty jProperty) ? (jProperty.Name + ((text3 == string.Empty) ? string.Empty : ("." + text3))) : ((!(parent is JArray jArray)) ? text3 : (jArray.IndexOf(item) + ((text3 == string.Empty) ? string.Empty : ("." + text3)))));
                                text3 = text4;
                                item = parent;
                            }

                            text3 = text3.Replace(".$parentVal", "");
                            if (!string.IsNullOrWhiteSpace(prefix))
                            {
                                text3 = prefix + "." + text3;
                            }
                            if (ModdedKeys.ContainsKey(text3))
                            {
                                ModdedKeys[text3] = item3.ToString();
                            }
                            else
                            {
                                ModdedKeys.Add(text3, item3.ToString());
                            }
                        }
                    }
                }
                else if (Path.GetExtension(file) == ".csv")
                {
                    using TextReader reader = new StringReader(translationFileContents);
                    using CsvReader csvReader = new(reader);
                    csvReader.Configuration.HasHeaderRecord = true;
                    if (!csvReader.ReadHeader())
                    {
                        return;
                    }

                    string[] fieldHeaders = csvReader.FieldHeaders;
                    int num = -1;
                    int num2 = -1;
                    for (int i = 0; i < fieldHeaders.Length; i++)
                    {
                        string text = fieldHeaders[i].ToLower();
                        if (text == "translation")
                        {
                            num2 = i;
                        }

                        if (text == "key")
                        {
                            num = i;
                        }
                    }

                    if (num == -1 || num2 == -1)
                    {
                        return;
                    }

                    int num3 = Math.Max(num, num2) + 1;
                    while (csvReader.Read())
                    {
                        string[] currentRecord = csvReader.CurrentRecord;
                        if (currentRecord.Length >= num3)
                        {
                            string text2 = prefix + '.' + currentRecord[num];
                            string value = currentRecord[num2];
                            if (!string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(value))
                            {
                                if (ModdedKeys.ContainsKey(text2))
                                {
                                    ModdedKeys[text2] = value;
                                }
                                else
                                {
                                    ModdedKeys.Add(text2, value);
                                }
                            }
                        }
                    }
                }


            }
        }

        public static (string cultureName, string prefix) GetCultureAndPrefixFromPath(string path)
        {
            path = Path.ChangeExtension(path, null);
            string gameCulture = null;
            string text = "";
            var filePath = Path.GetFileName(path);
            var dirPath = Path.GetDirectoryName(path);
            if (dirPath != null)
            {
                foreach (string s in dirPath.Split('/'))
                {
                    if (s.Contains('-'))
                    {
                        gameCulture = s;
                        text = filePath;
                        break;
                    }
                }
            }
            if (filePath != null) {
                var array = filePath.Split('_');
                if (array[0].Contains('-'))
                {
                    gameCulture = array[0];
                    text = array[1];
                }
            }

            if (gameCulture != null)
            {
                return (gameCulture, text);
            }
            else
            {
                throw new Exception("{path} is not found language form file ");
            }
        }

        public static void UpdateModdedLocalizedTexts()
        {

            if (LanguageManager.Instance.ActiveCulture.Name != "th-TH")
            {
                return;
            }

            MethodInfo LocalizedText_SetValue = typeof(LocalizedText).GetMethod("SetValue", BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (KeyValuePair<string, string> entry in ModdedKeys)
            {
                Console.WriteLine(entry.Key);
                LocalizedText txt = Language.GetText(entry.Key);
                LocalizedText_SetValue.Invoke(txt, [entry.Value]);

            }
        }

    }
}