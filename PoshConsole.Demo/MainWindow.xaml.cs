using System;
using System.Windows;
using System.Windows.Media;
using Fluent;

namespace PoshConsole.Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ZoomSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TextOptions.SetTextFormattingMode(this, e.NewValue > 1.0 ? TextFormattingMode.Ideal : TextFormattingMode.Display);
        }

        protected override void OnInitialized(EventArgs e)
        {
            // This is here just to make sure we can run commands in this event handler!
            PoshConsole.ExecuteCommand("Write-Output $PSVersionTable");
            base.OnInitialized(e);
        }
    }
}
