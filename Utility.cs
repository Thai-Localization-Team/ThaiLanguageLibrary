using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Terraria.ModLoader.Core;
using Terraria.ModLoader;
using Hjson;

namespace ThaiLanguageLibrary
{
    public class Utility
    {
        public static string ExtractLocalization(Mod mod)
        {
            PropertyInfo Mod_File = typeof(Mod).GetProperty("File", BindingFlags.NonPublic | BindingFlags.Instance);
            TmodFile tmodFile = (TmodFile)Mod_File.GetValue(mod);
            Regex matchLocaleRegex = new(@$"en-US.*(\.hjson$)");
            List<TmodFile.FileEntry> localeFiles = tmodFile.Where(x => matchLocaleRegex.IsMatch(x.Name)).ToList();
            string dir = ThaiLanguageLibrary.Export;
            foreach (var entry in localeFiles)
            {
                var stream = tmodFile.GetStream(entry.Name);
                if (stream == null)
                {
                    continue;
                }
                using var reader = new StreamReader(stream);

                var fileText = reader.ReadToEnd();
                var jsonObject = HjsonValue.Parse(fileText).Qo();

                var path = Path.Combine(ThaiLanguageLibrary.Export, entry.Name.Replace("hjson", "json"));
                path = path.Replace("Localization", mod.Name);
                dir = Path.GetDirectoryName(path);
                Directory.CreateDirectory(dir);
                var fileStream = File.Create(path);
                jsonObject.Save(fileStream,Stringify.Formatted);
            }
            return dir;
        }
    }
}
