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

namespace Picture_Audit_Community.wpf_forms
{
    /// <summary>
    /// Interaction logic for WarningBox.xaml
    /// </summary>
    public partial class WarningBox : Window
    {
        public WarningBox(string warning,string warningcode)
        {
            InitializeComponent();
            LBL_WarningCode.Text = warningcode;
            LBL_WarningCode.Text = warning;
        }
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
