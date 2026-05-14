using System.Windows;

namespace Picture_Audit_Community.WPF_FORMS
{
    public partial class InfoDialog : Window
    {
      

        public InfoDialog()
        {
            InitializeComponent();
        }

     

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}