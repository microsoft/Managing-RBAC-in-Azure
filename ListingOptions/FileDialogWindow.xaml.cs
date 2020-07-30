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

        /// <summary>
        /// This method opens a file dialog and uses the file specified to initialize the yaml instance in the main window.
        /// </summary>
        /// <param name="sender">The "Select File" button</param>
        /// <param name="e">The event that occurs when a button is clicked</param>
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
                try
                {
                    List<KeyVaultProperties> yaml = (new UpdatePoliciesFromYaml(true)).deserializeYaml(fileDialog.FileName);
                    
                    MainWindow main = new MainWindow(yaml);
                    this.Close();
                    main.ShowDialog();
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"{ex.Message}", "Invalid File", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    FileDialogRun_Click(sender, e);
                }
            }
        }

        /// <summary>
        /// This method makes the button a different color when the user hovers over it.
        /// </summary>
        /// <param name="sender">The "Select File" button</param>
        /// <param name="e">The event that occurs when a mouse hovers over the button</param>
        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            Button btn = sender as Button;
            btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(14, 77, 101));
        }

        /// <summary>
        /// This method returns the button to its original color when a user is no longer hovering over the button.
        /// </summary>
        /// <param name="sender">The "Select File" button</param>
        /// <param name="e">The event that occurs when a mouse exits the button area</param>
        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            Button btn = sender as Button;
            btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(25, 117, 151));
        }
    }
}
