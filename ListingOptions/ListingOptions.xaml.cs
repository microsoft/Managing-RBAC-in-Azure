using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LiveCharts;
using LiveCharts.Wpf;
using System.Windows.Media;
using System.Windows.Data;
using System.Collections.ObjectModel;

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

                string[] shorthandPermissions = DeserializedYaml.upInstance.getShorthandPermissions(shorthand.ToLower(), permissionType);
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
                    List<KeyVaultProperties> yaml = DeserializedYaml.Yaml;

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
            List<KeyVaultProperties> yaml = DeserializedYaml.Yaml;

            ComboBoxItem scope = PBPScopeDropdown.SelectedItem as ComboBoxItem;
            if (scope == null)
            {
                MessageBox.Show("Please select scope prior to hitting 'Run'.", "ScopeInvalid Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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

            var data = getPrincipalsByPermission(vaultsInScope, keysSelected, secretsSelected, certifsSelected);
            var keys = new List<ListSpResults>();
            var secrets = new List<ListSpResults>();
            var certificates = new List<ListSpResults>();

            KeyTitle.Visibility = Visibility.Visible;
            ListSPKey.Visibility = Visibility.Visible;
            SecTitle.Visibility = Visibility.Visible;
            ListSPSecret.Visibility = Visibility.Visible;
            CertTitle.Visibility = Visibility.Visible;
            ListSPCertificate.Visibility = Visibility.Visible;

            var k = data["Keys"];
            var s = data["Secrets"];
            var c = data["Certificates"];
            if(k.Count == 0)
            {
                KeyTitle.Visibility = Visibility.Collapsed;
                ListSPKey.Visibility = Visibility.Collapsed;
            }
            else
            {
                foreach (var key in k.Keys)
                {
                    var a = new ListSpResults
                    {
                        Permission = key,
                        KeyVaults = new List<KVsWithPermission>()
                    };
                    foreach (var p in k[key])
                    {
                        var toAdd = new KVsWithPermission
                        {
                            VaultName = p.Item1,
                            SecurityPrincipals = new List<SecPrincipals>()
                        };
                        foreach (var sp in p.Item2)
                        {
                            toAdd.SecurityPrincipals.Add(new SecPrincipals
                            {
                                Type = sp.Type,
                                Name = sp.DisplayName,
                                Alias = sp.Alias == null || sp.Alias.Length == 0 ? "N/A" : sp.Alias
                            });
                        }
                        a.KeyVaults.Add(toAdd);
                    }
                    keys.Add(a);
                }
            }
            if (s.Count == 0)
            {
                SecTitle.Visibility = Visibility.Collapsed;
                ListSPSecret.Visibility = Visibility.Collapsed;
            }
            else
            {
                foreach (var key in s.Keys)
                {
                    var a = new ListSpResults
                    {
                        Permission = key,
                        KeyVaults = new List<KVsWithPermission>()
                    };
                    foreach (var p in s[key])
                    {
                        var toAdd = new KVsWithPermission
                        {
                            VaultName = p.Item1,
                            SecurityPrincipals = new List<SecPrincipals>()
                        };
                        foreach (var sp in p.Item2)
                        {
                            toAdd.SecurityPrincipals.Add(new SecPrincipals
                            {
                                Type = sp.Type,
                                Name = sp.DisplayName,
                                Alias = sp.Alias == null || sp.Alias.Length == 0 ? "N/A" : sp.Alias
                            });
                        }
                        a.KeyVaults.Add(toAdd);
                    }
                    secrets.Add(a);
                }
            }
            if (c.Count == 0)
            {
                CertTitle.Visibility = Visibility.Collapsed;
                ListSPCertificate.Visibility = Visibility.Collapsed;
            }
            else
            {
                foreach (var key in c.Keys)
                {
                    var a = new ListSpResults
                    {
                        Permission = key,
                        KeyVaults = new List<KVsWithPermission>()
                    };
                    foreach (var p in c[key])
                    {
                        var toAdd = new KVsWithPermission
                        {
                            VaultName = p.Item1,
                            SecurityPrincipals = new List<SecPrincipals>()
                        };
                        foreach (var sp in p.Item2)
                        {
                            toAdd.SecurityPrincipals.Add(new SecPrincipals
                            {
                                Type = sp.Type,
                                Name = sp.DisplayName,
                                Alias = sp.Alias == null || sp.Alias.Length == 0 ? "N/A" : sp.Alias
                            });
                        }
                        a.KeyVaults.Add(toAdd);
                    }
                    certificates.Add(a);
                }
            }
            

            

            
            ListSPKey.ItemsSource = keys;
            ListSPCertificate.ItemsSource = certificates;
            ListSPSecret.ItemsSource = secrets;
            ListSPPopup.IsOpen = true;
        }
        /// <summary>
        /// This Class is Used to populate the datagrid for listing Security Principals by permission.
        /// It stores a Permission and a list of KeyVaults with access policies containing the permission.
        /// </summary>
        internal class ListSpResults
        {
            public string Permission { get; set; }
            public List<KVsWithPermission> KeyVaults { get; set; }
        }
        /// <summary>
        /// This Class is Used to populate the datagrid for listing Security Principals by permission.
        /// It stores a KeyVault and a List of Security Principals with a certain permission
        /// </summary>
        internal class KVsWithPermission
        {
            public string VaultName { get; set; }
            public List<SecPrincipals> SecurityPrincipals { get; set; }
        }
        /// <summary>
        /// This Class is Used to populate the datagrid for listing Security Principals by permission.
        /// It stores a Security Principal's type, name, and alias
        /// </summary>
        internal class SecPrincipals
        {
            public string Type { get; set; }
            public string Name { get; set; }
            public string Alias { get; set; }
        }

        /// <summary>
        /// This method returns the dictionary representing each selected permission and the list of principals with those permissions.
        /// </summary>
        /// <param name="vaultsInScope">The KeyVaults to parse through, or the scope, to generate the principals by permission results</param>
        /// <param name="keysSelected">The list of selected key permissions</param>
        /// <param name="secretsSelected">The list of selected secret permissions</param>
        /// <param name="certifsSelected">The list of selected certificate permissions</param>
        /// <returns>A dictionary with each selected permission, the list of principals that have those permissions, 
        /// and the KeyVault name for which that principal's access policy exists</returns>
        private Dictionary<string, Dictionary<string, List<Tuple<string, List<PrincipalPermissions>>>>> getPrincipalsByPermission(List<KeyVaultProperties> vaultsInScope, 
            List<string> keysSelected, List<string> secretsSelected, List<string> certifsSelected)
        {
            var principalsByPermission = new Dictionary<string, Dictionary<string, List<Tuple<string, List<PrincipalPermissions>>>>>();
            populatePrincipalDictKeys(keysSelected, secretsSelected, certifsSelected, principalsByPermission);

            foreach (KeyVaultProperties kv in vaultsInScope)
            {
                foreach (PrincipalPermissions principal in kv.AccessPolicies)
                {
                    DeserializedYaml.upInstance.translateShorthands(principal);
                    if (keysSelected.Count != 0)
                    {

                        foreach (string key in keysSelected)
                        {
                            List<PrincipalPermissions> keyPrincipals = new List<PrincipalPermissions>();
                            if (principal.PermissionsToKeys.Contains(key.ToLower()))
                            {
                                var lookup = principalsByPermission["Keys"][key].ToLookup(key => key.Item1)[kv.VaultName].ToList();
                                if (lookup.Count == 0)
                                {
                                    keyPrincipals.Add(principal);
                                    principalsByPermission["Keys"][key].Add(new Tuple<string, List<PrincipalPermissions>>(kv.VaultName, keyPrincipals));
                                }
                                else
                                {
                                    lookup[0].Item2.Add(principal);
                                }
                            }
                        }

                    }
                    if (secretsSelected.Count() != 0)
                    {
                        foreach (string secret in secretsSelected)
                        {
                            List<PrincipalPermissions> secretPrincipals = new List<PrincipalPermissions>();
                            if (principal.PermissionsToSecrets.Contains(secret.ToLower()))
                            {
                                var lookup = principalsByPermission["Secrets"][secret].ToLookup(se => se.Item1)[kv.VaultName].ToList();
                                if (lookup.Count == 0)
                                {
                                    secretPrincipals.Add(principal);
                                    principalsByPermission["Secrets"][secret].Add(new Tuple<string, List<PrincipalPermissions>>(kv.VaultName, secretPrincipals));
                                }
                                else
                                {
                                    lookup[0].Item2.Add(principal);
                                }
                            }
                        }
                    }
                    if (certifsSelected.Count() != 0)
                    { 
                        foreach (string certif in certifsSelected)
                        {
                            List<PrincipalPermissions> certifPrincipals = new List<PrincipalPermissions>();
                            if (principal.PermissionsToCertificates.Contains(certif.ToLower()))
                            {
                                var lookup = principalsByPermission["Certificates"][certif].ToLookup(cert => cert.Item1)[kv.VaultName].ToList();
                                if (lookup.Count == 0)
                                {
                                    certifPrincipals.Add(principal);
                                    principalsByPermission["Certificates"][certif].Add(new Tuple<string, List<PrincipalPermissions>>(kv.VaultName, certifPrincipals));
                                }
                                else
                                {
                                    lookup[0].Item2.Add(principal);
                                }
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
            Dictionary<string, Dictionary<string, List<Tuple<string, List<PrincipalPermissions>>>>> dict)
        {
            dict["Keys"] = new Dictionary<string, List<Tuple<string, List<PrincipalPermissions>>>>();
            if (keysSelected.Count != 0)
            {
                foreach (string key in keysSelected)
                {
                    dict["Keys"][key] = new List<Tuple<string, List<PrincipalPermissions>>>();
                }
            }

            dict["Secrets"] = new Dictionary<string, List<Tuple<string, List<PrincipalPermissions>>>>();
            if (secretsSelected.Count != 0)
            {
                foreach (string secret in secretsSelected)
                {
                    dict["Secrets"][secret] = new List<Tuple<string, List<PrincipalPermissions>>>();
                }
            }

            dict["Certificates"] = new Dictionary<string, List<Tuple<string, List<PrincipalPermissions>>>>();
            if (certifsSelected.Count() != 0)
            {
                foreach (string certif in certifsSelected)
                {
                    dict["Certificates"][certif] = new List<Tuple<string, List<PrincipalPermissions>>>();
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
                    List<KeyVaultProperties> yaml = DeserializedYaml.Yaml;

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
            List<KeyVaultProperties> yaml = DeserializedYaml.Yaml;

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

            if (type == "Permissions")
            {
                usages["keyBreakdown"] = populateBreakdownKeys(Constants.ALL_KEY_PERMISSIONS);
                usages["secretBreakdown"] = populateBreakdownKeys(Constants.ALL_SECRET_PERMISSIONS);
                usages["certificateBreakdown"] = populateBreakdownKeys(Constants.ALL_CERTIFICATE_PERMISSIONS);

                foreach (KeyVaultProperties kv in vaultsInScope)
                {
                    foreach (PrincipalPermissions principal in kv.AccessPolicies)
                    {
                        checkForPermissions(usages, principal);
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
                        checkForShorthands(usages, principal);
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
        /// <param name="usages">The dictionary that stores the permission breakdown usages for each permission block</param>
        /// <param name="principal">The current PrincipalPermissions object</param>
        private void checkForPermissions(Dictionary<string, Dictionary<string, int>> usages, PrincipalPermissions principal)
        {
            DeserializedYaml.upInstance.translateShorthands(principal);
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
        /// <param name="usages">A dictionary that stores the permission breakdown usages for each permission block</param>
        /// <param name="principal">The current PrincipalPermissions object</param>
        private void checkForShorthands(Dictionary<string, Dictionary<string, int>> usages, PrincipalPermissions principal)
        {
            DeserializedYaml.upInstance.translateShorthands(principal);
            foreach (string shorthand in Constants.SHORTHANDS_KEYS.Where(val => val != "all").ToArray())
            {
                var permissions = DeserializedYaml.upInstance.getShorthandPermissions(shorthand, "key");
                if (principal.PermissionsToKeys.Intersect(permissions).Count() == permissions.Count())
                {
                    ++usages["keyBreakdown"][shorthand];
                }
            }

            foreach (string shorthand in Constants.SHORTHANDS_SECRETS.Where(val => val != "all").ToArray())
            {
                var permissions = DeserializedYaml.upInstance.getShorthandPermissions(shorthand, "secret");
                if (principal.PermissionsToSecrets.Intersect(permissions).Count() == permissions.Count())
                {
                    ++usages["secretBreakdown"][shorthand];
                }
            }

            foreach (string shorthand in Constants.SHORTHANDS_CERTIFICATES.Where(val => val != "all").ToArray())
            {
                var permissions = DeserializedYaml.upInstance.getShorthandPermissions(shorthand, "certificate");
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
            var box = MostAccessedScopeDropdown.SelectedItem as ComboBoxItem;
            if (box == null)
                return;
            var choice = box.Content as string;
            if (MostAccessedSpecifyScopeLabel.Visibility == Visibility.Visible ||
               MostAccessedSpecifyScopeDropdown.Visibility == Visibility.Visible)
            {
                MostAccessedSpecifyScopeDropdown.Items.Clear();
                if (choice == "YAML")
                {
                    MostAccessedSpecifyScopeDropdown.Visibility = Visibility.Hidden;
                    MostAccessedSpecifyScopeLabel.Visibility = Visibility.Hidden;
                }
            }
            else
            {
                MostAccessedSpecifyScopeDropdown.Items.Clear();
                if (choice != "YAML")
                {
                    MostAccessedSpecifyScopeLabel.Visibility = Visibility.Visible;
                    MostAccessedSpecifyScopeDropdown.Visibility = Visibility.Visible;
                }
            }
            List<KeyVaultProperties> yaml = DeserializedYaml.Yaml;
            if (choice == "KeyVault")
            {
                foreach (KeyVaultProperties kv in yaml)
                {
                    CheckBox item = new CheckBox();
                    item.Content = kv.VaultName;
                    MostAccessedSpecifyScopeDropdown.Items.Add(item);
                }
            }
            if (choice == "ResourceGroup")
            {
                var rgs = new HashSet<string>();
                foreach (KeyVaultProperties kv in yaml)
                {
                    if (!rgs.Contains(kv.ResourceGroupName))
                    {
                        rgs.Add(kv.ResourceGroupName);
                    }
                }
                foreach (string s in rgs)
                {
                    CheckBox item = new CheckBox();
                    item.Content = s;
                    MostAccessedSpecifyScopeDropdown.Items.Add(item);
                }
            }
            if (choice == "Subscription")
            {
                var subs = new HashSet<string>();
                foreach (KeyVaultProperties kv in yaml)
                {
                    if (!subs.Contains(kv.SubscriptionId))
                    {
                        subs.Add(kv.SubscriptionId);
                    }
                }
                foreach (string s in subs)
                {
                    CheckBox item = new CheckBox();
                    item.Content = s;
                    MostAccessedSpecifyScopeDropdown.Items.Add(item);
                }
            }
        }

        // 6. Ranked Security Principal by Access ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// This method populates the "Specify the scope:" dropdown and makes it visible upon a selection.
        /// </summary>
        /// <param name="sender">The Security principal access scope block dropdown</param>
        /// <param name="e">The event that occurs when a selection changes</param>
        private void SecurityPrincipalAccessScopeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var box = SecurityPrincipalAccessScopeDropdown.SelectedItem as ComboBoxItem;
            if (box == null)
            {
                return;
            }
            var choice = box.Content as string;
            if (SecurityPrincipalAccessSpecifyScopeLabel.Visibility == Visibility.Visible ||
              SecurityPrincipalAccessSpecifyScopeDropdown.Visibility == Visibility.Visible)
            {
                SecurityPrincipalAccessSpecifyScopeDropdown.Items.Clear();
                if (choice == "YAML")
                {
                    SecurityPrincipalAccessSpecifyScopeDropdown.Visibility = Visibility.Hidden;
                    SecurityPrincipalAccessSpecifyScopeLabel.Visibility = Visibility.Hidden;
                }
            }
            else
            {
                SecurityPrincipalAccessSpecifyScopeDropdown.Items.Clear();
                if (choice != "YAML")
                {
                    SecurityPrincipalAccessSpecifyScopeLabel.Visibility = Visibility.Visible;
                    SecurityPrincipalAccessSpecifyScopeDropdown.Visibility = Visibility.Visible;
                }

            }
            List<KeyVaultProperties> yaml = DeserializedYaml.Yaml;
            if (choice == "KeyVault")
            {
                foreach (KeyVaultProperties kv in yaml)
                {
                    CheckBox item = new CheckBox();
                    item.Content = kv.VaultName;
                    SecurityPrincipalAccessSpecifyScopeDropdown.Items.Add(item);
                }
            }
            if (choice == "ResourceGroup")
            {
                var rgs = new HashSet<string>();
                foreach (KeyVaultProperties kv in yaml)
                {
                    if (!rgs.Contains(kv.ResourceGroupName))
                    {
                        rgs.Add(kv.ResourceGroupName);
                    }
                }
                foreach (string s in rgs)
                {
                    CheckBox item = new CheckBox();
                    item.Content = s;
                    SecurityPrincipalAccessSpecifyScopeDropdown.Items.Add(item);
                }
            }
            if (choice == "Subscription")
            {
                var subs = new HashSet<string>();
                foreach (KeyVaultProperties kv in yaml)
                {
                    if (!subs.Contains(kv.SubscriptionId))
                    {
                        subs.Add(kv.SubscriptionId);
                    }
                }
                foreach (string s in subs)
                {
                    CheckBox item = new CheckBox();
                    item.Content = s;
                    SecurityPrincipalAccessSpecifyScopeDropdown.Items.Add(item);
                }
            }
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
                ComboBoxItem selectedBlock = ShorthandPermissionTypesDropdown.SelectedItem as ComboBoxItem;
                ComboBoxItem selectedShorthand = ShorthandPermissionsDropdown.SelectedItem as ComboBoxItem;
                if (selectedBlock == null)
                {
                    MessageBox.Show("Please specify the permission block prior to hitting 'Run'.", "ScopeInvalid Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (selectedShorthand == null)
                {
                    MessageBox.Show("Please specify the shorthand prior to hitting 'Run'.", "ScopeInvalid Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    ShorthandPermissionsTranslation.IsOpen = true;
                }
            }
            else if (btn.Name == "PermissionsBySecurityPrincipalRun")
            {
                // Execute Code            
                permissionsBySecurityPrincipalRunMethod();
            }
            else if (btn.Name == "PermissionsRun")
            {
                RunPrincipalByPermissions_Click(sender, e);
            }
            else if (btn.Name == "BreakdownRun")
            {
                ComboBoxItem selectedType = BreakdownTypeDropdown.SelectedItem as ComboBoxItem;
                ComboBoxItem selectedScope = BreakdownScopeDropdown.SelectedItem as ComboBoxItem;
                if (selectedType == null)
                {
                    MessageBox.Show("Please specify the permission type prior to hitting 'Run'.", "ScopeInvalid Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (selectedScope == null)
                {
                    MessageBox.Show("Please specify scope to hitting 'Run'.", "ScopeInvalid Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    RunPermissionsBreakdown_Click(sender, e);
                }
            }
            else if (btn.Name == "MostAccessedRun")
            {
                RunTopKVs(sender, e);
            }
            else if (btn.Name == "SecurityPrincipalAccessRun")
            {
                RunTopSPs(sender, e);
            }
        }

        /// <summary>
        /// This method runs the code that finds the Security Principals with the most Access.
        /// </summary>
        /// <param name="sender">The Button that runs option 6</param>
        /// <param name="e">The event that occurs when run button is clicked</param>
        private void RunTopSPs(object sender, RoutedEventArgs e)
        {
            ComboBoxItem breakdownScope = SecurityPrincipalAccessScopeDropdown.SelectedItem as ComboBoxItem;
            if (breakdownScope == null)
            {
                MessageBox.Show("Please select scope prior to hitting 'Run'.", "ScopeInvalid Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string scope = breakdownScope.Content as string;
            var cb = SecurityPrincipalAccessSpecifyScopeDropdown;
            List<string> selected = getSelectedItemsTemplate(cb);
            if (scope != "YAML" && selected.Count() == 0)
            {
                MessageBox.Show("Please specify at least one scope prior to hitting 'Run'.", "ScopeInvalid Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                var typeBox = SecurityPrincipalAccessTypeDropdown.SelectedItem as ComboBoxItem;
                string type = typeBox.Content as string;
                List<KeyVaultProperties> vaults = getScopeKVs(scope, selected);
                var topSPs = getTopSPs(vaults, type);
                if (type == "KeyVaults")
                {
                    TopSPGrid.Columns.Clear();
                    var col1 = new DataGridTextColumn();
                    col1.Header = "Security Principal Type";
                    col1.Binding = new System.Windows.Data.Binding("type");
                    col1.Width = 187.5;
                    TopSPGrid.Columns.Add(col1);

                    var col2 = new DataGridTextColumn();
                    col2.Header = "Name";
                    col2.Binding = new System.Windows.Data.Binding("name");
                    col2.Width = 187.5;
                    TopSPGrid.Columns.Add(col2);
                    
                    var col3 = new DataGridTextColumn();
                    col3.Header = "Alias";
                    col3.Binding = new System.Windows.Data.Binding("alias");
                    col3.Width = 187.5;
                    TopSPGrid.Columns.Add(col3);

                    var col4 = new DataGridTextColumn();
                    col4.Header = "KeyVaults Accessed";
                    col4.Binding = new System.Windows.Data.Binding("count");
                    col4.Width = 187.5;
                    TopSPGrid.Columns.Add(col4);

                    TopSPGrid.ItemsSource = topSPs;
                    TopSPResults.IsOpen = true;
                }
                else
                {
                    TopSPGrid.Columns.Clear();
                    var col1 = new DataGridTextColumn();
                    col1.Header = "Security Principal Type";
                    col1.Binding = new System.Windows.Data.Binding("type");
                    col1.Width = 187.5;
                    TopSPGrid.Columns.Add(col1);

                    var col2 = new DataGridTextColumn();
                    col2.Header = "Name";
                    col2.Binding = new System.Windows.Data.Binding("name");
                    col2.Width = 187.5;
                    TopSPGrid.Columns.Add(col2);

                    var col3 = new DataGridTextColumn();
                    col3.Header = "Alias";
                    col3.Binding = new System.Windows.Data.Binding("alias");
                    col3.Width = 187.5;
                    TopSPGrid.Columns.Add(col3);

                    var col4 = new DataGridTextColumn();
                    col4.Header = "Permissions Granted";
                    col4.Binding = new System.Windows.Data.Binding("count");
                    col4.Width = 187.5;
                    TopSPGrid.Columns.Add(col4);

                    TopSPGrid.ItemsSource = topSPs;
                    TopSPResults.IsOpen = true;
                }
            }
        }

        /// <summary>
        /// This method gets the Security Principals with the most accesses in a scope.
        /// </summary>
        /// <param name="vaults">Vaults in scope selected</param>
        /// <param name="type">Sort by "KeyVaults" or "Permissions"</param>
        /// <returns></returns>
        private List<TopSp> getTopSPs(List<KeyVaultProperties> vaults, string type)
        {
            if (type == "KeyVaults")
            {
                var sps = new List<TopSp>();
                var found = new HashSet<string>();
                foreach (KeyVaultProperties kv in vaults)
                {
                    foreach (PrincipalPermissions pp in kv.AccessPolicies)
                    {
                        if ((pp.Type.ToLower() == "user" || pp.Type.ToLower() == "group") && found.Contains(pp.Alias))
                        {
                            var idx = sps.FindIndex(c => c.alias == pp.Alias);
                            sps[idx].count++;
                        }
                        else if ((pp.Type.ToLower() == "application" || pp.Type.ToLower() == "service principal") && found.Contains(pp.DisplayName))
                        {
                            var idx = sps.FindIndex(c => c.name == pp.DisplayName);
                            sps[idx].count++;
                        }
                        else if (pp.Type.ToLower() == "user" || pp.Type.ToLower() == "group")
                        {
                            sps.Add(new TopSp(pp.Type, pp.DisplayName, 1, pp.Alias));
                            found.Add(pp.Alias);
                        }
                        else
                        {
                            sps.Add(new TopSp(pp.Type, pp.DisplayName, 1));
                            found.Add(pp.DisplayName);
                        }
                    }
                }
                sps.Sort((a, b) => b.count.CompareTo(a.count));
                if (sps.Count > 10)
                {
                    sps = sps.GetRange(0, 10);
                }

                return sps;
            }
            else
            {
                var sps = new List<TopSp>();
                var found = new HashSet<string>();

                foreach (KeyVaultProperties kv in vaults)
                {
                    foreach (PrincipalPermissions pp in kv.AccessPolicies)
                    {
                        if ((pp.Type.ToLower() == "user" || pp.Type.ToLower() == "group") && found.Contains(pp.Alias))
                        {
                            var idx = sps.FindIndex(c => c.alias == pp.Alias);
                            sps[idx].count += pp.PermissionsToCertificates.Length + pp.PermissionsToKeys.Length + pp.PermissionsToSecrets.Length;
                        }
                        else if ((pp.Type.ToLower() == "application" || pp.Type.ToLower() == "service principal") && found.Contains(pp.DisplayName))
                        {
                            var idx = sps.FindIndex(c => c.name == pp.DisplayName);
                            sps[idx].count += pp.PermissionsToCertificates.Length + pp.PermissionsToKeys.Length + pp.PermissionsToSecrets.Length;
                        }
                        else if (pp.Type.ToLower() == "user" || pp.Type.ToLower() == "group")
                        {
                            sps.Add(new TopSp(pp.Type, pp.DisplayName, pp.PermissionsToCertificates.Length + pp.PermissionsToKeys.Length + pp.PermissionsToSecrets.Length, pp.Alias));
                            found.Add(pp.Alias);
                        }
                        else
                        {
                            sps.Add(new TopSp(pp.Type, pp.DisplayName, pp.PermissionsToCertificates.Length + pp.PermissionsToKeys.Length + pp.PermissionsToSecrets.Length));
                            found.Add(pp.DisplayName);
                        }
                    }
                }
                sps.Sort((a, b) => b.count.CompareTo(a.count));
                if (sps.Count > 10)
                {
                    sps = sps.GetRange(0, 10);
                }
                return sps;
            }
        }
        /// <summary>
        /// This class is used to list the top Security Principals in a data grid.
        /// It stores each principals's type, name, alias, and number of permissions/kevaults.
        /// </summary>
        internal class TopSp
        {
            public string type { get; set; }
            public string name { get; set; }
            public string alias { get; set; }
            public int count { get; set; }
            public TopSp(string type, string name, int count, string alias = "")
            {
                this.type = type;
                this.name = name;
                this.alias = alias;
                this.count = count;
            }
        }
        /// <summary>
        /// This method runs the code that finds the most accessible KeyVaults.
        /// </summary>
        /// <param name="sender">The Button that runs option 5</param>
        /// <param name="e">The event that occurs when run button is clicked</param>
        private void RunTopKVs(object sender, RoutedEventArgs e)
        {
            ComboBoxItem breakdownScope = MostAccessedScopeDropdown.SelectedItem as ComboBoxItem;
            if (breakdownScope == null)
            {
                MessageBox.Show("Please select scope prior to hitting 'Run'.", "ScopeInvalid Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string scope = breakdownScope.Content as string;
            var cb = MostAccessedSpecifyScopeDropdown;
            List<string> selected = getSelectedItemsTemplate(cb);

            if (scope != "YAML" && selected.Count() == 0)
            {
                MessageBox.Show("Please specify at least one scope prior to hitting 'Run'.", "ScopeInvalid Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                var typeBox = MostAccessedTypeDropdown.SelectedItem as ComboBoxItem;
                string type = typeBox.Content as string;
                List<KeyVaultProperties> vaults = getScopeKVs(scope, selected);
                var topVaults = getTopKVs(vaults, type);
                if (type == "Security Principal")
                {
                    var fill = new List<TopKVSPClass>();
                    for (int i = 0; i < topVaults.Count; i++)
                    {
                        if (i > 9)
                        {
                            break;
                        }
                        fill.Add(new TopKVSPClass(topVaults[i].Key, topVaults[i].Value));
                    }
                    TopKVGrid.Columns.Clear();
                    var col1 = new DataGridTextColumn();
                    col1.Header = "KeyVault Name";
                    col1.Binding = new System.Windows.Data.Binding("VaultName");
                    col1.Width = 300;
                    TopKVGrid.Columns.Add(col1);

                    var col2 = new DataGridTextColumn();
                    col2.Header = "Security Principals with Access";
                    col2.Binding = new System.Windows.Data.Binding("SecurityPrincipals");
                    col2.Width = 300;
                    TopKVGrid.Columns.Add(col2);

                    TopKVGrid.ItemsSource = fill;
                    TopKVResults.IsOpen = true;
                }
                else
                {
                    var fill = new List<TopKVPermClass>();
                    for (int i = 0; i < topVaults.Count; i++)
                    {
                        if (i > 9)
                        {
                            break;
                        }
                        fill.Add(new TopKVPermClass(topVaults[i].Key, topVaults[i].Value));
                    }
                    TopKVGrid.Columns.Clear();
                    var col1 = new DataGridTextColumn();
                    col1.Header = "KeyVault Name";
                    col1.Binding = new System.Windows.Data.Binding("VaultName");
                    col1.Width = 300;
                    TopKVGrid.Columns.Add(col1);

                    var col2 = new DataGridTextColumn();
                    col2.Header = "Permissions Granted in KeyVault";
                    col2.Binding = new System.Windows.Data.Binding("TotalPermissions");
                    col2.Width = 300;
                    TopKVGrid.Columns.Add(col2);

                    TopKVGrid.ItemsSource = fill;
                    TopKVResults.IsOpen = true;
                }
            }
        }
        /// <summary>
        /// This class is used to list the Top KeyVaults in a data grid.
        /// It stores the VaultName and number of principals with access.
        /// </summary>
        internal class TopKVSPClass
        {
            public string VaultName { get; set; }
            public int SecurityPrincipals { get; set; }
            public TopKVSPClass(string name, int count)
            {
                VaultName = name;
                SecurityPrincipals = count;
            }
        }
        /// <summary>
        /// This class is used to list the Top KeyVaults in a data grid.
        /// It stores the VaultName and number of individual permissions granted.
        /// </summary>
        internal class TopKVPermClass
        {
            public string VaultName { get; set; }
            public int TotalPermissions { get; set; }
            public TopKVPermClass(string name, int count)
            {
                VaultName = name;
                TotalPermissions = count;
            }
        }
        /// <summary>
        /// This method gets the most accessible KeyVaults within a scope.
        /// </summary>
        /// <param name="vaults">List of vaults in the scope</param>
        /// <param name="type">Sort by "Security Principals" or "Permissions"</param>
        /// <returns></returns>
        private List<KeyValuePair<string, int>> getTopKVs(List<KeyVaultProperties> vaults, string type)
        {
            if (type == "Security Principals")
            {
                var kvs = new Dictionary<string, int>();
                foreach (KeyVaultProperties kv in vaults)
                {
                    kvs.Add(kv.VaultName, kv.AccessPolicies.Count);
                }
                var ret = kvs.ToList();
                ret.Sort((a, b) => b.Value.CompareTo(a.Value));
                return ret;
            }
            else
            {
                var kvs = new Dictionary<string, int>();
                foreach (KeyVaultProperties kv in vaults)
                {
                    int count = 0;
                    foreach (PrincipalPermissions pp in kv.AccessPolicies)
                    {
                        count += pp.PermissionsToCertificates.Length + pp.PermissionsToKeys.Length + pp.PermissionsToSecrets.Length;
                    }
                    kvs.Add(kv.VaultName, count);
                }
                var ret = kvs.ToList();
                ret.Sort((a, b) => b.Value.CompareTo(a.Value));
                return ret;
            }
        }

        /// <summary>
        /// Gets list of KeyVaults within a specified scope
        /// </summary>
        /// <param name="scope">Type of scope</param>
        /// <param name="selected">Items specified in scope</param>
        /// <returns></returns>
        private List<KeyVaultProperties> getScopeKVs(string scope, List<string> selected)
        {
            List<KeyVaultProperties> yaml = DeserializedYaml.Yaml;
            var ret = new List<KeyVaultProperties>();
            if (scope == "YAML")
            {
                return yaml;
            }
            else if (scope == "KeyVault")
            {
                foreach (var kv in yaml)
                {
                    if (selected.Contains(kv.VaultName))
                    {
                        ret.Add(kv);
                    }
                }
            }
            else if (scope == "ResourceGroup")
            {
                foreach (var kv in yaml)
                {
                    if (selected.Contains(kv.ResourceGroupName))
                    {
                        ret.Add(kv);
                    }
                }
            }
            else if (scope == "Subscription")
            {
                foreach (var kv in yaml)
                {
                    if (selected.Contains(kv.SubscriptionId))
                    {
                        ret.Add(kv);
                    }
                }
            }
            return ret;
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
        /// <summary>
        /// This method closes popup for results of listing security principals by permissions.
        /// </summary>
        /// <param name="sender">The Close button</param>
        /// <param name="e">The event from clicking the button</param>
        private void CloseListSPPopup_Click(object sender, RoutedEventArgs e)
        {
            ListSPPopup.IsOpen = false;

            PBPScopeDropdown.SelectedIndex = -1;
            PBPSpecifyScopeLabel.Visibility = Visibility.Hidden;
            PBPSpecifyScopeDropdown.Visibility = Visibility.Hidden;
        }
        /// <summary>
        /// This method updates dropdown when a new item is selected.
        /// </summary>
        /// <param name="sender">MostAccessedSpecifyDropdown</param>
        /// <param name="e">Event triggered from Item selected</param>
        private void MostAccessedSpecifyScopeDropdown_DropDownClosed(object sender, EventArgs e)
        {
            dropDownClosedTemplate(sender, e);
        }
        /// <summary>
        /// This method shows more options when type is specified for Option 5.
        /// </summary>
        /// <param name="sender">MostAccessedTypeDropdown</param>
        /// <param name="e">Event triggered from choosing option</param>
        private void MostAccessedTypeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MostAccessedScopeDropdown.Visibility = Visibility.Visible;
            MostAccessedScopeLabel.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// This method closes the popup for listing the top KeyVaults and closes all dropdowns
        /// </summary>
        /// <param name="sender">CloseTopKVResults button</param>
        /// <param name="e">Event triggered from clicking button</param>
        private void CloseTopKVResults_Click(object sender, RoutedEventArgs e)
        {
            TopKVResults.IsOpen = false;

            MostAccessedTypeDropdown.SelectedItem = null;
            MostAccessedScopeLabel.Visibility = Visibility.Hidden;
            MostAccessedScopeDropdown.Visibility = Visibility.Hidden;
            MostAccessedScopeDropdown.SelectedItem = null;
            MostAccessedSpecifyScopeLabel.Visibility = Visibility.Hidden;
            MostAccessedSpecifyScopeDropdown.Visibility = Visibility.Hidden;
            MostAccessedSpecifyScopeDropdown.SelectedItem = null;
        }

        /// <summary>
        /// This method shows more options when type is specified for Option 6.
        /// </summary>
        /// <param name="sender">SecurityPrincipalAccessTypeDropdown</param>
        /// <param name="e">Event triggered from choosing option</param>
        private void SecurityPrincipalAccessTypeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SecurityPrincipalAccessScopeLabel.Visibility = Visibility.Visible;
            SecurityPrincipalAccessScopeDropdown.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// This method updates dropdown when a new item is selected.
        /// </summary>
        /// <param name="sender">SecurityPrincipalAccessSpecifyScopeDropdown</param>
        /// <param name="e">Event triggered from Item selected</param>
        private void SecurityPrincipalAccessSpecifyScopeDropdown_DropDownClosed(object sender, EventArgs e)
        {
            dropDownClosedTemplate(sender, e);
        }

        /// <summary>
        /// This method closes the popup for listing the top Security Principals and closes all dropdowns
        /// </summary>
        /// <param name="sender">CloseTopSPResults button</param>
        /// <param name="e">Event triggered from clicking button</param>
        private void CloseTopSPResults_Click(object sender, RoutedEventArgs e)
        {
            TopSPResults.IsOpen = false;

            SecurityPrincipalAccessTypeDropdown.SelectedItem = null;
            SecurityPrincipalAccessScopeDropdown.Visibility = Visibility.Hidden;
            SecurityPrincipalAccessScopeDropdown.SelectedItem = null;
            SecurityPrincipalAccessScopeLabel.Visibility = Visibility.Hidden;
            SecurityPrincipalAccessSpecifyScopeDropdown.Visibility = Visibility.Hidden;
            SecurityPrincipalAccessSpecifyScopeLabel.Visibility = Visibility.Hidden;
            SecurityPrincipalAccessSpecifyScopeDropdown.SelectedItem = null;
        }




        // 2. Permissions By Security Principal  ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// This method populates the specify scope dropdown and makes the dropdown/label visible when the scope dropdown is changed
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Mouse event</param>
        private void PermissionsBySecurityPrincipalScopeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PermissionsBySecurityPrincipalSpecifyScopeLabel.Visibility = Visibility.Hidden;
            PermissionsBySecurityPrincipalSpecifyScopeDropdown.Visibility = Visibility.Hidden;

            PermissionsBySecurityPrincipalTypeLabel.Visibility = Visibility.Hidden;
            PermissionsBySecurityPrincipalTypeDropdown.SelectedIndex = -1;
            PermissionsBySecurityPrincipalTypeDropdown.Visibility = Visibility.Hidden;

            PermissionsBySecurityPrincipalSpecifyTypeLabel.Visibility = Visibility.Hidden;
            PermissionsBySecurityPrincipalSpecifyTypeDropdown.SelectedIndex = -1;
            PermissionsBySecurityPrincipalSpecifyTypeDropdown.Visibility = Visibility.Hidden;

            if (PermissionsBySecurityPrincipalScopeDropdown.SelectedIndex == 0)
            {
                PermissionsBySecurityPrincipalTypeLabel.Visibility = Visibility.Visible;
                PermissionsBySecurityPrincipalTypeDropdown.Visibility = Visibility.Visible;
            }

            if (PermissionsBySecurityPrincipalScopeDropdown.SelectedIndex != -1)
            {
                ComboBoxItem selectedScope = PermissionsBySecurityPrincipalScopeDropdown.SelectedItem as ComboBoxItem;
                string scope = selectedScope.Content as string;

                try
                {
                    UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(false);
                    List<KeyVaultProperties> yaml = up.deserializeYaml(Constants.YAML_FILE_PATH);

                    if (yaml.Count() == 0)
                    {
                        throw new Exception("The YAML file path must be specified in the Constants.cs file. Please ensure this path is correct before proceeding.");
                    }

                    PermissionsBySecurityPrincipalSpecifyScopeDropdown.Items.Clear();
                    if (scope != "YAML")
                    {
                        populatePermissionsBySecurityPrincipalSpecifyScope(yaml);
                    }
                    else
                    {
                        PermissionsBySecurityPrincipalSpecifyScopeDropdown.Items.Add(new ComboBoxItem()
                        {
                            Content = "YAML",
                            IsSelected = true
                        });
                    }
                    PermissionsBySecurityPrincipalSpecifyScopeLabel.Visibility = Visibility.Visible;
                    PermissionsBySecurityPrincipalSpecifyScopeDropdown.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{ex.Message}", "FileNotFound Exception", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    PermissionsBySecurityPrincipalScopeDropdown.SelectedIndex = -1;
                    //  PermissionsBySecurityPrincipalScopeLabel.Visibility = Visibility.Hidden;
                    //  PermissionsBySecurityPrincipalScopeDropdown.Visibility = Visibility.Hidden;
                    PermissionsBySecurityPrincipalSpecifyScopeDropdown.SelectedIndex = -1;
                }
            }
        }

        /// <summary>
        /// This method populates the specify scope dropdown
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Mouse event</param>
        private void populatePermissionsBySecurityPrincipalSpecifyScope(List<KeyVaultProperties> yaml)
        {
            ComboBoxItem selectedScope = PermissionsBySecurityPrincipalScopeDropdown.SelectedItem as ComboBoxItem;
            string scope = selectedScope.Content as string;

            List<string> items = new List<string>();
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
                PermissionsBySecurityPrincipalSpecifyScopeDropdown.Items.Add(new CheckBox()
                {
                    Content = item
                });
            }
        }

        /// <summary>
        /// This method makes the type label/dropdown visible when the specify scope selection changes
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Mouse event</param>
        private void PermissionsBySecurityPrincipalSpecifyScopeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox dropdown = sender as ComboBox;
            if (dropdown.SelectedIndex != -1)
            {
                PermissionsBySecurityPrincipalTypeDropdown.SelectedIndex = -1;
                PermissionsBySecurityPrincipalTypeLabel.Visibility = Visibility.Visible;
                PermissionsBySecurityPrincipalTypeDropdown.Visibility = Visibility.Visible;

                PermissionsBySecurityPrincipalSpecifyTypeDropdown.SelectedIndex = -1;
                PermissionsBySecurityPrincipalSpecifyTypeLabel.Visibility = Visibility.Hidden;
                PermissionsBySecurityPrincipalSpecifyTypeDropdown.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// This method makes the specify scope dropdown indicate how many items were selected when the dropdown is close
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Mouse event</param>
        private void PermissionsBySecurityPrincipalSpecifyScopeDropdown_DropDownClosed(object sender, EventArgs e)
        {
            dropDownClosedTemplate(sender, e);
        }

        /// <summary>
        /// This method populates the specify type dropdown and make the specify type dropdown/label visible when a selection change occurs in the type dropdown
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Mouse event</param>
        private void PermissionsBySecurityPrincipalTypeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PermissionsBySecurityPrincipalSpecifyTypeDropdown.Items.Clear();

            if (PermissionsBySecurityPrincipalTypeDropdown.SelectedIndex != -1)
            {
                ComboBoxItem selectedtype = PermissionsBySecurityPrincipalTypeDropdown.SelectedItem as ComboBoxItem;
                string type = selectedtype.Content as string;

                ComboBoxItem selectedScope = PermissionsBySecurityPrincipalScopeDropdown.SelectedItem as ComboBoxItem;
                string scope = selectedScope.Content as string;

                ComboBox potentialSpecifyScope = PermissionsBySecurityPrincipalSpecifyScopeDropdown as ComboBox;
                ComboBox potentialSpecifyTypeScope = PermissionsBySecurityPrincipalSpecifyTypeDropdown as ComboBox;

                var selectedSpecifyScopeItems = getSelectedItemsTemplate(potentialSpecifyScope);

                UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(false);
                List<KeyVaultProperties> yaml = up.deserializeYaml(Constants.YAML_FILE_PATH);

                List<string> items = new List<string>();

                if (scope == "YAML")
                {
                    if (type == "All")
                    {
                        foreach (KeyVaultProperties kv in yaml)
                        {
                            foreach (PrincipalPermissions sp in kv.AccessPolicies)
                            {
                                if (sp.Type == "User" || sp.Type == "Group")
                                {
                                    items.Add(sp.Alias);
                                }
                                else if (sp.Type == "Service Principal")
                                {
                                    items.Add(sp.DisplayName);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (KeyVaultProperties kv in yaml)
                        {
                            foreach (PrincipalPermissions sp in kv.AccessPolicies)
                            {
                                if (sp.Type == type && (sp.Type == "User" || sp.Type == "Group"))
                                {
                                    items.Add(sp.Alias);
                                }
                                else if (sp.Type == type && (sp.Type == "Service Principal"))
                                {
                                    items.Add(sp.DisplayName);
                                }
                            }
                        }
                    }
                }
                else if (scope == "Subscription")
                {
                    if (type == "All")
                    {
                        foreach (KeyVaultProperties kv in yaml)
                        {
                            if (selectedSpecifyScopeItems.Contains(kv.SubscriptionId))
                            {
                                foreach (PrincipalPermissions sp in kv.AccessPolicies)
                                {
                                    if (sp.Type == "User" || sp.Type == "Group")
                                    {
                                        items.Add(sp.Alias);
                                    }
                                    else if (sp.Type == "Service Principal")
                                    {
                                        items.Add(sp.DisplayName);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (KeyVaultProperties kv in yaml)
                        {
                            if (selectedSpecifyScopeItems.Contains(kv.SubscriptionId))
                            {
                                foreach (PrincipalPermissions sp in kv.AccessPolicies)
                                {
                                    if (sp.Type == type && (sp.Type == "User" || sp.Type == "Group"))
                                    {
                                        items.Add(sp.Alias);
                                    }
                                    else if (sp.Type == type && (sp.Type == "Service Principal"))
                                    {
                                        items.Add(sp.DisplayName);
                                    }
                                }
                            }
                        }
                    }
                }
                else if (scope == "ResourceGroup")
                {
                    if (type == "All")
                    {
                        foreach (KeyVaultProperties kv in yaml)
                        {
                            if (selectedSpecifyScopeItems.Contains(kv.ResourceGroupName))
                            {
                                foreach (PrincipalPermissions sp in kv.AccessPolicies)
                                {
                                    if (sp.Type == "User" || sp.Type == "Group")
                                    {
                                        items.Add(sp.Alias);
                                    }
                                    else if (sp.Type == "Service Principal")
                                    {
                                        items.Add(sp.DisplayName);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (KeyVaultProperties kv in yaml)
                        {
                            if (selectedSpecifyScopeItems.Contains(kv.ResourceGroupName))
                            {
                                foreach (PrincipalPermissions sp in kv.AccessPolicies)
                                {
                                    if (sp.Type == type && (sp.Type == "User" || sp.Type == "Group"))
                                    {
                                        items.Add(sp.Alias);
                                    }
                                    else if (sp.Type == type && (sp.Type == "Service Principal"))
                                    {
                                        items.Add(sp.DisplayName);
                                    }
                                }
                            }
                        }
                    }
                }
                else if (scope == "KeyVault")
                {
                    if (type == "All")
                    {
                        foreach (KeyVaultProperties kv in yaml)
                        {
                            if (selectedSpecifyScopeItems.Contains(kv.VaultName))
                            {
                                foreach (PrincipalPermissions sp in kv.AccessPolicies)
                                {
                                    if (sp.Type == "User" || sp.Type == "Group")
                                    {
                                        items.Add(sp.Alias);
                                    }
                                    else if (sp.Type == "Service Principal")
                                    {
                                        items.Add(sp.DisplayName);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (KeyVaultProperties kv in yaml)
                        {
                            if (selectedSpecifyScopeItems.Contains(kv.VaultName))
                            {
                                foreach (PrincipalPermissions sp in kv.AccessPolicies)
                                {
                                    if (sp.Type == type && (sp.Type == "User" || sp.Type == "Group"))
                                    {
                                        items.Add(sp.Alias);
                                    }
                                    else if (sp.Type == type && (sp.Type == "Service Principal"))
                                    {
                                        items.Add(sp.DisplayName);
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (string item in items.Distinct())
                {
                    PermissionsBySecurityPrincipalSpecifyTypeDropdown.Items.Add(new CheckBox()
                    {
                        Content = item
                    });
                }

                PermissionsBySecurityPrincipalSpecifyTypeLabel.Visibility = Visibility.Visible;
                PermissionsBySecurityPrincipalSpecifyTypeDropdown.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// This method makes the specify type dropdown indicate how many items were selected when the dropdown is closed
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Mouse event</param>
        private void PermissionsBySecurityPrincipalSpecifyTypeDropdown_DropDownClosed(object sender, EventArgs e)
        {
            dropDownClosedTemplate(sender, e);
        }

        /// <summary>
        /// This method gets the last datagrid, cycles through the columns, and changes the visibility of column with the "header" param name
        /// </summary>
        /// <param name="header">The header name of a column </param>
        /// <param name="vis">The visibility of an object</param>
        private void gridColumnToggleVisibility(string header, Visibility vis)
        {
            foreach (DataGridColumn col in getLastStackPanelDataGrid().Columns)
            {
                if ((string)col.Header == header)
                {
                    col.Visibility = vis;
                }
            }

        }

        /// <summary>
        /// This method closes the pop up and clears all datagrid content
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Mouse event</param>
        private void ClosePermissionsBySecurityPrincipal_Clicked(object sender, RoutedEventArgs e)
        {
            PermissionsBySecurityPrincipalStackPanel.Children.RemoveRange(1, PermissionsBySecurityPrincipalStackPanel.Children.Count - 1);
            PermissionsBySecurityPrincipalPopUp.IsOpen = false;

            // Reset dropdowns
            PermissionsBySecurityPrincipalScopeDropdown.SelectedIndex = -1;
        }

        /// <summary>
        /// This method populates all the dataGrids and opens the pop up. It executes when the Run button is clicked
        /// </summary>
        public void permissionsBySecurityPrincipalRunMethod()
        {
            ComboBox specifyType = PermissionsBySecurityPrincipalSpecifyTypeDropdown as ComboBox;
            List<string> selectedSpecifyTypeItems = getSelectedItemsTemplate(specifyType);

            if (PermissionsBySecurityPrincipalPopUp.IsOpen)
            {
                PermissionsBySecurityPrincipalStackPanel.Children.RemoveRange(1, PermissionsBySecurityPrincipalStackPanel.Children.Count - 1);
            }

            if (PermissionsBySecurityPrincipalScopeDropdown.SelectedIndex == -1)
            {
                MessageBox.Show("Please specify as least one scope prior to hitting 'Run'.", "ScopeInvalid Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (PermissionsBySecurityPrincipalTypeDropdown.Visibility == Visibility.Hidden
                || PermissionsBySecurityPrincipalTypeDropdown.SelectedIndex == -1)
            {
                MessageBox.Show("Please specify as least one type prior to hitting 'Run'.", "ScopeInvalid Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else
            {
                if (selectedSpecifyTypeItems.Count == 0)
                {
                    MessageBox.Show("Please select at least one security principal prior to hitting 'Run'.", "ScopeInvalid Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(false);
            List<KeyVaultProperties> yaml = up.deserializeYaml(Constants.YAML_FILE_PATH);

            ComboBoxItem selectedScope = PermissionsBySecurityPrincipalScopeDropdown.SelectedItem as ComboBoxItem;
            string scope = selectedScope.Content as string;

            ComboBoxItem selectedtype = PermissionsBySecurityPrincipalTypeDropdown.SelectedItem as ComboBoxItem;
            string type = selectedtype.Content as string;

            PermissionsBySecurityPrincipalPopUp.IsOpen = true;


            PermissionsBySecurityPrincipalStackPanel.Children.Add(createDataGridTitle($"Listing Assigned Permissions by Security Principal"));

            if (scope == "YAML")
            {
                if (type == "All")
                {
                    PermissionsBySecurityPrincipalStackPanel.Children.Add(createDataGridHeader($" Scope: Yaml; Type: {type}:"));
                    PermissionsBySecurityPrincipalStackPanel.Children.Add(createDataGrid(false));

                    gridColumnToggleVisibility("Type", Visibility.Visible);
                    var kvs = new List<SecurityPrincipalData>();
                    foreach (KeyVaultProperties kv in yaml)
                    {
                        SecurityPrincipalData newkv = new SecurityPrincipalData { VaultName = kv.VaultName, SecurityPrincipals = new List<SPPermissions>() };
                        foreach (PrincipalPermissions sp in kv.AccessPolicies)
                        {
                            if ((sp.Type == "User" || sp.Type == "Group") && selectedSpecifyTypeItems.Contains(sp.Alias))
                            {
                                newkv.SecurityPrincipals.Add(new SPPermissions(sp.DisplayName, sp.Alias,
                                    sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates, sp.Type));
                            }
                            else if (sp.Type == "Service Principal" && selectedSpecifyTypeItems.Contains(sp.DisplayName))
                            {
                                newkv.SecurityPrincipals.Add(new SPPermissions(sp.DisplayName, "N/A",
                                        sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates, sp.Type));
                            }
                        }
                        if (newkv.SecurityPrincipals.Count != 0)
                        {
                            kvs.Add(newkv);
                        }
                    }
                    getLastStackPanelDataGrid().ItemsSource = kvs;
                }
                else
                {

                    PermissionsBySecurityPrincipalStackPanel.Children.Add(createDataGridHeader($" Scope: Yaml; Type: {type}:"));                   
                    PermissionsBySecurityPrincipalStackPanel.Children.Add(createDataGrid(true));
                    var kvs = new List<NoTypeData>();

                    foreach (KeyVaultProperties kv in yaml)
                    {
                        NoTypeData newkv = new NoTypeData { VaultName = kv.VaultName, SecurityPrincipals = new List<NoTypePermissions>() };
                        foreach (PrincipalPermissions sp in kv.AccessPolicies)
                        {
                            if ((sp.Type == "User" || sp.Type == "Group") && selectedSpecifyTypeItems.Contains(sp.Alias))
                            {
                                newkv.SecurityPrincipals.Add(new NoTypePermissions(sp.DisplayName, sp.Alias,
                                    sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates));
                            }
                            else if (sp.Type == "Service Principal" && selectedSpecifyTypeItems.Contains(sp.DisplayName))
                            {
                                gridColumnToggleVisibility("Alias", Visibility.Hidden);
                                newkv.SecurityPrincipals.Add(new NoTypePermissions(sp.DisplayName, "N/A",
                                        sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates));
                            }
                        }
                        if (newkv.SecurityPrincipals.Count != 0)
                        {
                            kvs.Add(newkv);
                        }
                    }
                    getLastStackPanelDataGrid().ItemsSource = kvs;
                }

                return;
            }

            ComboBox potentialSpecifyScope = PermissionsBySecurityPrincipalSpecifyScopeDropdown as ComboBox;
            var selectedSpecifyScopeItems = getSelectedItemsTemplate(potentialSpecifyScope);

            if (scope == "Subscription")
            {
                List<string> subscriptions = new List<string>();

                if (type == "All")
                {   
                    var kvs = new List<SecurityPrincipalData>();

                    foreach (KeyVaultProperties kv in yaml)
                    {
                        if (selectedSpecifyScopeItems.Contains(kv.SubscriptionId))
                        {
                            if (subscriptions.Contains(kv.SubscriptionId) == false)
                            {
                                subscriptions.Add(kv.SubscriptionId);
                                PermissionsBySecurityPrincipalStackPanel.Children.Add(createDataGridHeader($" Scope: Subscription: {kv.SubscriptionId}; Type: {type}:"));
                                PermissionsBySecurityPrincipalStackPanel.Children.Add(createDataGrid(false)) ;

                                gridColumnToggleVisibility("Type", Visibility.Visible);
                            }
                            SecurityPrincipalData newkv = new SecurityPrincipalData { VaultName = kv.VaultName, SecurityPrincipals = new List<SPPermissions>() };
                            foreach (PrincipalPermissions sp in kv.AccessPolicies)
                            {
                                if ((sp.Type == "User" || sp.Type == "Group") && selectedSpecifyTypeItems.Contains(sp.Alias))
                                {
                                    newkv.SecurityPrincipals.Add(new SPPermissions(sp.DisplayName, sp.Alias,
                                    sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates, sp.Type));
                                }
                                else if ((sp.Type == "Service Principal") && selectedSpecifyTypeItems.Contains(sp.DisplayName))
                                {
                                    newkv.SecurityPrincipals.Add(new SPPermissions( sp.DisplayName, "N/A",
                                        sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates, sp.Type));
                                }
                            }

                            if (getLastStackPanelDataGrid().Items.IsEmpty == true)
                            {                           
                              //  PermissionsBySecurityPrincipalStackPanel.Children.Remove(getLastStackPanelDataGrid());
                              //  PermissionsBySecurityPrincipalStackPanel.Children.Add(createEmptyDataGridHeader($"  - No Permissions of Type: '{type}' found!"));
                            }

                            if (newkv.SecurityPrincipals.Count != 0)
                            {
                                kvs.Add(newkv);
                            }
                        }
                        
                    }
                    getLastStackPanelDataGrid().ItemsSource = kvs;
                }
                else
                {
                    var kvs = new List<NoTypeData>();

                    foreach (KeyVaultProperties kv in yaml)
                    {
                        if (selectedSpecifyScopeItems.Contains(kv.SubscriptionId))
                        {
                            if (subscriptions.Contains(kv.SubscriptionId) == false)
                            {
                                subscriptions.Add(kv.SubscriptionId);

                                PermissionsBySecurityPrincipalStackPanel.Children.Add(createDataGridHeader($" Scope: Subscription: {kv.SubscriptionId}; Type: {type}:"));
                                PermissionsBySecurityPrincipalStackPanel.Children.Add(createDataGrid(true));
                            }
                            NoTypeData newkv = new NoTypeData { VaultName = kv.VaultName, SecurityPrincipals = new List<NoTypePermissions>() };
                            foreach (PrincipalPermissions sp in kv.AccessPolicies)
                            {
                                if (sp.Type == type && (type == "User" || type == "Group") && selectedSpecifyTypeItems.Contains(sp.Alias))
                                {
                                    newkv.SecurityPrincipals.Add(new NoTypePermissions( sp.DisplayName, sp.Alias,
                                    sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates));
                                }
                                else if (sp.Type == type && (type == "Service Principal") && selectedSpecifyTypeItems.Contains(sp.DisplayName))
                                {
                                    gridColumnToggleVisibility("Alias", Visibility.Hidden);
                                    newkv.SecurityPrincipals.Add(new NoTypePermissions( sp.DisplayName, "N/A",
                                        sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates));
                                }
                            }
                            if (getLastStackPanelDataGrid().Items.IsEmpty == true)
                            {
                             //  PermissionsBySecurityPrincipalStackPanel.Children.Remove(getLastStackPanelDataGrid());
                             //  PermissionsBySecurityPrincipalStackPanel.Children.Add(createEmptyDataGridHeader($"  - No Permissions of Type: '{type}' found!"));
                            }
                            
                            if (newkv.SecurityPrincipals.Count != 0)
                            {
                                kvs.Add(newkv);
                            }
                        }
                    }
                    getLastStackPanelDataGrid().ItemsSource = kvs;
                }
            }
            else if (scope == "ResourceGroup")
            {
                List<string> resourceGroups = new List<string>();

                if (type == "All")
                {
                    var kvs = new List<SecurityPrincipalData>();
                    foreach (KeyVaultProperties kv in yaml)
                    {
                        if (selectedSpecifyScopeItems.Contains(kv.ResourceGroupName))
                        {
                            if (resourceGroups.Contains(kv.ResourceGroupName) == false)
                            {
                                resourceGroups.Add(kv.ResourceGroupName);
                                PermissionsBySecurityPrincipalStackPanel.Children.Add(createDataGridHeader($" Scope: Resource Group: {kv.ResourceGroupName}; Type: {type}:"));
                                PermissionsBySecurityPrincipalStackPanel.Children.Add(createDataGrid(false));
                                gridColumnToggleVisibility("Type", Visibility.Visible);
                            }

                            SecurityPrincipalData newkv = new SecurityPrincipalData { VaultName = kv.VaultName, SecurityPrincipals = new List<SPPermissions>() };
                            foreach (PrincipalPermissions sp in kv.AccessPolicies)
                            {
                                if ((sp.Type == "User" || sp.Type == "Group") && selectedSpecifyTypeItems.Contains(sp.Alias))
                                {
                                    newkv.SecurityPrincipals.Add(new SPPermissions(sp.DisplayName, sp.Alias,
                                    sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates, sp.Type));
                                }
                                else if ((sp.Type == "Service Principal") && selectedSpecifyTypeItems.Contains(sp.DisplayName))
                                {
                                    newkv.SecurityPrincipals.Add(new SPPermissions(sp.DisplayName, "N/A",
                                        sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates, sp.Type));
                                }
                            }

                            if (newkv.SecurityPrincipals.Count == 0)
                            {
                                try
                                {
                                    PermissionsBySecurityPrincipalStackPanel.Children.Remove(getLastStackPanelDataGrid());
                                    PermissionsBySecurityPrincipalStackPanel.Children.Add(createEmptyDataGridHeader($"  - No Permissions of Type: '{type}' found!"));
                                }
                                catch
                                {
                                }
                            }
                            else if (newkv.SecurityPrincipals.Count != 0)
                            {
                                kvs.Add(newkv);
                            }

                            try
                            {
                                getLastStackPanelDataGrid().ItemsSource = kvs;
                            }
                            catch
                            {

                            }
                        }
                    }
                }
                else
                {
                    var kvs = new List<NoTypeData>();
                    foreach (KeyVaultProperties kv in yaml)
                    {
                        if (selectedSpecifyScopeItems.Contains(kv.ResourceGroupName))
                        {
                            if (resourceGroups.Contains(kv.ResourceGroupName) == false)
                            {
                                resourceGroups.Add(kv.ResourceGroupName);
                                PermissionsBySecurityPrincipalStackPanel.Children.Add(createDataGridHeader($" Scope: Resource Group: {kv.ResourceGroupName}; Type: {type}:"));
                                PermissionsBySecurityPrincipalStackPanel.Children.Add(createDataGrid(true));
                                gridColumnToggleVisibility("Type", Visibility.Visible);
                            }

                            NoTypeData newkv = new NoTypeData { VaultName = kv.VaultName, SecurityPrincipals = new List<NoTypePermissions>() };
                            foreach (PrincipalPermissions sp in kv.AccessPolicies)
                            {
                                if (sp.Type == type && (type == "User" || type == "Group") && selectedSpecifyTypeItems.Contains(sp.Alias))
                                {
                                    newkv.SecurityPrincipals.Add(new NoTypePermissions(sp.DisplayName, sp.Alias,
                                    sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates));
                                }
                                else if (sp.Type == type && (type == "Service Principal") && selectedSpecifyTypeItems.Contains(sp.DisplayName))
                                {
                                    gridColumnToggleVisibility("Alias", Visibility.Hidden);
                                    newkv.SecurityPrincipals.Add(new NoTypePermissions(sp.DisplayName, "N/A",
                                        sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates));
                                }
                            }
                            if (newkv.SecurityPrincipals.Count == 0)
                            {
                                try
                                {
                                    PermissionsBySecurityPrincipalStackPanel.Children.Remove(getLastStackPanelDataGrid());
                                    PermissionsBySecurityPrincipalStackPanel.Children.Add(createEmptyDataGridHeader($"  - No Permissions of Type: '{type}' found!"));
                                }
                                catch
                                {
                                }
                            }
                            else if (newkv.SecurityPrincipals.Count != 0)
                            {
                                kvs.Add(newkv);
                            }

                            try
                            {
                                getLastStackPanelDataGrid().ItemsSource = kvs;
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
            else if (scope == "KeyVault")
            {
                if (type == "All")
                {

                    PermissionsBySecurityPrincipalStackPanel.Children.Add(createDataGridHeader($" Scope: KeyVaults; Type: {type}:"));
                    PermissionsBySecurityPrincipalStackPanel.Children.Add(createDataGrid(false));

                    gridColumnToggleVisibility("Type", Visibility.Visible);
                    var kvs = new List<SecurityPrincipalData>();
                    foreach (KeyVaultProperties kv in yaml)
                    {
                        if (selectedSpecifyScopeItems.Contains(kv.VaultName))
                        {
                            SecurityPrincipalData newkv = new SecurityPrincipalData { VaultName = kv.VaultName, SecurityPrincipals = new List<SPPermissions>() };
                            foreach (PrincipalPermissions sp in kv.AccessPolicies)
                            {
                                if ((sp.Type == "User" || sp.Type == "Group") && selectedSpecifyTypeItems.Contains(sp.Alias))
                                {
                                    newkv.SecurityPrincipals.Add(new SPPermissions( sp.DisplayName, sp.Alias,
                                        sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates, sp.Type));

                                }
                                else if ((sp.Type == "Service Principal") && selectedSpecifyTypeItems.Contains(sp.DisplayName))
                                {
                                    newkv.SecurityPrincipals.Add(new SPPermissions( sp.DisplayName, "N/A",
                                        sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates, sp.Type));
                                }
                            }
                            if (newkv.SecurityPrincipals.Count != 0)
                            {
                                kvs.Add(newkv);
                            }
                            else
                            {
                                newkv.SecurityPrincipals.Add(new SPPermissions("None", "N/A", new string[] { "N/A" }, new string[] { "N/A" }, new string[] { "N/A" }, "N/A"));
                                kvs.Add(newkv);
                            }
                        }
                    }
                    getLastStackPanelDataGrid().ItemsSource = kvs;
                }
                else
                {
                    PermissionsBySecurityPrincipalStackPanel.Children.Add(createDataGridHeader($" Scope: KeyVaults; Type: {type}:"));
                    PermissionsBySecurityPrincipalStackPanel.Children.Add(createDataGrid(true));

                    gridColumnToggleVisibility("Type", Visibility.Hidden);
                    var kvs = new List<NoTypeData>();
                    foreach (KeyVaultProperties kv in yaml)
                    {
                        if (selectedSpecifyScopeItems.Contains(kv.VaultName))
                        {
                            NoTypeData newkv = new NoTypeData { VaultName = kv.VaultName, SecurityPrincipals = new List<NoTypePermissions>() };
                            foreach (PrincipalPermissions sp in kv.AccessPolicies)
                            {
                                if (sp.Type == type && (type == "User" || type == "Group") && selectedSpecifyTypeItems.Contains(sp.Alias))
                                {
                                    newkv.SecurityPrincipals.Add(new NoTypePermissions(sp.DisplayName, sp.Alias,
                                        sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates));
                                }
                                else if (sp.Type == type && (type == "Service Principal") && selectedSpecifyTypeItems.Contains(sp.DisplayName))
                                {
                                    gridColumnToggleVisibility("Alias", Visibility.Hidden);
                                    newkv.SecurityPrincipals.Add(new NoTypePermissions(sp.DisplayName, "N/A",
                                        sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates));
                                }
                            }
                            if (newkv.SecurityPrincipals.Count != 0)
                            {
                                kvs.Add(newkv);
                            }
                            else
                            {
                                newkv.SecurityPrincipals.Add(new NoTypePermissions("None", "N/A", new string[] { "N/A" }, new string[] { "N/A" }, new string[] { "N/A" }));
                                kvs.Add(newkv);
                            }
                        }
                    }
                    getLastStackPanelDataGrid().ItemsSource = kvs;
                }
            }
        }

        /// <summary>
        /// This method returns the last added datagrid
        /// </summary>
        /// <returns>Last added datagrid.</returns>
        public DataGrid getLastStackPanelDataGrid()
        {
            return (DataGrid)PermissionsBySecurityPrincipalStackPanel.Children[PermissionsBySecurityPrincipalStackPanel.Children.Count - 1];        
        }

        public TextBlock createDataGridTitle(string text)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = text;
            textBlock.Style = (Style)Resources["ChartTitleStyle"];
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            textBlock.FontSize = 24;
            return textBlock;
        }

        /// <summary>
        /// This method creates and returns a data grid header with a specified text
        /// </summary>
        /// <param name="text">The Display of the TextBlock </param>
        /// <returns>A TextBlock </returns>
        public TextBlock createDataGridHeader(string text)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = text;
            textBlock.Style = (Style)Resources["ChartHeaderStyle"];
            textBlock.HorizontalAlignment = HorizontalAlignment.Left;
            return textBlock;
        }

        public TextBlock createEmptyDataGridHeader(string text)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = text;
            textBlock.Style = (Style)Resources["EmptyChartHeaderStyle"];
            textBlock.HorizontalAlignment = HorizontalAlignment.Left;
            return textBlock;
        }

        /// <summary>
        /// This method creates and returns data grid with keyvault, type (hidden), displayname, alias, key/secret/certificate permissions columns
        /// </summary>
        /// <returns>Data Grid </returns>
        public DataGrid createDataGrid(bool type) {

            DataGrid dataGrid = new DataGrid();
            dataGrid.ColumnHeaderStyle = (Style)Resources["DataGridHeaderStyle"];
            dataGrid.AutoGenerateColumns = false;
            dataGrid.IsReadOnly = true;

            DataGridTextColumn columnKeyVault = new DataGridTextColumn();
            columnKeyVault.Header = "KeyVault";
            columnKeyVault.Binding = new System.Windows.Data.Binding("VaultName");
            columnKeyVault.Width = 250;
            columnKeyVault.FontSize = 18;
            columnKeyVault.FontFamily = new FontFamily("Sitka Text");
            columnKeyVault.FontWeight = FontWeights.Bold;
            dataGrid.Columns.Add(columnKeyVault);

            DataGridTemplateColumn SPColumn = new DataGridTemplateColumn();
            SPColumn.IsReadOnly = true;
            SPColumn.Width = 1050;
            SPColumn.Header = "Security Principal Permissions";
            FrameworkElementFactory nested = new FrameworkElementFactory(typeof(CustomDG));
            nested.SetValue(DataGrid.ColumnHeaderStyleProperty, (Style)Resources["DataGridSmallHeaderStyle"]);
            nested.SetBinding(DataGrid.ItemsSourceProperty, new Binding("SecurityPrincipals"));
            nested.SetValue(DataGrid.AutoGenerateColumnsProperty, true);
            nested.SetValue(DataGrid.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            nested.SetValue(DataGrid.CellStyleProperty, (Style)Resources["DataGridCellStyle"]);
            nested.SetValue(DataGrid.IsReadOnlyProperty, true);
            if (type)
            {
                nested.SetValue(CustomDG.ColumnWidthProperty, new DataGridLength(210));
            }
            else
            {
                nested.SetValue(CustomDG.ColumnWidthProperty, new DataGridLength(175));
            }
            
            SPColumn.CellTemplate = new DataTemplate() { VisualTree = nested };
            dataGrid.Columns.Add(SPColumn);
            return dataGrid;
        }

        
        internal class CustomDG : DataGrid
        {
            public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register("Columns", typeof(ObservableCollection<DataGridColumn>), typeof(CustomDG), new FrameworkPropertyMetadata(new ObservableCollection<DataGridColumn>()));
            public new ObservableCollection<DataGridColumn> Columns { get { return (ObservableCollection<DataGridColumn>)GetValue(ColumnsProperty); } set { SetValue(ColumnsProperty, value); } }
        }


        internal class SecurityPrincipalData
        {
            public string VaultName { get; set; }
            public List<SPPermissions> SecurityPrincipals { get; set; }

        }

        internal class NoTypeData
        {
            public string VaultName { get; set; }
            public List<NoTypePermissions> SecurityPrincipals { get; set; }
        }

        internal class SPPermissions : NoTypePermissions
        {
            public string Type { get; set; }

            public SPPermissions(string displayName, string alias,
                string[] keyPermissions, string[] secretPermissions, string[] certificatePermissions, string type) : base(displayName, alias, keyPermissions, secretPermissions, certificatePermissions)
            {
                this.Type = type;
            }
        }

        internal class NoTypePermissions
        {
            public string DisplayName { get; set; }
            public string Alias { get; set; }
            public string KeyPermissions { get; set; }
            public string SecretPermissions { get; set; }
            public string CertificatePermissions { get; set; }

            public NoTypePermissions(string displayName, string alias,
                string[] keyPermissions, string[] secretPermissions, string[] certificatePermissions)
            {
                this.DisplayName = displayName;
                this.Alias = alias;

                string keyString = "";
                for (int i = 0; i < keyPermissions.Length; i++)
                {
                    keyString += "- " + keyPermissions[i] + "\n";
                }
                this.KeyPermissions = keyString;


                string secretString = "";
                for (int i = 0; i < secretPermissions.Length; i++)
                {
                    secretString += "- " + secretPermissions[i] + "\n";
                }
                this.SecretPermissions = secretString;


                string certificateString = "";
                for (int i = 0; i < certificatePermissions.Length; i++)
                {
                    certificateString += "- " + certificatePermissions[i] + "\n";
                }
                this.CertificatePermissions = certificateString;
            }
        }
    }

    /// <summary>
    /// This class creates static instances of UpdatePoliciesFromYaml and the deserialized yaml.
    /// </summary>
    public static class DeserializedYaml
    {
        public static UpdatePoliciesFromYaml upInstance = new UpdatePoliciesFromYaml(false);
        public static List<KeyVaultProperties> Yaml = upInstance.deserializeYaml(Constants.YAML_FILE_PATH);
    }
}

