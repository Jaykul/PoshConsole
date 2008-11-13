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

namespace PoshConsole.Cmdlets
{
   [Cmdlet("Quake", "Mode", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None)]
   public class QuakeModeCommand : PSCmdlet
   {
      [Parameter(
         Position = 0,
         Mandatory = false,
         HelpMessage = "The id of the Paragraph to return")]
      public int id { get; set; }

      private int _id = 0;
      private Window _window;
      private PoshConsole.PSHost.PoshOptions _options;
      private Block _numbered;

      protected override void BeginProcessing()
      {

         _options = (PoshConsole.PSHost.PoshOptions)Host.PrivateData.BaseObject;
         var topLeft = new System.Drawing.Point(
                              (int)_options.XamlUI.RootWindow.Left,
                              (int)_options.XamlUI.RootWindow.Top);
         var screen = System.Windows.Forms.Screen.FromPoint(topLeft);
         
         _options.Settings.Save();
         switch (_options.Settings.SettingsKey)
         {
            case "Default":
               {
                  _options.Settings.SettingsKey = "Quake";
                  _options.Settings.Reload();
                  if (_options.Settings.WindowWidth != screen.WorkingArea.Width)
                  {
                     _options.Settings.ShowInTaskbar = false;
                     _options.Settings.AutoHide = true;
                     _options.Settings.AlwaysOnTop = true;
                     _options.Settings.Animate = true;
                     _options.Settings.Opacity = 0.8;
                     _options.Settings.BorderThickness = new Thickness(0,0,0,5);
                     _options.Settings.BorderColorBottomRight = Colors.Red;
                     _options.Settings.BorderColorTopLeft = new Color() { A = 0xCC, R= 0xFF, G=0x33, B=0x00 };
                     _options.Settings.SnapToScreenEdge = true;

                     _options.Settings.WindowHeight = screen.WorkingArea.Height / 3;
                     _options.Settings.WindowWidth = screen.WorkingArea.Width;
                     _options.Settings.WindowTop = screen.WorkingArea.Top;
                     _options.Settings.WindowLeft = screen.WorkingArea.Left;

                     _options.Colors.DefaultBackground = ConsoleColor.Black;
                     _options.Colors.DefaultForeground = ConsoleColor.White;
                     //_options.Settings.FocusKey = "Win+OemTilde";
                     //_options.Settings.StartupBanner = false;
                     _options.Settings.Save();
                  }

               } break;
            case "Quake":
               {
                  _options.Settings.SettingsKey = "Default";
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
            default: break;
         }
      }
   }
}