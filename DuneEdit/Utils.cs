using System;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace DuneEdit
{
    public static class ExtUtils
    {
        public static void ScaleTo(this Image img, double height, double width, double seconds = 0.5)
        {
            AnimationTimeline scaleX = new DoubleAnimation(img.Height, height, TimeSpan.FromSeconds(seconds));
            AnimationTimeline scaleY = new DoubleAnimation(img.Width, width, TimeSpan.FromSeconds(seconds));
            img.BeginAnimation(Image.HeightProperty, scaleX);
            img.BeginAnimation(Image.WidthProperty, scaleY);
        }

        public static void ScaleBack(this Image img, double seconds = 0.5)
        {
            double origHeight = (double)img.GetAnimationBaseValue(Image.HeightProperty);
            double origWidth = (double)img.GetAnimationBaseValue(Image.WidthProperty);
            AnimationTimeline scaleX = new DoubleAnimation(img.Height, origHeight, TimeSpan.FromSeconds(seconds));
            AnimationTimeline scaleY = new DoubleAnimation(img.Width, origWidth, TimeSpan.FromSeconds(seconds));
            img.BeginAnimation(Image.HeightProperty, scaleX);
            img.BeginAnimation(Image.WidthProperty, scaleY);
        }
    }
}
