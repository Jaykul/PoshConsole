using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Management.Automation;

namespace PoshConsole.Controls
{
	public partial class ProgressPanel
	{
        
		#region [rgn] Fields (5)

		public static DependencyProperty ActivityProperty = DependencyProperty.RegisterAttached("Activity", typeof(string), typeof(ProgressPanel));
		public static DependencyProperty OperationProperty =
            DependencyProperty.Register("Operation",
            typeof(string), typeof(ProgressPanel));
		public static DependencyProperty PercentCompleteProperty =
            DependencyProperty.Register("PercentComplete",
            typeof(int), typeof(ProgressPanel));
		public static DependencyProperty StatusProperty =
            DependencyProperty.Register("Status",
            typeof(string), typeof(ProgressPanel));
		public static DependencyProperty TimeRemainingProperty =
            DependencyProperty.Register("TimeRemaining",
            typeof(TimeSpan), typeof(ProgressPanel));

		#endregion [rgn]

		#region [rgn] Constructors (1)

		public ProgressPanel()
		{
			this.InitializeComponent();

			// Insert code required on object creation below this point.
		}

      public ProgressPanel(ProgressRecord record) : this()
      {
         Record = record;
      }

		#endregion [rgn]

      private ProgressRecord _record;
      public ProgressRecord Record
      {
         get { return _record; }
         set {
            _record = value;
            Activity = _record.Activity;
            Status = _record.StatusDescription;
            Operation = _record.CurrentOperation;
            PercentComplete = _record.PercentComplete;
            TimeRemaining = TimeSpan.FromSeconds(_record.SecondsRemaining);
         }
      }

		#region [rgn] Properties (5)

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
		
		#endregion [rgn]

	}
}