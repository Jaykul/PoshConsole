using System;
using System.Collections;
using System.ComponentModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PoshWpf.Commands
{
   [Cmdlet(VerbsCommon.New, "ScriptDataSource")]
   public class NewScriptDataSourceCommand : Cmdlet
   {
      [Parameter(Mandatory= true, Position=0, HelpMessage = "A scriptblock to execute")]
      public ScriptBlock Script { get; set; }

      protected override void ProcessRecord()
      {
         WriteObject(new PowerShellDataSource { Script = Script });

         base.ProcessRecord();
      }
   }

   public class PowerShellDataSource : INotifyPropertyChanged
   {
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

      //public PowerShell Command
      //{
      //   get
      //   {
      //      return _powerShellCommand;
      //   }
      //}


      ScriptBlock _script;

      public ScriptBlock Script
      {
         get
         {
            return _script;
         }
         set
         {
            _script = value;
            //try
            //{
               _powerShellCommand.Commands.Clear();
               _powerShellCommand.AddScript(_script.ToString(), false);
               LastDebug = null;
               LastError = null;
               _outputCollection.Clear();
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
               _powerShellCommand.BeginInvoke<Object, PSObject>(null, _outputCollection);
            //}
            //catch
            //{

            //}
         }
      }

      readonly PowerShell _powerShellCommand;
      PSDataCollection<PSObject> _outputCollection;
      public PowerShellDataSource()
      {
         _powerShellCommand = PowerShell.Create();
         Runspace runspace = RunspaceFactory.CreateRunspace();
         runspace.Open();
         _powerShellCommand.Runspace = runspace;
         _outputCollection = new PSDataCollection<PSObject>();
         _outputCollection.DataAdded += OutputCollection_DataAdded;
         _powerShellCommand.Streams.Progress.DataAdded += Progress_DataAdded;
      }


      void Initialize()
      {
         _powerShellCommand.Streams.Debug.DataAdded += Debug_DataAdded;
         _powerShellCommand.Streams.Error.DataAdded += Error_DataAdded;
         _outputCollection = new PSDataCollection<PSObject>();
         _outputCollection.DataAdded += OutputCollection_DataAdded;
         _powerShellCommand.Streams.Progress.DataAdded += Progress_DataAdded;
         _powerShellCommand.Streams.Verbose.DataAdded += Verbose_DataAdded;
         _powerShellCommand.Streams.Warning.DataAdded += Warning_DataAdded;
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

      void OutputCollection_DataAdded(object sender, DataAddedEventArgs e)
      {
         if (PropertyChanged == null) return;
         var collection = sender as PSDataCollection<PSObject>;
         if (collection == null) return;
         LastOutput = collection[e.Index];
         PropertyChanged(this, new PropertyChangedEventArgs("Output"));
         PropertyChanged(this, new PropertyChangedEventArgs("LastOutput"));
      }
      public event PropertyChangedEventHandler PropertyChanged;
   }
}
