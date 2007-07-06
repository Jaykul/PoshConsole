using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

namespace Huddled.PoshConsole
{
	public partial class ProgressPanel
	{

        public static DependencyProperty ActivityProperty = DependencyProperty.RegisterAttached("Activity", typeof(string), typeof(ProgressPanel));

        public string Activity
        {
            get { return (string)GetValue(ActivityProperty); }
            set {
                if (string.IsNullOrEmpty(value)) {
                    this.activity.Visibility = Visibility.Collapsed;
                } else {
                    this.activity.Visibility = Visibility.Visible;
                }

                SetValue(ActivityProperty, value); 
            
            }
        }

        public static DependencyProperty StatusProperty =
            DependencyProperty.Register("Status",
            typeof(string), typeof(ProgressPanel));

        public string Status
        {
            get { return ((string)base.GetValue(StatusProperty)); }
            set {
                if (string.IsNullOrEmpty(value))
                {
                    this.status.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.status.Visibility = Visibility.Visible;
                }

                base.SetValue(StatusProperty, value); }
        }

        public static DependencyProperty OperationProperty =
            DependencyProperty.Register("Operation",
            typeof(string), typeof(ProgressPanel));

        public string Operation
        {
            get { return ((string)base.GetValue(OperationProperty)); }
            set {
                if (string.IsNullOrEmpty(value))
                {
                    this.operation.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.operation.Visibility = Visibility.Visible;
                }                
                
                base.SetValue(OperationProperty, value); }
        }

        public static DependencyProperty PercentCompleteProperty =
            DependencyProperty.Register("PercentComplete",
            typeof(int), typeof(ProgressPanel));

        public int PercentComplete
        {
            get { return ((int)base.GetValue(PercentCompleteProperty)); }
            set {
                if (value <= 0)
                {
                    this.progressBar.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.progressBar.Visibility = Visibility.Visible;
                }

                base.SetValue(PercentCompleteProperty, value); }
        }

        public static DependencyProperty TimeRemainingProperty =
            DependencyProperty.Register("TimeRemaining",
            typeof(TimeSpan), typeof(ProgressPanel));

        public TimeSpan TimeRemaining
        {
            get { return ((TimeSpan)base.GetValue(TimeRemainingProperty)); }
            set {
                if (value == null || value.TotalSeconds <= 0)
                {
                    this.secondsRemaining.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.secondsRemaining.Visibility = Visibility.Visible;
                }
                
                base.SetValue(TimeRemainingProperty, value); }
        }

		public ProgressPanel()
		{
			this.InitializeComponent();

			// Insert code required on object creation below this point.
		}
	}
}