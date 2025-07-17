using CsvHelper;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using Newtonsoft.Json.Linq;
using ReLogic.Content.Sources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using ThaiLanguageLibrary.Common.Config;

namespace ThaiLanguageLibrary
{
	public partial class ThaiLanguageLibrary : Mod
	{

        private static  readonly List<float> buttonState = [0.8f];
		private void LoadHooks()
		{

			if (!MoreLocales)
            {
                IL_Main.DrawMenu += HookLanguageSelection;
            }
            On_LanguageManager.LoadActiveCultureTranslationsFromSources += HookLoadActiveCultureTranslationsFromSources;
			On_LanguageManager.LoadFilesForCulture += HookLoadFilesForCulture;
        }


		private void UnloadHooks()
		{
            if (!MoreLocales) { 
                IL_Main.DrawMenu -= HookLanguageSelection; 
            }
			On_LanguageManager.LoadActiveCultureTranslationsFromSources -= HookLoadActiveCultureTranslationsFromSources;
            On_LanguageManager.LoadFilesForCulture -= HookLoadFilesForCulture;
        }



        private void HookLoadFilesForCulture(On_LanguageManager.orig_LoadFilesForCulture orig, LanguageManager self, GameCulture culture)
		{
			orig.Invoke(self, culture);
            UpdateModdedLocalizedTexts();
        }

		private void HookLoadActiveCultureTranslationsFromSources(On_LanguageManager.orig_LoadActiveCultureTranslationsFromSources orig, LanguageManager self)
		{
            if (self.ActiveCulture.Name == "th-TH")
			{
				Config config = ModContent.GetInstance<Config>();
				if (config.ExternalAsset)
				{
					LoadFromExternalAsset(self);
                    LoadFromExternalAssetMods();
                }
				else
				{
                    if (!MoreLocales)
                    {
                        LoadFromModAsset(self);
                    }
                    AddTranslations(this, [ "Asset/Mod/th-TH_Mods.ThaiLanguageLibrary.json" ]);
                }

                UpdateModdedLocalizedTexts();
            }
            else
			{
				LoadFromPack(self);
			}
        }

		private static void LoadFromPack(LanguageManager self)
		{
            var contentSources = self.GetType().GetField("_contentSources", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(self) as IContentSource[];
            string assetNameStart = string.Concat(str2: self.ActiveCulture.Name, str0: "Localization", str1: Path.DirectorySeparatorChar.ToString()).ToLower();
            IContentSource[] array = contentSources;
            foreach (IContentSource item in array)
            {
                foreach (string item2 in GetAllAssetsStartingWith(item, assetNameStart))
                {
                    string extension = Path.GetExtension(item2);

                    if (extension != ".json" && extension != ".csv")
                    {
                        continue;
                    }

                    using Stream stream = item.OpenStream(item2);
                    using StreamReader streamReader = new(stream);
                    string fileText = streamReader.ReadToEnd();
                    if (extension == ".json")
                    {
                        self.LoadLanguageFromFileTextJson(fileText, canCreateCategories: false);
					}
                    if (extension == ".csv")
                    {
                        self.LoadLanguageFromFileTextCsv(fileText);
                    }
                }
            }
        }

		private static void LoadFromExternalAsset(LanguageManager self)
		{
            foreach (FileInfo file in AssetDirectory.GetFiles())
            {
                if (file.Name.StartsWith("th-TH."))
                {
                    using StreamReader streamReader = file.OpenText();
                    string fileText = streamReader.ReadToEnd();
					if (file.Extension == ".json")
					{
                        self.LoadLanguageFromFileTextJson(fileText, canCreateCategories: false);
					}
					else if (file.Extension == ".csv")
                    {
                        self.LoadLanguageFromFileTextCsv(fileText);
                    }
				}
            }
        }

        private static void LoadFromExternalAssetMods()
        {
            foreach (FileInfo file in AssetModsDirectory.GetFiles())
            {
                using StreamReader streamReader = file.OpenText(); ;
                string translationFileContents = streamReader.ReadToEnd();
                (string culture, string prefix) = GetCultureAndPrefixFromPath(file.FullName);
                if (file.Extension == ".json")
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
                else if (file.Extension == ".csv")
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
                                if (!ModdedKeys.TryAdd(text2, value))
                                {
                                    ModdedKeys[text2] = value;
                                }
							}
						}
                    }
                }
            }
        }

        private void LoadFromModAsset(LanguageManager self)
        {
            foreach (String file in GetFileNames())
            {
                string extension = Path.GetExtension(file);
                if (extension != ".json" && extension != ".csv")
                {
                    continue;
                }
                if (file.StartsWith("Asset/Localization/th-TH."))
                {
                    using Stream stream = GetFileStream(file);
                    using StreamReader streamReader = new(stream);
                    string fileText = streamReader.ReadToEnd();
                    if (extension == ".json")
                    {
                        self.LoadLanguageFromFileTextJson(fileText, canCreateCategories: false);
                    }
                    if (extension == ".csv")
                    {
                        self.LoadLanguageFromFileTextCsv(fileText);
                    }
                }
            }
        }



        private void HookLanguageSelection(ILContext il)
		{
			try
			{
                
                // Main.menuMode == 1212
                var iLCursor = new ILCursor(il);
                iLCursor.GotoNext(i => i.MatchLdstr("Language.Polish"));
				iLCursor.GotoNext(i => i.MatchStelemRef());
                iLCursor.Index++;

				// 497: Add Language to the list of languages
				iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc_S, (byte)26);
				iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_S, (sbyte)CultureId);
                iLCursor.EmitDelegate<Func<string>>(() =>
                {
                    return (LanguageManager.Instance.ActiveCulture.Name == "th-TH") ? "ภาษาไทย" : "ภาษาไทย (Thai)";
                });
				// iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Call, typeof(Language).GetMethod("GetTextValue", new Type[] { typeof(string) }));
				iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Stelem_Ref);

				// 498: Replace numButtons = 10 => numButtons = 10 + SupportedLanguages.Count
				iLCursor.Remove();
				iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_S, (sbyte)(11));

				// Main.menuMode == 1213
				iLCursor.GotoNext(i => i.MatchLdcI4(1213));
				iLCursor.GotoNext(i => i.MatchLdstr("Language.Polish"));
				iLCursor.Index += 3;

				// 525: Add Language.ChineseTraditional to the list of languages
				iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc_S, (byte)26);
				iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_S, (sbyte)10);
                iLCursor.EmitDelegate<Func<string>>(() =>
                {
                    return (LanguageManager.Instance.ActiveCulture.Name == "th-TH") ? "ภาษาไทย" : "ภาษาไทย (Thai)";
                });
                // iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Call, typeof(Language).GetMethod("GetTextValue", new Type[] { typeof(string) }));
                iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Stelem_Ref);

				// 526: Replace array9[10] = Lang.menu[5].Value with array9[10 + SupportedLanguages.Count]
				iLCursor.GotoNext(i => i.MatchLdcI4(10));
				iLCursor.Remove();
				iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_S, (sbyte)(11));

				// 527: Replace numButtons = 11 => numButtons = 11 + SupportedLanguages.Count
				iLCursor.GotoNext(i => i.MatchLdcI4(11));
				iLCursor.Remove();
				iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_S, (sbyte)(12));

				// 528: if (this.selectedMenu == 10 || backButtonDown) => if (this.selectedMenu == (10 + SupportedLanguages.Count) || backButtonDown)
				iLCursor.GotoNext(i => i.MatchLdfld("Terraria.Main", "selectedMenu"));
				iLCursor.Index += 1;
				iLCursor.Remove();
				iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_S, (sbyte)(11));

				// 544: array4[10] = 10 => array4[10 + SupportedLanguages.Count] = 10
				iLCursor.GotoNext(i => i.MatchLdloc(19) && i.Next.MatchLdcI4(10));
				iLCursor.Index += 1;
				iLCursor.Remove();
				iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_S, (sbyte)(11));


				// 550: array7[10] = 0.95f => array7[10 + SupportedLanguages.Count] = 0.95f
				iLCursor.GotoNext(i => i.MatchLdcR4(0.95f));
				iLCursor.Index -= 1;
				iLCursor.Remove();
				iLCursor.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_S, (sbyte)(11));
			}
			catch (Exception e)
			{
				MonoModHooks.DumpIL(ModContent.GetInstance<ThaiLanguageLibrary>(), il);
				throw new ILPatchFailureException(ModContent.GetInstance<ThaiLanguageLibrary>(), il, e);
			}
		}

		private static IEnumerable<string> GetAllAssetsStartingWith(IContentSource self, string assetNameStart)
		{
			return self.EnumerateAssets().Where(s => s.StartsWith(assetNameStart));
		}

    }
}