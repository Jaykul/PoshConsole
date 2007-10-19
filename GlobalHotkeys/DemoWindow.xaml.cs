using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace GlobalHotkeys
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>

    public partial class DemoWindow : System.Windows.Window
    {

        public DemoWindow()
        {
            InitializeComponent();
            //new System.Windows.Input.ExecutedRoutedEventHandler(
            ApplicationCommands.Stop.CanExecute(null, this);
        }
    }
}