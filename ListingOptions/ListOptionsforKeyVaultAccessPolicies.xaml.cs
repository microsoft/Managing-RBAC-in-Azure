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
using System.Windows.Data;
using System.ComponentModel;
using Microsoft.Azure.Management.CosmosDB.Fluent.Models;
using System.IO;
using Microsoft.Graph;

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

                    if (scope != "YAML")
                    {
                        populateSelectedScopeBreakdown(yaml);
                        SelectedScopeBreakdownLabel.Visibility = Visibility.Visible;
                        SelectedScopeBreakdownDropdown.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        SelectedScopeBreakdownDropdown.SelectedIndex = -1;
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
        /// This method populates the selectedScope dropdown based off of the selected permissions breakdown scope item.
        /// </summary>
        /// <param name="yaml">The deserialized list of KeyVaultProperties objects</param>
        private void populateSelectedScopeBreakdown(List<KeyVaultProperties> yaml)
        {
            SelectedScopeBreakdownDropdown.Items.Clear();

            ComboBoxItem selectedScope = BreakdownScopeDropdown.SelectedItem as ComboBoxItem;
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
                SelectedScopeBreakdownDropdown.Items.Add(new CheckBox()
                {
                    Content = item
                });
            }
        }

        /// <summary>
        /// This method allows you to select multiple scopes and shows how many you selected on the ComboBox.
        /// </summary>
        /// <param name="sender">The permissions breakdown selected scope dropdown</param>
        /// <param name="e">The event that occurs when a selection changes</param>
        private void SelectedScopeBreakdownDropdown_DropDownClosed(object sender, EventArgs e)
        {
            ComboBox scopeDropdown = sender as ComboBox;
            ItemCollection items = scopeDropdown.Items;

            List<string> selected = getSelectedItemsScopeBreakdown(items);
            int numChecked = selected.Count();

            // Make the ComboBox show how many are selected
            items.Add(new ComboBoxItem()
            {
                Content = $"{numChecked} selected",
                Visibility = Visibility.Collapsed
            });
            scopeDropdown.Text = $"{numChecked} selected";
        }

        /// <summary>
        /// This method gets the list of selected items from the checkbox selected scope dropdown.
        /// </summary>
        /// <param name="items">The ItemCollection from the permissions breakdown selected scope dropdown</param>
        /// <returns>A list of the selected items</returns>
        private List<string> getSelectedItemsScopeBreakdown(ItemCollection items)
        {
            List<string> selected = new List<string>();
            try
            {
                ComboBoxItem selectedItem = (ComboBoxItem)SelectedScopeBreakdownDropdown.SelectedItem;
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
                    SelectedScopeBreakdownDropdown.SelectedIndex = -1;

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
        /// This method runs the permissions breakdown list option upon clicking the button.
        /// </summary>
        /// <param name="sender">The 'Run' button for the permissions breakdown</param>
        /// <param name="e">The event that occurs when the button is clicked</param>
        private void RunPermissionsBreakdown_Click(object sender, RoutedEventArgs e)
        {
            ComboBox scopeDropdown = SelectedScopeBreakdownDropdown as ComboBox;
            List<string> selected = getSelectedItemsScopeBreakdown(scopeDropdown.Items);

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
                throw new Exception("The YAML file path must be specified in the Constants.cs file. Please ensure this path is correct before proceeding.");
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
            var box = MostAccessedScopeDropdown.SelectedItem as ComboBoxItem;
            var choice = box.Content as string;
            if (MostAccessedSpecifyScopeLabel.Visibility == Visibility.Visible ||
               MostAccessedSpecifyScopeDropdown.Visibility == Visibility.Visible)
            {
                MostAccessedSpecifyScopeDropdown.Items.Clear();
                if(choice == "YAML")
                {
                    MostAccessedSpecifyScopeDropdown.Visibility = Visibility.Hidden;
                    MostAccessedSpecifyScopeLabel.Visibility = Visibility.Hidden;
                }
            }
            else
            {
                if (choice != "YAML")
                {
                    MostAccessedSpecifyScopeLabel.Visibility = Visibility.Visible;
                    MostAccessedSpecifyScopeDropdown.Visibility = Visibility.Visible;
                }
            }
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(false);
            List<KeyVaultProperties> yaml = up.deserializeYaml(Constants.YAML_FILE_PATH);
            if (choice == "KeyVault")
            {
                foreach(KeyVaultProperties kv in yaml)
                {
                    CheckBox item = new CheckBox();
                    item.Content = kv.VaultName;
                    MostAccessedSpecifyScopeDropdown.Items.Add(item);
                }
            }
            if(choice == "ResourceGroup")
            {
                var rgs = new HashSet<string>();
                foreach (KeyVaultProperties kv in yaml)
                {
                    if (!rgs.Contains(kv.ResourceGroupName))
                    {
                        rgs.Add(kv.ResourceGroupName);
                    }
                }
                foreach(string s in rgs)
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
                if(choice != "YAML")
                {
                    SecurityPrincipalAccessSpecifyScopeLabel.Visibility = Visibility.Visible;
                    SecurityPrincipalAccessSpecifyScopeDropdown.Visibility = Visibility.Visible;
                }
                
            }
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(false);
            List<KeyVaultProperties> yaml = up.deserializeYaml(Constants.YAML_FILE_PATH);
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
                ShorthandPermissionsTranslation.IsOpen = true;
            }
            else if (btn.Name == "PermissionsBySecurityPrincipalRun")
            {
                // Execute Code
                PermissionsBySecurityPrincipalPopUp.IsOpen = true;
                permissionsBySecurityPrincipalRunMethod();
            }
            else if (btn.Name == "PermissionsRun")
            {
                // Execute Code
            }
            else if (btn.Name == "BreakdownRun")
            {
                RunPermissionsBreakdown_Click(sender, e);
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

        private void RunTopSPs(object sender, RoutedEventArgs e)
        {
            ComboBoxItem breakdownScope = SecurityPrincipalAccessScopeDropdown.SelectedItem as ComboBoxItem;
            if (breakdownScope == null)
            {
                MessageBox.Show("Please specify scope type prior to hitting 'Run'.", "ScopeInvalid Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string scope = breakdownScope.Content as string;
            var cb = SecurityPrincipalAccessSpecifyScopeDropdown;
            List<string> selected = getSelectedItemsSPAccess(cb.Items);
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
                if(type == "Key Vault")
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
                    col4.Header = "Key Vaults Accessed";
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

        private List<TopSp> getTopSPs(List<KeyVaultProperties> vaults, string type)
        {
            if(type == "Key Vault")
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
                if(sps.Count > 10)
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
        }
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
        private void RunTopKVs(object sender, RoutedEventArgs e)
        {
            ComboBoxItem breakdownScope = MostAccessedScopeDropdown.SelectedItem as ComboBoxItem;
            if(breakdownScope == null)
            {
                MessageBox.Show("Please specify scope type prior to hitting 'Run'.", "ScopeInvalid Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string scope = breakdownScope.Content as string;
            var cb = MostAccessedSpecifyScopeDropdown;
            List<string> selected = getSelectedItemsMostAccessed(cb.Items);

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
                if(type == "Security Principal")
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
                    col1.Header = "Key Vault Name";
                    col1.Binding = new System.Windows.Data.Binding("VaultName");
                    col1.Width = 250;
                    TopKVGrid.Columns.Add(col1);

                    var col2 = new DataGridTextColumn();
                    col2.Header = "Security Principals with Access";
                    col2.Binding = new System.Windows.Data.Binding("SecurityPrincipals");
                    col2.Width = 250;
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
                    col1.Header = "Key Vault Name";
                    col1.Binding = new System.Windows.Data.Binding("VaultName");
                    col1.Width = 250;
                    TopKVGrid.Columns.Add(col1);

                    var col2 = new DataGridTextColumn();
                    col2.Header = "Permissions Granted in Key Vault";
                    col2.Binding = new System.Windows.Data.Binding("TotalPermissions");
                    col2.Width = 250;
                    TopKVGrid.Columns.Add(col2);

                    TopKVGrid.ItemsSource = fill;
                    TopKVResults.IsOpen = true;

                    
                }
            }
        }
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
        private List<KeyValuePair<string, int>> getTopKVs(List<KeyVaultProperties> vaults, string type)
        {
            if(type == "Security Principal")
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

        private List<KeyVaultProperties> getScopeKVs(string scope, List<string> selected)
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(false);
            var yamlVaults = up.deserializeYaml(Constants.YAML_FILE_PATH);
            var ret = new List<KeyVaultProperties>();
            if (scope == "YAML")
            {
                return yamlVaults;
            }
            else if(scope == "KeyVault")
            {
                foreach(var kv in yamlVaults)
                {
                    if (selected.Contains(kv.VaultName))
                    {
                        ret.Add(kv);
                    }
                }
            }
            else if(scope == "ResourceGroup")
            {
                foreach (var kv in yamlVaults)
                {
                    if (selected.Contains(kv.ResourceGroupName))
                    {
                        ret.Add(kv);
                    }
                }
            }
            else if (scope == "Subscription")
            {
                foreach (var kv in yamlVaults)
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

        private void MostAccessedSpecifyScopeDropdown_DropDownClosed(object sender, EventArgs e)
        {
            ComboBox scopeDropdown = sender as ComboBox;
            ItemCollection items = scopeDropdown.Items;

            List<string> selected = getSelectedItemsMostAccessed(items);
            int numChecked = selected.Count();

            // Make the ComboBox show how many are selected
            items.Add(new ComboBoxItem()
            {
                Content = $"{numChecked} selected",
                Visibility = Visibility.Collapsed
            });
            scopeDropdown.Text = $"{numChecked} selected";
        }

        private List<string> getSelectedItemsMostAccessed(ItemCollection items)
        {
            List<string> selected = new List<string>();
            try
            {
                ComboBoxItem selectedItem = MostAccessedSpecifyScopeDropdown.SelectedItem as ComboBoxItem;
                if (selectedItem != null && selectedItem.Content.ToString().EndsWith("selected"))
                {
                    items.RemoveAt(items.Count - 1);
                }
            }
            catch
            {
                try
                {
                    ComboBoxItem lastItem = items.GetItemAt(items.Count - 1) as ComboBoxItem;
                    MostAccessedSpecifyScopeDropdown.SelectedIndex = -1;

                    if (lastItem != null && lastItem.Content.ToString().EndsWith("selected"))
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

        private void MostAccessedTypeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MostAccessedScopeDropdown.Visibility = Visibility.Visible;
            MostAccessedScopeLabel.Visibility = Visibility.Visible;
        }

        private void CloseTopKVResults_Click(object sender, RoutedEventArgs e)
        {
            TopKVResults.IsOpen = false;
        }

        private void SecurityPrincipalAccessTypeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SecurityPrincipalAccessScopeLabel.Visibility = Visibility.Visible;
            SecurityPrincipalAccessScopeDropdown.Visibility = Visibility.Visible;
        }

        private void SecurityPrincipalAccessSpecifyScopeDropdown_DropDownClosed(object sender, EventArgs e)
        {
            ComboBox scopeDropdown = sender as ComboBox;
            ItemCollection items = scopeDropdown.Items;

            List<string> selected = getSelectedItemsSPAccess(items);
            int numChecked = selected.Count();

            // Make the ComboBox show how many are selected
            items.Add(new ComboBoxItem()
            {
                Content = $"{numChecked} selected",
                Visibility = Visibility.Collapsed
            });
            scopeDropdown.Text = $"{numChecked} selected";
        }

        private List<string> getSelectedItemsSPAccess(ItemCollection items)
        {
            List<string> selected = new List<string>();
            try
            {
                ComboBoxItem selectedItem = SecurityPrincipalAccessSpecifyScopeDropdown.SelectedItem as ComboBoxItem;
                if (selectedItem != null && selectedItem.Content.ToString().EndsWith("selected"))
                {
                    items.RemoveAt(items.Count - 1);
                }
            }
            catch
            {
                try
                {
                    ComboBoxItem lastItem = items.GetItemAt(items.Count - 1) as ComboBoxItem;
                    SecurityPrincipalAccessSpecifyScopeDropdown.SelectedIndex = -1;

                    if (lastItem != null && lastItem.Content.ToString().EndsWith("selected"))
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

        private void CloseTopSPResults_Click(object sender, RoutedEventArgs e)
        {
            TopSPResults.IsOpen = false;
        }

        // 2. Permissions By Security Principal  ----------------------------------------------------------------------------------------------------

        private void PermissionsBySecurityPrincipalScopeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PermissionsBySecurityPrincipalSpecifyScopeLabel.Visibility = Visibility.Hidden;
            PermissionsBySecurityPrincipalSpecifyScopeDropdown.Visibility = Visibility.Hidden;

            PermissionsBySecurityPrincipalTypeLabel.Visibility = Visibility.Hidden;
            PermissionsBySecurityPrincipalTypeDropdown.Visibility = Visibility.Hidden;

            PermissionsBySecurityPrincipalSpecifyTypeLabel.Visibility = Visibility.Hidden;
            PermissionsBySecurityPrincipalSpecifyTypeDropdown.Visibility = Visibility.Hidden;


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

                    if (scope != "YAML")
                    {
                        populatePermissionsBySecurityPrincipalSpecifyScope(yaml);
                        PermissionsBySecurityPrincipalSpecifyScopeLabel.Visibility = Visibility.Visible;
                        PermissionsBySecurityPrincipalSpecifyScopeDropdown.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        PermissionsBySecurityPrincipalSpecifyScopeDropdown.SelectedIndex = -1;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{ex.Message}", "FileNotFound Exception", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    PermissionsBySecurityPrincipalScopeDropdown.SelectedIndex = -1;
                    PermissionsBySecurityPrincipalScopeLabel.Visibility = Visibility.Hidden;
                    PermissionsBySecurityPrincipalScopeDropdown.Visibility = Visibility.Hidden;
                    PermissionsBySecurityPrincipalSpecifyScopeDropdown.SelectedIndex = -1;
                }
            }
        }
        private void populatePermissionsBySecurityPrincipalSpecifyScope(List<KeyVaultProperties> yaml)
        {
            PermissionsBySecurityPrincipalSpecifyScopeDropdown.Items.Clear();

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
        private void PermissionsBySecurityPrincipalSpecifyScopeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            PermissionsBySecurityPrincipalSpecifyTypeLabel.Visibility = Visibility.Hidden;
            PermissionsBySecurityPrincipalSpecifyTypeDropdown.Visibility = Visibility.Hidden;
        }

        private void PermissionsBySecurityPrincipalSpecifyScopeDropdown_DropDownClosed(object sender, EventArgs e)
        {
            ComboBox scopeDropdown = sender as ComboBox;
            ItemCollection items = scopeDropdown.Items;

            List<string> selected = getSelectedPermissionsBySecurityPrincipalSpecifyScope(items);
            int numChecked = selected.Count();

            // Make the ComboBox show how many are selected
            items.Add(new ComboBoxItem()
            {
                Content = $"{numChecked} selected",
                Visibility = Visibility.Collapsed
            });
            scopeDropdown.Text = $"{numChecked} selected";

            PermissionsBySecurityPrincipalTypeLabel.Visibility = Visibility.Visible;
            PermissionsBySecurityPrincipalTypeDropdown.Visibility = Visibility.Visible;
        }

        private List<string> getSelectedPermissionsBySecurityPrincipalSpecifyScope(ItemCollection items)
        {
            List<string> selected = new List<string>();
            try
            {
                ComboBoxItem selectedItem = PermissionsBySecurityPrincipalSpecifyScopeDropdown.SelectedItem as ComboBoxItem;
                if (selectedItem != null && selectedItem.Content.ToString().EndsWith("selected"))
                {
                    items.RemoveAt(items.Count - 1);
                }
            }
            catch
            {
                try
                {
                    ComboBoxItem lastItem = items.GetItemAt(items.Count - 1) as ComboBoxItem;
                    PermissionsBySecurityPrincipalSpecifyScopeDropdown.SelectedIndex = -1;

                    if (lastItem != null && lastItem.Content.ToString().EndsWith("selected"))
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

        private void PermissionsBySecurityPrincipalTypeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PermissionsBySecurityPrincipalSpecifyTypeDropdown.Items.Clear();

            if (PermissionsBySecurityPrincipalTypeDropdown.SelectedIndex != -1)
            {
                ComboBoxItem selectedtype = PermissionsBySecurityPrincipalTypeDropdown.SelectedItem as ComboBoxItem;
                string type = selectedtype.Content as string;

                ComboBoxItem selectedScope = PermissionsBySecurityPrincipalScopeDropdown.SelectedItem as ComboBoxItem;
                string scope = selectedScope.Content as string;
                // workin here

                ComboBox potentialSpecifyScope = PermissionsBySecurityPrincipalSpecifyScopeDropdown as ComboBox;
                ComboBox potentialSpecifyTypeScope = PermissionsBySecurityPrincipalSpecifyTypeDropdown as ComboBox;

                List<string> selectedSpecifyScopeItems = getSelectedPermissionsBySecurityPrincipalSpecifyScope(potentialSpecifyScope.Items);
               // List<string> selectedSpecifyTypeScopeItems = getSelectedPermissionsBySecurityPrincipalSpecifyType(potentialSpecifyTypeScope.Items);

                UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(false);
                List<KeyVaultProperties> yaml = up.deserializeYaml(Constants.YAML_FILE_PATH);

                List<string> items = new List<string>();

                if (scope == "Subscription")
                {
                    if (type == "All")
                    {
                        foreach (KeyVaultProperties kv in yaml)
                        {
                            if (selectedSpecifyScopeItems.Contains(kv.SubscriptionId))
                            {
                                foreach (PrincipalPermissions sp in kv.AccessPolicies)
                                {
                                    if (sp.Alias != " " && sp.Alias != "")
                                    {
                                        items.Add(sp.Alias);
                                    }
                                    else
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
                                    if (sp.Type == type)
                                    {
                                        if (sp.Alias != " " && sp.Alias != "")
                                        {
                                            items.Add(sp.Alias);
                                        }
                                        else
                                        {
                                            items.Add(sp.DisplayName);
                                        }
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
                                    if (sp.Alias != " " && sp.Alias != "")
                                    {
                                        items.Add(sp.Alias);
                                    }
                                    else
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
                                    if (sp.Type == type)
                                    {
                                        if (sp.Alias != " " && sp.Alias != "")
                                        {
                                            items.Add(sp.Alias);
                                        }
                                        else
                                        {
                                            items.Add(sp.DisplayName);
                                        }
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
                                    if (sp.Alias != " " && sp.Alias != "")
                                    {
                                        items.Add(sp.Alias);
                                    }
                                    else
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
                                    if (sp.Type == type)
                                    {
                                        if (sp.Alias != " " && sp.Alias != "")
                                        {
                                            items.Add(sp.Alias);
                                        }
                                        else
                                        {
                                            items.Add(sp.DisplayName);
                                        }

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

                // PermissionsBySecurityPrincipalSpecifyScopeDropdown.Text = selected";
                PermissionsBySecurityPrincipalSpecifyTypeLabel.Visibility = Visibility.Visible;
                PermissionsBySecurityPrincipalSpecifyTypeDropdown.Visibility = Visibility.Visible;
            }
        }
        private void PermissionsBySecurityPrincipalSpecifyTypeDropdown_DropDownClosed(object sender, EventArgs e)
        {

            ComboBox scopeDropdown = sender as ComboBox;
            ItemCollection items = scopeDropdown.Items;

            List<string> selected = getSelectedPermissionsBySecurityPrincipalSpecifyType(items);
            int numChecked = selected.Count();

            // Make the ComboBox show how many are selected
            items.Add(new ComboBoxItem()
            {
                Content = $"{numChecked} selected",
                Visibility = Visibility.Collapsed
            });
            scopeDropdown.Text = $"{numChecked} selected";

            PermissionsBySecurityPrincipalTypeLabel.Visibility = Visibility.Visible;
            PermissionsBySecurityPrincipalTypeDropdown.Visibility = Visibility.Visible;
        }

        private List<string> getSelectedPermissionsBySecurityPrincipalSpecifyType (ItemCollection items)
        {
            List<string> selected = new List<string>();
            try
            {
                ComboBoxItem selectedItem = PermissionsBySecurityPrincipalSpecifyTypeDropdown.SelectedItem as ComboBoxItem;
                if (selectedItem != null && selectedItem.Content.ToString().EndsWith("selected"))
                {
                    items.RemoveAt(items.Count - 1);
                }
            }
            catch
            {
                try
                {
                    ComboBoxItem lastItem = items.GetItemAt(items.Count - 1) as ComboBoxItem;
                    PermissionsBySecurityPrincipalSpecifyTypeDropdown.SelectedIndex = -1;

                    if (lastItem != null && lastItem.Content.ToString().EndsWith("selected"))
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

        private void ClosePermissionsBySecurityPrincipal_Clicked(object sender, RoutedEventArgs e)
        {
            PermissionsBySecurityPrincipalDataGrid.Items.Clear();          
            foreach(DataGridColumn col in PermissionsBySecurityPrincipalDataGrid.Columns)
            {
                if ( (string)col.Header == "Type")
                {
                    PermissionsBySecurityPrincipalDataGrid.Columns.Remove(col);
                    break;
                }
            }
            PermissionsBySecurityPrincipalPopUp.IsOpen = false;

            //PermissionsBySecurityPrincipalDataGrid.Columns.RemoveAt(2);
            //PermissionsBySecurityPrincipalDataGrid.Columns.Clear();
            // SecurityPrincipalDataGrid.Items.Refresh();
            // gvUsers.Columns["ID"].Visibility = false;
            // gvUsers.Columns.RemoveAt(IndexOfColumn);
        }

        public void permissionsBySecurityPrincipalRunMethod()
        {
            /*
            var columnKeyVault = new DataGridTextColumn();
            columnKeyVault.Header = "KeyVault";
            columnKeyVault.Binding = new System.Windows.Data.Binding("vaultName");
            PermissionsBySecurityPrincipalDataGrid.Columns.Add(columnKeyVault);

            var columnDisplayName = new DataGridTextColumn();
            columnDisplayName.Header = "Display Name";
            columnDisplayName.Binding = new System.Windows.Data.Binding("displayName");
            PermissionsBySecurityPrincipalDataGrid.Columns.Add(columnDisplayName);

            var columnAlias = new DataGridTextColumn();
            columnAlias.Header = "Alias";
            columnAlias.Binding = new System.Windows.Data.Binding("alias");
            PermissionsBySecurityPrincipalDataGrid.Columns.Add(columnAlias);

            var columnKeyPermissions = new DataGridTemplateColumn();
            columnKeyPermissions.Header = "Key Permissions";
            columnKeyPermissions.Width = 200;
        //  var listBox = columnKeyPermissions.CellTemplate.DataType = new ListBox();
            
            PermissionsBySecurityPrincipalDataGrid.Columns.Add(columnKeyPermissions);
            */

            ComboBoxItem selectedtype = PermissionsBySecurityPrincipalTypeDropdown.SelectedItem as ComboBoxItem;
            string type = selectedtype.Content as string;

            ComboBoxItem selectedScope = PermissionsBySecurityPrincipalScopeDropdown.SelectedItem as ComboBoxItem;
            string scope = selectedScope.Content as string;

            ComboBox potentialSpecifyScope = PermissionsBySecurityPrincipalSpecifyScopeDropdown as ComboBox;
            ComboBox potentialSpecifyType = PermissionsBySecurityPrincipalSpecifyTypeDropdown as ComboBox;

            List<string> selectedSpecifyScopeItems = getSelectedPermissionsBySecurityPrincipalSpecifyScope(potentialSpecifyScope.Items);
            List<string> selectedSpecifyTypeItems = getSelectedPermissionsBySecurityPrincipalSpecifyType(potentialSpecifyType.Items);

            // List<SecurityPrincipalData> spData = new List<SecurityPrincipalData>(); // ADDED CODE HEREEEE
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(false);
            List<KeyVaultProperties> yaml = up.deserializeYaml(Constants.YAML_FILE_PATH);

            if (scope == "Yaml")
            {

                foreach (KeyVaultProperties kv in yaml)
                {
                    foreach (PrincipalPermissions sp in kv.AccessPolicies)
                    {
                        SecurityPrincipalData newAddition = new SecurityPrincipalData(kv.VaultName, sp.DisplayName, sp.Alias,
                                    sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates);
                        PermissionsBySecurityPrincipalDataGrid.Items.Add(newAddition);
                    }
                }
            }

            else if (scope == "Subscription")
            {
                if (type == "All")
                {
                    var columnType = new DataGridTextColumn();
                    columnType.DisplayIndex = 1;
                    columnType.Header = "Type";
                    columnType.Binding = new System.Windows.Data.Binding("type");
                    columnType.Width = 120;

                    foreach (KeyVaultProperties kv in yaml)
                    {
                        if (selectedSpecifyScopeItems.Contains(kv.SubscriptionId))
                        {
                            foreach (PrincipalPermissions sp in kv.AccessPolicies)
                            {
                                if ((sp.Type == "User" || sp.Type == "Group") && selectedSpecifyTypeItems.Contains(sp.Alias))
                                {
                                    SecurityPrincipalData newAddition = new SecurityPrincipalData(kv.VaultName, sp.DisplayName, sp.Alias,
                                    sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates, sp.Type);
                                    PermissionsBySecurityPrincipalDataGrid.Items.Add(newAddition);
                                }
                                else if ((sp.Type == "Service Principal") && selectedSpecifyTypeItems.Contains(sp.DisplayName))
                                {
                                    SecurityPrincipalData newAddition = new SecurityPrincipalData(kv.VaultName, sp.DisplayName, "N/A",
                                        sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates, sp.Type);
                                    PermissionsBySecurityPrincipalDataGrid.Items.Add(newAddition);
                                }
                            }
                        }
                    }
                    PermissionsBySecurityPrincipalDataGrid.Columns.Add(columnType);
                }
                else
                {
                    foreach (KeyVaultProperties kv in yaml)
                    {
                        if (selectedSpecifyScopeItems.Contains(kv.SubscriptionId))
                        {
                            foreach (PrincipalPermissions sp in kv.AccessPolicies)
                            {
                                if (sp.Type == type && (type == "User" || type == "Group") && selectedSpecifyTypeItems.Contains(sp.Alias))
                                {
                                    SecurityPrincipalData newAddition = new SecurityPrincipalData(kv.VaultName, sp.DisplayName, sp.Alias,
                                    sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates);
                                    PermissionsBySecurityPrincipalDataGrid.Items.Add(newAddition);
                                }
                                else if (sp.Type == type && (type == "Service Principal") && selectedSpecifyTypeItems.Contains(sp.DisplayName))
                                {
                                    SecurityPrincipalData newAddition = new SecurityPrincipalData(kv.VaultName, sp.DisplayName, "N/A",
                                        sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates);
                                    PermissionsBySecurityPrincipalDataGrid.Items.Add(newAddition);
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
                    var columnType = new DataGridTextColumn();
                    columnType.DisplayIndex = 1;
                    columnType.Header = "Type";
                    columnType.Binding = new System.Windows.Data.Binding("type");
                    columnType.Width = 120;

                    foreach (KeyVaultProperties kv in yaml)
                    {
                        if (selectedSpecifyScopeItems.Contains(kv.ResourceGroupName))
                        {
                            foreach (PrincipalPermissions sp in kv.AccessPolicies)
                            {
                                if ((sp.Type == "User" || sp.Type == "Group") && selectedSpecifyTypeItems.Contains(sp.Alias))
                                {
                                    SecurityPrincipalData newAddition = new SecurityPrincipalData(kv.VaultName, sp.DisplayName, sp.Alias,
                                    sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates, sp.Type);
                                    PermissionsBySecurityPrincipalDataGrid.Items.Add(newAddition);
                                }
                                else if ((sp.Type == "Service Principal") && selectedSpecifyTypeItems.Contains(sp.DisplayName))
                                {
                                    SecurityPrincipalData newAddition = new SecurityPrincipalData(kv.VaultName, sp.DisplayName, "N/A",
                                        sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates, sp.Type);
                                    PermissionsBySecurityPrincipalDataGrid.Items.Add(newAddition);
                                }
                            }
                        }
                    }
                    PermissionsBySecurityPrincipalDataGrid.Columns.Add(columnType);
                }
                else
                {
                    foreach (KeyVaultProperties kv in yaml)
                    {
                        if (selectedSpecifyScopeItems.Contains(kv.ResourceGroupName))
                        {
                            foreach (PrincipalPermissions sp in kv.AccessPolicies)
                            {
                                if (sp.Type == type && (type == "User" || type == "Group") && selectedSpecifyTypeItems.Contains(sp.Alias))
                                {
                                    SecurityPrincipalData newAddition = new SecurityPrincipalData(kv.VaultName, sp.DisplayName, sp.Alias,
                                    sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates);
                                    PermissionsBySecurityPrincipalDataGrid.Items.Add(newAddition);
                                }
                                else if (sp.Type == type && (type == "Service Principal") && selectedSpecifyTypeItems.Contains(sp.DisplayName))
                                {
                                    SecurityPrincipalData newAddition = new SecurityPrincipalData(kv.VaultName, sp.DisplayName, "N/A",
                                        sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates);
                                    PermissionsBySecurityPrincipalDataGrid.Items.Add(newAddition);
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
                    var columnType= new DataGridTextColumn();
                    columnType.DisplayIndex = 1;
                    columnType.Header = "Type";
                    columnType.Binding = new System.Windows.Data.Binding("type");
                    columnType.Width = 120;
                    foreach (KeyVaultProperties kv in yaml)
                    {
                        if (selectedSpecifyScopeItems.Contains(kv.VaultName))
                        {
                            foreach (PrincipalPermissions sp in kv.AccessPolicies)
                            {
                                if ((sp.Type == "User" || sp.Type == "Group") && selectedSpecifyTypeItems.Contains(sp.Alias))
                                {
                                    SecurityPrincipalData newAddition = new SecurityPrincipalData(kv.VaultName, sp.DisplayName, sp.Alias,
                                    sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates, sp.Type);
                                    PermissionsBySecurityPrincipalDataGrid.Items.Add(newAddition);
                                }
                                else if ((sp.Type == "Service Principal") && selectedSpecifyTypeItems.Contains(sp.DisplayName))
                                {
                                    SecurityPrincipalData newAddition = new SecurityPrincipalData(kv.VaultName, sp.DisplayName, "N/A",
                                        sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates, sp.Type);
                                    PermissionsBySecurityPrincipalDataGrid.Items.Add(newAddition);
                                }
                            }
                        }
                    }
                    PermissionsBySecurityPrincipalDataGrid.Columns.Add(columnType);
                }
                else
                {
                    foreach (KeyVaultProperties kv in yaml)
                    {
                        if (selectedSpecifyScopeItems.Contains(kv.VaultName))
                        {
                            foreach (PrincipalPermissions sp in kv.AccessPolicies)
                            {
                                if (sp.Type == type && (type == "User" || type == "Group") && selectedSpecifyTypeItems.Contains(sp.Alias))
                                {
                                    SecurityPrincipalData newAddition = new SecurityPrincipalData(kv.VaultName, sp.DisplayName, sp.Alias,
                                    sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates);
                                    PermissionsBySecurityPrincipalDataGrid.Items.Add(newAddition);
                                }
                                else if (sp.Type == type && (type == "Service Principal") && selectedSpecifyTypeItems.Contains(sp.DisplayName))
                                {
                                    SecurityPrincipalData newAddition = new SecurityPrincipalData(kv.VaultName, sp.DisplayName, "N/A",
                                        sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates);
                                    PermissionsBySecurityPrincipalDataGrid.Items.Add(newAddition);
                                }
                            }
                        }
                    }
                }
            }
        }

        public class SecurityPrincipalData
        {
            public string vaultName { get; set; }
            public string displayName { get; set; }
            public string alias { get; set; }
            public List<string> keyPermissions { get; set; }
            public List<string> secretPermissions { get; set; }
            public List<string> certificatePermissions { get; set; }
            public string type { get; set; }

            public SecurityPrincipalData(string vaultName, string displayName, string alias,
                string[] keyPermissions, string[] secretPermissions, string[] certificatePermissions, string type=null)
            {
                this.vaultName = vaultName;
                this.displayName = displayName;
                this.alias = alias;
                this.type = type;

                List<string> keyList = new List<string>();
                for (int i = 0; i < keyPermissions.Length; i++)
                {
                    keyList.Add("- " + keyPermissions[i]);
                }
                this.keyPermissions = keyList;


                List<string> secretList = new List<string>();
                for (int i = 0; i < secretPermissions.Length; i++)
                {
                    secretList.Add("- " + secretPermissions[i]);
                }
                this.secretPermissions = secretList;


                List<string> certificateList = new List<string>();
                for (int i = 0; i < certificatePermissions.Length; i++)
                {
                    certificateList.Add("- " + certificatePermissions[i]);
                }
                this.certificatePermissions = certificateList;
            }

        }
    }
}
