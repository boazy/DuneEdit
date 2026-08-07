using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;
using System.Windows.Media;

namespace DuneEdit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static MainWindow()
        {
            selectionEffect.GlowColor = Colors.DeepSkyBlue;
        }

        private static SelectionEffect selectionEffect = new SelectionEffect();

        public MainWindow()
        {
            InitializeComponent();
            InitMapImages(new string[] {"Sietch", "Arrakeen", "Carthag", "Village", "Fort", "Unknown"});
        }

        private void InitMapImages(string[] names)
        {
            foreach (string name in names)
            {
                mapImages[name] = new MapImage((BitmapImage)Resources[name]);
            }

            // A slightly larger scale for sietches.
            mapImages["Sietch"].ImageScale = 0.35;
        }

        private int ConvertCoord(byte coord, double max, byte bmax = 255)
        {
            double margin = 10 * scaleFactor;
            max -= margin * 2;
            double adjusted = margin + System.Math.Round(((double)coord / bmax) * max);
            return Convert.ToInt32(adjusted);
        }

        private string GetSietchImageName(Sietch sietch)
        {
            return sietch.LocationTypeGroup;
        }

        private void SelectImage(Image img, MapImage src)
        {
            if (selectedImage != null)
            {
                selectedImage.ScaleBack();
                selectedImage.Effect = null;
            }

            img.Effect = selectionEffect;
            img.ScaleTo(src.AdjustedHeight * 1.25, src.AdjustedWidth * 1.25);

            // Set new selected image
            selectedImage = img;
        }

        private void DrawSietch(Sietch sietch)
        {
            byte bx = sietch.MapPosX;
            byte by = sietch.MapPosY;
            
            if (by > 180)
                by -= 180;
            else
                by = by += 75;
            
            int x = ConvertCoord(bx, map.ActualWidth);
            int y = ConvertCoord(by, map.ActualHeight, 150);

            var mapImg = mapImages[GetSietchImageName(sietch)];
            var adjustedHeight = mapImg.AdjustedHeight;
            var adjustedWidth = mapImg.AdjustedWidth;

            var img = new Image();
            img.Source = mapImg.img;
            img.Height = adjustedHeight;
            img.Width = adjustedWidth;

            Canvas.SetTop(img, y - (img.Height / 2));
            Canvas.SetLeft(img, x - (img.Width / 2));
            var tt = new ToolTip();
            tt.Content = sietch.Name;
            img.ToolTip = tt;
            img.Cursor = Cursors.Hand;
            img.MouseUp += (object sender, MouseButtonEventArgs e) =>
            {
                SelectImage(img, mapImg);
                CurrentSietch = sietch;
                ShowDetailsPanel();
            };

            map.Children.Add(img);
        }

        private void DumpSietch(Sietch sietch)
        {
            var s = String.Join(" ", sietch.RawData.Select((byte b) => { return String.Format("{0:X2}", b); }));
            Clipboard.SetText(s);
            MessageBox.Show(s);
        }

        private void DrawMap()
        {
            scaleFactor = ActualHeight / MinHeight;
            foreach (var mapImg in mapImages.Values)
                mapImg.Adjust(scaleFactor);
            map.Children.Clear();
            foreach (var sietch in savegame.sietchesMatrix)
            {
                if (sietch != null)
                    DrawSietch(sietch);
            }
        }

        private void OpenCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".sav";
            dlg.Filter = "Savegames (.sav)|*.sav";

            // Show open file dialog box
            bool? result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                savegame = new DuneSavegame();
                savegame.Load(new FileInfo(dlg.FileName));
                CurrentSietch = null;
                HideDetailsPanel();
                DrawMap();
            }
        }

        private void SaveCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            savegame.Save();
        }

        private void map_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (savegame != null)
                DrawMap();
        }


        private void btnHideDetails_Click(object sender, RoutedEventArgs e)
        {
            HideDetailsPanel();
        }

        public bool DetailsPanelVisible
        {
            get
            {
                return detailsPanel.Visibility == Visibility.Visible;
            }
        }

        private void ToggleDetailsPanel(object sender, RoutedEventArgs e)
        {
            if (DetailsPanelVisible)
                HideDetailsPanel();
            else
                ShowDetailsPanel();
        }

        private void HideDetailsPanel()
        {
            if (DetailsPanelVisible)
            {
                // Hide
                detailsPanel.Visibility = Visibility.Collapsed;
                Splitter.Visibility = Visibility.Collapsed;
                detailsColumnWidth = DetailsColumn.Width;
                DetailsColumn.Width = new GridLength(0);
                Width -= detailsColumnWidth.Value + Splitter.Width;
            }
        }

        private void ShowDetailsPanel()
        {
            if (!DetailsPanelVisible && (CurrentSietch != null))
            {
                // Show
                detailsPanel.Visibility = Visibility.Visible;
                Splitter.Visibility = Visibility.Visible;
                DetailsColumn.Width = detailsColumnWidth;
                Width += detailsColumnWidth.Value + Splitter.Width;
                detailsPanelFrame.Navigate(new Uri("SietchDetailsPage.xaml", UriKind.Relative));

                // A workaround to use when loading the panel frame for the first time.
                // Otherwise the sietch information would not be updated.B
                detailsPanelFrame.LoadCompleted += (sender, e) =>
                {
                    Sietch current = CurrentSietch;
                    CurrentSietch = null;
                    CurrentSietch = current;
                };
            }
        }

        private Sietch CurrentSietch
        {
            get
            {
                return (Sietch)detailsPanel.DataContext;
            }
            set
            {
                detailsPanel.DataContext = value;
            }
        }

        private GridLength detailsColumnWidth = new GridLength(215);

        private DuneSavegame savegame;
        private Dictionary<string, MapImage> mapImages = new Dictionary<string, MapImage>();
        private double scaleFactor;
        private Image selectedImage;

        private void TestClick(object sender, RoutedEventArgs e)
        {
        }
    }
}
