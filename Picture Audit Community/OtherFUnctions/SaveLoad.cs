using Microsoft.VisualBasic;
using Picture_Audit_Community.wpf_forms;
using Picture_Audit_Community.WPF_FORMS;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using XmpCore.Impl.XPath;
namespace Picture_Audit_Community.OtherFUnctions
{
    public static class SaveLoad
    {
        public static string ImagePath { get; set; } = "";
        private static string FolderName { get; set; } = "PictureAuditCommunity";
        private static string AppDirectory { get; set; } ="";
        public static string SaveDirectory { get; set; } = "";

        public static List<string> ProjectSubtitles { get; set; } = new List<string>();
        public static List<string> ProjectTitles { get; set; } = new List<string>();
        public static int SelectedProject { get; set; } = -1;
        public static string Base_File { get; set; } = "";



        public static void Initialize() {
            ProjectSubtitles.Clear();
            ProjectTitles.Clear();
            //Initialise the save folder path and read if any project exists
            AppDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), FolderName);
            if (!Directory.Exists(AppDirectory))
            {
                Directory.CreateDirectory(AppDirectory);
            }




            DirectoryInfo dirInfo = new DirectoryInfo(AppDirectory);
            var sortedSubdirs = dirInfo.GetDirectories()
                               .OrderByDescending(d => d.LastWriteTime)
                               .ToList();

            foreach (var dir in sortedSubdirs)
            {
                // Add the full path (Subtitle)
                ProjectSubtitles.Add(dir.FullName);

                // Add the folder name (Title)
                ProjectTitles.Add(dir.Name);
            }
        }

        public static void MarkProjectAsRecent(string projectPath)
        {
            try
            {
                if (Directory.Exists(projectPath))
                {
                    // Sets the 'Last Write Time' to right now
                    Directory.SetLastWriteTime(projectPath, DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                // Fail silently or log - usually fails if the folder is locked by another process
                System.Diagnostics.Debug.WriteLine($"Could not update timestamp: {ex.Message}");
            }
        }

        public static void SaveInitiate(string Path) {
            //Create the save Direcotry and save it as current (allways run before saving anything)
            string FilenamePath=System.IO.Path.GetFileNameWithoutExtension(Path);
            SaveDirectory = System.IO.Path.Combine(AppDirectory,FilenamePath);
            if (!Directory.Exists(SaveDirectory))
            {
                Directory.CreateDirectory(SaveDirectory);
            }
        }
        public static string CoppyFileInitialFile(string sourcePath)
        {
            // 1. Determine the extension and set the target filename
            string extension = System.IO.Path.GetExtension(sourcePath).ToLower();
            string fileNameNoExt = System.IO.Path.GetFileNameWithoutExtension(sourcePath);

            // List of formats SixLabors supports natively
            string[] nativeFormats = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff", ".tga", ".webp" };

            bool isNative = Array.Exists(nativeFormats, e => e == extension);

            // If not native, we force the extension to .png for the working copy
            string targetExtension = isNative ? extension : ".png";
            
                string newbasefile= System.IO.Path.Combine(SaveDirectory, fileNameNoExt + targetExtension);
            //check if the file is already openned (bypass instansiate)
            if (Base_File != newbasefile) {
                Base_File = newbasefile;
                if (File.Exists(sourcePath) && sourcePath != Base_File)
                {
                    // Clean up existing file in the AppData project folder
                    if (File.Exists(Base_File))
                    {
                        try
                        {
                            System.IO.File.Delete(Base_File);
                        }
                        catch (System.Exception ex)
                        {
                            WarningBox info = new WarningBox("Cannot Open Image", ex.Message);
                            info.Owner = Application.Current.MainWindow;
                            info.ShowDialog();

                        }

                    }

                    if (isNative)
                    {
                        // Standard Copy for supported formats
                        System.IO.File.Copy(sourcePath, Base_File);
                    }
                    else
                    {
                        // Conversion Bridge for AVIF, HEIC, etc.
                        ConvertToStandardPng(sourcePath, Base_File);
                    }
                }
            
            }

            return Base_File;
        }

        private static void  ConvertToStandardPng(string inputPath, string outputPath)
        {
            // Use WPF to decode formats Windows supports natively (AVIF/HEIC/etc)
            BitmapDecoder decoder = BitmapDecoder.Create(
                new Uri(inputPath, UriKind.Absolute),
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);

            BitmapFrame frame = decoder.Frames[0];
            BitmapMetadata sourceMetadata = frame.Metadata as BitmapMetadata;

            // Use a PngBitmapEncoder to keep the conversion lossless
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(frame));

            using (FileStream fs = new FileStream(outputPath, FileMode.Create))
            {
                encoder.Save(fs);
            }

        }
        public static void CoppyFile(string Path,string PathAdd="")
        {
            string endingfilename = System.IO.Path.GetFileNameWithoutExtension(Path) + PathAdd + System.IO.Path.GetExtension(Path);
            Base_File = System.IO.Path.Combine(SaveDirectory, endingfilename);
          
            if (File.Exists(Path) && Path!= Base_File) {

                if (File.Exists(Base_File)) {
                    System.IO.File.Delete(Base_File);
                }
                System.IO.File.Copy(Path, Base_File); 
            
            
            }

            
            
        }
        public static string GetPathFile(string Path, string PathAdd = "")
        {
            string endingfilename = System.IO.Path.GetFileNameWithoutExtension(Path) + PathAdd + System.IO.Path.GetExtension(Path);
            string NewFileNamePath = System.IO.Path.Combine(SaveDirectory, endingfilename);
            return NewFileNamePath;


        }

        public static string LoadProject(string OpenPath)
        {
            MarkProjectAsRecent(OpenPath);
            string newpath = "";
            SaveDirectory = OpenPath;
            string[] files = Directory.GetFiles(SaveDirectory);
            int minLength = int.MaxValue;
            for (int i = 0; i < files.Length; i++)
            {
                ProjectSubtitles.Add(files[i]);

                string fileName = System.IO.Path.GetFileName(files[i]);

                if (fileName.Length < minLength)
                {
                    minLength = fileName.Length;
                    newpath = files[i];
                }
            }
            return newpath;
        }
        public static void DeleteItem(string OpenPath)
        {
            try
            {
                if (System.IO.Directory.Exists(OpenPath))
                {
                    
                    System.IO.Directory.Delete(OpenPath, true);
                }
            }
            catch (System.IO.IOException ex)
            {
                WarningBox info = new WarningBox("Error deleting project:", ex.Message );
                info.Owner = Application.Current.MainWindow;
                info.ShowDialog();

            }
            catch (System.UnauthorizedAccessException)
            {
                WarningBox info = new WarningBox("Permission denied:", "Ensure the folder is not open in another program.");
                info.Owner = Application.Current.MainWindow;
                info.ShowDialog();
                
            }
        }

        public static bool IsImageFile(string filePath)
        {
            string[] validExtensions = {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff",
        ".ico", ".webp", ".wmp", ".dds", ".heic", ".heif", ".avif",
        ".hdp", ".wdp", ".jpe", ".jfif"
    };

            string ext = System.IO.Path.GetExtension(filePath).ToLower();
            return Array.Exists(validExtensions, e => e == ext);
        }
      
       
    }
}
