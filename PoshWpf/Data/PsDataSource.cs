using System;
using System.Collections;
using System.ComponentModel;
using System.Management.Automation;
using System.Windows.Threading;

namespace PoshWpf.Data
{
   public class PSDataSource : INotifyPropertyChanged
   {
      public event PropertyChangedEventHandler PropertyChanged;

      readonly PowerShell _powerShellCommand;
      readonly PSDataCollection<PSObject> _outputCollection;
      ScriptBlock _script;
      private DispatcherTimer _timer;

      #region Properties {
      public IEnumerable Output
      {
         get
         {
            var returnValue = new PSObject[_outputCollection.Count];
            _outputCollection.CopyTo(returnValue, 0);
            return returnValue;
         }
      }

      public PSObject LastOutput { get; private set; }

      public IEnumerable Error
      {
         get
         {
            var returnValue = new ErrorRecord[_powerShellCommand.Streams.Error.Count];
            _powerShellCommand.Streams.Error.CopyTo(returnValue, 0);
            return returnValue;
         }
      }

      public ErrorRecord LastError { get; private set; }

      public IEnumerable Warning
      {
         get
         {
            var returnValue = new WarningRecord[_powerShellCommand.Streams.Warning.Count];
            _powerShellCommand.Streams.Warning.CopyTo(returnValue, 0);
            return returnValue;
         }
      }

      public WarningRecord LastWarning { get; private set; }

      public IEnumerable Verbose
      {
         get
         {
            var returnValue = new VerboseRecord[_powerShellCommand.Streams.Verbose.Count];
            _powerShellCommand.Streams.Verbose.CopyTo(returnValue, 0);
            return returnValue;
         }
      }

      public VerboseRecord LastVerbose { get; private set; }


      public IEnumerable Debug
      {
         get
         {
            var returnValue = new DebugRecord[_powerShellCommand.Streams.Debug.Count];
            _powerShellCommand.Streams.Debug.CopyTo(returnValue, 0);
            return returnValue;
         }
      }

      public DebugRecord LastDebug { get; private set; }


      public IEnumerable Progress
      {
         get
         {
            var returnValue = new ProgressRecord[_powerShellCommand.Streams.Progress.Count];
            _powerShellCommand.Streams.Progress.CopyTo(returnValue, 0);
            return returnValue;
         }
      }

      public ProgressRecord LastProgress { get; private set; }
      #endregion }

      public ScriptBlock Script
      {
         get
         {
            return _script;
         }
         set
         {
            _script = value;

            _outputCollection.Clear();
            _powerShellCommand.Streams.ClearStreams();

            _powerShellCommand.Commands.Clear();
            _powerShellCommand.AddScript(_script.ToString(), false);

            LastDebug = null;
            LastError = null;
            LastOutput = null;
            LastProgress = null;
            LastVerbose = null;
            LastWarning = null;
            if (PropertyChanged != null)
            {
               PropertyChanged(this, new PropertyChangedEventArgs("Script"));

               PropertyChanged(this, new PropertyChangedEventArgs("Debug"));
               PropertyChanged(this, new PropertyChangedEventArgs("LastDebug"));

               PropertyChanged(this, new PropertyChangedEventArgs("Error"));
               PropertyChanged(this, new PropertyChangedEventArgs("LastError"));

               PropertyChanged(this, new PropertyChangedEventArgs("Output"));
               PropertyChanged(this, new PropertyChangedEventArgs("LastOutput"));

               PropertyChanged(this, new PropertyChangedEventArgs("Progress"));
               PropertyChanged(this, new PropertyChangedEventArgs("LastProgress"));

               PropertyChanged(this, new PropertyChangedEventArgs("Verbose"));
               PropertyChanged(this, new PropertyChangedEventArgs("LastVerbose"));

               PropertyChanged(this, new PropertyChangedEventArgs("Warning"));
               PropertyChanged(this, new PropertyChangedEventArgs("LastWarning"));
            }
         }
      }

      public void Start()
      {
         _timer.Start();
      }

      public void Stop()
      {
         _timer.Stop();
      }

      public void Invoke( PSObject[] input = null)
      {
         if(input != null)
         {
            Input = new PSDataCollection<PSObject>(input);
         }

         _powerShellCommand.BeginInvoke(Input, _outputCollection);
      }

      public TimeSpan TimeSpan
      {
         get { return _timer.Interval; }
         set
         {
            if(_timer == null)
            {
               _timer = new DispatcherTimer { Interval = value };
            }
            else if(_timer.IsEnabled) {
               _timer.Stop();
               _timer.Interval = value;
               _timer.Start();
            } 
            else
            {
               _timer.Interval = value;
            }
            _timer.Tick += Invoke;
         }
      }

      private void Invoke(object sender, EventArgs e)
      {
         Invoke();
      }

      public PSDataCollection<PSObject> Input { get; private set; }

      public PSDataSource(
         ScriptBlock script = null,  
         PSDataCollection<PSObject> input = null, 
         TimeSpan interval = new TimeSpan(), 
         bool invokeImmediately = false)
      {
         _powerShellCommand = PowerShell.Create();
         _outputCollection = new PSDataCollection<PSObject>();
         _outputCollection.DataAdded += Output_DataAdded;
         _powerShellCommand.Streams.Debug.DataAdded += Debug_DataAdded;
         _powerShellCommand.Streams.Error.DataAdded += Error_DataAdded;
         _powerShellCommand.Streams.Progress.DataAdded += Progress_DataAdded;
         _powerShellCommand.Streams.Verbose.DataAdded += Verbose_DataAdded;
         _powerShellCommand.Streams.Warning.DataAdded += Warning_DataAdded;


         Script = script;
         TimeSpan = TimeSpan.Zero;
         Input = input ?? new PSDataCollection<PSObject>();
         
         if (invokeImmediately || TimeSpan.Zero < interval) { Invoke(); }
         
         if (TimeSpan.Zero < interval)
         {
            _timer.Start();
         }
      }

      void Debug_DataAdded(object sender, DataAddedEventArgs e)
      {
         if (PropertyChanged == null) return;
         var collection = sender as PSDataCollection<DebugRecord>;
         if (collection == null) return;
         LastDebug = collection[e.Index];
         PropertyChanged(this, new PropertyChangedEventArgs("Debug"));
         PropertyChanged(this, new PropertyChangedEventArgs("LastDebug"));
      }

      void Error_DataAdded(object sender, DataAddedEventArgs e)
      {
         if (PropertyChanged == null) return;
         var collection = sender as PSDataCollection<ErrorRecord>;
         if (collection == null) return;
         LastError = collection[e.Index];
         PropertyChanged(this, new PropertyChangedEventArgs("Error"));
         PropertyChanged(this, new PropertyChangedEventArgs("LastError"));
      }

      void Warning_DataAdded(object sender, DataAddedEventArgs e)
      {
         if (PropertyChanged == null) return;
         var collection = sender as PSDataCollection<WarningRecord>;
         if (collection == null) return;
         LastWarning = collection[e.Index];
         PropertyChanged(this, new PropertyChangedEventArgs("Warning"));
         PropertyChanged(this, new PropertyChangedEventArgs("LastWarning"));
      }

      void Verbose_DataAdded(object sender, DataAddedEventArgs e)
      {
         if (PropertyChanged == null) return;
         var collection = sender as PSDataCollection<VerboseRecord>;
         if (collection == null) return;
         LastVerbose = collection[e.Index];
         PropertyChanged(this, new PropertyChangedEventArgs("Verbose"));
         PropertyChanged(this, new PropertyChangedEventArgs("LastVerbose"));
      }

      void Progress_DataAdded(object sender, DataAddedEventArgs e)
      {
         if (PropertyChanged == null) return;
         var collection = sender as PSDataCollection<ProgressRecord>;
         if (collection == null) return;
         LastProgress = collection[e.Index];
         PropertyChanged(this, new PropertyChangedEventArgs("Progress"));
         PropertyChanged(this, new PropertyChangedEventArgs("LastProgress"));
      }

      void Output_DataAdded(object sender, DataAddedEventArgs e)
      {
         if (PropertyChanged == null) return;
         var collection = sender as PSDataCollection<PSObject>;
         if (collection == null) return;
         LastOutput = collection[e.Index];
         PropertyChanged(this, new PropertyChangedEventArgs("Output"));
         PropertyChanged(this, new PropertyChangedEventArgs("LastOutput"));
      }
   }
}