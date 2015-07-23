using System.Threading;

namespace PoshCode.PowerShell
{
    internal class SyncEvents
    {
        // the exit thread event needs to be in every signalling array
        private readonly EventWaitHandle _abortProcessingEvent;
        private readonly EventWaitHandle _emptyQueueEvent;
        private readonly WaitHandle[] _endQueueHandles;
        private readonly EventWaitHandle _exitThreadEvent;
        // this event signals new items
        private readonly EventWaitHandle _newItemEvent;
        // these events signal the end of processing
        // the empty queue event: temporary end of processing because there's no items left


        private readonly WaitHandle[] _newItemHandles;
        private readonly EventWaitHandle _pipelineFinishedEvent;


        public SyncEvents()
        {
            _newItemEvent = new AutoResetEvent(false);
            _exitThreadEvent = new ManualResetEvent(false);
            _emptyQueueEvent = new ManualResetEvent(false);
            _abortProcessingEvent = new ManualResetEvent(false);
            _pipelineFinishedEvent = new AutoResetEvent(false);

            _newItemHandles = new WaitHandle[3];
            _newItemHandles[0] = _exitThreadEvent;
            _newItemHandles[1] = _abortProcessingEvent;
            _newItemHandles[2] = _newItemEvent;

            _endQueueHandles = new WaitHandle[3];
            _endQueueHandles[0] = _exitThreadEvent;
            _endQueueHandles[1] = _abortProcessingEvent;
            _endQueueHandles[2] = _emptyQueueEvent;
        }

        public EventWaitHandle PipelineFinishedEvent
        {
            get { return _pipelineFinishedEvent; }
        }

        /// <summary>
        /// Gets the exit thread event.
        /// </summary>
        /// <value>The exit thread event.</value>
        public EventWaitHandle ExitThreadEvent
        {
            get { return _exitThreadEvent; }
        }

        /// <summary>
        /// Gets the new item event.
        /// </summary>
        /// <value>The new item event.</value>
        public EventWaitHandle NewItemEvent
        {
            get { return _newItemEvent; }
        }

        /// <summary>
        /// Gets the empty queue event.
        /// </summary>
        /// <value>The empty queue event.</value>
        public EventWaitHandle EmptyQueueEvent
        {
            get { return _emptyQueueEvent; }
        }

        /// <summary>
        /// Gets the abort queue event.
        /// </summary>
        /// <value>The abort queue event.</value>
        public EventWaitHandle AbortQueueEvent
        {
            get { return _abortProcessingEvent; }
        }


        public WaitHandle[] NewItemEvents
        {
            get { return _newItemHandles; }
        }

        public WaitHandle[] TerminationEvents
        {
            get { return _endQueueHandles; }
        }
    }
}