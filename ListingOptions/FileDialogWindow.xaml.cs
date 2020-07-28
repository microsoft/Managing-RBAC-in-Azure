using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RBAC
{
    /// <summary>
    /// Interaction logic for FileDialogWindow.xaml
    /// </summary>
    public partial class FileDialogWindow : Window
    {
        public FileDialogWindow()
        {
            InitializeComponent();
        }

        private void FileDialogRun_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = false;
            fileDialog.Filter = "YAML Files|*.yml";
            fileDialog.DefaultExt = ".yml";
            fileDialog.Title = "Select YAML File";
            var dialogOK = fileDialog.ShowDialog();

            if (dialogOK == true)
            {
                var yaml = (new UpdatePoliciesFromYaml(true)).deserializeYaml(fileDialog.FileName);
                MainWindow main = new MainWindow(yaml);
                this.Close();
                main.ShowDialog();
            }
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            Button btn = sender as Button;
            btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(14, 77, 101));
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            Button btn = sender as Button;
            btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(25, 117, 151));
        }
    }
}
