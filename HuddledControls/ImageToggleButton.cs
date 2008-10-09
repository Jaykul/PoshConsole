using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace Huddled.WPF.Controls
{
   public class ImageToggleButton : ToggleButton
   {
      static ImageToggleButton()
      {
         DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageToggleButton), new FrameworkPropertyMetadata(typeof(ImageToggleButton)));
      }

      public ImageSource DefaultImage
      {
         get { return (ImageSource)GetValue(DefaultImageProperty); }
         set { SetValue(DefaultImageProperty, value); }
      }

      // Using a DependencyProperty as the backing store for Default.  This enables animation, styling, binding, etc...
      public static readonly DependencyProperty DefaultImageProperty =
          DependencyProperty.Register("DefaultImage", typeof(ImageSource), typeof(ImageToggleButton), new UIPropertyMetadata(null));



      public ImageSource CheckedImage
      {
         get { return (ImageSource)GetValue(CheckedImageProperty); }
         set { SetValue(CheckedImageProperty, value); }
      }

      // Using a DependencyProperty as the backing store for Checked.  This enables animation, styling, binding, etc...
      public static readonly DependencyProperty CheckedImageProperty =
          DependencyProperty.Register("CheckedImage", typeof(ImageSource), typeof(ImageToggleButton), new UIPropertyMetadata(null));


      public Orientation Orientation
      {
         get { return (Orientation)GetValue(OrientationProperty); }
         set { SetValue(OrientationProperty, value); }
      }

      // Using a DependencyProperty as the backing store for Orientation.  This enables animation, styling, binding, etc...
      public static readonly DependencyProperty OrientationProperty =
          DependencyProperty.Register("Orientation", typeof(Orientation), typeof(ImageToggleButton), new UIPropertyMetadata(Orientation.Horizontal));

   }
}
