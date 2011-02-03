using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Huddled.Wpf;
using PoshConsole.Controls;
using PoshConsole.Host;
using PoshConsole.Properties;

namespace PoshConsole {

   /// <summary>
   /// Implementation of a WPF host for PowerShell
   /// </summary>
   public partial class PoshConsoleWindow : IPSUI {

      #region  Fields (11)


      private static DependencyProperty _consoleProperty;
      static PoshConsoleWindow() {
         try {
            _consoleProperty = DependencyProperty.Register("Console", typeof(IPoshConsoleControl), typeof(PoshConsoleWindow));
         }
         catch (Exception ex) {
            Trace.WriteLine(ex.Message);
         }
      }

      /// <summary>
      /// The PSHost implementation for this interpreter.
      /// </summary>
      private PoshHost _host;

      #endregion

      #region  Constructors (1)

      private TextBox _search;

      /// <summary>
      /// Initializes a new instance of the <see cref="PoshConsole"/> class.
      /// </summary>
      public PoshConsoleWindow() {
         Cursor = Cursors.AppStarting;

         // Create the host and runspace instances for this interpreter. Note that
         // this application doesn't support console files so only the default snapins
         // will be available.
         InitializeComponent();

         // "buffer" is defined in the XAML
         Console = buffer;

         Style = (Style)Resources["MetroStyle"];

         // buffer.TitleChanged += new passDelegate<string>(delegate(string val) { Title = val; });
         Settings.Default.PropertyChanged += SettingsPropertyChanged;

         buffer.Finished += (source, results) =>
            Dispatcher.BeginInvoke(
               DispatcherPriority.Background,
               (Action)delegate {
                     progress.Children.Clear();
                     ProgressRecords.Clear();
                     Cursor = Cursors.Arrow;
                  });
      }



      #endregion

      #region  Properties (1)

      public IPoshConsoleControl Console {
         get { return ((IPoshConsoleControl)GetValue(_consoleProperty)); }
         set { SetValue(_consoleProperty, value); }
      }

      #endregion

      #region  Delegates and Events (5)

      //  Delegates (5)

      // Universal Delegates
      internal delegate void PassDelegate<in T>(T input);
      internal delegate TRet PassReturnDelegate<in T, out TRet>(T input);
      internal delegate TRet ReturnDelegate<out TRet>();
      private delegate void SettingsChangedDelegate(object sender, System.ComponentModel.PropertyChangedEventArgs e);
      private delegate void VoidVoidDelegate();

      #endregion

      #region  Methods (9)

      //  Protected Methods (2)

      /// <summary>
      /// Raises the <see cref="E:System.Windows.Window.Closing"></see> event, and executes the ShutdownProfile
      /// </summary>
      /// <param name="e">A <see cref="T:System.ComponentModel.CancelEventArgs"></see> that contains the event data.</param>
      protected override void OnClosing(System.ComponentModel.CancelEventArgs e) {
         // // This doesn't fix the COM RCW problem
         // Dispatcher.Invoke((Action)(() => { _host.KillConsole(); }));
         _host.KillConsole();
         base.OnClosing(e);
      }

      private HotkeysBehavior _hotkeys;


      //private void buffer_SizeChanged(object sender, SizeChangedEventArgs e)
      //{
      //    RecalculateSizes();
      //}
      protected override void OnGotFocus(RoutedEventArgs e) {
         buffer.Focus();
         base.OnGotFocus(e);
      }

      //  Private Methods (7)

      void IPSUI.SetShouldExit(int exitCode) {
         Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
            (Action)(() => Application.Current.Shutdown(exitCode)));
      }

      ///// <summary>
      ///// Handles the HotkeyPressed event from the Hotkey Manager
      ///// </summary>
      ///// <param name="Window">The Window.</param>
      ///// <param name="hotkey">The hotkey.</param>
      //void Hotkey_Pressed(Window Window, Hotkey hotkey)
      //{
      //    if(hotkey.Equals(FocusKey))
      //    {
      //        if(!IsActive)
      //        {
      //            Activate(); // Focus();
      //        }
      //        else
      //        {
      //            // if they used the hotkey while the Window has focus, they want it to hide...
      //            // but we only need to do that HERE if AutoHide is false 
      //            // if AutoHide is true, it hides during the Deactivate handler
      //            if (Properties.Settings.Default.AutoHide == false) HideWindow();
      //            NativeMethods.ActivateNextWindow(NativeMethods.GetWindowHandle(this));
      //        }
      //    }
      //}

      #region IPSUI Members
      protected Dictionary<int, ProgressPanel> ProgressRecords = new Dictionary<int, ProgressPanel>();

      void IPSUI.WriteProgress(long sourceId, ProgressRecord record) {
         if (!Dispatcher.CheckAccess()) {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => ((IPSUI)this).WriteProgress(sourceId, record)));
         }
         else {
            if (ProgressRecords.ContainsKey(record.ActivityId)) {
               if (record.RecordType == ProgressRecordType.Completed) {
                  progress.Children.Remove(ProgressRecords[record.ActivityId]);
                  ProgressRecords.Remove(record.ActivityId);
               }
               else {
                  ProgressRecords[record.ActivityId].Record = record;
               }
            }
            else {
               ProgressRecords[record.ActivityId] = new ProgressPanel(record);
               if (record.ParentActivityId < 0 || !ProgressRecords.ContainsKey(record.ParentActivityId)) {
                  progress.Children.Add(ProgressRecords[record.ActivityId]);
               }
               else {
                  progress.Children.Insert(progress.Children.IndexOf(ProgressRecords[record.ParentActivityId]) + 1, ProgressRecords[record.ActivityId]);
               }
            }
         }
      }

      PSCredential IPSUI.PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options) {
         throw new Exception("The method or operation is not implemented.");
      }

      PSCredential IPSUI.PromptForCredential(string caption, string message, string userName, string targetName) {
         throw new Exception("The method or operation is not implemented.");
      }

      System.Security.SecureString IPSUI.ReadLineAsSecureString() {
         throw new Exception("The method or operation is not implemented.");
      }

      #endregion


      private void OnCanHandleControlC(object sender, CanExecuteRoutedEventArgs e) {
         e.CanExecute = true;
      }

      private void OnHandleControlC(object sender, ExecutedRoutedEventArgs e) {
         try {
            _host.StopPipeline();
            e.Handled = true;
         }
         catch (Exception exception) {
            _host.UI.WriteErrorLine(exception.ToString());
         }

      }

      /// <summary>
      /// Handles the Activated event of the Window control.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
      private void OnWindowActivated(object sender, EventArgs e) {
         if (Style == Resources["QuakeTopStyle"]) {
            var showAnimation = (Storyboard)Resources["QuakeShowTopMetro"];
            ((DoubleAnimation)showAnimation.Children[0]).To = Settings.Default.Opacity;
            ((DoubleAnimation)showAnimation.Children[1]).To = Settings.Default.WindowHeight;
            showAnimation.FillBehavior = FillBehavior.HoldEnd;
            showAnimation.Begin(this);
         }
         else if (Visibility == Visibility.Hidden && Style == Resources["MetroStyle"]) {
            var showAnimation = (Storyboard)Resources["QuakeShowBottomMetro"];
            ((DoubleAnimation)showAnimation.Children[0]).To = Settings.Default.Opacity;
            ((DoubleAnimation)showAnimation.Children[1]).To = Settings.Default.WindowHeight;
            ((DoubleAnimation)showAnimation.Children[2]).To = RestoreBounds.Bottom - Settings.Default.WindowHeight;
            showAnimation.FillBehavior = FillBehavior.HoldEnd;
            showAnimation.Begin(this);
         }
         Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new VoidVoidDelegate(() => buffer.Focus()));
      }


      /// <summary>
      /// Handles the Deactivated event of the window control.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
      private void OnWindowDeactivated(object sender, EventArgs e) {
         if (Style == Resources["QuakeTopStyle"]) {
            var hideMetro = (Storyboard)Resources["QuakeHideTopMetro"];
            hideMetro.Begin(this);
         }
         else if (Style == Resources["QuakeBottomStyle"]) {
            var hideMetro = (Storyboard)Resources["QuakeHideBottomMetro"];
            ((DoubleAnimation)hideMetro.Children[2]).To = RestoreBounds.Bottom;
            hideMetro.Begin(this);            
         }
      }
      /// <summary>Handles the Closing event of the Window control.</summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
      private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
         // save our current location for next time
         Settings.Default.WindowTop = Top;
         Settings.Default.WindowLeft = Left;
         Settings.Default.WindowWidth = Width;
         Settings.Default.WindowHeight = Height;

         Settings.Default.Save();
         Properties.Colors.Default.Save();

         if (_host != null) {
            _host.IsClosing = true;
            _host.SetShouldExit(0);
         }
      }


      /// <summary>
      /// Handles the Loaded event of the Window control.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
      private void OnWindowLoaded(object sender, RoutedEventArgs e) {
         //buffer.Document.IsColumnWidthFlexible = false;
         try {
            _host = new PoshHost(this);
         }
         catch (Exception ex) {
            MessageBox.Show("Can't create PowerShell interface, are you sure PowerShell is installed? \n" + ex.Message + "\nAt:\n" + ex.Source, "Error Starting PoshConsole", MessageBoxButton.OK, MessageBoxImage.Stop);
            Application.Current.Shutdown(1);
         }

         // TODO: put back the (extra) user-settable object ...
         // note that it ought to just be an "object" so you could set it to anything
         //Binding statusBinding = new Binding("StatusText"){ Source = _host.Options };
         //statusTextBlock.SetBinding(TextBlock.TextProperty, statusBinding);

         //TOP         OnTopWindow.Content = Settings.Default.AlwaysOnTop ? "TopMost" : "Window";
         //TOP         OnTopWindow.ToolTip = Settings.Default.AlwaysOnTop ? "Take off Always on Top" : "Make Window Always on Top";

         if (Interop.NativeMethods.IsUserAnAdmin()) {
            // StatusBarItems:: Title, Separator, Admin, Separator, Status
            //TOP            ElevatedButton.ToolTip = "PoshConsole is running as Administrator";
            //TOP            ElevatedButton.IsEnabled = false;
            //el = status.Items[2] as StatusBarItem;
            //if (el != null)
            //{
            //   el.Content = "Elevated!";
            //   el.Foreground = new SolidColorBrush(Color.FromRgb((byte)204, (byte)119, (byte)17));
            //   el.ToolTip = "PoshConsole is running as Administrator";
            //   el.Cursor = this.Cursor;
            //}
         }
         Cursor = Cursors.IBeam;
      }

      ///// <summary>Handles the LocationChanged event of the Window control.</summary>
      ///// <param name="sender">The source of the event.</param>
      ///// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
      //private void OnWindowLocationChanged(object sender, EventArgs e)
      //{
      //   if (WindowState == WindowState.Normal)
      //   {
      //      CornerRadius cornerRadius = _defaultCornerRadius;
      //      if (_defaultCornerRadius == default(CornerRadius))
      //      {
      //         cornerRadius = new CornerRadius(20, 0, 5, 5);
      //      }
      //      Rect workarea = new Rect(SystemParameters.VirtualScreenLeft,
      //                                SystemParameters.VirtualScreenTop,
      //                                SystemParameters.VirtualScreenWidth,
      //                                SystemParameters.VirtualScreenHeight);

      //      if (this.Left == workarea.Left)
      //      {
      //         cornerRadius.BottomLeft = 0.0;
      //         cornerRadius.TopLeft = 0.0;
      //      }
      //      if (this.Top == workarea.Top)
      //      {
      //         cornerRadius.TopLeft = 0.0;
      //         cornerRadius.TopRight = 0.0;
      //      }
      //      if (this.RestoreBounds.Right == workarea.Right)
      //      {
      //         cornerRadius.TopRight = 0.0;
      //         cornerRadius.BottomRight = 0.0;
      //      }
      //      if (this.RestoreBounds.Bottom >= workarea.Bottom)
      //      {
      //         cornerRadius.BottomRight = 0.0;
      //         cornerRadius.BottomLeft = 0.0;
      //      }

      //      foreach (CustomChrome chrome in NativeWpf.SelectBehaviors<CustomChrome>(this))
      //      {
      //         chrome.CornerRadius = cornerRadius;
      //      }
      //   }
      //   else
      //   {
      //      foreach (CustomChrome chrome in NativeWpf.SelectBehaviors<CustomChrome>(this))
      //      {
      //         chrome.CornerRadius = _defaultCornerRadius;
      //      }
      //   }
      //}

      ///// <summary>
      ///// Handles the SizeChanged event of the Window control.
      ///// </summary>
      ///// <param name="sender">The source of the event.</param>
      ///// <param name="e">The <see cref="System.Windows.SizeChangedEventArgs"/> instance containing the event data.</param>
      //private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
      //{
      //   // we only reset the saved settings when something other than animation changes the Window size
      //   double h = (double)this.GetAnimationBaseValue(HeightProperty);
      //   if (Properties.Settings.Default.WindowHeight != h)
      //   {
      //      Properties.Settings.Default.WindowHeight = h;
      //      double w = (double)this.GetAnimationBaseValue(WidthProperty);
      //      if(!Double.IsNaN(w)) {
      //         Properties.Settings.Default.WindowWidth = w;
      //      }
      //   }
      //}

      /// <summary>
      /// Handles the SourceInitialized event of the Window control.
      /// </summary>
      /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
      protected override void OnSourceInitialized(EventArgs e) {
         // NOTE: we override OnSourceInitialized so we can control the order
         // this way, the base event (and it's handlers) happen before us
         // and we can handle the unregistered hotkeys (should probably make that an event on the HotkeysBehavior)
         base.OnSourceInitialized(e);

         _search = (TextBox)Template.FindName("Search", this);
         Cursor = Cursors.AppStarting;
         // this.TryExtendFrameIntoClientArea(new Thickness(-1));
         var initWarnings = new StringBuilder();

         // so now we can ask which keys are still unregistered.
         // TODO: get the new HotkeysBehavior
         _hotkeys = Interaction.GetBehaviors(this).OfType<HotkeysBehavior>().Single();
         Interaction.GetBehaviors(this).OfType<SnapToBehavior>().Single();
         if (_hotkeys != null) {
            int k = -1;
            int count = _hotkeys.UnregisteredKeys.Count;
            while (++k < count) {
               KeyBinding key = _hotkeys.UnregisteredKeys[k];
               // hypothetically, you would show them a GUI for changing the hotkeys... 

               // but you could try modifying them yourself ...
               ModifierKeys mk = HotkeysBehavior.AddModifier(key.Modifiers);
               if (mk != ModifierKeys.None) {
                  initWarnings.AppendFormat("Hotkey taken: {0} + {1} for {2}\nModifying it to {3}, {0} + {1}.\n\n", key.Modifiers, key.Key, key.Command, mk);
                  key.Modifiers |= mk;
                  _hotkeys.Hotkeys.Add(key);
               }
               else {
                  initWarnings.AppendFormat("Can't register hotkey for {2}\nWe tried registering it as {0} + {1}.\n\n", key.Modifiers, key.Key, key.Command);
                  //   // MessageBox.Show(string.Format("Can't register hotkey: {0}+{1} \nfor {2}.", key.Modifiers, key.Key, key.Command, mk));
                  //   //key.Modifiers |= mk;
                  //   //hk.Add(key);
               }
            }
         }
         // LOAD the startup banner only when it's set (instead of removing it after)
         if (Settings.Default.StartupBanner && System.IO.File.Exists("StartupBanner.xaml")) {
            try {
               Paragraph banner;
               ErrorRecord error;
               System.IO.FileInfo startup = new System.IO.FileInfo("StartupBanner.xaml");
               if (startup.TryLoadXaml(out banner, out error)) {
                  // Copy over *all* resources from the DOCUMENT to the BANNER
                  // NOTE: be careful not to put resources in the document you're not willing to expose
                  // NOTE: this will overwrite resources with matching keys, so banner-makers need to be aware
                  foreach (string key in buffer.Document.Resources.Keys) {
                     banner.Resources[key] = buffer.Document.Resources[key];
                  }
                  banner.Padding = new Thickness(5);
                  buffer.Document.Blocks.InsertBefore(buffer.Document.Blocks.FirstBlock, banner);
               }
               else {
                  ((IPSConsole)buffer).WriteLine("PoshConsole 1.0.2010.308");
               }

               // Document.Blocks.InsertBefore(Document.Blocks.FirstBlock, new Paragraph(new Run("PoshConsole`nVersion 1.0.2007.8150")));
               // Document.Blocks.AddRange(LoadXamlBlocks("StartupBanner.xaml"));
            }
            catch (Exception ex) {
               Trace.TraceError(@"Problem loading StartupBanner.xaml\n{0}", ex.Message);
               buffer.Document.Blocks.Clear();
               ((IPSConsole)buffer).WriteLine("PoshConsole 1.0.2010.308");
            }
         }

         if (initWarnings.Length > 0) {
            ((IPSConsole)buffer).WriteWarningLine(initWarnings.ToString());
         }

         // hook mousedown and call DragMove() to make the whole Window a drag handle
         //TOP         Toolbar.PreviewMouseLeftButtonDown += DragHandler;
         progress.PreviewMouseLeftButtonDown += DragHandler;
         buffer.PreviewMouseLeftButtonDown += DragHandler;

         OnStyleChanged(null, Style);
         buffer.Focus();
      }

      void SettingsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
         if (!buffer.Dispatcher.CheckAccess()) {
            buffer.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new SettingsChangedDelegate(SettingsPropertyChanged), sender, new object[] { e });
            return;
         }

         switch (e.PropertyName) {
            case "ShowInTaskbar": {
                  ShowInTaskbar = Settings.Default.ShowInTaskbar;
               }
               break;
            case "ToolbarVisibility": {
                  //TOP                  this.Toolbar.Visibility = Properties.Settings.Default.ToolbarVisibility;
                  //switch (Properties.Settings.Default.ToolbarVisibility)
                  //{
                  //   case Visibility.Hidden:
                  //   case Visibility.Collapsed:
                  //      this.TryExtendFrameIntoClientArea(new Thickness(0.0));
                  //      break;
                  //   case Visibility.Visible:
                  //      this.TryExtendFrameIntoClientArea(new Thickness(0.0, Toolbar.ActualHeight, 0.0, 0.0));
                  //      break;
                  //}

               }
               break;
            // TODO: let the new top-toolbars be hidden
            //case "StatusBar":
            //   {
            //      status.Visibility = Properties.Settings.Default.StatusBar ? Visibility.Visible : Visibility.Collapsed;
            //   } break;
            case "WindowHeight": {
                  // do nothing, this setting is set when height changes, so we don't want to get into a loop.
                  //this.Height = Properties.Settings.Default.WindowHeight;
               }
               break;
            case "WindowLeft": {
                  Left = Settings.Default.WindowLeft;
               }
               break;
            case "WindowWidth": {
                  // do nothing, this setting is set when width changes, so we don't want to get into a loop.
                  //this.Width = Properties.Settings.Default.WindowWidth;
               }
               break;
            case "WindowTop": {
                  Top = Settings.Default.WindowTop;
               }
               break;
            case "Animate": {
                  // do nothing, this setting is checked for each animation.
               }
               break;
            case "AutoHide": {
                  // do nothing, this setting is checked for each hide event.
               }
               break;
            case "SnapToScreenEdge": {
                  // do nothing, this setting is checked for each move
               }
               break;
            case "SnapDistance": {
                  // do nothing, this setting is checked for each move
               }
               break;
            case "AlwaysOnTop": {
                  Topmost = Settings.Default.AlwaysOnTop;
               }
               break;
            case "Opacity": {
                  // stop any animation before we try to apply the setting
                  var op = new DoubleAnimation(Settings.Default.Opacity, new Duration(TimeSpan.FromSeconds(0.5)));
                  BeginAnimation(OpacityProperty, op);
               }
               break;
            case "WindowStyle": {
                  //((IPSConsole)buffer).WriteWarningLine("Window Style change requires a restart to take effect");
                  //this.WindowStyle = Properties.Settings.Default.WindowStyle;
                  //this.Hide();
                  //this.AllowsTransparency = (Properties.Settings.Default.WindowStyle == WindowStyle.None);
                  //this.Show();
               }
               break;
            case "BorderColorTopLeft": {
                  if (BorderBrush is LinearGradientBrush) {
                     ((LinearGradientBrush)BorderBrush).GradientStops[0].Color = Settings.Default.BorderColorTopLeft;
                  }
               }
               break;
            case "BorderColorBottomRight": {
                  if (BorderBrush is LinearGradientBrush) {
                     ((LinearGradientBrush)BorderBrush).GradientStops[1].Color = Settings.Default.BorderColorBottomRight;
                  }
               }
               break;
            case "BorderThickness": {
                  BorderThickness = Settings.Default.BorderThickness;
               }
               break;
            case "FocusKeyGesture":
            case "FocusKey": {
                  KeyBinding focusKey = null;
                  foreach (var hk in _hotkeys.Hotkeys) {
                     if (hk.Command is GlobalCommands.ActivateCommand) {
                        focusKey = hk;
                     }
                  }
                  var kv = new KeyValueSerializer();
                  var km = new ModifierKeysValueSerializer();
                  KeyGesture newGesture = null;
                  try {
                     var modifiers = Settings.Default.FocusKey.Split(new[] { '+' }).ToList();
                     var character = modifiers.Last();
                     modifiers.Remove(character);
                     // ReSharper disable AssignNullToNotNullAttribute
                     // ReSharper disable PossibleNullReferenceException
                     newGesture = new KeyGesture((Key)kv.ConvertFromString(character, null),
                                       (ModifierKeys)km.ConvertFromString(string.Join("+", modifiers), null));
                     // ReSharper restore PossibleNullReferenceException
                     // ReSharper restore AssignNullToNotNullAttribute
                  }
                  catch (Exception) {
                     if (focusKey != null)
                        Settings.Default.FocusKey = focusKey.Modifiers.ToString().Replace(", ", "+") + "+" + focusKey.Key;
                  }

                  if (focusKey != null && newGesture != null) {
                     _hotkeys.Hotkeys.Remove(focusKey);
                     _hotkeys.Hotkeys.Add(new KeyBinding(GlobalCommands.ActivateWindow, newGesture));
                  }

               }
               break;
            case "FontSize": {
                  buffer.FontSize = Settings.Default.FontSize;
               }
               break;
            case "FontFamily": {
                  buffer.FontFamily = Settings.Default.FontFamily;
               }
               break;
            default:
               break;
         }
      }

      //  Internal Methods (1)

      internal void DragHandler(object sender, MouseButtonEventArgs e) {

         if (e.Source is Border || e.Source is ProgressPanel || e.Source is Grid || (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) {
            DragMove();
            e.Handled = true;
         }
      }

      #endregion

      private void DockedStateChanged(object sender, RoutedEventArgs e) {
         // The actual source of this event is the behavior, not the window (yay)
         switch (((SnapToBehavior)e.OriginalSource).WindowState) {
            case SnapToBehavior.AdvancedWindowState.DockedTop:
               Style = (Style)Resources["QuakeTopStyle"];
               break;
            case SnapToBehavior.AdvancedWindowState.DockedBottom:
               Style = (Style)Resources["QuakeBottomStyle"];
               break;
            default:
               Style = (Style)Resources["MetroStyle"];
               break;
         }
      }


      //void OnWindowStateChanged(object sender, Huddled.Wpf.SnapToBehavior.AdvancedWindowStateChangedArgs e)
      //{
      //   if (Settings.Default.QuakeMode)
      //   {
      //      // Switch from Metro to Quake when docking
      //      if (Style == (Style)Resources["MetroStyle"])
      //      {
      //         if (e.WindowState == SnapToBehavior.AdvancedWindowState.DockedTop ||
      //             e.WindowState == SnapToBehavior.AdvancedWindowState.SnapTop ||
      //             e.WindowState == SnapToBehavior.AdvancedWindowState.SnapTopLeft ||
      //             e.WindowState == SnapToBehavior.AdvancedWindowState.SnapTopRight
      //         )
      //         {
      //            SnapToQuakeMode(true);
      //         }
      //         else
      //            if (e.WindowState == SnapToBehavior.AdvancedWindowState.DockedBottom ||
      //                e.WindowState == SnapToBehavior.AdvancedWindowState.SnapBottom ||
      //                e.WindowState == SnapToBehavior.AdvancedWindowState.SnapBottomLeft ||
      //                e.WindowState == SnapToBehavior.AdvancedWindowState.SnapBottomRight
      //            )
      //            {
      //               SnapToQuakeMode(false);
      //            }
      //      }
      //      else if (Style == (Style)Resources["QuakeTopStyle"] || Style == (Style)Resources["QuakeBottomStyle"])
      //      {
      //         if (e.WindowState != SnapToBehavior.AdvancedWindowState.DockedTop &&
      //             e.WindowState != SnapToBehavior.AdvancedWindowState.DockedBottom &&
      //             e.WindowState != SnapToBehavior.AdvancedWindowState.SnapTop &&
      //             e.WindowState != SnapToBehavior.AdvancedWindowState.SnapTopLeft &&
      //             e.WindowState != SnapToBehavior.AdvancedWindowState.SnapTopRight &&
      //             e.WindowState != SnapToBehavior.AdvancedWindowState.SnapBottom &&
      //             e.WindowState != SnapToBehavior.AdvancedWindowState.SnapBottomLeft &&
      //             e.WindowState != SnapToBehavior.AdvancedWindowState.SnapBottomRight)
      //         {
      //            RemoveQuakeMode();
      //         }
      //      }
      //   }
      //}

      //private void SnapToQuakeMode(bool top)
      //{
      //   Settings.Default.WindowTop = Top;
      //   Settings.Default.WindowLeft = Left;
      //   Settings.Default.WindowWidth = Width;
      //   Settings.Default.WindowHeight = Height;

      //   Settings.Default.Save();

      //   var validArea = this.GetLocalWorkArea();
      //   var snap = _Snapto.SnapDistance;
      //   snap.Left = validArea.Width / 2;
      //   snap.Right = validArea.Width / 2;
      //   if (top)
      //   {
      //      Style = (Style)Resources["QuakeTopStyle"];
      //      snap.Top = 120;
      //      snap.Bottom = 0;
      //   }
      //   else
      //   {
      //      Style = (Style)Resources["QuakeBottomStyle"];
      //      snap.Bottom = validArea.Height / 4;
      //      snap.Top = 0;
      //   }

      //   _Snapto.SnapDistance = snap;
      //}

      //private void RemoveQuakeMode()
      //{
      //   _Snapto.SnapDistance = Settings.Default.SnapDistance;

      //   Style = (Style)Resources["MetroStyle"];

      //   Width = Settings.Default.WindowWidth;
      //   Height = Settings.Default.WindowHeight;
      //}

      void OnHandleDecreaseZoom(object sender, ExecutedRoutedEventArgs e) {
         buffer.Zoom--;
      }

      void OnHandleIncreaseZoom(object sender, ExecutedRoutedEventArgs e) {
         buffer.Zoom++;
      }

      void OnHandleSetZoom(object sender, ExecutedRoutedEventArgs e) {
         double zoom;
         if (double.TryParse(e.Parameter.ToString(), out zoom)) {
            buffer.Zoom = zoom;
         }
      }

      //ADMIN    private void OnAdminToggle(object sender, RoutedEventArgs e)
      //ADMIN    {
      //ADMIN       if (!PoshConsole.Interop.NativeMethods.IsUserAnAdmin())
      //ADMIN       {
      //ADMIN          Process current = Process.GetCurrentProcess();
      //ADMIN    
      //ADMIN          Process proc = new Process();
      //ADMIN          proc.StartInfo = new ProcessStartInfo();
      //ADMIN          //proc.StartInfo.CreateNoWindow = true;
      //ADMIN          proc.StartInfo.UseShellExecute = true;
      //ADMIN          proc.StartInfo.Verb = "RunAs";
      //ADMIN          proc.StartInfo.FileName = current.MainModule.FileName;
      //ADMIN          proc.StartInfo.Arguments = current.StartInfo.Arguments;
      //ADMIN          try
      //ADMIN          {
      //ADMIN             if (proc.Start())
      //ADMIN             {
      //ADMIN                this._host.SetShouldExit(0);
      //ADMIN             }
      //ADMIN          }
      //ADMIN          catch (System.ComponentModel.Win32Exception we)
      //ADMIN          {
      //ADMIN             // if( w32.Message == "The operation was canceled by the user" )
      //ADMIN             // if( w32.NativeErrorCode == 1223 ) {
      //ADMIN             ((IPSConsole)buffer).WriteErrorLine("Error Starting new instance:" + we.Message);
      //ADMIN             // myHost.Prompt();
      //ADMIN          }
      //ADMIN       }
      //ADMIN    }

      //TOP      private void OnTopmost(object sender, RoutedEventArgs e)
      //TOP      {
      //TOP         Settings.Default.AlwaysOnTop = !Settings.Default.AlwaysOnTop;
      //TOP         OnTopWindow.Content = Settings.Default.AlwaysOnTop ? "TopMost" : "Window";
      //TOP         OnTopWindow.ToolTip = Settings.Default.AlwaysOnTop ? "Take off Always on Top" : "Make Window Always on Top";
      //TOP      }


      // Handles F3 by default
      private void OnSearchCommand(object sender, ExecutedRoutedEventArgs e) {
         if (_search.Text.Length > 0) {
            Find(_search.Text);
         }
         else {
            _search.Focus();
         }
      }

      // Handles Ctrl+F by default
      private void OnFindCommand(object sender, ExecutedRoutedEventArgs e) {
         if (_search.Text.Length > 1) {
            _search.Select(0, _search.Text.Length - 1);
            _search.Focus();
         }
      }

      private void SearchGotFocus(object sender, RoutedEventArgs e) {
         _search.SelectAll();
      }

      private void SearchPreviewKeyDown(object sender, KeyEventArgs e) {
         if (e.Key == Key.Enter) {
            Find(_search.Text);
         }
      }

      TextPointer _lastSearchPoint;
      String _lastSearchString = String.Empty;
      private void Find(string input) {
         if (_lastSearchPoint == null || input != _lastSearchString) {
            _lastSearchPoint = buffer.Document.ContentStart;
            _lastSearchString = input;
         }

         TextRange found = buffer.FindNext(ref _lastSearchPoint, input);
         if (found == null) {
            System.Media.SystemSounds.Asterisk.Play();
            _lastSearchPoint = buffer.Document.ContentStart;
         }
      }

      private void SkinToggleClick(object sender, RoutedEventArgs e) {
         if (Style == Resources["GlassStyle"]) {
            Style = Resources["MetroStyle"] as Style;
         }
         else {
            Style = Resources["GlassStyle"] as Style;
         }
      }

      private void OnTopWindowClick(object sender, RoutedEventArgs e) {
         var btn = sender as Button;
         if (btn != null) {

            if (Topmost) {
               Settings.Default.AlwaysOnTop = Topmost = false;
               if (btn.Content is Image) {

               }
               else {
                  btn.Content = "Normal";
               }
            }
            else {
               Settings.Default.AlwaysOnTop = Topmost = true;
               if (btn.Content is Image) {

               }
               else {
                  btn.Content = "Topmost";
               }
            }
         }
      }





   }
}
