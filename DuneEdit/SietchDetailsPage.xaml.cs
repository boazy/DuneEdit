using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DuneEdit
{
    /// <summary>
    /// Interaction logic for Page1.xaml
    /// </summary>
    public partial class SietchDetailsPage : Page
    {
        public SietchDetailsPage()
        {
            InitializeComponent();
            InitDetails();
        }

        private void InitDetails()
        {
            // Main details
            AddDetailEdit("Spice Density", "Spice");
            AddDetailEdit("Harvesters", "Harvesters");
            AddDetailEdit("Ornis", "Ornis");
            AddDetailEdit("Krys", "Krys");
            AddDetailEdit("Laserguns", "Laserguns");
            AddDetailEdit("Wierding Modules", "WierdingModules");
            AddDetailEdit("Atomics", "Atomics");
            AddDetailEdit("Bulbs", "Bulbs");
            AddDetailEdit("Water", "Water");

            // Boolean fields
            AddDetailCheck("Has Vegetation", "Vegetation");
            AddDetailCheck("Under Attack", "UnderAttack");
            AddDetailCheck("Infiltrated", "Infiltrated");
            AddDetailCheck("Battle Won", "BattleWon");
            AddDetailCheck("Inventory Visible", "InventoryVisible");
            AddDetailCheck("Has Windtrap", "HasWindtrap");
            AddDetailCheck("Prospected", "Prospected");
            AddDetailCheck("Discovered", "Discovered");

            // Advanced fields
            AddDetailSubTitle("Advanced");
            AddDetailEdit("Map X Pos.", "MapPosX");
            AddDetailEdit("Map Y Pos.", "MapPosY");
            AddDetailEdit("Desert Around", "DesertAroundSietch");
            AddDetailEdit("Location Type", "LocationType");

            // Unknown fields
            AddDetailSubTitle("Unknown fields");
            AddDetailEdit("PosX", "PosX");
            AddDetailEdit("PosY", "PosY");
            AddDetailEdit("Spice Field", "SpiceFieldId");
            AddDetailEdit("Unknown 05", "Unk05");
            AddDetailEdit("Unknown 0B", "Unk0B");
            AddDetailEdit("Unknown 0C", "Unk0C");
            AddDetailEdit("Unknown 0D", "Unk0D");
            AddDetailEdit("Unknown 0E", "Unk0E");
            AddDetailEdit("Unknown 0F", "Unk0F");
            AddDetailEdit("Unknown 11", "Unk11");
            AddDetailEdit("Unknown 13", "Unk13");
        }

        private int AddDetailRow()
        {
            var rowDef = new RowDefinition();
            rowDef.Height = new System.Windows.GridLength(0, GridUnitType.Auto);
            int row = detailsPanelData.RowDefinitions.Count - 1;
            detailsPanelData.RowDefinitions.Insert(row - 1, rowDef);

            return row;
        }

        private void AddDetailEdit(string title, string bindingPath)
        {
            int row = AddDetailRow();

            // Detail label
            var label = new Label();
            label.SetValue(Grid.RowProperty, row);
            label.Style = (Style)detailsPanelData.Resources["DetailsValueName"];
            label.Content = title;
            detailsPanelData.Children.Add(label);

            // Textbox binding
            var binding = new Binding(bindingPath);
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

            // Textbox for editing
            var textbox = new TextBox();
            textbox.SetValue(Grid.RowProperty, row);
            textbox.Style = (Style)detailsPanelData.Resources["DetailsValueText"];
            textbox.SetBinding(TextBox.TextProperty, binding);
            detailsPanelData.Children.Add(textbox);
        }

        private void AddDetailCheck(string title, string bindingPath)
        {
            int row = AddDetailRow();

            // Detail label
            var label = new Label();
            label.SetValue(Grid.RowProperty, row);
            label.Style = (Style)detailsPanelData.Resources["DetailsValueName"];
            label.Content = title;
            detailsPanelData.Children.Add(label);

            // Textbox for editing
            var checkbox = new CheckBox();
            checkbox.SetValue(Grid.RowProperty, row);
            checkbox.Style = (Style)detailsPanelData.Resources["DetailsValueCheck"];
            checkbox.SetBinding(CheckBox.IsCheckedProperty, new Binding(bindingPath));
            detailsPanelData.Children.Add(checkbox);
        }

        private void AddDetailSubTitle(string title)
        {
            int row = AddDetailRow();

            // Detail label
            var label = new Label();
            label.SetValue(Grid.RowProperty, row);
            label.Style = (Style)detailsPanelData.Resources["DetailsSubTitle"];
            label.Content = title;
            detailsPanelData.Children.Add(label);
        }
    }
}
