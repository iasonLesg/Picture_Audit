using Picture_Audit_Community.ClassItems;
using Picture_Audit_Community.Forensics_Functions;
using Picture_Audit_Community.OtherFUnctions;
using System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using static System.Net.WebRequestMethods;

namespace Picture_Audit_Community.WPF_FORMS
{

    public partial class MainWindow : Window
    {
        private Point _lastMousePosition;
        private bool _isDragging = false;
        private string _currentFilePath;
        private MetaReport _metadata;
        private bool createinduvidualhighlights = false;
        public MainWindow()
        {

            InitializeComponent();
            checkeverything();
            hideprogress();
            //InstantiateImage();
        }

        public MainWindow(string ProjectPath)
        {

            InitializeComponent();
            hideprogress();
            checkeverything();
            passpath(ProjectPath);
            //InstantiateImage();


        }
        private void BackToProjects_Click(object sender, RoutedEventArgs e)
        {
            // Create the StartScreen
            WPF_FORMS.StartScreen startWin = new WPF_FORMS.StartScreen();


            startWin.Show();
            this.Close();
        }

        private void passpath(string ProjectPath) {



            if (checkifpicture(SaveLoad.LoadProject(ProjectPath))) {
                //after checking picture show image, and save the path to _currentFilePath:
                ClearPreviousAudit();
                _currentFilePath = SaveLoad.LoadProject(ProjectPath);
                DroppedImage.Source = new BitmapImage(new Uri(_currentFilePath));
                DropPrompt.Visibility = Visibility.Collapsed;
                InstantiateZoom();
                BTN_Open_Results.Visibility = Visibility.Collapsed;
            }


        }
        private void checkeverything() {
            Btn_ELA.IsChecked = true;
            Btn_MetaScan.IsChecked = true;
            Btn_DoubleCuant.IsChecked = true;
            Btn_GhostImage.IsChecked = true;
            Btn_BLOCK.IsChecked = true;
            Btn_MEDIAN.IsChecked = true;
            Btn_WAVELET.IsChecked = true;
            Btn_Cagi.IsChecked = true;


        }

        private void hideprogress() {
            AnalysisProgressBar.Visibility = Visibility.Collapsed;
            Txt_Status.Visibility = Visibility.Collapsed;

        }
        private void ChangeStatus(int Progress, float ProgressMult, string text)
        {

            // Ensure they are visible
            if (AnalysisProgressBar.Visibility != Visibility.Visible)
            {
                AnalysisProgressBar.Visibility = Visibility.Visible;
                Txt_Status.Visibility = Visibility.Visible;
            }

            // Update Values
            Txt_Status.Text = text;
            AnalysisProgressBar.Value += (Progress * ProgressMult);

            // FORCE WPF to paint the pixels NOW
            // We use Render priority to ensure the Progress Bar actually moves
            AnalysisProgressBar.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            Txt_Status.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
        }

        private bool checkifpicture(string ProjectPath) {

            if (!SaveLoad.IsImageFile(ProjectPath)) {
                // If picture return true if not give an explanation with a window
                InfoDialog info = new InfoDialog();
                info.Owner = Application.Current.MainWindow;
                info.ShowDialog();

                return false;

            }
            return true;
        }





        private void Grid_Drop(object sender, DragEventArgs e)
        {
            checkeverything();
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    if (checkifpicture(files[0]))
                    {
                        //Drag and drop image (files[0]=path)
                        ClearPreviousAudit();
                        _currentFilePath = files[0];
                        DroppedImage.Source = new BitmapImage(new Uri(_currentFilePath));
                        DropPrompt.Visibility = Visibility.Collapsed;
                        BTN_Open_Results.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private void ClearPreviousAudit()
        {
            FootnoteList.ItemsSource = null;
            _currentFilePath = null;
            MetadataList.ItemsSource = null;
            LBL_MetaTitle.Text = "Image Properties Audit";
            DataColumn.Width = new GridLength(0);
        }

        private async void Open_OuputFile(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(SaveLoad.SaveDirectory))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = SaveLoad.SaveDirectory,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }

        }
        private async void Calculate_Click(object sender, RoutedEventArgs e)
        {
            BTN_Open_Results.Visibility = Visibility.Collapsed;
            SaveLoad.SaveInitiate(_currentFilePath);
            _currentFilePath=SaveLoad.CoppyFileInitialFile(_currentFilePath);
            DroppedImage.Source = new BitmapImage(new Uri(_currentFilePath));
            AnalysisProgressBar.Value = 0;

            //Get all settings before analysis (in case of user changes them in the midle of analysis)
            bool Metascan = Btn_MetaScan.IsChecked == true;
            bool Ela = Btn_ELA.IsChecked == true;
            bool DoubleCuant = Btn_DoubleCuant.IsChecked == true;
            bool GhostImage = Btn_GhostImage.IsChecked == true;
            bool BLOCK = Btn_BLOCK.IsChecked == true;
            bool MEDIAN = Btn_MEDIAN.IsChecked == true;
            bool WAVELET = Btn_WAVELET.IsChecked == true;
            bool Cagi = Btn_Cagi.IsChecked == true;
            bool useWinnerTakesAll = Toggle_WinnerTakesAll.IsChecked == true;
            bool heatmapannotation = Toggle_AnalysisGraph.IsChecked == true;

            bool runanalismode = Toggle_OPTIMIZATION.IsChecked == true;
            int runanalismodemult=0;
            if (runanalismode)
            {
                 runanalismodemult = 50;
            }
            else {

                runanalismodemult = 0;
            }





                float Multiplier = calculatebuttons(runanalismode);
                if (Multiplier > 0) {

                List<string> files = new List<string>();
                int sensitivity = 1;
                ChangeStatus(0, Multiplier, "Starting Analysis...");
                if (Metascan)
                {
                    ChangeStatus(0, Multiplier, "Extracting Meta Data...");
                    RunMetaAnalysis();

                    ChangeStatus(10, Multiplier, "Meta Data Extracted Successfully...");

                }
                if (Ela)
                {
                    ChangeStatus(0, Multiplier, "Performing Error Level Analysis...");

                    files.Add(SaveLoad.GetPathFile(SaveLoad.Base_File, "_ELA"));
                    await Task.Run(() => {
                        if (runanalismode)
                        {
                            ErrorLevelAnalysis.GetBestELAAnalysisPath(SaveLoad.Base_File, SaveLoad.GetPathFile(SaveLoad.Base_File, "_ELA"));
                        }
                        else {
                            ErrorLevelAnalysis.GenerateErrorLevelAnalysis(SaveLoad.Base_File, SaveLoad.GetPathFile(SaveLoad.Base_File, "_ELA"), 90);
                        }
                       
                    });
                    ChangeStatus(5+ runanalismodemult, Multiplier, "Saving generated file...");
                    ImageHighlight.GenerateTransparentMask(SaveLoad.Base_File, SaveLoad.GetPathFile(SaveLoad.Base_File, "_ELA"), SaveLoad.GetPathFile(SaveLoad.Base_File, "_Highlighted_ELA"), sensitivity);
                    ChangeStatus(5, Multiplier, "Error Level Analysis Completed...");
                }
                if (DoubleCuant)
                {
                    ChangeStatus(0, Multiplier, "Performing Double Quantization Analysis...");
                    files.Add(SaveLoad.GetPathFile(SaveLoad.Base_File, "_DQ"));
                    await Task.Run(() => {
                        DoubleQuantizationAnalysis.GenerateDQAnalysis(SaveLoad.Base_File, SaveLoad.GetPathFile(SaveLoad.Base_File, "_DQ"), 90);
                    });
                    ChangeStatus(5, Multiplier, "Saving generated file...");
                    ImageHighlight.GenerateTransparentMask(SaveLoad.Base_File, SaveLoad.GetPathFile(SaveLoad.Base_File, "_DQ"), SaveLoad.GetPathFile(SaveLoad.Base_File, "_Highlighted_DQ"), sensitivity);
                    ChangeStatus(5, Multiplier, "Double Quantization Analysis Complete...");
                }
                if (GhostImage)
                {
                    ChangeStatus(0, Multiplier, "Performing Ghost Image Analysis...");
                    files.Add(SaveLoad.GetPathFile(SaveLoad.Base_File, "_Ghost"));
                    await Task.Run(() => {
                        if (runanalismode) { GhostImageAnalysis.GetBestGhostAnalysisPath(SaveLoad.Base_File, SaveLoad.GetPathFile(SaveLoad.Base_File, "_Ghost")); }
                        else { GhostImageAnalysis.GenerateGhostAnalysis(SaveLoad.Base_File, SaveLoad.GetPathFile(SaveLoad.Base_File, "_Ghost"), 90); }
                        
                    });
                    ChangeStatus(5+ runanalismodemult, Multiplier, "Saving generated file...");
                    ImageHighlight.GenerateTransparentMask(SaveLoad.Base_File, SaveLoad.GetPathFile(SaveLoad.Base_File, "_Ghost"), SaveLoad.GetPathFile(SaveLoad.Base_File, "_Highlighted_Ghost"), sensitivity);
                    AnalysisProgressBar.Value += 5 * Multiplier;
                    ChangeStatus(5, Multiplier, "Ghost Image Analysis Complete...");
                }
                if (BLOCK)
                {
                    ChangeStatus(0, Multiplier, "Performing Block Decomposition Analysis...");
                    files.Add(SaveLoad.GetPathFile(SaveLoad.Base_File, "_Block"));
                    await Task.Run(() => {
                        BlockDecompositionAnalysis.GenerateBlockAnalysis(SaveLoad.Base_File, SaveLoad.GetPathFile(SaveLoad.Base_File, "_Block"));
                    });
                    ChangeStatus(5, Multiplier, "Saving generated file...");
                    ImageHighlight.GenerateTransparentMask(SaveLoad.Base_File, SaveLoad.GetPathFile(SaveLoad.Base_File, "_Block"), SaveLoad.GetPathFile(SaveLoad.Base_File, "_Highlighted_Block"), sensitivity);
                    ChangeStatus(5, Multiplier, " Block Decomposition Analysis Complete...");
                }
                if (MEDIAN)
                {
                    ChangeStatus(0, Multiplier, "Performing Median Analysis...");
                    files.Add(SaveLoad.GetPathFile(SaveLoad.Base_File, "_Median"));
                    await Task.Run(() => {
                        if (runanalismode)
                        {
                            MedianAnalysis.GetBestMedianAnalysisPath(SaveLoad.Base_File, SaveLoad.GetPathFile(SaveLoad.Base_File, "_Median"));
                        }
                        else { MedianAnalysis.GenerateMedianAnalysis(SaveLoad.Base_File, SaveLoad.GetPathFile(SaveLoad.Base_File, "_Median"), 5); }
                        
                    });
                    ChangeStatus(5, Multiplier, "Saving generated file...");
                    ImageHighlight.GenerateTransparentMask(SaveLoad.Base_File, SaveLoad.GetPathFile(SaveLoad.Base_File, "_Median"), SaveLoad.GetPathFile(SaveLoad.Base_File, "_Highlighted_Median"), sensitivity);
                    ChangeStatus(5, Multiplier, "Median Analysis Complete...");
                }
                if (WAVELET)
                {
                    ChangeStatus(0, Multiplier, "Performing Wavelet Analysis...");
                    files.Add(SaveLoad.GetPathFile(SaveLoad.Base_File, "_Wavelet_1"));
                    files.Add(SaveLoad.GetPathFile(SaveLoad.Base_File, "_Wavelet_2"));
                    files.Add(SaveLoad.GetPathFile(SaveLoad.Base_File, "_Wavelet_3"));
                    files.Add(SaveLoad.GetPathFile(SaveLoad.Base_File, "_Wavelet_4"));
                    await Task.Run(() => {
                        WaveletAnalysis.GenerateWaveletAnalysis(SaveLoad.Base_File, SaveLoad.GetPathFile(SaveLoad.Base_File, "_Wavelet"));
                    });
                    ChangeStatus(20, Multiplier, "Saving generated file...");
                    ImageHighlight.GenerateTransparentMask(SaveLoad.Base_File, SaveLoad.GetPathFile(SaveLoad.Base_File, "_Wavelet_1"), SaveLoad.GetPathFile(SaveLoad.Base_File, "_Highlighted_Wavelet_1"), sensitivity);
                    ChangeStatus(5, Multiplier, "1 File from 4 Saved ...");
                    ImageHighlight.GenerateTransparentMask(SaveLoad.Base_File, SaveLoad.GetPathFile(SaveLoad.Base_File, "_Wavelet_2"), SaveLoad.GetPathFile(SaveLoad.Base_File, "_Highlighted_Wavelet_2"), sensitivity);
                    ChangeStatus(5, Multiplier, "2 Files from 4 Saved ...");
                    ImageHighlight.GenerateTransparentMask(SaveLoad.Base_File, SaveLoad.GetPathFile(SaveLoad.Base_File, "_Wavelet_3"), SaveLoad.GetPathFile(SaveLoad.Base_File, "_Highlighted_Wavelet_3"), sensitivity);
                    ChangeStatus(5, Multiplier, "3 Files from 4 Saved ...");
                    ImageHighlight.GenerateTransparentMask(SaveLoad.Base_File, SaveLoad.GetPathFile(SaveLoad.Base_File, "_Wavelet_4"), SaveLoad.GetPathFile(SaveLoad.Base_File, "_Highlighted_Wavelet_4"), sensitivity);
                    ChangeStatus(5, Multiplier, "4 Files from 4 Saved ...");

                }
                if (Cagi)
                {
                    ChangeStatus(0, Multiplier, "Performing Cagi Analysis...");
                    files.Add(SaveLoad.GetPathFile(SaveLoad.Base_File, "_Cagi"));
                    await Task.Run(() => {
                        CAGIAnalysis.GenerateCAGIAnalysis(SaveLoad.Base_File, SaveLoad.GetPathFile(SaveLoad.Base_File, "_Cagi"));
                    });
                    ChangeStatus(5, Multiplier, "Saving generated file...");
                    ImageHighlight.GenerateTransparentMask(SaveLoad.Base_File, SaveLoad.GetPathFile(SaveLoad.Base_File, "_Cagi"), SaveLoad.GetPathFile(SaveLoad.Base_File, "_Highlighted_Cagi"), sensitivity);
                    AnalysisProgressBar.Value += 5 * Multiplier;
                    ChangeStatus(5, Multiplier, "Cagi Analysis Complete...");
                }
                if (files.Count > 0)
                {
                    ChangeStatus(10, Multiplier, "Combining Results...");

                    // 1. CAPTURE ON THE UI THREAD
                    // Move these lines ABOVE the Task.Run
              
                    string detectionPath = SaveLoad.GetPathFile(SaveLoad.Base_File, "_DetectionResult");
                    string finalResultPath = SaveLoad.GetPathFile(SaveLoad.Base_File, "_Result");
                    string baseFile = SaveLoad.Base_File; // Capture strings too just in case

                    // 2. RUN THE BACKGROUND TASK
                    await Task.Run(() =>
                    {
                        // Inside here, use the local 'useWinnerTakesAll' variable, 
                        // NOT the 'Toggle_WinnerTakesAll' control.
                        if (useWinnerTakesAll)
                        {
                            ForensicEnsemble.GenerateConsensusMapWTA(files, detectionPath);
                        }
                        else
                        {
                            ForensicEnsemble.GenerateConsensusMap(files, detectionPath);
                        }
                        ForensicOverlay.GenerateFinalHeatmap(baseFile, detectionPath, finalResultPath, heatmapannotation);

                    });

                    // 2. Small delay to allow the OS to release the file handle if necessary
                    // (Usually 50ms is enough to prevent "File in use" errors)
                    await Task.Delay(100);

                    ChangeStatus(10, Multiplier, "Updating UI Display...");

                    try
                    {
                        // 3. Load the image into memory to avoid locking the file on disk
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(finalResultPath, UriKind.Absolute);

                        // OnLoad ensures the entire image is read into RAM now
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;

                        // IgnoreImageCache ensures that if you run analysis twice on the same filename, 
                        // WPF doesn't show you the "old" cached version from the first run.
                        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;

                        bitmap.EndInit();

                        // Freeze the bitmap (makes it cross-thread safe and faster to render)
                        bitmap.Freeze();

                        DroppedImage.Source = bitmap;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Image loading failed: {ex.Message}");
                        ChangeStatus(0, 0, "Error: Could not refresh display.");
                    }

                    ChangeStatus(20, Multiplier, "Analysis Complete...");
                }
                BTN_Open_Results.Visibility = Visibility.Visible;
                hideprogress();

            }
           
        }


        public float calculatebuttons(bool optimized) {
            float buttons = 0;
            if (Btn_MetaScan.IsChecked == true)
            {
                buttons += 10;
            }
            if (Btn_ELA.IsChecked == true)
            {

                buttons += 60;

            }
            if (Btn_DoubleCuant.IsChecked == true)
            {
                buttons += 10;
            }
            if (Btn_GhostImage.IsChecked == true)
            {
                if (optimized)
                {
                    buttons += 60;
                }
                else
                {
                    buttons += 10;

                }
            }
            if (Btn_BLOCK.IsChecked == true)
            {
                buttons += 10;
            }
            if (Btn_MEDIAN.IsChecked == true)
            {
                if (optimized)
                {
                    buttons += 60;
                }
                else
                {
                    buttons += 10;

                }
            }
            if (Btn_WAVELET.IsChecked == true)
            {
                if (optimized)
                {
                    buttons += 60;
                }
                else {
                    buttons += 10;

                }
                

            }
            if (Btn_Cagi.IsChecked == true)
            {
                buttons += 10;
            }


            if (buttons > 0)
            {
                buttons += 10;
                buttons += 10;
                buttons += 10;


            }


            if (buttons > 0) { return 100 / buttons; }
            return 0;

        }

        private void MetaCopyToClipboard_click(object sender, RoutedEventArgs e)
        {

            string Clip = "MetaData:";
            for (int i = 0; i < _metadata.Meta_Data.Count; i++)
            {
                Clip += "\n" + "\n";
                Clip += (i + 1).ToString() + " Category - " + _metadata.Meta_Data[i].Name;
                Clip += "\n";
                foreach (MetaTag Tag in _metadata.Meta_Data[i].Tags) {
                    Clip += "\n";
                    if (Tag.TagName != Tag.TagValue) {
                        Clip += "Name: " + Tag.TagName;
                        Clip += "| Value: " + Tag.TagValue;
                    }
                    else {
                        Clip += "Hidden Tag: " + Tag.TagValue;
                    }


                }


            }
            System.Windows.Clipboard.SetText(Clip);
        }



        private void RunMetaAnalysis()
        {

            if (string.IsNullOrEmpty(_currentFilePath)) return;
            LBL_MetaTitle.Text = System.IO.Path.GetFileName(_currentFilePath);
            DataColumn.Width = new GridLength(400);
            _metadata = HiddenMetadataGen.ExtractAllHiddenClasses(_currentFilePath);
            MetadataList.ItemsSource = _metadata.Meta_Data.Where(m => !string.IsNullOrEmpty(m.Name)).ToList();
            _metadata.CreateReport();
            FootnoteList.ItemsSource = _metadata.StringReport;


        }

   

        #region buttons and UI

        private Point _lastMousePos;
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            // If the window was maximized or restored to normal size
            if (this.WindowState == WindowState.Maximized || this.WindowState == WindowState.Normal)
            {
                // Run the reset logic
                InstantiateZoom();
            }
        }
        private void Toggle_ModeChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded) return;

            bool isWinnerTakesAll = Toggle_WinnerTakesAll.IsChecked == true;

            if (isWinnerTakesAll)
            {
                Toggle_WinnerTakesAll.Content = "Winner Takes All";
                Txt_Status.Text = "Mode: Winner Takes All (Max Intensity)";
            }
            else
            {
                Toggle_WinnerTakesAll.Content = "Standard Average";
                Txt_Status.Text = "Mode: Standard Consensus (Average)";
            }

        }
        private void Toggle_ModeGraph(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded) return;

            bool isWinnerTakesAll = Toggle_AnalysisGraph.IsChecked == true;

            if (isWinnerTakesAll)
            {
                Toggle_AnalysisGraph.Content = "High-Impact Heatmap";
               
            }
            else
            {
                Toggle_AnalysisGraph.Content = "Standard Heatmap";
              
            }

        }
        private void Toggle_AlgorOptim(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded) return;

            bool isWinnerTakesAll = Toggle_OPTIMIZATION.IsChecked == true;

            if (isWinnerTakesAll)
            {
                Toggle_OPTIMIZATION.Content = "Optimized";
                LBL_AlgOptSub.Text = "Slower but more efficient";
            }
            else
            {
                Toggle_OPTIMIZATION.Content = "No Optimization";

                LBL_AlgOptSub.Text = "Faster, but less efficient";

            }

        }

        
        // Reset zoom to 1:1 and fit to screen
        private void InstantiateZoom()
        {
            ImageScale.ScaleX = 1.0;
            ImageScale.ScaleY = 1.0;

            // This tells WPF to re-evaluate the MaxWidth/MaxHeight bindings
            // based on the CURRENT window size.
            DroppedImage.InvalidateMeasure();
            ImageScrollViewer.ScrollToHome();
        }
        private void ImageScrollViewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;

            double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
            double newScale = ImageScale.ScaleX * zoomFactor;

            // Clamp: 1.0 is "Fit to Screen", 20.0 is Deep Zoom
            if (newScale < 1.0) newScale = 1.0;
            if (newScale > 20.0) newScale = 20.0;

            // Capture mouse position for anchor-based zooming
            Point mousePos = e.GetPosition(ImageScrollViewer);
            double relativeX = (mousePos.X + ImageScrollViewer.HorizontalOffset) / ImageScale.ScaleX;
            double relativeY = (mousePos.Y + ImageScrollViewer.VerticalOffset) / ImageScale.ScaleY;

            ImageScale.ScaleX = newScale;
            ImageScale.ScaleY = newScale;

            // Force layout update so scrollbars realize the image is now 'bigger' than the MaxWidth
            ImageScrollViewer.UpdateLayout();

            if (newScale > 1.0)
            {
                double newX = (relativeX * newScale) - mousePos.X;
                double newY = (relativeY * newScale) - mousePos.Y;

                ImageScrollViewer.ScrollToHorizontalOffset(newX);
                ImageScrollViewer.ScrollToVerticalOffset(newY);
            }
            else
            {
                ImageScrollViewer.ScrollToHome();
            }
        }

        private void ImageScrollViewer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ImageScale.ScaleX > 1.0)
            {
                _lastMousePos = e.GetPosition(ImageScrollViewer);
                ImageScrollViewer.CaptureMouse();
                ImageScrollViewer.Cursor = Cursors.Hand;
            }
        }

        private void ImageScrollViewer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ImageScrollViewer.ReleaseMouseCapture();
            ImageScrollViewer.Cursor = Cursors.Arrow;
        }

        private void ImageScrollViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (ImageScrollViewer.IsMouseCaptured)
            {
                Point currentPos = e.GetPosition(ImageScrollViewer);
                Vector delta = _lastMousePos - currentPos;

                ImageScrollViewer.ScrollToHorizontalOffset(ImageScrollViewer.HorizontalOffset + delta.X);
                ImageScrollViewer.ScrollToVerticalOffset(ImageScrollViewer.VerticalOffset + delta.Y);

                _lastMousePos = currentPos;
            }
        }
        #endregion
       
      
    }
}