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

namespace PoshCode.Wpf.Controls
{
   public class ImageToggleButton : ToggleButton
   {
      static ImageToggleButton()
      {
         DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageToggleButton), new FrameworkPropertyMetadata(typeof(ImageToggleButton)));
      }

      #region DefaultImage
      public ImageSource DefaultImage
      {
         get { return (ImageSource)GetValue(DefaultImageProperty); }
         set { SetValue(DefaultImageProperty, value); }
      }

      // Using a DependencyProperty as the backing store for Default.  This enables animation, styling, binding, etc...
      public static readonly DependencyProperty DefaultImageProperty =
          DependencyProperty.Register("DefaultImage", typeof(ImageSource), typeof(ImageToggleButton), new UIPropertyMetadata(null));
      #endregion DefaultImage

      #region CheckedImage
      public ImageSource CheckedImage
      {
         get { return (ImageSource)GetValue(CheckedImageProperty); }
         set { SetValue(CheckedImageProperty, value); }
      }

      // Using a DependencyProperty as the backing store for Checked.  This enables animation, styling, binding, etc...
      public static readonly DependencyProperty CheckedImageProperty =
          DependencyProperty.Register("CheckedImage", typeof(ImageSource), typeof(ImageToggleButton), new UIPropertyMetadata(null));
      #endregion CheckedImage

      #region Orientation
      public Orientation Orientation
      {
         get { return (Orientation)GetValue(OrientationProperty); }
         set { SetValue(OrientationProperty, value); }
      }

      // Using a DependencyProperty as the backing store for Orientation.  This enables animation, styling, binding, etc...
      public static readonly DependencyProperty OrientationProperty =
          DependencyProperty.Register("Orientation", typeof(Orientation), typeof(ImageToggleButton), new UIPropertyMetadata(Orientation.Horizontal));
      #endregion Orientation


      private Image _image = new Image();

      protected override void OnInitialized(EventArgs e)
      {
         if (Content == null)
         {
            Content = _image;
            // TODO: This control would be a lot more useful if the size wasn't fixed
            _image.Width = 16;
            _image.Height = 16;
         }

         _image.Source = (this.IsChecked.GetValueOrDefault(false)) ? CheckedImage : DefaultImage;

         base.OnInitialized(e);
      }

      protected override void OnContentChanged(object oldContent, object newContent)
      {
         base.OnContentChanged(oldContent, newContent);
      }

      protected override void OnChecked(RoutedEventArgs e)
      {
         _image.Source = CheckedImage;
         base.OnChecked(e);
      }

      protected override void OnUnchecked(RoutedEventArgs e)
      {
         _image.Source = DefaultImage;
         base.OnUnchecked(e);
      }
   }
}
