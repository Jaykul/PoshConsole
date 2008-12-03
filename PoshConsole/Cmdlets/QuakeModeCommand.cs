using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.IO;
using System.Windows.Media;
using Huddled.Wpf;

namespace PoshConsole.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "QuakeMode", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None)]
    public class QuakeModeCommand : PSCmdlet
    {
        private Window _window;
        private PoshConsole.PSHost.PoshOptions _options;
        private Block _numbered;

        protected override void BeginProcessing()
       {
         ((PoshConsole.PSHost.PoshOptions)Host.PrivateData.BaseObject
             ).XamlUI.Dispatcher.BeginInvoke((Action)(() =>
         {

             _options = (PoshConsole.PSHost.PoshOptions)Host.PrivateData.BaseObject;
             var topLeft = new System.Drawing.Point(
                                  (int)_options.XamlUI.RootWindow.Left,
                                  (int)_options.XamlUI.RootWindow.Top);
             var screen = System.Windows.Forms.Screen.FromPoint(topLeft);
             
             _options.Settings.Save();
             switch (_options.Settings.SettingsKey)
             {
                case "Quake":
                   {
                       foreach (CustomChrome chrome in NativeBehaviors.SelectBehaviors<CustomChrome>(_options.XamlUI.RootWindow))
                       {
                           //chrome.UseGlassFrame = true;
                          chrome.ClientBorderThickness = new Thickness(8, 58, 8, 8);
                       }
                      _options.Settings.SettingsKey = "";
                      _options.Settings.Reload();
                      //_options.Settings.ShowInTaskbar = true;
                      //_options.Settings.AutoHide = false;
                      //_options.Settings.AlwaysOnTop = false;
                      //_options.Settings.Animate = true;
                      //_options.Settings.Opacity = 1.0;
                      //_options.Settings.BorderThickness = "2,10,2,2";
                      //_options.Settings.BorderColorBottomRight = "Red";
                      //_options.Settings.BorderColorTopLeft = "#CCFF3300";
                      //_options.Settings.WindowHeight = _options.FullPrimaryScreenHeight/2;
                      //_options.Settings.WindowWidth = _options.FullPrimaryScreenWidth * (2/3);
                      //_options.Settings.WindowTop = _options.FullPrimaryScreenHeight / 4;
                      //_options.Settings.WindowLeft = _options.FullPrimaryScreenWidth * (1/6);
                      //_options.Settings.ConsoleDefaultBackground = "DarkBlue";
                      //_options.Settings.ConsoleDefaultForeground = "White";
                      //_options.Settings.StartupBanner = true;

                   } break;

                default:
                    {
                        _options.Settings.SettingsKey = "Quake";
                        _options.Settings.Reload();

                        foreach (CustomChrome chrome in NativeBehaviors.SelectBehaviors<CustomChrome>(_options.XamlUI.RootWindow))
                        {
                            //chrome.UseGlassFrame = false;
                           chrome.ClientBorderThickness = new Thickness(0, 0, 0, 5);
                        }

                        if (_options.Settings.WindowWidth != screen.WorkingArea.Width)
                        {
                            _options.Settings.ToolbarVisibility = Visibility.Collapsed;

                            _options.Settings.ShowInTaskbar = false;
                            _options.Settings.AutoHide = true;
                            _options.Settings.AlwaysOnTop = true;
                           // TODO: When ANIMATE, the window resizes WIDTH too, WHY?
                           // TODO: Also doesn't quite restore to the right place.
                            //_options.Settings.Animate = true;
                            _options.Settings.Opacity = 0.8;
                            _options.Settings.BorderThickness = new Thickness(0, 0, 0, 5);
                            _options.Settings.BorderColorBottomRight = Colors.Red;
                            _options.Settings.BorderColorTopLeft = new Color() { A = 0xCC, R = 0xFF, G = 0x33, B = 0x00 };
                            _options.Settings.SnapToScreenEdge = true;

                            _options.Settings.WindowHeight = screen.WorkingArea.Height / 3;

                            _options.Colors.DefaultBackground = ConsoleColor.Black;
                            _options.Colors.DefaultForeground = ConsoleColor.White;
                            //_options.Settings.FocusKey = "Win+OemTilde";
                            //_options.Settings.StartupBanner = false;
                            _options.Settings.Save();
                        }
                        // Always force reset the width/top/left, but NOT the HEIGHT
                        _options.Settings.WindowWidth = screen.WorkingArea.Width;
                        _options.Settings.WindowTop = screen.WorkingArea.X;
                        _options.Settings.WindowLeft = screen.WorkingArea.Y;
                    } break; 
             }
         }));
      }
    }
}