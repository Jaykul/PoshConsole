using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;
using System.Windows.Data;
using System.Windows.Threading;

namespace PoshWpf.Data {
    public class PSDataSource : ListCollectionView {
        private readonly PowerShell _powerShellCommand;
        private readonly DispatcherTimer _timer;

        public ListCollectionView Progress { get; private set; }
        public ListCollectionView Verbose { get; private set; }
        public ListCollectionView Warning { get; private set; }
        public ListCollectionView Error { get; private set; }

        protected bool AccumulateOutput { get; set; }

        protected TimeSpan TimeSpan { get; set; }

        protected ScriptBlock Script { get; set; }

        public PSDataSource( ScriptBlock script,
                             IEnumerable<PSObject> input = null,
                             TimeSpan interval = new TimeSpan(), 
                             bool invokeImmediately = false, 
                             bool accumulateOutput = false) : base(new PSDataCollection<PSObject>()) {

            Script = script;
            TimeSpan = TimeSpan.Zero;
            AccumulateOutput = accumulateOutput;

            _powerShellCommand = PowerShell.Create().AddScript( Script.ToString() );            

            Error = new ListCollectionView(_powerShellCommand.Streams.Error);
            Warning = new ListCollectionView(_powerShellCommand.Streams.Warning);
            Verbose = new ListCollectionView(_powerShellCommand.Streams.Verbose);
            Progress = new ListCollectionView(_powerShellCommand.Streams.Progress);

            if (invokeImmediately || TimeSpan.Zero < interval) { Invoke(input); }

            if (TimeSpan.Zero < interval) {
               _timer = new DispatcherTimer(TimeSpan, DispatcherPriority.Normal, Invoke, Dispatcher.CurrentDispatcher);
               _timer.Start();
            }
        }

        public void Start() {
           if (_timer != null)
            _timer.Start();
        }

        public void Stop() {
           if (_timer != null)
              _timer.Stop();
        }

        private void Invoke(object sender, EventArgs e) {
            Invoke();
        }

        public void Invoke(IEnumerable<PSObject> input = null) {
            using (var inputCollection = input != null ? new PSDataCollection<PSObject>(input) : new PSDataCollection<PSObject>()) {
                if (!AccumulateOutput) {
                    InternalList.Clear();
                }
                _powerShellCommand.BeginInvoke<PSObject,PSObject>(inputCollection, InternalList as PSDataCollection<PSObject>);
            }
        }
    }

    //public class PSDataSource2 : INotifyPropertyChanged, ICollectionView, IDisposable {
    //    public event PropertyChangedEventHandler PropertyChanged;

    //    readonly PowerShell _powerShellCommand;
    //    readonly PSDataCollection<PSObject> _outputCollection;
    //    ScriptBlock _script;
    //    private DispatcherTimer _timer;

    //    #region Properties {
    //    public IEnumerable Output {
    //        get {
    //            var returnValue = new PSObject[_outputCollection.Count];
    //            _outputCollection.CopyTo(returnValue, 0);
    //            return returnValue;
    //        }
    //    }

    //    public PSObject LastOutput { get; private set; }

    //    public IEnumerable Error {
    //        get {
    //            var returnValue = new ErrorRecord[_powerShellCommand.Streams.Error.Count];
    //            _powerShellCommand.Streams.Error.CopyTo(returnValue, 0);
    //            return returnValue;
    //        }
    //    }

    //    public ErrorRecord LastError { get; private set; }

    //    public IEnumerable Warning {
    //        get {
    //            var returnValue = new WarningRecord[_powerShellCommand.Streams.Warning.Count];
    //            _powerShellCommand.Streams.Warning.CopyTo(returnValue, 0);
    //            return returnValue;
    //        }
    //    }

    //    public WarningRecord LastWarning { get; private set; }

    //    public IEnumerable Verbose {
    //        get {
    //            var returnValue = new VerboseRecord[_powerShellCommand.Streams.Verbose.Count];
    //            _powerShellCommand.Streams.Verbose.CopyTo(returnValue, 0);
    //            return returnValue;
    //        }
    //    }

    //    public VerboseRecord LastVerbose { get; private set; }

    //    public IEnumerable Debug {
    //        get {
    //            var returnValue = new DebugRecord[_powerShellCommand.Streams.Debug.Count];
    //            _powerShellCommand.Streams.Debug.CopyTo(returnValue, 0);
    //            return returnValue;
    //        }
    //    }

    //    public DebugRecord LastDebug { get; private set; }

    //    public IEnumerable Progress {
    //        get {
    //            var returnValue = new ProgressRecord[_powerShellCommand.Streams.Progress.Count];
    //            _powerShellCommand.Streams.Progress.CopyTo(returnValue, 0);
    //            return returnValue;
    //        }
    //    }

    //    public ProgressRecord LastProgress { get; private set; }
    //    #endregion }

    //    public ScriptBlock Script {
    //        get {
    //            return _script;
    //        }
    //        set {
    //            _script = value;

    //            _outputCollection.Clear();
    //            _powerShellCommand.Streams.ClearStreams();

    //            _powerShellCommand.Commands.Clear();
    //            _powerShellCommand.AddScript(_script.ToString(), false);

    //            LastDebug = null;
    //            LastError = null;
    //            LastOutput = null;
    //            LastProgress = null;
    //            LastVerbose = null;
    //            LastWarning = null;

    //            // Notify that all of the streams have been cleared
    //            if (PropertyChanged != null) {
    //                PropertyChanged(this, new PropertyChangedEventArgs("Script"));

    //                PropertyChanged(this, new PropertyChangedEventArgs("Debug"));
    //                PropertyChanged(this, new PropertyChangedEventArgs("LastDebug"));

    //                PropertyChanged(this, new PropertyChangedEventArgs("Error"));
    //                PropertyChanged(this, new PropertyChangedEventArgs("LastError"));

    //                PropertyChanged(this, new PropertyChangedEventArgs("Output"));
    //                PropertyChanged(this, new PropertyChangedEventArgs("LastOutput"));

    //                PropertyChanged(this, new PropertyChangedEventArgs("Progress"));
    //                PropertyChanged(this, new PropertyChangedEventArgs("LastProgress"));

    //                PropertyChanged(this, new PropertyChangedEventArgs("Verbose"));
    //                PropertyChanged(this, new PropertyChangedEventArgs("LastVerbose"));

    //                PropertyChanged(this, new PropertyChangedEventArgs("Warning"));
    //                PropertyChanged(this, new PropertyChangedEventArgs("LastWarning"));
    //            }
    //        }
    //    }



    //    public TimeSpan TimeSpan {
    //        get { return _timer.Interval; }
    //        set {
    //            if (_timer == null) {
    //                _timer = new DispatcherTimer { Interval = value };
    //            }
    //            else if (_timer.IsEnabled) {
    //                _timer.Stop();
    //                _timer.Interval = value;
    //                _timer.Start();
    //            }
    //            else {
    //                _timer.Interval = value;
    //            }
    //            _timer.Tick += Invoke;
    //        }
    //    }



    //    private PSDataCollection<PSObject> _inputCollection;
    //    public PSDataCollection<PSObject> Input { get { return _inputCollection; } }

    //    public bool AccumulateOutput { get; set; }

    //    public PSDataSource2(
    //       ScriptBlock script = null,
    //       IEnumerable<PSObject> input = null,
    //       TimeSpan interval = new TimeSpan(),
    //       bool invokeImmediately = false,
    //       bool accumulateOutput = false) {
    //        AccumulateOutput = accumulateOutput;

    //        _powerShellCommand = PowerShell.Create();
    //        _outputCollection = new PSDataCollection<PSObject>();

    //        _outputCollection.DataAdded += Output_DataAdded;
    //        _powerShellCommand.Streams.Debug.DataAdded += Debug_DataAdded;
    //        _powerShellCommand.Streams.Error.DataAdded += Error_DataAdded;
    //        _powerShellCommand.Streams.Progress.DataAdded += Progress_DataAdded;
    //        _powerShellCommand.Streams.Verbose.DataAdded += Verbose_DataAdded;
    //        _powerShellCommand.Streams.Warning.DataAdded += Warning_DataAdded;

    //        Script = script;
    //        TimeSpan = TimeSpan.Zero;
    //        _inputCollection = input == null ? new PSDataCollection<PSObject>() : new PSDataCollection<PSObject>(input);

    //        if (invokeImmediately || TimeSpan.Zero < interval) { Invoke(); }

    //        if (TimeSpan.Zero < interval) {
    //            _timer.Start();
    //        }
    //    }

    //    void Debug_DataAdded(object sender, DataAddedEventArgs e) {
    //        if (PropertyChanged == null)
    //            return;
    //        var collection = sender as PSDataCollection<DebugRecord>;
    //        if (collection == null)
    //            return;
    //        LastDebug = collection[e.Index];
    //        PropertyChanged(this, new PropertyChangedEventArgs("Debug"));
    //        PropertyChanged(this, new PropertyChangedEventArgs("LastDebug"));
    //    }

    //    void Error_DataAdded(object sender, DataAddedEventArgs e) {
    //        if (PropertyChanged == null)
    //            return;
    //        var collection = sender as PSDataCollection<ErrorRecord>;
    //        if (collection == null)
    //            return;
    //        LastError = collection[e.Index];
    //        PropertyChanged(this, new PropertyChangedEventArgs("Error"));
    //        PropertyChanged(this, new PropertyChangedEventArgs("LastError"));
    //    }

    //    void Warning_DataAdded(object sender, DataAddedEventArgs e) {
    //        if (PropertyChanged == null)
    //            return;
    //        var collection = sender as PSDataCollection<WarningRecord>;
    //        if (collection == null)
    //            return;
    //        LastWarning = collection[e.Index];
    //        PropertyChanged(this, new PropertyChangedEventArgs("Warning"));
    //        PropertyChanged(this, new PropertyChangedEventArgs("LastWarning"));
    //    }

    //    void Verbose_DataAdded(object sender, DataAddedEventArgs e) {
    //        if (PropertyChanged == null)
    //            return;
    //        var collection = sender as PSDataCollection<VerboseRecord>;
    //        if (collection == null)
    //            return;
    //        LastVerbose = collection[e.Index];
    //        PropertyChanged(this, new PropertyChangedEventArgs("Verbose"));
    //        PropertyChanged(this, new PropertyChangedEventArgs("LastVerbose"));
    //    }

    //    void Progress_DataAdded(object sender, DataAddedEventArgs e) {
    //        if (PropertyChanged == null)
    //            return;
    //        var collection = sender as PSDataCollection<ProgressRecord>;
    //        if (collection == null)
    //            return;
    //        LastProgress = collection[e.Index];
    //        PropertyChanged(this, new PropertyChangedEventArgs("Progress"));
    //        PropertyChanged(this, new PropertyChangedEventArgs("LastProgress"));
    //    }

    //    void Output_DataAdded(object sender, DataAddedEventArgs e) {
    //        if (PropertyChanged == null)
    //            return;
    //        var collection = sender as PSDataCollection<PSObject>;
    //        if (collection == null)
    //            return;
    //        LastOutput = collection[e.Index];
    //        PropertyChanged(this, new PropertyChangedEventArgs("Output"));
    //        PropertyChanged(this, new PropertyChangedEventArgs("LastOutput"));
    //    }

    //    /// <summary>Releases unmanaged resources and performs other cleanup operations 
    //    /// before the <see cref="PSDataSource"/> is reclaimed by garbage collection.
    //    /// Use C# destructor syntax for finalization code.
    //    /// This destructor will run only if the Dispose method does not get called.
    //    /// </summary>
    //    /// <remarks>Do not provide destructors in types derived from this class.</remarks>
    //    ~PSDataSource2() {
    //        // Instead of cleaning up in BOTH Dispose() and here ...
    //        // We call Dispose(false) for the best readability and maintainability.
    //        Dispose(false);
    //    }

    //    /// <summary>
    //    /// Returns an enumerator that iterates through a collection.
    //    /// </summary>
    //    /// <returns>
    //    /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
    //    /// </returns>
    //    /// <filterpriority>2</filterpriority>
    //    public IEnumerator GetEnumerator() {
    //        return _outputCollection.GetEnumerator();
    //    }

    //    /// <summary>
    //    /// Implement IDisposable
    //    /// Performs application-defined tasks associated with 
    //    /// freeing, releasing, or resetting unmanaged resources.
    //    /// </summary>
    //    public void Dispose() {
    //        // This object will be cleaned up by the Dispose method.
    //        // Therefore, we call GC.SupressFinalize to tell the runtime 
    //        // that we dont' need to be finalized (we would clean up twice)
    //        Dispose(true);
    //        GC.SuppressFinalize(this);

    //    }

    //    private bool _disposed;
    //    /// <summary>
    //    /// Handles actual cleanup actions, under two different scenarios
    //    /// </summary>
    //    /// <param name="disposing">if set to <c>true</c> we've been called directly or 
    //    /// indirectly by user code and can clean up both managed and unmanaged resources.
    //    /// Otherwise it's been called from the destructor/finalizer and we can't
    //    /// reference other managed objects (they might already be disposed).
    //    ///</param>
    //    private void Dispose(bool disposing) {
    //        // Check to see if Dispose has already been called.
    //        if (!_disposed) {
    //            try {
    //                // // If disposing equals true, dispose all managed resources ALSO.
    //                if (disposing) {
    //                    _outputCollection.Dispose();
    //                    _inputCollection.Dispose();
    //                }

    //                // Clean up UnManaged resources
    //                // If disposing is false, only the following code is executed.
    //            }
    //            catch (Exception e) {
    //                Trace.WriteLine(e.Message);
    //                Trace.WriteLine(e.StackTrace);
    //                throw;
    //            }

    //        }
    //        _disposed = true;
    //    }

    //    #region Implementation of INotifyCollectionChanged

    //    public event NotifyCollectionChangedEventHandler CollectionChanged;

    //    #endregion

    //    #region Implementation of ICollectionView

    //    /// <summary>
    //    /// Returns a value that indicates whether a given item belongs to this collection view.
    //    /// </summary>
    //    /// <returns>
    //    /// true if the item belongs to this collection view; otherwise, false.
    //    /// </returns>
    //    /// <param name="item">The object to check.</param>
    //    public bool Contains(object item) {
    //        return _outputCollection.Contains(item as PSObject);
    //    }

    //    /// <summary>
    //    /// Recreates the view.
    //    /// </summary>
    //    public void Refresh() {
    //        throw new NotImplementedException();
    //    }

    //    /// <summary>
    //    /// Enters a defer cycle that you can use to merge changes to the view and delay automatic refresh.
    //    /// </summary>
    //    /// <returns>
    //    /// An <see cref="T:System.IDisposable"/> object that you can use to dispose of the calling object.
    //    /// </returns>
    //    public IDisposable DeferRefresh() {
    //        throw new NotImplementedException();
    //    }

    //    /// <summary>
    //    /// Sets the first item in the view as the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem"/>.
    //    /// </summary>
    //    /// <returns>
    //    /// true if the resulting <see cref="P:System.ComponentModel.ICollectionView.CurrentItem"/> is an item within the view; otherwise, false.
    //    /// </returns>
    //    public bool MoveCurrentToFirst() {
    //        throw new NotImplementedException();
    //    }

    //    /// <summary>
    //    /// Sets the last item in the view as the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem"/>.
    //    /// </summary>
    //    /// <returns>
    //    /// true if the resulting <see cref="P:System.ComponentModel.ICollectionView.CurrentItem"/> is an item within the view; otherwise, false.
    //    /// </returns>
    //    public bool MoveCurrentToLast() {
    //        throw new NotImplementedException();
    //    }

    //    /// <summary>
    //    /// Sets the item after the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem"/> in the view as the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem"/>.
    //    /// </summary>
    //    /// <returns>
    //    /// true if the resulting <see cref="P:System.ComponentModel.ICollectionView.CurrentItem"/> is an item within the view; otherwise, false.
    //    /// </returns>
    //    public bool MoveCurrentToNext() {
    //        throw new NotImplementedException();
    //    }

    //    /// <summary>
    //    /// Sets the item before the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem"/> in the view as the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem"/>.
    //    /// </summary>
    //    /// <returns>
    //    /// true if the resulting <see cref="P:System.ComponentModel.ICollectionView.CurrentItem"/> is an item within the view; otherwise, false.
    //    /// </returns>
    //    public bool MoveCurrentToPrevious() {
    //        throw new NotImplementedException();
    //    }

    //    /// <summary>
    //    /// Sets the specified item to be the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem"/> in the view.
    //    /// </summary>
    //    /// <returns>
    //    /// true if the resulting <see cref="P:System.ComponentModel.ICollectionView.CurrentItem"/> is within the view; otherwise, false.
    //    /// </returns>
    //    /// <param name="item">The item to set as the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem"/>.</param>
    //    public bool MoveCurrentTo(object item) {
    //        throw new NotImplementedException();
    //    }

    //    /// <summary>
    //    /// Sets the item at the specified index to be the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem"/> in the view.
    //    /// </summary>
    //    /// <returns>
    //    /// true if the resulting <see cref="P:System.ComponentModel.ICollectionView.CurrentItem"/> is an item within the view; otherwise, false.
    //    /// </returns>
    //    /// <param name="position">The index to set the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem"/> to.</param>
    //    public bool MoveCurrentToPosition(int position) {
    //        throw new NotImplementedException();
    //    }

    //    /// <summary>
    //    /// Gets or sets the cultural info for any operations of the view that may differ by culture, such as sorting.
    //    /// </summary>
    //    /// <returns>
    //    /// The culture to use during sorting.
    //    /// </returns>
    //    public CultureInfo Culture {
    //        get { throw new NotImplementedException(); }
    //        set { throw new NotImplementedException(); }
    //    }

    //    /// <summary>
    //    /// Returns the underlying collection.
    //    /// </summary>
    //    /// <returns>
    //    /// An <see cref="T:System.Collections.IEnumerable"/> object that is the underlying collection.
    //    /// </returns>
    //    public IEnumerable SourceCollection {
    //        get { throw new NotImplementedException(); }
    //    }

    //    /// <summary>
    //    /// Gets or sets a callback used to determine if an item is suitable for inclusion in the view.
    //    /// </summary>
    //    /// <returns>
    //    /// A method used to determine if an item is suitable for inclusion in the view.
    //    /// </returns>
    //    public Predicate<object> Filter {
    //        get { throw new NotImplementedException(); }
    //        set { throw new NotImplementedException(); }
    //    }

    //    /// <summary>
    //    /// Gets a value that indicates whether this view supports filtering via the <see cref="P:System.ComponentModel.ICollectionView.Filter"/> property.
    //    /// </summary>
    //    /// <returns>
    //    /// true if this view support filtering; otherwise, false.
    //    /// </returns>
    //    public bool CanFilter {
    //        get { throw new NotImplementedException(); }
    //    }

    //    /// <summary>
    //    /// Gets a collection of <see cref="T:System.ComponentModel.SortDescription"/> objects that describe how the items in the collection are sorted in the view.
    //    /// </summary>
    //    /// <returns>
    //    /// A collection of <see cref="T:System.ComponentModel.SortDescription"/> objects that describe how the items in the collection are sorted in the view.
    //    /// </returns>
    //    public SortDescriptionCollection SortDescriptions {
    //        get { throw new NotImplementedException(); }
    //    }

    //    /// <summary>
    //    /// Gets a value that indicates whether this view supports sorting via the <see cref="P:System.ComponentModel.ICollectionView.SortDescriptions"/> property.
    //    /// </summary>
    //    /// <returns>
    //    /// true if this view supports sorting; otherwise, false.
    //    /// </returns>
    //    public bool CanSort {
    //        get { throw new NotImplementedException(); }
    //    }

    //    /// <summary>
    //    /// Gets a value that indicates whether this view supports grouping via the <see cref="P:System.ComponentModel.ICollectionView.GroupDescriptions"/> property.
    //    /// </summary>
    //    /// <returns>
    //    /// true if this view supports grouping; otherwise, false.
    //    /// </returns>
    //    public bool CanGroup {
    //        get { throw new NotImplementedException(); }
    //    }

    //    /// <summary>
    //    /// Gets a collection of <see cref="T:System.ComponentModel.GroupDescription"/> objects that describe how the items in the collection are grouped in the view.
    //    /// </summary>
    //    /// <returns>
    //    /// A collection of <see cref="T:System.ComponentModel.GroupDescription"/> objects that describe how the items in the collection are grouped in the view.
    //    /// </returns>
    //    public ObservableCollection<GroupDescription> GroupDescriptions {
    //        get { throw new NotImplementedException(); }
    //    }

    //    /// <summary>
    //    /// Gets the top-level groups.
    //    /// </summary>
    //    /// <returns>
    //    /// A read-only collection of the top-level groups or null if there are no groups.
    //    /// </returns>
    //    public ReadOnlyObservableCollection<object> Groups {
    //        get { throw new NotImplementedException(); }
    //    }

    //    /// <summary>
    //    /// Returns a value that indicates whether the resulting view is empty.
    //    /// </summary>
    //    /// <returns>
    //    /// true if the resulting view is empty; otherwise, false.
    //    /// </returns>
    //    public bool IsEmpty {
    //        get { throw new NotImplementedException(); }
    //    }

    //    /// <summary>
    //    /// Gets the current item in the view.
    //    /// </summary>
    //    /// <returns>
    //    /// The current item of the view or null if there is no current item.
    //    /// </returns>
    //    public object CurrentItem {
    //        get { throw new NotImplementedException(); }
    //    }

    //    /// <summary>
    //    /// Gets the ordinal position of the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem"/> within the view.
    //    /// </summary>
    //    /// <returns>
    //    /// The ordinal position of the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem"/> within the view.
    //    /// </returns>
    //    public int CurrentPosition {
    //        get { throw new NotImplementedException(); }
    //    }

    //    /// <summary>
    //    /// Gets a value that indicates whether the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem"/> of the view is beyond the end of the collection.
    //    /// </summary>
    //    /// <returns>
    //    /// Returns true if the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem"/> of the view is beyond the end of the collection; otherwise, false.
    //    /// </returns>
    //    public bool IsCurrentAfterLast {
    //        get { throw new NotImplementedException(); }
    //    }

    //    /// <summary>
    //    /// Gets a value that indicates whether the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem"/> of the view is beyond the beginning of the collection.
    //    /// </summary>
    //    /// <returns>
    //    /// Returns true if the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem"/> of the view is beyond the beginning of the collection; otherwise, false.
    //    /// </returns>
    //    public bool IsCurrentBeforeFirst {
    //        get { throw new NotImplementedException(); }
    //    }

    //    public event CurrentChangingEventHandler CurrentChanging;
    //    public event EventHandler CurrentChanged;

    //    #endregion
    //}
}