using RBAC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Managing_RBAC_in_AzureListOptions
{
    /// <summary>
    /// Interaction logic for ListOptionsforKeyVaultAccessPolicies.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ShorthandScopeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox scopeDropdown = (ComboBox)sender;
            ComboBoxItem selectedScope = (ComboBoxItem)scopeDropdown.SelectedItem;
            string val = (string)selectedScope.Content;

            if (val == "PermissionsToKeys")
            {
                populateShorthandDropdown(Constants.SHORTHANDS_KEYS);
            }
            else if (val == "PermissionsToSecrets")
            {
                populateShorthandDropdown(Constants.SHORTHANDS_SECRETS);
            }
            else if (val == "PermissionsToCertificates")
            {
                populateShorthandDropdown(Constants.SHORTHANDS_CERTIFICATES);
            }
            ShorthandDropdownLabel.Visibility = Visibility.Visible;
            ShorthandDropdown.Visibility = Visibility.Visible;
        }

        private void populateShorthandDropdown(string[] permissions)
        {
            ShorthandDropdown.Items.Clear();
            foreach (string keyword in permissions)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = keyword.Substring(0, 1).ToUpper() + keyword.Substring(1);
                ShorthandDropdown.Items.Add(item);
            }
        }
        private void ShorthandDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShorthandTranslation.IsOpen = false;

            // Remove any translation prior, preventing a rolling list
            if (ShorthandTranslationStackPanel.Children.Count == 2)
            {
                ShorthandTranslationStackPanel.Children.RemoveAt(1);
            }

            ComboBoxItem selectedBlock = (ComboBoxItem)ShorthandScopeDropdown.SelectedItem;
            string block = (string)selectedBlock.Content;

            // Wait for selection in second dropdown if the first has changed
            ComboBox shorthandDropdown = (ComboBox)sender;
            if (shorthandDropdown.IsDropDownOpen) 
            {
                ComboBoxItem selectedShorthand = (ComboBoxItem)shorthandDropdown.SelectedItem;
                string shorthand = (string)selectedShorthand.Content;

                string permissionType = "";
                if (block == "PermissionsToKeys")
                {
                    permissionType = "key";
                }
                else if (block == "PermissionsToSecrets")
                {
                    permissionType = "secret";
                }
                else if (block == "PermissionsToCertificates")
                {
                    permissionType = "certificate";
                }

                UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(false);
                string[] shorthandPermissions = up.getShorthandPermissions(shorthand.ToLower(), permissionType);

                ShorthandTranslationStackPanel.Children.Add(new TextBlock()
                {
                    Text = $"The {permissionType} shorthand '{shorthand}' translates to {string.Join(", ", shorthandPermissions)}.",
                    Margin = new Thickness(15, 15, 15, 15),
                    TextWrapping = TextWrapping.Wrap,
                });

                ShorthandTranslation.IsOpen = true;
            }
        }
       private void ClosePopUp_Click(object sender, RoutedEventArgs e)
       {
            ShorthandTranslation.IsOpen = false;
            ShorthandDropdownLabel.Visibility = Visibility.Hidden;
            ShorthandDropdown.Visibility = Visibility.Hidden;
       }
    }
}
