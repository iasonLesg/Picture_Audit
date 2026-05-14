using Picture_Audit_Community.OtherFUnctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Picture_Audit_Community.WPF_FORMS
{
    /// <summary>
    /// Interaction logic for StartScreen.xaml
    /// </summary>
    public partial class StartScreen : Window
    {
        public StartScreen()
        {
          
            InitializeComponent();
            SaveLoad.Initialize();
            LoadAnyProjects();
        }

        private void CreateProject_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWin = new MainWindow();

            // 2. Show the new window
            mainWin.Show();

            // 3. Close this StartScreen window
            this.Close();
        }

        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ListBoxItem clickedItem)
            {
                // 2. Retrieve the subtitle (path) from the Tag property
                string projectPath = clickedItem.Tag?.ToString();
              
                if (!string.IsNullOrEmpty(projectPath))
                {
                    // Now you have the specific path! 
                    // You can pass this to your MainWindow or a loader function
                    MainWindow mainWin = new MainWindow(projectPath);

                    // Example: mainWin.LoadExistingProject(projectPath);

                    mainWin.Show();
                    this.Close();
                }
            }
        }

        private void ExitApp_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        public void LoadAnyProjects() {

            LoadRecentProjects(SaveLoad.ProjectTitles, SaveLoad.ProjectSubtitles);


        }

        private void HandleDeleteProject(string path)
        {
            // 1. Show the confirmation window we created earlier
            DeleteConfirmation confirmWin = new DeleteConfirmation();
            confirmWin.Owner = this; // Centers it over this window

            if (confirmWin.ShowDialog() == true)
            {
                // 2. User clicked "Delete"
                SaveLoad.DeleteItem(path);

             
                SaveLoad.Initialize();
                LoadAnyProjects();
            }
        }
        public void LoadRecentProjects(List<string> titles, List<string> paths)
        {
            RecentProjectsList.Items.Clear();

            if (titles != null && titles.Count > 0)
            {
                for (int i = 0; i < titles.Count; i++)
                {
                    // 1. THE CONTAINER
                    // Set Background to null (instead of Transparent) so it doesn't trigger ListBox hover effects easily
                    Grid containerGrid = new Grid
                    {
                        Margin = new Thickness(0, 2, 0, 2),
                        Background = null // Makes the grid 'invisible' to mouse hits
                    };

                    containerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    containerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    // 2. THE MAIN PROJECT BUTTON
                    Button btnProject = new Button
                    {
                        Style = (Style)FindResource("ActionButton"),
                        Tag = paths[i],
                        Width = 350,
                        HorizontalContentAlignment = HorizontalAlignment.Left
                    };

                    Grid stableGrid = new Grid();
                    stableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
                    stableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    TextBlock txtNumber = new TextBlock
                    {
                        Text = (i + 1).ToString("D2"),
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Foreground = (Brush)FindResource("PrimaryAccent"),
                        VerticalAlignment = VerticalAlignment.Center,
                        Opacity = 0.6
                    };

                    // PROJECT BUTTON HOVER LOGIC
                    btnProject.MouseEnter += (s, e) =>
                    {
                        txtNumber.Foreground = Brushes.White;
                        txtNumber.Opacity = 1.0;
                    };
                    btnProject.MouseLeave += (s, e) =>
                    {
                        txtNumber.Foreground = (Brush)FindResource("PrimaryAccent");
                        txtNumber.Opacity = 0.6;
                    };

                    Grid.SetColumn(txtNumber, 0);

                    StackPanel infoPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                    infoPanel.Children.Add(new TextBlock
                    {
                        Text = titles[i],
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    });
                    infoPanel.Children.Add(new TextBlock
                    {
                        Text = paths[i],
                        FontSize = 11,
                        Opacity = 0.6,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    });
                    Grid.SetColumn(infoPanel, 1);

                    stableGrid.Children.Add(txtNumber);
                    stableGrid.Children.Add(infoPanel);
                    btnProject.Content = stableGrid;
                    btnProject.Click += (s, e) => OpenProject_Manual(((Button)s).Tag.ToString());

                    // 3. THE DELETE BUTTON
                    Brush deleteColor = new SolidColorBrush(Color.FromRgb(176, 88, 87));
                    Button btnDelete = new Button
                    {
                        Content = "🗑",
                        FontSize = 16,
                        Width = 40,
                        Background = Brushes.Transparent,
                        BorderBrush = Brushes.Transparent,
                        Foreground = deleteColor,
                        ToolTip = "Delete Project",
                        Tag = paths[i],
                        Margin = new Thickness(5, 0, 0, 0),
                        Cursor = Cursors.Hand
                    };

                    // DELETE BUTTON HOVER LOGIC
                    btnDelete.MouseEnter += (s, e) =>
                    {
                        btnDelete.Foreground = Brushes.White; // Changes trash can to white
                                                              // btnDelete.Background = deleteColor; // Optional: fill background with red
                    };
                    btnDelete.MouseLeave += (s, e) =>
                    {
                        btnDelete.Foreground = deleteColor;
                        btnDelete.Background = Brushes.Transparent;
                    };

                    btnDelete.Click += (s, e) => HandleDeleteProject(((Button)s).Tag.ToString());

                    // 4. ASSEMBLY
                    Grid.SetColumn(btnProject, 0);
                    Grid.SetColumn(btnDelete, 1);
                    containerGrid.Children.Add(btnProject);
                    containerGrid.Children.Add(btnDelete);

                    RecentProjectsList.Items.Add(containerGrid);
                }
            }
        }
        // Helper to handle opening when we have a direct path string
        private void OpenProject_Manual(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                MainWindow mainWin = new MainWindow(path);
                mainWin.Show();
                this.Close();
            }
        }
    }
}
