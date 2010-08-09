using System;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.PowerShell.Commands;

namespace PoshWpf.Commands
{
   [Cmdlet(VerbsData.Export, "BootsImage", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ShowAll")]
   public class ExportBootsImageCommand : PSCmdlet
   {
      private const string ParamSetLiteral = "Literal";
      private const string ParmamSetPath = "Path";

      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), Parameter(Position = 1, Mandatory = true, ValueFromPipeline = true)]
      public Control[] Control { get; set; }

      [Parameter(Position = 0, Mandatory = false, ParameterSetName = ParmamSetPath)]
      [ValidateNotNullOrEmpty]
      public string ImagePath
      {
         get { return _path; }
         set { _path = value; }
      }
      private string _path;


      [Parameter(Position = 2, Mandatory = false)]
      public System.Windows.Size Resolution
      {
         get { return _resolution; }
         set { _resolution = value; }
      }
      System.Windows.Size _resolution = new System.Windows.Size(96.0, 96.0);

      [Parameter(Position = 3, Mandatory = false)]
      public PixelFormat PixelFormat
      {
         get { return _pixelFormat; }
         set { _pixelFormat = value; }
      }
      PixelFormat _pixelFormat = PixelFormats.Pbgra32;

      protected override void BeginProcessing()
      {
         // This will hold information about the provider containing
         // the items that this path string might resolve to.                
         ProviderInfo provider;
         // This will be used by the method that processes literal paths
         PSDriveInfo drive;
         // no wildcards, so don't try to expand any * or ? symbols.                    
         _path = this.SessionState.Path.GetUnresolvedProviderPathFromPSPath(_path, out provider, out drive);

         // ensure that this path is on the filesystem
         if (IsFileSystemPath(provider, _path) == false)
         {
            _path = string.Empty;
         }
         base.BeginProcessing();
      }

      protected override void ProcessRecord()
      {
         foreach (var control in Control)
         {
            TakeScreenCapture(control, _path);
         }
         base.ProcessRecord();
      }

      private bool IsFileSystemPath(ProviderInfo provider, string path)
      {
         bool isFileSystem = true;
         // check that this provider is the filesystem
         if (provider.ImplementingType != typeof(FileSystemProvider))
         {
            // create a .NET exception wrapping our error text
            ArgumentException ex = new ArgumentException(path +
                " does not resolve to a path on the FileSystem provider.");
            // wrap this in a powershell errorrecord
            ErrorRecord error = new ErrorRecord(ex, "InvalidProvider",
                ErrorCategory.InvalidArgument, path);
            // write a non-terminating error to pipeline
            WriteError(error);
            // tell our caller that the item was not on the filesystem
            isFileSystem = false;
         }
         return isFileSystem;
      }

      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling"),
       System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
      private void TakeScreenCapture(Control control, string imageFileName)
      {
         UIElement element = control;
         if (element.Dispatcher.Thread.IsAlive && !element.Dispatcher.HasShutdownStarted)
         {
            WriteObject(
            element.Dispatcher.Invoke((Func<object>)(() =>
            {
               try
               {
                  Window w = element as Window;
                  if (w != null && w.Content is UIElement)
                  {
                     element = ((Window)element).Content as UIElement;
                  }

                  var rtb = new RenderTargetBitmap((int)element.RenderSize.Width, (int)element.RenderSize.Height, _resolution.Width, _resolution.Height, _pixelFormat);
                  rtb.Render(element);

                  if (string.IsNullOrEmpty(imageFileName))
                  {
                     Clipboard.SetImage(rtb);
                     WriteWarning("No file path provided for image, screen capture placed on clipboard");
                     return null;
                  }
                  else
                  {
                     BitmapEncoder encoder = null;

                     switch (Path.GetExtension(imageFileName).ToUpperInvariant())
                     {
                        case ".BMP": { encoder = new BmpBitmapEncoder(); break; }
                        case ".GIF": { encoder = new GifBitmapEncoder(); break; }
                        case ".JPG": { encoder = new JpegBitmapEncoder(); break; }
                        case ".JPEG": { encoder = new JpegBitmapEncoder(); break; }
                        case ".PNG": { encoder = new PngBitmapEncoder(); break; }
                        case ".TIF": { encoder = new TiffBitmapEncoder(); break; }
                        case ".TIFF": { encoder = new TiffBitmapEncoder(); break; }
                        case ".WDP": { encoder = new WmpBitmapEncoder(); break; }
                        default:
                           {
                              encoder = new PngBitmapEncoder();
                              imageFileName = Path.Combine(Path.GetDirectoryName(imageFileName), Path.GetFileNameWithoutExtension(imageFileName) + ".png");
                              break;
                           }
                     }

                     encoder.Frames.Add(BitmapFrame.Create(rtb));
                     // Make sure .Net is thinking the same thing as PowerShell
                     var now = Environment.CurrentDirectory;
                     Environment.CurrentDirectory = base.CurrentProviderLocation("FileSystem").ProviderPath;

                     int i = 0;
                     var fileName = imageFileName;
                     while (File.Exists(fileName))
                     {
                        fileName = Path.GetFileNameWithoutExtension(imageFileName) + String.Format(CultureInfo.InvariantCulture, "{0:000}", i++) + Path.GetExtension(imageFileName);
                     }

                     using(var stream = File.Create(Path.Combine(Path.GetDirectoryName(imageFileName), fileName)))
                     {
                        encoder.Save(stream);
                     }
                     Environment.CurrentDirectory = now;
                     return new FileInfo(fileName);
                  }
               }
               catch (Exception ex)
               {
                  return new ErrorRecord(ex, "ScreenCaptureError", ErrorCategory.InvalidOperation, element);
               }
            })));
         }
         else
         {
            WriteError(new ErrorRecord(new System.Threading.ThreadStateException("Can't take screenshot of a window that's not running"), "WindowStopped", ErrorCategory.ResourceUnavailable, element));
         }
      }

   }
}
