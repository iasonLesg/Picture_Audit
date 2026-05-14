using MetadataExtractor;
using MetadataExtractor.Formats.Xmp;
using Picture_Audit_Community.ClassItems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Shapes;
using XmpCore; //
using XmpCore.Options;

namespace Picture_Audit_Community
{
    public static class HiddenMetadataGen
    {
        public static MetaReport ExtractAllHiddenClasses(string imagePath)
        {
            MetaReport report =new MetaReport();
         
            if (!File.Exists(imagePath)) return null;

            try
            {
                // Finds every standard and non-standard 'Class' (Directory)
                IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(imagePath);

                foreach (var directory in directories)
                {
                    MetaData Meta = new MetaData();
                    // Regular Tag Extraction
                    foreach (var tag in directory.Tags)
                    {
                        AddTag(Meta, $"{directory.Name}: {tag.Name}", tag.Description);
                    }

                    // Deep Crawl for XMP using the XmpIterator class directly
                    if (directory is XmpDirectory xmpDirectory && xmpDirectory.XmpMeta != null)
                    {
                        CrawlXmpProperties(xmpDirectory.XmpMeta, Meta);
                    }

                    report.Meta_Data.Add(Meta);
                }
                MetaData Meta2 = new MetaData();
                // Raw binary scan for signatures that survived stripping
                //BruteForceBinarySignatures(imagePath, Meta2);
                BruteForceEngine.ExtractUnknownSignatures(imagePath, Meta2);
                if (Meta2.Tags.Count > 0) {
                    report.Meta_Data.Add(Meta2);
                }
               
            }
            catch (Exception ex)
            {
              
            }
            foreach (MetaData meta in report.Meta_Data) {
                meta.AddCaption();


            }
            MetaReport tempreport = new MetaReport();
            foreach (MetaData meta in report.Meta_Data)
            {
                if (meta.Tags.Count > 0) {
                    tempreport.Meta_Data.Add(meta);
                }


            }

            report = tempreport;
            return report;
        }

        private static void AddTag(MetaData meta, string name, string value)
        {
            if (string.IsNullOrEmpty(value)) value = "None";
            meta.Tags.Add(new MetaTag(name, value));
        }

        private static void CrawlXmpProperties(IXmpMeta xmpMeta, MetaData meta)
        {
            try
            {
                // Manual Iterator instantiation to avoid extension method issues
                // This will walk through every node in the XMP tree
                var iterator = new XmpCore.Impl.XmpIterator((XmpCore.Impl.XmpMeta)xmpMeta, null, null, null);

                while (iterator.HasNext())
                {
                    // IXmpPropertyInfo is usually in the root XmpCore namespace or Impl
                    var prop = (XmpCore.IXmpPropertyInfo)iterator.Next();

                    if (!string.IsNullOrEmpty(prop.Path) && !string.IsNullOrEmpty(prop.Value))
                    {
                        AddTag(meta, $"XMP_Deep_Class: {prop.Path}", prop.Value);
                    }
                }
            }
            catch { /* Skip unreadable XMP nodes */ }
        }

     
    }



}