using RBAC;
using System.Linq;
using System.Windows;
using System.Windows.Controls;


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

        /// <summary>
        /// This method populates the "Choose your shorthand:" dropdown and makes it visible upon a selection.
        /// </summary>
        /// <param name="sender">The shorthand permission block dropdown</param>
        /// <param name="e">The event that occurs when a selection changes</param>
        private void ShorthandBlockDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox blockDropdown = (ComboBox)sender;
            if (blockDropdown.SelectedIndex != -1)
            {
                ComboBoxItem selectedScope = (ComboBoxItem)blockDropdown.SelectedItem;
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
        }

        /// <summary>
        /// This method populates the "Choose your shorthand:" dropdown.
        /// </summary>
        /// <param name="permissions">The permissions with which to populate the dropdown</param>
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

        /// <summary>
        /// This method translates the shorthand to its respective permissions and makes the popup with this information visable.
        /// </summary>
        /// <param name="sender">The shorthand dropdown</param>
        /// <param name="e">The event that occurs when a selection changes</param>
        private void ShorthandDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShorthandTranslation.IsOpen = false;

            // Remove any translation prior, preventing a rolling list
            if (ShorthandTranslationStackPanel.Children.Count == 2)
            {
                ShorthandTranslationStackPanel.Children.RemoveAt(1);
            }

            ComboBoxItem selectedBlock = (ComboBoxItem)ShorthandBlockDropdown.SelectedItem;
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
                shorthandPermissions = shorthandPermissions.Select(val => (val.Substring(0, 1).ToUpper() + val.Substring(1))).ToArray();

                ShorthandTranslationStackPanel.Children.Add(new TextBlock()
                {
                    Text = $"{block}: {shorthand}: \n- {string.Join("\n- ", shorthandPermissions)}",
                    Margin = new Thickness(15, 15, 15, 15),
                    TextWrapping = TextWrapping.Wrap,
                });

                ShorthandTranslation.IsOpen = true;
            }
        }

        /// <summary>
        /// This method closes the popup.
        /// </summary>
        /// <param name="sender">The close popup button</param>
        /// <param name="e">The event that occurs when the button is clicked</param>
        private void ClosePopUp_Click(object sender, RoutedEventArgs e)
        {
            ShorthandTranslation.IsOpen = false;

            // Reset block dropdown and hide shorthand dropdown
            ShorthandBlockDropdown.SelectedIndex = -1;
            ShorthandDropdownLabel.Visibility = Visibility.Hidden;
            ShorthandDropdown.Visibility = Visibility.Hidden;
        }
    }
}
