using MetadataExtractor;
using MetadataExtractor.Formats.Xmp;
using Picture_Audit_Community.ClassItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;

namespace Picture_Audit_Community.ClassItems
{
    public static class BruteForceEngine
    {
       
        public static void ExtractUnknownSignatures(string path, MetaData meta)
        {
            try
            {
                meta.Name = "Hidden Attributes";
                byte[] buffer = File.ReadAllBytes(path); 

               
                // between 5 and 60 characters long.
                string pattern = @"[a-zA-Z0-9\-_,\.;: \(\)\[\]\{\}<>\/\\\|!@#$%^&\*\+=~`]{5,160}";
                // Convert buffer to a string (using Latin1 to preserve byte values)
                string raw = Encoding.GetEncoding("ISO-8859-1").GetString(buffer);

                MatchCollection matches = Regex.Matches(raw, pattern);

                foreach (Match match in matches)
                {
                    string foundString = match.Value;

                    // Filter random characters from things you might need to search for
                    if (foundString.ToLower().Contains("photoshop") || foundString.ToLower().Contains("paint") || foundString.ToLower().Contains("krita") || foundString.ToLower().Contains("gimp") || foundString.ToLower().Contains("ai") || foundString.ToLower().Contains("nano") || foundString.ToLower().Contains("dal") || foundString.ToLower().Contains("adobe") || foundString.ToLower().Contains("photopea") || foundString.ToLower().Contains("generate") || foundString.ToLower().Contains("image") || foundString.ToLower().Contains("process") || foundString.ToLower().Contains("http"))
                    {
                        meta.Tags.Add(new MetaTag(foundString, foundString));
                    }
                }
            }
            catch {  }
        }

    }
}
