using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace DuneEdit
{
    class MapImage
    {
        public MapImage(BitmapImage img)
        {
            this.img = img;
            this.AdjustedHeight = 0;
            this.AdjustedWidth = 0;
            this.ImageScale = 0.27;
        }

        public void Adjust(double scaleFactor)
        {
            scaleFactor *= ImageScale;
            AdjustedHeight = img.Height * scaleFactor;
            AdjustedWidth = img.Width * scaleFactor;
        }

        public readonly BitmapImage img;
        public double AdjustedWidth, AdjustedHeight, ImageScale;
    }
}
