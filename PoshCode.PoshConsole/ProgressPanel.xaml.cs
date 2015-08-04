using System;
using System.Management.Automation;
using System.Windows;

namespace PoshCode.Controls
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
            InitializeComponent();

            // Insert code required on object creation below this point.
        }

        public ProgressPanel(ProgressRecord record)
            : this()
        {
            Record = record;
        }

        #endregion [rgn]

        private ProgressRecord _record;
        public ProgressRecord Record
        {
            get { return _record; }
            set
            {
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
            set
            {
                activity.Visibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;

                SetValue(ActivityProperty, value);
            }
        }

        public string Operation
        {
            get { return ((string)GetValue(OperationProperty)); }
            set
            {
                operation.Visibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;

                SetValue(OperationProperty, value);
            }
        }

        public int PercentComplete
        {
            get { return ((int)GetValue(PercentCompleteProperty)); }
            set
            {
                progressBar.Visibility = value <= 0 ? Visibility.Collapsed : Visibility.Visible;

                SetValue(PercentCompleteProperty, value);
            }
        }

        public string Status
        {
            get { return ((string)GetValue(StatusProperty)); }
            set
            {
                status.Visibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;

                SetValue(StatusProperty, value);
            }
        }

        public TimeSpan TimeRemaining
        {
            get { return ((TimeSpan)GetValue(TimeRemainingProperty)); }
            set
            {
                secondsRemaining.Visibility = value.TotalSeconds <= 0
                                                ? Visibility.Collapsed
                                                : Visibility.Visible;

                SetValue(TimeRemainingProperty, value);
            }
        }

        #endregion [rgn]

    }
}