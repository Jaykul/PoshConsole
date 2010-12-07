using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.IO;
using System.Windows.Interactivity;
using System.Windows.Media;
using Huddled.Wpf;
using Huddled.Interop;
using Huddled.Interop.Windows;

namespace PoshConsole.Cmdlets
{
   [Cmdlet(VerbsCommon.Set, "QuakeMode", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None)]
   public class QuakeModeCommand : PSCmdlet
   {
      // private Window _window;
      private PoshConsole.Host.PoshOptions _options;
      // private Block _numbered;

      protected override void BeginProcessing()
      {
         ((PoshConsole.Host.PoshOptions)Host.PrivateData.BaseObject).WpfConsole.Dispatcher.BeginInvoke((Action)(() =>
         {
            _options = (PoshConsole.Host.PoshOptions)Host.PrivateData.BaseObject;
            var win = _options.WpfConsole.RootWindow;
            var topLeft = new System.Drawing.Point((int)win.Left, (int)win.Top);

            Rect workingArea = win.GetLocalWorkArea();

            _options.Settings.Save();
            switch (_options.Settings.SettingsKey)
            {
               case "Quake":
                  {
                     // make sure it's active so we don't turn off quakemode while it's hidden
                     _options.WpfConsole.RootWindow.Activate();
                     foreach (QuakeMode quake in Interaction.GetBehaviors(_options.WpfConsole.RootWindow).OfType<QuakeMode>())
                     {
                        quake.Enabled = false;
                        //quake.Duration = 0;
                     }
                     
					  // switch to the default settings, and reload ...
                     _options.Settings.SettingsKey = "";
                     _options.Settings.Reload();
                     // win.WindowState = WindowState.Normal;
                     // this shouldn't be necessary, because it should happen in the Settings switch?
                     _options.Settings.ToolbarVisibility = Visibility.Visible;


                  } break;

               default:
                  {
                     var height = win.Height;
                     _options.Settings.SettingsKey = "Quake";
                     _options.Settings.Reload();

                     foreach (QuakeMode quake in Interaction.GetBehaviors(win).OfType<QuakeMode>())
                     {
                        quake.Enabled = true;
                        // TODO: expose the duration as a setting
                        //quake.Duration = _options.Settings.Animate ? 1 : 0;
                     }

                     // this shouldn't be necessary, because it should happen in the Settings switch?
                     _options.Settings.ToolbarVisibility = Visibility.Collapsed;
                     
                     // Initial population of the 
                     if (_options.Settings.WindowWidth != workingArea.Width)
                     {
                        _options.Settings.ToolbarVisibility = Visibility.Collapsed;

                        _options.Settings.ShowInTaskbar = false;
                        _options.Settings.AutoHide = true;
                        _options.Settings.AlwaysOnTop = true;
                        // TODO: When ANIMATE, the window resizes WIDTH too, WHY?
                        // TODO: Also doesn't quite restore to the right place.
                        //_options.Settings.Animate = true;
                        _options.Settings.Opacity = 0.7;
                        //_options.Settings.BorderThickness = new Thickness(0, 0, 0, 5);
                        //_options.Settings.BorderColorBottomRight = Colors.Red;
                        //_options.Settings.BorderColorTopLeft = new Color() { A = 0xCC, R = 0xFF, G = 0x33, B = 0x00 };
                        //_options.Settings.SnapToScreenEdge = true;
                        //_options.Settings.SnapDistance = workingArea.Width / 3;
                        //_options.Settings.WindowHeight = workingArea.Height / 3;

                        _options.Colors.DefaultBackground = ConsoleColor.Black;
                        _options.Colors.DefaultForeground = ConsoleColor.White;
                        //_options.Settings.FocusKey = "Win+OemTilde";
                        //_options.Settings.StartupBanner = false;
                        _options.Settings.Save();
                     }
                     // Always force reset the width/top/left, but NOT the HEIGHT
					 _options.Settings.WindowTop = workingArea.Top - win.Margin.Top;
					 _options.Settings.WindowLeft = workingArea.Left - win.Margin.Left;
					 _options.Settings.WindowWidth = workingArea.Width - (win.Margin.Left + win.Margin.Right);
                  } break;
            }
         }));
      }
   }
}