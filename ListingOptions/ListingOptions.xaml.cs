using RBAC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Constants = RBAC.Constants;
using LiveCharts;
using LiveCharts.Wpf;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Data;
using Microsoft.Azure.Management.CosmosDB.Fluent.Models;
using System.Runtime.ConstrainedExecution;
using Microsoft.Azure.Management.Sql.Fluent.Models;
using System.Text;

namespace RBAC
{
    /// <summary>
    /// Interaction logic for ListingOptions.xaml
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
            ComboBox blockDropdown = sender as ComboBox;
            if (blockDropdown.SelectedIndex != -1)
            {
                ComboBoxItem selectedScope = blockDropdown.SelectedItem as ComboBoxItem;
                string val = selectedScope.Content as string;

                if (val == "Key Permissions")
                {
                    populateShorthandDropdown(Constants.SHORTHANDS_KEYS);
                }
                else if (val == "Secret Permissions")
                {
                    populateShorthandDropdown(Constants.SHORTHANDS_SECRETS);
                }
                else if (val == "Certificate Permissions")
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

            ComboBoxItem selectedBlock = ShorthandPermissionTypesDropdown.SelectedItem as ComboBoxItem;
            string block = selectedBlock.Content as string;

            // Wait for selection in second dropdown if the first has changed
            ComboBox shorthandDropdown = sender as ComboBox;
            if (shorthandDropdown.IsDropDownOpen)
            {
                ComboBoxItem selectedShorthand = shorthandDropdown.SelectedItem as ComboBoxItem;
                string shorthand = selectedShorthand.Content as string;

                string permissionType = "";
                if (block == "Key Permissions")
                {
                    permissionType = "key";
                }
                else if (block == "Secret Permissions")
                {
                    permissionType = "secret";
                }
                else if (block == "Certificate Permissions")
                {
                    permissionType = "certificate";
                }

                UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(false);
                string[] shorthandPermissions = up.getShorthandPermissions(shorthand.ToLower(), permissionType);
                shorthandPermissions = shorthandPermissions.Select(val => (val.Substring(0, 1).ToUpper() + val.Substring(1))).ToArray();

                ShorthandTranslationStackPanel.Children.Add(new TextBlock()
                {
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 15,
                    Text = $"{block}: {shorthand}:",
                    Margin = new Thickness(20, 0, 15, 2)
                });
                ShorthandTranslationStackPanel.Children.Add(new TextBlock()
                {
                    Text = $"- {string.Join("\n- ", shorthandPermissions)}",
                    Margin = new Thickness(25, 0, 15, 22),
                    FontSize = 14
                });
            }
        }

        /// <summary>
        /// This method closes the popup for shorthand permissions.
        /// </summary>
        /// <param name="sender">The close popup button</param>
        /// <param name="e">The event that occurs when the button is clicked</param>

        private void CloseShorthandPermissionsTranslation_Click(object sender, RoutedEventArgs e)
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
        /// This method gets the selected scope and populates the "Specify the scope:" dropdown, if applicable.
        /// </summary>
        /// <param name="sender">The "Select your scope:" dropdown</param>
        /// <param name="e">The event that occurs when a selection changes</param>
        private void PBPScopeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PBPSpecifyScopeDropdown.SelectedIndex = -1;
            PBPKeysLabel.Visibility = Visibility.Hidden;
            PBPKeysDropdown.Visibility = Visibility.Hidden;
            PBPSecretsLabel.Visibility = Visibility.Hidden;
            PBPSecretsDropdown.Visibility = Visibility.Hidden;
            PBPCertificatesLabel.Visibility = Visibility.Hidden;
            PBPCertificatesDropdown.Visibility = Visibility.Hidden;

            ComboBox dropdown = sender as ComboBox;
            if (dropdown.SelectedIndex != -1)
            {
                ComboBoxItem selectedScope = dropdown.SelectedItem as ComboBoxItem;
                string scope = selectedScope.Content as string;

                try
                {
                    UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(false);
                    List<KeyVaultProperties> yaml = up.deserializeYaml(Constants.YAML_FILE_PATH);

                    if (yaml.Count() == 0)
                    {
                        throw new Exception("The YAML file path must be specified in the Constants.cs file. Please ensure this path is correct before proceeding.");
                    }

                    ComboBox specifyDropdown = PBPSpecifyScopeDropdown as ComboBox;
                    populateSelectedScopeTemplate(specifyDropdown, scope, yaml);
                    PBPSpecifyScopeLabel.Visibility = Visibility.Visible;
                    PBPSpecifyScopeDropdown.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{ex.Message}", "FileNotFound Exception", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    dropdown.SelectedIndex = -1;
                }
            }
        }

        /// <summary>
        /// This method populates the "Specify your scope:" dropdown based off of the selected scope item.
        /// </summary>
        /// <param name="specifyScope">The "Specify your scope:" dropdown</param>
        /// <param name="scope">The selected item from the "Select your scope:" dropdown</param>
        /// <param name="yaml">The deserialized list of KeyVaultProperties objects</param>
        private void populateSelectedScopeTemplate(ComboBox specifyScope, string scope, List<KeyVaultProperties> yaml)
        {
            specifyScope.Items.Clear();

            List<string> items = new List<string>();
            if (scope == "YAML")
            {
                specifyScope.Items.Add(new ComboBoxItem()
                {
                    Content = "YAML",
                    IsSelected = true
                });
            }
            else
            {
                if (scope == "Subscription")
                {
                    foreach (KeyVaultProperties kv in yaml)
                    {
                        if (kv.SubscriptionId.Length == 36 && kv.SubscriptionId.ElementAt(8).Equals('-')
                            && kv.SubscriptionId.ElementAt(13).Equals('-') && kv.SubscriptionId.ElementAt(18).Equals('-'))
                        {
                            items.Add(kv.SubscriptionId);
                        }
                    }
                }
                else if (scope == "ResourceGroup")
                {
                    foreach (KeyVaultProperties kv in yaml)
                    {
                        items.Add(kv.ResourceGroupName);
                    }
                }
                else
                {
                    foreach (KeyVaultProperties kv in yaml)
                    {
                        items.Add(kv.VaultName);
                    }
                }

                // Only add distinct items
                foreach (string item in items.Distinct())
                {
                    specifyScope.Items.Add(new CheckBox()
                    {
                        Content = item
                    });
                }
            }
        }

        /// <summary>
        /// This method allows you to select multiple items and displays how many items were selected 
        /// on the dropdown relating to the specified scope.
        /// </summary>
        /// <param name="sender">The "Specify your scope:" dropdown</param>
        /// <param name="e">The event that occurs when the dropdown closes</param>
        private void PBPSpecifyScopeDropdown_DropDownClosed(object sender, EventArgs e)
        {
            dropDownClosedTemplate(sender, e);
        }

        /// <summary>
        /// This method populates the key, secret, and certificate dropdowns and makes them visible upon a selection change.
        /// </summary>
        /// <param name="sender">The "Specify your scope" dropdown</param>
        /// <param name="e">The event that occurs when a selection changes</param>
        private void PBPSpecifyScopeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox dropdown = sender as ComboBox;
            if (dropdown.SelectedIndex != -1)
            {
                ComboBox keys = PBPKeysDropdown as ComboBox;
                populatePermissionsTemplate(keys.Items, Constants.ALL_KEY_PERMISSIONS);
                PBPKeysLabel.Visibility = Visibility.Visible;
                PBPKeysDropdown.Visibility = Visibility.Visible;

                ComboBox secrets = PBPSecretsDropdown as ComboBox;
                populatePermissionsTemplate(secrets.Items, Constants.ALL_SECRET_PERMISSIONS);
                PBPSecretsLabel.Visibility = Visibility.Visible;
                PBPSecretsDropdown.Visibility = Visibility.Visible;

                ComboBox certifs = PBPCertificatesDropdown as ComboBox;
                populatePermissionsTemplate(certifs.Items, Constants.ALL_CERTIFICATE_PERMISSIONS);
                PBPCertificatesLabel.Visibility = Visibility.Visible;
                PBPCertificatesDropdown.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// This method populates the specified dropdown with the array of permissions. 
        /// </summary>
        /// <param name="items">The ItemCollection from a dropdown</param>
        /// <param name="permissions">The permissions with which to populate the dropdown</param>
        private void populatePermissionsTemplate(ItemCollection items, string[] permissions)
        {
            items.Clear();
            foreach (string keyword in permissions)
            {
                CheckBox item = new CheckBox();
                item.Content = keyword.Substring(0, 1).ToUpper() + keyword.Substring(1);
                items.Add(item);
            }
        }

        /// <summary>
        /// This method allows you to select multiple items and displays how many items were selected 
        /// on the dropdown related to keys.
        /// </summary>
        /// <param name="sender">The keys dropdown</param>
        /// <param name="e">The event that occurs when the dropdown closes</param>
        private void PBPKeysDropdown_DropDownClosed(object sender, EventArgs e)
        {
            dropDownClosedTemplate(sender, e);
        }

        /// <summary>
        /// This method allows you to select multiple items and displays how many items were selected 
        /// on the dropdown related to secrets.
        /// </summary>
        /// <param name="sender">The secrets dropdown</param>
        /// <param name="e">The event that occurs when the dropdown closes</param>
        private void PBPSecretsDropdown_DropDownClosed(object sender, EventArgs e)
        {
            dropDownClosedTemplate(sender, e);
        }

        /// <summary>
        /// This method allows you to select multiple items and displays how many items were selected 
        /// on the dropdown related to certificates.
        /// </summary>
        /// <param name="sender">The certificates dropdown</param>
        /// <param name="e">The event that occurs when the dropdown closes</param>
        private void PBPCertificatesDropdown_DropDownClosed(object sender, EventArgs e)
        {
            dropDownClosedTemplate(sender, e);
        }

        /// <summary>
        /// This method shows how many items were selected on the dropdown.
        /// </summary>
        /// <param name="sender">The ComboBox for which you want to display the number of selected items</param>
        /// <param name="e">The event that occurs when the dropdown closes</param>
        private void dropDownClosedTemplate(object sender, EventArgs e)
        {
            ComboBox dropdown = sender as ComboBox;
            ItemCollection items = dropdown.Items;

            List<string> selected = getSelectedItemsTemplate(dropdown);
            if (selected != null)
            {
                int numChecked = selected.Count();

                // Make the ComboBox show how many are selected
                items.Add(new ComboBoxItem()
                {
                    Content = $"{numChecked} selected",
                    Visibility = Visibility.Collapsed
                });
                dropdown.Text = $"{numChecked} selected";
            }
        }

        /// <summary>
        /// This method gets the list of selected items from the specified dropdown.
        /// </summary>
        /// <param name="comboBox">The ComboBox item from which you want to get the selected items</param>
        /// <returns>A list of the selected items</returns>
        private List<string> getSelectedItemsTemplate(ComboBox comboBox)
        {
            ItemCollection items = comboBox.Items;
            List<string> selected = new List<string>();
            try
            {
                ComboBoxItem selectedItem = (ComboBoxItem)comboBox.SelectedItem;
                if (selectedItem != null && selectedItem.Content.ToString() == "YAML")
                {
                    return null;
                }
                if (selectedItem != null && selectedItem.Content.ToString().EndsWith("selected"))
                {
                    items.RemoveAt(items.Count - 1);
                }
            }
            catch
            {
                try
                {
                    ComboBoxItem lastItem = (ComboBoxItem)items.GetItemAt(items.Count - 1);
                    comboBox.SelectedIndex = -1;

                    if (lastItem.Content.ToString().EndsWith("selected"))
                    {
                        items.RemoveAt(items.Count - 1);
                    }
                }
                catch
                {
                    // Do nothing, means the last item is a CheckBox and thus no removal is necessary
                }
            }
            foreach (var item in items)
            {
                CheckBox checkBox = item as CheckBox;
                if ((bool)(checkBox.IsChecked))
                {
                    selected.Add((string)(checkBox.Content));
                }
            }
            return selected;
        }

        /// <summary>
        /// This method runs the principals by permission list option upon clicking the button.
        /// </summary>
        /// <param name="sender">The 'Run' button for the permissions breakdown</param>
        /// <param name="e">The event that occurs when the button is clicked</param>
        private void RunPrincipalByPermissions_Click(object sender, RoutedEventArgs e)
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(false);
            List<KeyVaultProperties> yaml = up.deserializeYaml(Constants.YAML_FILE_PATH);

            ComboBox keysDropdown = PBPKeysDropdown as ComboBox;
            List<string> keysSelected = getSelectedItemsTemplate(keysDropdown);
            ComboBox secretsDropdown = PBPSecretsDropdown as ComboBox;
            List<string> secretsSelected = getSelectedItemsTemplate(secretsDropdown);
            ComboBox certifsDropdown = PBPCertificatesDropdown as ComboBox;
            List<string> certifsSelected = getSelectedItemsTemplate(certifsDropdown);

            if (keysSelected.Count() == 0 && secretsSelected.Count() == 0 && certifsSelected.Count() == 0)
            {
                MessageBox.Show("Please select as least one permission prior to hitting 'Run'.", "NoPermissionsSelected Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ComboBoxItem scope = PBPScopeDropdown.SelectedItem as ComboBoxItem;
            List<KeyVaultProperties> vaultsInScope = new List<KeyVaultProperties>();
            if (scope.Content.ToString() == "YAML")
            {
                vaultsInScope = yaml;
            }
            else
            {
                ComboBox specifyScopeDropdown = PBPSpecifyScopeDropdown as ComboBox;
                List<string> selected = getSelectedItemsTemplate(specifyScopeDropdown);

                if (selected.Count() == 0)
                {
                    MessageBox.Show("Please specify as least one scope prior to hitting 'Run'.", "ScopeInvalid Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                ILookup<string, KeyVaultProperties> lookup;
                if (scope.Content.ToString() == "Subscription")
                {
                    lookup = yaml.ToLookup(kv => kv.SubscriptionId);
                }
                else if (scope.Content.ToString() == "ResourceGroup")
                {
                    lookup = yaml.ToLookup(kv => kv.ResourceGroupName);
                }
                else
                {
                    lookup = yaml.ToLookup(kv => kv.VaultName);
                }

                foreach (var specifiedScope in selected)
                {
                    vaultsInScope.AddRange(lookup[specifiedScope].ToList());
                }
            }

            var data = getPrincipalsByPermission(up, vaultsInScope, keysSelected, secretsSelected, certifsSelected);
            var print = new List<ListSpResults>();
            //PUT DATA IN DATAGRID
            foreach(var s in data.Keys)
            {
                var a = data[s];
                var b = new ListSpResults()
                {
                    PermissionType = s,
                    Sps = new List<SpsWithPermission>()
                };

                foreach(var t in a)
                {
                    var sps = new StringBuilder();
                    foreach(var sp in t.Value)
                    {
                        sps.Append($"{sp.Item2.Type} {((sp.Item2.Alias == null || sp.Item2.Alias.Length == 0) ? sp.Item2.DisplayName : sp.Item2.Alias)} in {sp.Item1}\n");
                    }
                    b.Sps.Add(new SpsWithPermission()
                    {
                        Permission = t.Key,
                        Sps = sps.ToString()
                    });
                }
                print.Add(b);
            }
            ListSPGrid.ItemsSource = print;
            ListSPPopup.IsOpen = true;
            // Once close datagrid, reset dropdowns
            PBPScopeDropdown.SelectedIndex = -1;
            PBPSpecifyScopeLabel.Visibility = Visibility.Hidden;
            PBPSpecifyScopeDropdown.Visibility = Visibility.Hidden;
        }
        internal class ListSpResults
        {
            public string PermissionType { get; set; }
            public List<SpsWithPermission> Sps { get; set;}
        }
        internal class SpsWithPermission
        {
            public string Permission { get; set; }
            public string Sps { get; set; }
        }
        /// <summary>
        /// This method returns the dictionary representing each selected permission and the list of principals with those permissions.
        /// </summary>
        /// <param name="up">The UpdatePoliciesFromYaml instance</param>
        /// <param name="vaultsInScope">The KeyVaults to parse through, or the scope, to generate the principals by permission results</param>
        /// <param name="keysSelected">The list of selected key permissions</param>
        /// <param name="secretsSelected">The list of selected secret permissions</param>
        /// <param name="certifsSelected">The list of selected certificate permissions</param>
        /// <returns>A dictionary with each selected permission, the list of principals that have those permissions, 
        /// and the KeyVault name for which that principal's access policy exists</returns>
        private Dictionary<string, Dictionary<string, List<Tuple<string, PrincipalPermissions>>>> getPrincipalsByPermission(UpdatePoliciesFromYaml up, 
            List<KeyVaultProperties> vaultsInScope, List<string> keysSelected, List<string> secretsSelected, List<string> certifsSelected)
        {
            var principalsByPermission = new Dictionary<string, Dictionary<string, List<Tuple<string, PrincipalPermissions>>>>();
            populatePrincipalDictKeys(keysSelected, secretsSelected, certifsSelected, principalsByPermission);

            foreach (KeyVaultProperties kv in vaultsInScope)
            {
                foreach (PrincipalPermissions principal in kv.AccessPolicies)
                {
                    up.translateShorthands(principal);
                    if (keysSelected.Count != 0)
                    {
                        foreach (string key in keysSelected)
                        {
                            if (principal.PermissionsToKeys.Contains(key.ToLower()))
                            {
                                principalsByPermission["Keys"][key].Add(new Tuple<string, PrincipalPermissions>(kv.VaultName, principal));
                            }
                        }
                    }
                    if (secretsSelected.Count() != 0)
                    {
                        foreach (string secret in secretsSelected)
                        {
                            if (principal.PermissionsToKeys.Contains(secret.ToLower()))
                            {
                                principalsByPermission["Secrets"][secret].Add(new Tuple<string, PrincipalPermissions>(kv.VaultName, principal));
                            }
                        }
                    }
                    if (certifsSelected.Count() != 0)
                    {
                        foreach (string certif in certifsSelected)
                        {
                            if (principal.PermissionsToKeys.Contains(certif.ToLower()))
                            {
                                principalsByPermission["Certificates"][certif].Add(new Tuple<string, PrincipalPermissions>(kv.VaultName, principal));
                            }
                        }
                    }
                }
            }
            return principalsByPermission;
        }

        /// <summary>
        /// This method initializes the keys of the dictionary with each selected permission.
        /// </summary>
        /// <param name="keys">The list of selected key permissions</param>
        /// <param name="secrets">The list of selected secret permissions</param>
        /// <param name="certifs">The list of selected certificate permissions</param>
        /// <param name="dict">A dictionary initialized with each selected permission as keys</param>
        private void populatePrincipalDictKeys(List<string> keysSelected, List<string> secretsSelected, List<string> certifsSelected, 
            Dictionary<string, Dictionary<string, List<Tuple<string, PrincipalPermissions>>>> dict)
        {
            dict["Keys"] = new Dictionary<string, List<Tuple<string, PrincipalPermissions>>>();
            if (keysSelected.Count != 0)
            {
                foreach (string key in keysSelected)
                {
                    dict["Keys"][key] = new List<Tuple<string, PrincipalPermissions>>();
                }
            }

            dict["Secrets"] = new Dictionary<string, List<Tuple<string, PrincipalPermissions>>>();
            if (secretsSelected.Count != 0)
            {
                foreach (string secret in secretsSelected)
                {
                    dict["Secrets"][secret] = new List<Tuple<string, PrincipalPermissions>>();
                }
            }

            dict["Certificates"] = new Dictionary<string, List<Tuple<string, PrincipalPermissions>>>();
            if (certifsSelected.Count() != 0)
            {
                foreach (string certif in certifsSelected)
                {
                    dict["Certificates"][certif] = new List<Tuple<string, PrincipalPermissions>>();
                }
            }
        }

        // 4. Breakdown of Permission Usage by Percentage -------------------------------------------------------------------------------------------

        /// <summary>
        /// This method hides the permissions breakdown selected scope dropdown if this dropdown is re-selected.
        /// </summary>
        /// <param name="sender">The permissions breakdown type dropdown</param>
        /// <param name="e">The event that occurs when a selection changes</param>
        private void BreakdownTypeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BreakdownScopeDropdown.SelectedIndex = -1;

            BreakdownScopeLabel.Visibility = Visibility.Visible;
            BreakdownScopeDropdown.Visibility = Visibility.Visible;

            SelectedScopeBreakdownLabel.Visibility = Visibility.Hidden;
            SelectedScopeBreakdownDropdown.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// This method populates the selectedScope dropdown if "YAML" was not selected. 
        /// Otherwise, this method resets and hides the selectedScope dropdown.
        /// </summary>
        /// <param name="sender">The permissions breakdown scope dropdown</param>
        /// <param name="e">The event that occurs when a selection changes</param>
        private void BreakdownScopeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedScopeBreakdownLabel.Visibility = Visibility.Hidden;
            SelectedScopeBreakdownDropdown.Visibility = Visibility.Hidden;

            if (BreakdownTypeDropdown.SelectedIndex != -1 && BreakdownScopeDropdown.SelectedIndex != -1)
            {
                ComboBoxItem selectedScope = BreakdownScopeDropdown.SelectedItem as ComboBoxItem;
                string scope = selectedScope.Content as string;

                try
                {
                    UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(false);
                    List<KeyVaultProperties> yaml = up.deserializeYaml(Constants.YAML_FILE_PATH);

                    if (yaml.Count() == 0)
                    {
                        throw new Exception("The YAML file path must be specified in the Constants.cs file. Please ensure this path is correct before proceeding.");
                    }

                    ComboBox specifyDropdown = SelectedScopeBreakdownDropdown as ComboBox;
                    if (scope != "YAML")
                    {
                        populateSelectedScopeTemplate(specifyDropdown, scope, yaml);
                        SelectedScopeBreakdownLabel.Visibility = Visibility.Visible;
                        SelectedScopeBreakdownDropdown.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        specifyDropdown.SelectedIndex = -1;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{ex.Message}", "FileNotFound Exception", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    BreakdownTypeDropdown.SelectedIndex = -1;
                    BreakdownScopeDropdown.SelectedIndex = -1;
                    BreakdownScopeLabel.Visibility = Visibility.Hidden;
                    BreakdownScopeDropdown.Visibility = Visibility.Hidden;
                    SelectedScopeBreakdownDropdown.SelectedIndex = -1;
                }
            }
        }

        /// <summary>
        /// This method allows you to select multiple scopes and shows how many you selected on the ComboBox.
        /// </summary>
        /// <param name="sender">The permissions breakdown selected scope dropdown</param>
        /// <param name="e">The event that occurs when the drop down closes</param>
        private void SelectedScopeBreakdownDropdown_DropDownClosed(object sender, EventArgs e)
        {
            dropDownClosedTemplate(sender, e);
        }

        /// <summary>
        /// This method runs the permissions breakdown list option upon clicking the button.
        /// </summary>
        /// <param name="sender">The 'Run' button for the permissions breakdown</param>
        /// <param name="e">The event that occurs when the button is clicked</param>
        private void RunPermissionsBreakdown_Click(object sender, RoutedEventArgs e)
        {
            ComboBox scopeDropdown = SelectedScopeBreakdownDropdown as ComboBox;
            //List<string> selected = getSelectedItems(scopeDropdown.Items);
            List<string> selected = getSelectedItemsTemplate(scopeDropdown);

            ComboBoxItem breakdownScope = BreakdownScopeDropdown.SelectedItem as ComboBoxItem;
            string scope = breakdownScope.Content as string;

            if (scope != "YAML" && selected.Count() == 0)
            {
                MessageBox.Show("Please specify as least one scope prior to hitting 'Run'.", "ScopeInvalid Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                calculatePermissionBreakdown(scope, selected);
            }
        }

        /// <summary>
        /// This method calculates the permissions breakdown usages and displays the results on the popup.
        /// If the file path is not defined in the Constants file, an exception is thrown.
        /// </summary>
        /// <param name="scope">The selected item from the permissions breakdown scope dropdown</param>
        /// <param name="selected">The list of selected items from the permissions breakdown selected scope dropdown, if applicable</param>
        private void calculatePermissionBreakdown(string scope, List<string> selected)
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(false);
            List<KeyVaultProperties> yaml = up.deserializeYaml(Constants.YAML_FILE_PATH);

            if (yaml.Count() == 0)
            {
                MessageBox.Show("The YAML file path must be specified in the Constants.cs file. Please ensure this path is correct before proceeding.", "InvalidFilePath Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ComboBoxItem selectedType = BreakdownTypeDropdown.SelectedItem as ComboBoxItem;
            string type = selectedType.Content as string;

            Dictionary<string, Dictionary<string, int>> count;
            if (scope == "YAML")
            {
                count = countPermissions(yaml, type);
            }
            else
            {
                ILookup<string, KeyVaultProperties> lookup;
                if (scope == "Subscription")
                {
                    lookup = yaml.ToLookup(kv => kv.SubscriptionId);
                }
                else if (scope == "ResourceGroup")
                {
                    lookup = yaml.ToLookup(kv => kv.ResourceGroupName);
                }
                else
                {
                    lookup = yaml.ToLookup(kv => kv.VaultName);
                }

                List<KeyVaultProperties> vaultsInScope = new List<KeyVaultProperties>();
                foreach (var specifiedScope in selected)
                {
                    vaultsInScope.AddRange(lookup[specifiedScope].ToList());
                }
                count = countPermissions(vaultsInScope, type);
            }

            PieChart keys = (LiveCharts.Wpf.PieChart)PermissionsToKeysChart;
            setChartData(keys, count["keyBreakdown"]);
            PieChart secrets = (LiveCharts.Wpf.PieChart)PermissionsToSecretsChart;
            setChartData(secrets, count["secretBreakdown"]);
            PieChart certificates = (LiveCharts.Wpf.PieChart)PermissionsToCertificatesChart;
            setChartData(certificates, count["certificateBreakdown"]);

            PermissionBreakdownResults.IsOpen = true;
        }

        /// <summary>
        /// This method counts the permission/shorthand usages for each type of permission and returns a dictionary that stores the data.
        /// </summary>
        /// <param name="vaultsInScope">The KeyVaults to parse through, or the scope, to generate the permission usage results</param>
        /// <param name="type">The type by which to generate the permission usage results, i.e. by permissions or by shorthands</param>
        /// <returns>A dictionary that stores the permission breakdown usages for each permission block</returns>
        private Dictionary<string, Dictionary<string, int>> countPermissions(List<KeyVaultProperties> vaultsInScope, string type)
        {
            Dictionary<string, Dictionary<string, int>> usages = new Dictionary<string, Dictionary<string, int>>();
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(false);

            if (type == "Permissions")
            {
                usages["keyBreakdown"] = populateBreakdownKeys(Constants.ALL_KEY_PERMISSIONS);
                usages["secretBreakdown"] = populateBreakdownKeys(Constants.ALL_SECRET_PERMISSIONS);
                usages["certificateBreakdown"] = populateBreakdownKeys(Constants.ALL_CERTIFICATE_PERMISSIONS);

                foreach (KeyVaultProperties kv in vaultsInScope)
                {
                    foreach (PrincipalPermissions principal in kv.AccessPolicies)
                    {
                        checkForPermissions(up, usages, principal);
                    }
                }
            }
            else
            {
                usages["keyBreakdown"] = populateBreakdownKeys(Constants.SHORTHANDS_KEYS.Where(val => val != "all").ToArray());
                usages["secretBreakdown"] = populateBreakdownKeys(Constants.SHORTHANDS_SECRETS.Where(val => val != "all").ToArray());
                usages["certificateBreakdown"] = populateBreakdownKeys(Constants.SHORTHANDS_CERTIFICATES.Where(val => val != "all").ToArray());

                foreach (KeyVaultProperties kv in vaultsInScope)
                {
                    foreach (PrincipalPermissions principal in kv.AccessPolicies)
                    {
                        checkForShorthands(up, usages, principal);
                    }
                }
            }
            return usages;
        }

        /// <summary>
        /// This method initializes the keys of the dictionary with each permission or shorthand keyword.
        /// </summary>
        /// <param name="permissions">The permissions block or shorthands array for which to add keys</param>
        /// <returns>A dictionary initialized with each permission or shorthand as keys</returns>
        private Dictionary<string, int> populateBreakdownKeys(string[] permissions)
        {
            Dictionary<string, int> breakdown = new Dictionary<string, int>();
            foreach (string str in permissions)
            {
                breakdown.Add(str, 0);
            }
            return breakdown;
        }

        /// <summary>
        /// This method counts the occurrences of each permission type and stores the results in a dictionary.
        /// </summary>
        /// <param name="up">The UpdatePoliciesFromYaml instance</param>
        /// <param name="usages">The dictionary that stores the permission breakdown usages for each permission block</param>
        /// <param name="principal">The current PrincipalPermissions object</param>
        private void checkForPermissions(UpdatePoliciesFromYaml up, Dictionary<string, Dictionary<string, int>> usages, PrincipalPermissions principal)
        {
            up.translateShorthands(principal);
            foreach (string key in principal.PermissionsToKeys)
            {
                ++usages["keyBreakdown"][key];
            }
            foreach (string secret in principal.PermissionsToSecrets)
            {
                ++usages["secretBreakdown"][secret];
            }
            foreach (string certif in principal.PermissionsToCertificates)
            {
                ++usages["certificateBreakdown"][certif];
            }
        }

        /// <summary>
        /// This method counts the occurrences of each shorthand type and stores the results in a dictionary.
        /// </summary>
        /// <param name="up">The UpdatePoliciesFromYaml instance</param>
        /// <param name="usages">A dictionary that stores the permission breakdown usages for each permission block</param>
        /// <param name="principal">The current PrincipalPermissions object</param>
        private void checkForShorthands(UpdatePoliciesFromYaml up, Dictionary<string, Dictionary<string, int>> usages, PrincipalPermissions principal)
        {
            up.translateShorthands(principal);
            foreach (string shorthand in Constants.SHORTHANDS_KEYS.Where(val => val != "all").ToArray())
            {
                var permissions = up.getShorthandPermissions(shorthand, "key");
                if (principal.PermissionsToKeys.Intersect(permissions).Count() == permissions.Count())
                {
                    ++usages["keyBreakdown"][shorthand];
                }
            }

            foreach (string shorthand in Constants.SHORTHANDS_SECRETS.Where(val => val != "all").ToArray())
            {
                var permissions = up.getShorthandPermissions(shorthand, "secret");
                if (principal.PermissionsToSecrets.Intersect(permissions).Count() == permissions.Count())
                {
                    ++usages["secretBreakdown"][shorthand];
                }
            }

            foreach (string shorthand in Constants.SHORTHANDS_CERTIFICATES.Where(val => val != "all").ToArray())
            {
                var permissions = up.getShorthandPermissions(shorthand, "certificate");
                if (principal.PermissionsToCertificates.Intersect(permissions).Count() == permissions.Count())
                {
                    ++usages["certificateBreakdown"][shorthand];
                }
            }
        }

        /// <summary>
        /// This method translates the "all" keyword into it's respective permissions
        /// </summary>
        /// <param name="principal"></param>
        private void translateAllKeyword(PrincipalPermissions principal)
        {
            if (principal.PermissionsToKeys.Contains("all"))
            {
                principal.PermissionsToKeys = Constants.ALL_KEY_PERMISSIONS;
            }
            if (principal.PermissionsToSecrets.Contains("all"))
            {
                principal.PermissionsToSecrets = Constants.ALL_SECRET_PERMISSIONS;
            }
            if (principal.PermissionsToCertificates.Contains("all"))
            {
                principal.PermissionsToCertificates = Constants.ALL_CERTIFICATE_PERMISSIONS;
            }
        }

        /// <summary>
        /// This method adds and sets the pie chart data to the respective pie chart.
        /// </summary>
        /// <param name="chart">The pie chart to which we want to add data</param>
        /// <param name="breakdownCount">The permissions count corresponding to a particular permissions block</param>
        private void setChartData(PieChart chart, Dictionary<string, int> breakdownCount)
        {
            SeriesCollection data = new SeriesCollection();
            var descendingOrder = breakdownCount.OrderByDescending(i => i.Value);

            int total = 0;
            foreach (int val in breakdownCount.Values)
            {
                total += val;
            }

            foreach (var item in descendingOrder)
            {
                // Create custom label
                double percentage = item.Value / (double)total;
                FrameworkElementFactory stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
                stackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
                FrameworkElementFactory label = new FrameworkElementFactory(typeof(TextBlock));
                label.SetValue(TextBlock.TextProperty, string.Format("{0:P}", percentage));
                label.SetValue(TextBlock.FontSizeProperty, new Binding("15"));
                label.SetValue(TextBlock.ForegroundProperty, new Binding("Black"));
                stackPanelFactory.AppendChild(label);

                data.Add(new LiveCharts.Wpf.PieSeries()
                {
                    Title = item.Key,
                    Values = new ChartValues<int>() { item.Value },
                    DataLabels = true,
                    LabelPoint = (chartPoint => string.Format("{0}", chartPoint.Y)),
                    LabelPosition = PieLabelPosition.OutsideSlice,
                    DataLabelsTemplate = new DataTemplate()
                    {
                        VisualTree = stackPanelFactory
                    }
                });
                chart.Series = data;

                var tooltip = chart.DataTooltip as DefaultTooltip;
                tooltip.SelectionMode = TooltipSelectionMode.OnlySender;
            }
        }

        /// <summary>
        /// This method closes the permissions breakdown popup and resets the dropdowns.
        /// </summary>
        /// <param name="sender">The close popup button</param>
        /// <param name="e">The event that occurs when the button is clicked</param>
        private void CloseBreakdownResults_Click(object sender, RoutedEventArgs e)
        {
            PermissionBreakdownResults.IsOpen = false;

            BreakdownTypeDropdown.SelectedIndex = -1;
            BreakdownScopeDropdown.SelectedIndex = -1;
            BreakdownScopeLabel.Visibility = Visibility.Hidden;
            BreakdownScopeDropdown.Visibility = Visibility.Hidden;
            SelectedScopeBreakdownDropdown.SelectedIndex = -1;
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
        /// This method displays an output when a button is clicked
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Mouse event</param>
        private void Run_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;

            if (btn.Name == "ShorthandPermissionsRun")
            {
                ShorthandPermissionsTranslation.IsOpen = true;
            }
            else if (btn.Name == "SecurityPrincipalRun")
            {
                // Execute Code
            }
            else if (btn.Name == "PermissionsRun")
            {
                RunPrincipalByPermissions_Click(sender, e);
            }
            else if (btn.Name == "BreakdownRun")
            {
                RunPermissionsBreakdown_Click(sender, e);
            }
            else if (btn.Name == "MostAccessedRun")
            {
                // Execute Code
            }
            else if (btn.Name == "SecurityPrincipalAccessRun")
            {
                // Execute Code
            }
        }

        /// <summary>
        /// This method makes the button a different color when the user hovers over it
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Mouse event</param>
        private void Run_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Button btn = sender as Button;
            btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(14, 77, 101));
        }

        /// <summary>
        /// This method returns the button to its original color when a user exits or isn't hovering over the button
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Mouse event</param>
        private void Run_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Button btn = sender as Button;
            btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(25, 117, 151));
        }

        private void CloseListSPPopup_Click(object sender, RoutedEventArgs e)
        {
            ListSPPopup.IsOpen = false;
        }
    }
}

