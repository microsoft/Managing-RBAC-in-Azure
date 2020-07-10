using Microsoft.Azure.Management.KeyVault.Models;
using System.Collections.Generic;
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

        // 1. List the Permissions by Shorthand ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// This method populates the "Choose your shorthand:" dropdown and makes it visible upon a selection.
        /// </summary>
        /// <param name="sender">The shorthand permission block dropdown</param>
        /// <param name="e">The event that occurs when a selection changes</param>
        /// 
        private void ShorthandPermissionTypesDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
                ShorthandPermissionsLabel.Visibility = Visibility.Visible;
                ShorthandPermissionsDropdown.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// This method populates the "Choose your shorthand:" dropdown.
        /// </summary>
        /// <param name="permissions">The permissions with which to populate the dropdown</param>
        private void populateShorthandDropdown(string[] permissions)
        {
            ShorthandPermissionsDropdown.Items.Clear();
            foreach (string keyword in permissions)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = keyword.Substring(0, 1).ToUpper() + keyword.Substring(1);
                ShorthandPermissionsDropdown.Items.Add(item);
            }
        }

        /// <summary>
        /// This method translates the shorthand to its respective permissions and makes the popup with this information visable.
        /// </summary>
        /// <param name="sender">The shorthand dropdown</param>
        /// <param name="e">The event that occurs when a selection changes</param>
        /// 
        private void ShorthandPermissionsDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShorthandPermissionsTranslation.IsOpen = false;

            // Remove any translation prior, preventing a rolling list
            if (ShorthandTranslationStackPanel.Children.Count == 3)
            {
                ShorthandTranslationStackPanel.Children.RemoveRange(1, 2);
            }

            ComboBoxItem selectedBlock = (ComboBoxItem)ShorthandPermissionTypesDropdown.SelectedItem;
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
                    FontWeight = FontWeights.SemiBold,
                    Text = $"{block}: {shorthand}:",
                    Margin = new Thickness(15, 0, 15, 0)
                });
                ShorthandTranslationStackPanel.Children.Add(new TextBlock()
                {
                    Text = $"- {string.Join("\n- ", shorthandPermissions)}",
                    Margin = new Thickness(15, 0, 15, 15)
                });

                ShorthandPermissionsTranslation.IsOpen = true;
            }
        }

        /// <summary>
        /// This method closes the popup for shorthand permissions.
        /// </summary>
        /// <param name="sender">The close popup button</param>
        /// <param name="e">The event that occurs when the button is clicked</param>
        private void closeShorthandPermissionsTranslation(object sender, RoutedEventArgs e)
        {
            ShorthandPermissionsTranslation.IsOpen = false;

            // Reset block dropdown and hide shorthand dropdown
            ShorthandPermissionTypesDropdown.SelectedIndex = -1;
            ShorthandPermissionsLabel.Visibility = Visibility.Hidden;
            ShorthandPermissionsDropdown.Visibility = Visibility.Hidden;
        }

        // 2. List by Security Principal ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// This method populates the "Specify the scope:" dropdown and makes it visible upon a selection.
        /// </summary>
        /// <param name="sender">The security principal scope block dropdown</param>
        /// <param name="e">The event that occurs when a selection changes</param>
        private void SecurityPrincipalScopeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SecurityPrincipalSpecifyScopeLabel.Visibility == Visibility.Visible ||
                SecurityPrincipalSpecifyScopeDropdown.Visibility == Visibility.Visible)
            {
                SecurityPrincipalSpecifyScopeDropdown.Items.Clear();
            }
            else
            {
                SecurityPrincipalSpecifyScopeLabel.Visibility = Visibility.Visible;
                SecurityPrincipalSpecifyScopeDropdown.Visibility = Visibility.Visible;
            }
            ComboBoxItem item1 = new ComboBoxItem();
            item1.Content = "Test1";
            SecurityPrincipalSpecifyScopeDropdown.Items.Add(item1);

            ComboBoxItem item2 = new ComboBoxItem();
            item2.Content = "Test2";
            SecurityPrincipalSpecifyScopeDropdown.Items.Add(item2);

        }

        /// <summary>
        /// This method populates the "Choose the type:" dropdown and makes it visible upon a selection.
        /// </summary>
        /// <param name="sender">The security principal specify scope block dropdown</param>
        /// <param name="e">The event that occurs when a selection changes</param>
        private void SecurityPrincipalSpecifyScopeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SecurityPrincipalTypeLabel.Visibility = Visibility.Visible;
            SecurityPrincipalTypeDropdown.Visibility = Visibility.Visible;
        }

        // 3. List by Permissions ------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// This method populates the "Specify the scope:" dropdown and makes it visible upon a selection.
        /// </summary>
        /// <param name="sender">The permissions scope block dropdown</param>
        /// <param name="e">The event that occurs when a selection changes</param>
        private void PermissionsScopeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PermissionsSpecifyScopeLabel.Visibility == Visibility.Visible ||
                PermissionsSpecifyScopeDropdown.Visibility == Visibility.Visible)
            {
                PermissionsSpecifyScopeDropdown.Items.Clear();
            }
            else
            {
                PermissionsSpecifyScopeLabel.Visibility = Visibility.Visible;
                PermissionsSpecifyScopeDropdown.Visibility = Visibility.Visible;
            }
            ComboBoxItem item1 = new ComboBoxItem();
            item1.Content = "Test1";
            PermissionsSpecifyScopeDropdown.Items.Add(item1);

            ComboBoxItem item2 = new ComboBoxItem();
            item2.Content = "Test2";
            PermissionsSpecifyScopeDropdown.Items.Add(item2);
        }

        /// <summary>
        /// This method populates the "Key Permissions:" dropdown and makes it visible upon a selection.
        /// </summary>
        /// <param name="sender">The specify scope block dropdown</param>
        /// <param name="e">The event that occurs when a selection changes</param>
        private void PermissionsSpecifyScopeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            KeyPermissionsDropdown.Items.Clear();
            string[] allKeyPermissions = Constants.ALL_KEY_PERMISSIONS;

            ComboBoxItem item = new ComboBoxItem();
            item.Content = "All";
            KeyPermissionsDropdown.Items.Add(item);
            item = new ComboBoxItem();
            item.Content = "None";
            KeyPermissionsDropdown.Items.Add(item);

            foreach (string permission in allKeyPermissions)
            {
                item = new ComboBoxItem();
                item.Content = permission;
                KeyPermissionsDropdown.Items.Add(item);
            }
            KeyPermissionsLabel.Visibility = Visibility.Visible;
            KeyPermissionsDropdown.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// This method populates the "Secret Permissions:" dropdown and makes it visible upon a selection.
        /// </summary>
        /// <param name="sender">The key permissions block dropdown</param>
        /// <param name="e">The event that occurs when a selection changes</param>
        private void KeyPermissionsDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SecretPermissionsDropdown.Items.Clear();
            string[] allSecretPermissions = Constants.ALL_SECRET_PERMISSIONS;

            ComboBoxItem item = new ComboBoxItem();
            item.Content = "All";
            SecretPermissionsDropdown.Items.Add(item);
            item = new ComboBoxItem();
            item.Content = "None";
            SecretPermissionsDropdown.Items.Add(item);

            foreach (string permission in allSecretPermissions)
            {
                item = new ComboBoxItem();
                item.Content = permission;
                SecretPermissionsDropdown.Items.Add(item);
            }
            SecretPermissionsLabel.Visibility = Visibility.Visible;
            SecretPermissionsDropdown.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// This method populates the "Certificate Permissions:" dropdown and makes it visible upon a selection.
        /// </summary>
        /// <param name="sender">The secret permissions block dropdown</param>
        /// <param name="e">The event that occurs when a selection changes</param>
        private void SecretPermissionsDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CertificatePermissionsDropdown.Items.Clear();
            string[] allCertificatePermissions = Constants.ALL_CERTIFICATE_PERMISSIONS;

            ComboBoxItem item = new ComboBoxItem();
            item.Content = "All";
            CertificatePermissionsDropdown.Items.Add(item);
            item = new ComboBoxItem();
            item.Content = "None";
            CertificatePermissionsDropdown.Items.Add(item);

            foreach (string permission in allCertificatePermissions)
            {
                item = new ComboBoxItem();
                item.Content = permission;
                CertificatePermissionsDropdown.Items.Add(item);
            }
            CertificatePermissionsLabel.Visibility = Visibility.Visible;
            CertificatePermissionsDropdown.Visibility = Visibility.Visible;
        }

        // 4. Breakdown of Permission Usage by Percentage -------------------------------------------------------------------------------------------

        /// <summary>
        /// This method populates the "Specify the scope:" dropdown and makes it visible upon a selection.
        /// </summary>
        /// <param name="sender">The breakdown scope block dropdown</param>
        /// <param name="e">The event that occurs when a selection changes</param>
        private void BreakdownScopeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BreakdownSpecifyScopeLabel.Visibility == Visibility.Visible ||
                BreakdownSpecifyScopeDropdown.Visibility == Visibility.Visible)
            {
                BreakdownSpecifyScopeDropdown.Items.Clear();
            }
            else
            {
                BreakdownSpecifyScopeLabel.Visibility = Visibility.Visible;
                BreakdownSpecifyScopeDropdown.Visibility = Visibility.Visible;
            }
            ComboBoxItem item1 = new ComboBoxItem();
            item1.Content = "Test1";
            BreakdownSpecifyScopeDropdown.Items.Add(item1);

            ComboBoxItem item2 = new ComboBoxItem();
            item2.Content = "Test2";
            BreakdownSpecifyScopeDropdown.Items.Add(item2);
        }

        // 5. Most Accessed Keyvaults ---------------------------------------------------------------------------------------------

        /// <summary>
        /// This method populates the "Specify the scope:" dropdown and makes it visible upon a selection.
        /// </summary>
        /// <param name="sender">The Most Accessed scope block dropdown</param>
        /// <param name="e">The event that occurs when a selection changes</param>
        private void MostAccessedScopeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MostAccessedSpecifyScopeLabel.Visibility == Visibility.Visible ||
               MostAccessedSpecifyScopeDropdown.Visibility == Visibility.Visible)
            {
                MostAccessedSpecifyScopeDropdown.Items.Clear();
            }
            else
            {
                MostAccessedSpecifyScopeLabel.Visibility = Visibility.Visible;
                MostAccessedSpecifyScopeDropdown.Visibility = Visibility.Visible;
            }
            ComboBoxItem item1 = new ComboBoxItem();
            item1.Content = "Test1";
            MostAccessedSpecifyScopeDropdown.Items.Add(item1);

            ComboBoxItem item2 = new ComboBoxItem();
            item2.Content = "Test2";
            MostAccessedSpecifyScopeDropdown.Items.Add(item2);
        }

        // 6. Ranked Security Principal by Access ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// This method populates the "Specify the scope:" dropdown and makes it visible upon a selection.
        /// </summary>
        /// <param name="sender">The Security principal access scope block dropdown</param>
        /// <param name="e">The event that occurs when a selection changes</param>
        private void SecurityPrincipalAccessScopeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SecurityPrincipalAccessSpecifyScopeLabel.Visibility == Visibility.Visible ||
              SecurityPrincipalAccessSpecifyScopeDropdown.Visibility == Visibility.Visible)
            {
                SecurityPrincipalAccessScopeDropdown.Items.Clear();
            }
            else
            {
                SecurityPrincipalAccessSpecifyScopeLabel.Visibility = Visibility.Visible;
                SecurityPrincipalAccessSpecifyScopeDropdown.Visibility = Visibility.Visible;
            }
            ComboBoxItem item1 = new ComboBoxItem();
            item1.Content = "Test1";
            SecurityPrincipalAccessSpecifyScopeDropdown.Items.Add(item1);

            ComboBoxItem item2 = new ComboBoxItem();
            item2.Content = "Test2";
            SecurityPrincipalAccessSpecifyScopeDropdown.Items.Add(item2);
        }


        // "Run" Buttons that Execute Code & Output ----------------------------------------------------------------------------------

        /// <summary>
        /// This method executes when a mouse enters/hovers the button. It changes color to light blue and changes button text to "Click"
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Mouse event</param>
        private void ShorthandPermissionsRun_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ShorthandPermissionsRun.Content = "Click";
            ShorthandPermissionsRun.Background = Constants.MOUSE_ENTER_BUTTON_COLOR;
        }

        private void ShorthandPermissionsRun_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ShorthandPermissionsRun.Content = "Run";
            ShorthandPermissionsRun.Background = Constants.MOUSE_LEAVE_BUTTON_COLOR;
        }

        /// <summary>
        /// This method executes when a mouse enters/hovers the button. It changes color to light blue and changes button text to "Click"
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Mouse event</param>
        private void SecurityPrincipalRun_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SecurityPrincipalRun.Content = "Click";
            SecurityPrincipalRun.Background = Constants.MOUSE_ENTER_BUTTON_COLOR;
        }

        private void SecurityPrincipalRun_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SecurityPrincipalRun.Content = "Run";
            SecurityPrincipalRun.Background = Constants.MOUSE_LEAVE_BUTTON_COLOR;
        }

        /// <summary>
        /// This method executes when a mouse enters/hovers the button. It changes color to light blue and changes button text to "Click"
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Mouse event</param>
        private void PermissionsRun_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            PermissionsRun.Content = "Click";
            PermissionsRun.Background = Constants.MOUSE_ENTER_BUTTON_COLOR;
        }

        /// <summary>
        /// This method executes when a mouse exits/leaves the button. It changes color back to a darker blue and changes button text to "Run"
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Mouse event</param>
        private void PermissionsRun_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            PermissionsRun.Content = "Run";
            PermissionsRun.Background = Constants.MOUSE_LEAVE_BUTTON_COLOR;
        }

        /// <summary>
        /// This method executes when a mouse enters/hovers the button. It changes color to light blue and changes button text to "Click"
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Mouse event</param>
        private void BreakdownRun_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            BreakdownRun.Content = "Click";
            BreakdownRun.Background = Constants.MOUSE_ENTER_BUTTON_COLOR;
        }

        /// <summary>
        /// This method executes when a mouse exits/leaves the button. It changes color back to a darker blue and changes button text to "Run"
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Mouse event</param>
        private void BreakdownRun_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
           BreakdownRun.Content = "Run";
            BreakdownRun.Background = Constants.MOUSE_LEAVE_BUTTON_COLOR;
        }

        /// <summary>
        /// This method executes when a mouse enters/hovers the button. It changes color to light blue and changes button text to "Click"
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Mouse event</param>
        private void MostAccessedRun_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            MostAccessedRun.Content = "Click";
            MostAccessedRun.Background = Constants.MOUSE_ENTER_BUTTON_COLOR;
        }

        /// <summary>
        /// This method executes when a mouse exits/leaves the button. It changes color back to a darker blue and changes button text to "Run"
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Mouse event</param>
        private void MostAccessedRun_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            MostAccessedRun.Content = "Run";
            MostAccessedRun.Background = Constants.MOUSE_LEAVE_BUTTON_COLOR;
        }

        /// <summary>
        /// This method executes when a mouse enters/hovers the button. It changes color to light blue and changes button text to "Click"
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Mouse event</param>
        private void SecurityPrincipalAccessRun_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
           SecurityPrincipalAccessRun.Content = "Click";
           SecurityPrincipalAccessRun.Background = Constants.MOUSE_ENTER_BUTTON_COLOR;
        }

        /// <summary>
        /// This method executes when a mouse exits/leaves the button. It changes color back to a darker blue and changes button text to "Run"
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Mouse event</param>
        private void SecurityPrincipalAccessRun_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SecurityPrincipalAccessRun.Content = "Run";
            SecurityPrincipalAccessRun.Background = Constants.MOUSE_LEAVE_BUTTON_COLOR;
        }
    }
}
