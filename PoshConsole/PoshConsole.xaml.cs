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
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using Huddled.Hotkeys;
using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using System.Diagnostics;

namespace Huddled.PoshConsole
{

	/// <summary>
	/// Implementation of a WPF host for PowerShell
	/// </summary>
	public partial class PoshConsole : System.Windows.Window
	{

		/// <summary>
		/// The PSHost implementation for this interpreter.
		/// </summary>
		private PoshHost myHost;

		/// <summary>
		/// A global hotkey manager
		/// </summary>
		WPFHotkeyManager hkManager;

		/// <summary>
		/// A hotkey used to bring our window into focus
		/// </summary>
		Hotkey FocusKey;

		double characterWidth = 1.0;

        // Universal Delegates
        delegate void passDelegate<T>(T input);
        delegate RET returnDelegate<RET>();
        delegate RET passReturnDelegate<T,RET>(T input);
        

		/// <summary>
		/// Initializes a new instance of the <see cref="PoshConsole"/> class.
		/// </summary>
		public PoshConsole()
		{
			// Create the host and runspace instances for this interpreter. Note that
			// this application doesn't support console files so only the default snapins
			// will be available.

			InitializeComponent();

			// before we start animating, set the animation endpoints to the current values.
			hideOpacityAnimations.From = showOpacityAnimation.To = Opacity;
			hideHeightAnimations.From = showHeightAnimation.To = this.Height;

            buffer.InputHandler = new InputHandler();

            try
            {
                myHost = new PoshHost(buffer);
            }
            catch
            {
                MessageBox.Show("Can't create PowerShell interface, are you sure PowerShell is installed?", "Error Starting PoshConsole", MessageBoxButton.OK, MessageBoxImage.Stop);
                Application.Current.Shutdown(1);
            }

            Binding statusBinding = new Binding("StatusText");
            statusBinding.Source = myHost.Options;
            // StatusBarItems:: Title, Separator, Admin, Separator, Status
            StatusBarItem el = status.Items[status.Items.Count - 1] as StatusBarItem;
            if (el != null)
            {
                el.SetBinding(StatusBarItem.ContentProperty, statusBinding);
            }

            // buffer.TitleChanged += new passDelegate<string>(delegate(string val) { Title = val; });
            Properties.Settings.Default.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(SettingsPropertyChanged);

            buffer.GotProgressUpdate += new WriteProgressDelegate(OnProgressUpdate);

            if (Win32.Application.IsUserAnAdmin())
            {
                // StatusBarItems:: Title, Separator, Admin, Separator, Status
                el = status.Items[2] as StatusBarItem;
                if( el != null ) {
                    el.Content = "Elevated!";
                    el.Foreground = new SolidColorBrush(Color.FromRgb((byte)204, (byte)119, (byte)17));
                    el.ToolTip = "PoshConsole is running as Administrator";
                    el.Cursor = this.Cursor;
                }
            }

            // problems with data binding 
            WindowStyle = Properties.Settings.Default.WindowStyle;
            if (Properties.Settings.Default.WindowStyle == WindowStyle.None)
            {
                AllowsTransparency = true;
                ResizeMode = ResizeMode.CanResizeWithGrip;
            }
            else
            {
                // we ignore the border if the windowstyle isn't "None"
                border.BorderThickness = new Thickness(0D, 0D, 0D, 0D);
                ResizeMode = ResizeMode.CanResize;
            }
            
		}

        void HandleIncreaseZoom(object sender, ExecutedRoutedEventArgs e)
        {
            buffer.FontSize += 1;
            RecalculateSizes();
        }

        void HandleDecreaseZoom(object sender, ExecutedRoutedEventArgs e)
        {
            buffer.FontSize -= 1;
            RecalculateSizes();
        }

        void HandleSetZoom(object sender, ExecutedRoutedEventArgs e)
        {
            double zoom;
            if (e.Parameter is double)
            {
                zoom = (double)e.Parameter;
                buffer.FontSize = zoom * Properties.Settings.Default.FontSize;
            }else if (e.Parameter is string && double.TryParse(e.Parameter.ToString(), out zoom)) {
                buffer.FontSize = zoom * Properties.Settings.Default.FontSize;
            }
            RecalculateSizes();
        }
        void OnProgressUpdate(long sourceId, ProgressRecord record)
        {
            if (!buffer.Dispatcher.CheckAccess())
            {
                buffer.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new WriteProgressDelegate(OnProgressUpdate), sourceId, record);
            }
            else
            {
                if (record.RecordType == ProgressRecordType.Completed)
                {
                    progress.Visibility = Visibility.Collapsed;
                }
                else
                {
                    progress.Visibility = Visibility.Visible;

                    progress.Activity = record.Activity;
                    progress.Status = record.StatusDescription;
                    progress.Operation = record.CurrentOperation;
                    progress.PercentComplete = record.PercentComplete;
                    progress.TimeRemaining = TimeSpan.FromSeconds(record.SecondsRemaining);
                }
            }
        }
		/// <summary>
		/// Recalculates various sizes that PSRawUI needs
		/// <remarks>This should be called any time the font or window size change</remarks>
		/// </summary>
		private void RecalculateSizes()
		{
			int imHigh = (int)(double.IsPositiveInfinity(buffer.MaxHeight) ? System.Windows.SystemParameters.MaximumWindowTrackHeight : buffer.MaxHeight);
			int imWide = (int)(double.IsPositiveInfinity(buffer.MaxWidth) ? System.Windows.SystemParameters.MaximumWindowTrackWidth : buffer.MaxWidth);
			double fontSize = (buffer.FontSize * characterWidth); // (72/96) convert from points to device independent pixels ... 

			//int linHeight = (int)(double.IsNaN(buffer.Document.LineHeight) ? fontSize : buffer.Document.LineHeight);

			//System.Drawing.Font f = new System.Drawing.Font(buffer.FontFamily, buffer.FontSize, buffer.FontStyle, System.Drawing.GraphicsUnit.Point);
			//TextRenderer.MeasureText("W", f).Width

			//myRawUI._BufferSize = buffer.BufferSize;

			//myRawUI._WindowSize = buffer.WindowSize;
            ((PoshRawUI)myHost.UI.RawUI)._MaxWindowSize = buffer.MaxWindowSize;

			// TODO: What's the difference between MaxWindowSize and MaxPhysicalWindowSize?
            ((PoshRawUI)myHost.UI.RawUI)._MaxPhysicalWindowSize = buffer.MaxPhysicalWindowSize;

			//myRawUI._CursorPosition = buffer.CursorPosition;

			//myRawUI._WindowPosition = new Coordinates((int)this.Left, (int)this.Top);

		}

		private void buffer_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			RecalculateSizes();
		}

		private void buffer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			if((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
			{
				buffer.FontSize += (e.Delta > 0) ? 1 : -1;
				RecalculateSizes();
				e.Handled = true;
			}
		}

        private delegate void SettingsChangedDelegate(object sender, System.ComponentModel.PropertyChangedEventArgs e);
        void SettingsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!buffer.Dispatcher.CheckAccess())
            {
                buffer.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new SettingsChangedDelegate(SettingsPropertyChanged), sender, new object[] { e });
                return;
            }

            switch (e.PropertyName)
            {
                case "ShowInTaskbar":
                    {
                        this.ShowInTaskbar = Properties.Settings.Default.ShowInTaskbar;
                    } break;
                case "StatusBar":
                    { 
                        status.Visibility = Properties.Settings.Default.StatusBar ? Visibility.Visible : Visibility.Collapsed;
                    } break;
                case "WindowHeight":
                    {
                        this.Height = Properties.Settings.Default.WindowHeight;
                    } break;
                case "WindowLeft":
                    {
                        this.Left = Properties.Settings.Default.WindowLeft;
                    } break;
                case "WindowWidth":
                    {
                        this.Width = Properties.Settings.Default.WindowWidth;
                    } break;
                case "WindowTop":
                    {
                        this.Top = Properties.Settings.Default.WindowTop;
                    } break;
                case "Animate":
                    {
                        // do nothing, this setting is checked for each animation.
                    } break;
                case "AutoHide":
                    {
                        // do nothing, this setting is checked for each hide event.
                    } break;
                case "SnapToScreenEdge":
                    {
                        // do nothing, this setting is checked for each move
                    } break;
                case "SnapDistance":
                    {
                        // do nothing, this setting is checked for each move
                    } break;
                case "AlwaysOnTop":
                    {
                        this.Topmost = Properties.Settings.Default.AlwaysOnTop;
                    } break;
                case "Opacity":
                    {
                        this.Opacity = Properties.Settings.Default.Opacity;
                    } break;
                case "WindowStyle":
                    {
                        buffer.WriteWarningLine("Window Style change requires a restart to take effect");
                        //this.WindowStyle = Properties.Settings.Default.WindowStyle;
                        //this.Hide();
                        //this.AllowsTransparency = (Properties.Settings.Default.WindowStyle == WindowStyle.None);
                        //this.Show();
                    } break;
                case "BorderColorTopLeft":
                    {
                        if (border.BorderBrush is LinearGradientBrush)
                        {
                            ((LinearGradientBrush)border.BorderBrush).GradientStops[0].Color = Properties.Settings.Default.BorderColorTopLeft;
                        }
                    } break;
                case "BorderColorBottomRight":
                    {
                        if (border.BorderBrush is LinearGradientBrush)
                        {
                            ((LinearGradientBrush)border.BorderBrush).GradientStops[1].Color = Properties.Settings.Default.BorderColorBottomRight;
                        }
                    } break;
                case "BorderThickness":
                    {
                        border.BorderThickness = Properties.Settings.Default.BorderThickness;
                    } break;
                case "FocusKey":
                    {
                        if (FocusKey != null && FocusKey.Id != 0) hkManager.Unregister(FocusKey);

                        if (Properties.Settings.Default.FocusKey != null)
                        {
                            FocusKey = Properties.Settings.Default.FocusKey;
                            hkManager.Register(FocusKey);
                        }
                    } break;
                default: break;
            }
        }

		/// <summary>
		/// Handles the SourceInitialized event of the Window control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void Window_SourceInitialized(object sender, EventArgs e)
		{
			// make the whole window glassy with -1
			Win32.Vista.Glass.ExtendGlassFrame(this, new Thickness(-1));

			// hook mousedown and call DragMove() to make the whole window a drag handle
            MouseButtonEventHandler mbeh = new MouseButtonEventHandler(DragHandler);
            progress.PreviewMouseLeftButtonDown += mbeh;
            border.PreviewMouseLeftButtonDown += mbeh;
            buffer.PreviewMouseLeftButtonDown += mbeh;

            hkManager = new WPFHotkeyManager(this);
            hkManager.HotkeyPressed += new WPFHotkeyManager.HotkeyPressedEvent(Hotkey_Pressed);

            FocusKey = Properties.Settings.Default.FocusKey;

			if(FocusKey == null)
			{
				Properties.Settings.Default.FocusKey = new Hotkey(Modifiers.Win, Keys.Oemtilde);
			}

            if (!Properties.Settings.Default.StartupBanner)
            {
                buffer.ClearScreen();
            }

			// this shouldn't be needed, because we hooked the settings.change event earlier
			if(FocusKey == null || FocusKey.Id == 0)
			{
				hkManager.Register(FocusKey);
			}

		}

		void DragHandler(object sender, MouseButtonEventArgs e)
		{

            if (e.Source is Border || e.Source is ProgressPanel || (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
			{
				DragMove();
				e.Handled = true;
			}
		}

		/// <summary>
		/// Handles the Loaded event of the Window control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			//buffer.Document.IsColumnWidthFlexible = false;

			RecalculateSizes();
		}

        bool IsHiding = false;

		private static Duration lasts = Duration.Automatic;
		private DoubleAnimation hideHeightAnimations = new DoubleAnimation(0.0, lasts);
		private DoubleAnimation hideOpacityAnimations = new DoubleAnimation(0.0, lasts);
		private DoubleAnimation showOpacityAnimation = new DoubleAnimation(1.0, lasts);
		private DoubleAnimation showHeightAnimation = new DoubleAnimation(1.0, lasts);
		private static DiscreteObjectKeyFrame visKeyHidden = new DiscreteObjectKeyFrame(Visibility.Hidden, KeyTime.FromPercent(1.0));
		private static DiscreteObjectKeyFrame visKeyVisible = new DiscreteObjectKeyFrame(Visibility.Visible, KeyTime.FromPercent(0.0));
		private static DiscreteBooleanKeyFrame trueEndFrame = new DiscreteBooleanKeyFrame(true, KeyTime.FromPercent(1.0));
		/// <summary>
		/// Handles the Deactivated event of the Window control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void Window_Deactivated(object sender, EventArgs e)
		{
            if (!myHost.IsClosing && Properties.Settings.Default.AutoHide)
            {
                HideWindow();
            }
		}

        /// <summary>
        /// Handles the Activated event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Window_Activated(object sender, EventArgs e)
        {
            if (IsHiding)
            {
                if (Properties.Settings.Default.Animate)
                {
                    ObjectAnimationUsingKeyFrames visi = new ObjectAnimationUsingKeyFrames();
                    visi.Duration = lasts;
                    visi.KeyFrames.Add(visKeyVisible);

                    // Go!
                    this.BeginAnimation(HeightProperty, showHeightAnimation, HandoffBehavior.SnapshotAndReplace);
                    this.BeginAnimation(OpacityProperty, showOpacityAnimation, HandoffBehavior.SnapshotAndReplace);
                    this.BeginAnimation(VisibilityProperty, visi, HandoffBehavior.SnapshotAndReplace);
                }
                else
                {
                    Show();
                }
            }
            buffer.Focus();
        }

        /// <summary>
        /// Hides the window.
        /// </summary>
        private void HideWindow()
        {
            IsHiding = true;
            if (Properties.Settings.Default.Animate)
            {
                ObjectAnimationUsingKeyFrames visi = new ObjectAnimationUsingKeyFrames();
                visi.Duration = lasts;
                visi.KeyFrames.Add(visKeyHidden);

                hideOpacityAnimations.AccelerationRatio = 0.25;
                hideHeightAnimations.AccelerationRatio = 0.5;
                showOpacityAnimation.AccelerationRatio = 0.25;
                showHeightAnimation.AccelerationRatio = 0.5;

                // before we start animating, set the animation endpoints to the current values.
                hideOpacityAnimations.From = showOpacityAnimation.To = (double)this.GetAnimationBaseValue(OpacityProperty);
                hideHeightAnimations.From = showHeightAnimation.To = (double)this.GetAnimationBaseValue(HeightProperty);

                // GO!
                this.BeginAnimation(HeightProperty, hideHeightAnimations, HandoffBehavior.SnapshotAndReplace);
                this.BeginAnimation(OpacityProperty, hideOpacityAnimations, HandoffBehavior.SnapshotAndReplace);
                this.BeginAnimation(VisibilityProperty, visi, HandoffBehavior.SnapshotAndReplace);
            }
            else
            {
                Hide();
            }
        }

		/// <summary>Handles the LocationChanged event of the Window control.</summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void Window_LocationChanged(object sender, EventArgs e)
		{
			//System.Windows.SystemParameters.VirtualScreenHeight
			if(Properties.Settings.Default.SnapToScreenEdge)
			{
				CornerRadius radi = new CornerRadius(5.0);

                Rect workarea = new Rect(SystemParameters.VirtualScreenLeft,
                                          SystemParameters.VirtualScreenTop,
                                          SystemParameters.VirtualScreenWidth,
                                          SystemParameters.VirtualScreenHeight);

				if(Properties.Settings.Default.SnapDistance > 0)
				{
					if(this.Left - workarea.Left < Properties.Settings.Default.SnapDistance) this.Left = workarea.Left;
					if(this.Top - workarea.Top < Properties.Settings.Default.SnapDistance) this.Top = workarea.Top;
					if(workarea.Right - this.RestoreBounds.Right < Properties.Settings.Default.SnapDistance) this.Left = workarea.Right - this.RestoreBounds.Width;
					if(workarea.Bottom - this.RestoreBounds.Bottom < Properties.Settings.Default.SnapDistance) this.Top = workarea.Bottom - this.RestoreBounds.Height;
				}

				if(this.Left <= workarea.Left)
				{
					radi.BottomLeft = 0.0;
					radi.TopLeft = 0.0;
					this.Left = workarea.Left;
				}
				if(this.Top <= workarea.Top)
				{
					radi.TopLeft = 0.0;
					radi.TopRight = 0.0;
					this.Top = workarea.Top;
				}
				if(this.RestoreBounds.Right >= workarea.Right)
				{
					radi.TopRight = 0.0;
					radi.BottomRight = 0.0;
					this.Left = workarea.Right - this.RestoreBounds.Width;
				}
				if(this.RestoreBounds.Bottom >= workarea.Bottom)
				{
					radi.BottomRight = 0.0;
					radi.BottomLeft = 0.0;
					this.Top = workarea.Bottom - this.RestoreBounds.Height;
				}

				border.CornerRadius = radi;
			}


			Properties.Settings.Default.WindowLeft = Left;
			Properties.Settings.Default.WindowTop = Top;
			RecalculateSizes();
		}


		/// <summary>Handles the Closing event of the Window control.</summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
            myHost.IsClosing = true;
            myHost.SetShouldExit(0);

			Properties.Settings.Default.Save();
		}


        void CanHandleControlC(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        void HandleControlC(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                myHost.StopPipeline();
                e.Handled = true;
            }
            catch (Exception exception)
            {
                myHost.UI.WriteErrorLine(exception.ToString());
            }

        }
		/// <summary>
		/// Handles the SizeChanged event of the Window control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.SizeChangedEventArgs"/> instance containing the event data.</param>
		private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			// we only recalculate when something other than animation changes the window size
			double h = (double)this.GetAnimationBaseValue(HeightProperty);
			if(Properties.Settings.Default.WindowHeight != h)
			{
				Properties.Settings.Default.WindowHeight = h;
				Properties.Settings.Default.WindowWidth = (double)this.GetAnimationBaseValue(WidthProperty);
				RecalculateSizes();
			}
		}


		private delegate void VoidVoidDelegate();

		/// <summary>
		/// Handles the HotkeyPressed event from the Hotkey Manager
		/// </summary>
		/// <param name="window">The window.</param>
		/// <param name="hotkey">The hotkey.</param>
		void Hotkey_Pressed(Window window, Hotkey hotkey)
		{
			if(hotkey.Equals(FocusKey))
			{
				if(!IsActive)
				{
					Activate();
					if(Properties.Settings.Default.Animate)
					{
						Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new VoidVoidDelegate(delegate { buffer.Focus(); }));
					}
				}
				else
				{
					Win32.Application.ActivateNextWindow();
                    if (Properties.Settings.Default.Animate)
                    {
                        HideWindow();
                    }
				}
			}
		}

        private void admin_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!Win32.Application.IsUserAnAdmin())
            {
                Process current = Process.GetCurrentProcess();

                Process proc = new Process();
                proc.StartInfo = new ProcessStartInfo();
                //proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.Verb = "RunAs";
                proc.StartInfo.FileName = current.MainModule.FileName;
                proc.StartInfo.Arguments = current.StartInfo.Arguments;
                try
                {
                    if( proc.Start() ) {
                        this.myHost.SetShouldExit(0);
                    }
                }
                catch (System.ComponentModel.Win32Exception w32)
                {
                    // if( w32.Message == "The operation was canceled by the user" )
                    // if( w32.NativeErrorCode == 1223 ) {
                    buffer.WriteErrorLine("Error Starting new instance:" + w32.Message );
                    // myHost.Prompt();
                }
            }
        }
    }
}
