using System.Windows;

namespace Picture_Audit_Community.WPF_FORMS
{
    public partial class DeleteConfirmation : Window
    {
        public DeleteConfirmation()
        {
            InitializeComponent();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}