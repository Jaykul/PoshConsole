// PresentationInterface by  LunaticExperimentalist http://lunex.spaces.live.com/blog/
// End User License: Public Domain/Unrestricted Use
// Provided "as is," with out warranty, and without any assertions of suitability
// for any purpose either express or implied. 

using System;
using System.Threading;
using System.Windows;
using System.Xml;
using System.Windows.Threading;
using System.Management.Automation.Runspaces;
using System.Management.Automation;

namespace PoshWpf
{
	// structure returned by the stop method
	public struct PresentationResult
	{
		// Contains the value of the DialogResult property of the window
		public bool DialogResult;
		// Contains the value of the Tag property of the window
		// may be used if a bool is insufficient
		public object WindowTag;
		public PresentationResult(bool dialogResult, object tag)
		{
			DialogResult = dialogResult;
			WindowTag = tag;
		}
	}

	// public interface can give a reference to the dispatcher assosiated with a window
	public interface IExposeDispatcher
	{
		System.Windows.Threading.Dispatcher Dispatcher { get; }
	}

	public delegate void SetWindowProperties(Window window);

	// this is an IAsynResult object returned by the start method
	// may be passed to the stop method to retrieve the dialog result or
	// may be discarded safely
	public class WindowDispatcherAsyncResult : IAsyncResult, IExposeDispatcher
	{
		EventWaitHandle DoneHandle;
		PresentationResult DialogResult;
		Window _window;
		object StateObject;
		bool Completed;
		bool IsError;
		System.Exception Error;
		System.Windows.Threading.Dispatcher _Dispatcher;

		public WindowDispatcherAsyncResult(EventWaitHandle done)
		{
			Completed = false;
			DoneHandle = done;
		}

		/// <summary> returns the user defined object that was given at the start method</summary>
		public object AsyncState { get { return StateObject; } }
		/// <summary> returns a wait handle that will be signaled when the window is closed</summary>
		public WaitHandle AsyncWaitHandle { get { return DoneHandle; } }
		/// <summary> returns false, this is an asyncronous method</summary>
		public bool CompletedSynchronously { get { return false; } }
		/// <summary> returns true if the window is closed, or if initialization has failed</summary>
		public bool IsCompleted { get { return Completed; } }
		/// <summary> returns the dispatcher of the UI thread of the window</summary>
		public System.Windows.Threading.Dispatcher Dispatcher { get { return _Dispatcher; } }
		/// <summary>Returns the actual Window object</summary>
		public Window Window { get { return _window; } }

		/// <summary> used by the stop method to get the result</summary>
		internal PresentationResult Result
		{
			get
			{
				if (IsError) throw Error;
				return DialogResult;
			}
		}

		/// <summary> used to set the result when the window is closed</summary>
		internal void SetComplete(PresentationResult result)
		{
			DialogResult = result;
			Completed = true;
			DoneHandle.Set();
		}

		/// <summary> used to set the dispatcher once the window is initialized</summary>
		internal void SetWindow(Window window)
		{
			_window = window;
			_Dispatcher = window.Dispatcher;
		}

		/// <summary> used to set the result to throw an exception</summary>
		internal void SetException(System.Exception ex)
		{
			Error = ex;
			IsError = true;
			Completed = true;
			DoneHandle.Set();
		}
		/// <summary>Update the AsyncState return object</summary>
		internal void SetState(object stateObject)
		{
			StateObject = stateObject;
		}
	}

	/// <summary> Arguments which are passed to the window thread </summary>
	internal class WindowArgs
	{
		/// <summary> [xml]containing the xaml for the window</summary>
		public XmlNode WindowXaml;
		/// <summary> an internal reference to the IAsyncResult object so the the result can be set</summary>
		public WindowDispatcherAsyncResult AsyncResult;
		/// <summary> initialization wait handle, signaled once initialization is completed</summary>
		public EventWaitHandle InitHandle;
		/// <summary> Delegate to </summary>
		public SetWindowProperties Initialize;
		/// <summary> an initialization exception that, if set, will be thown by the start method</summary>
		public Exception InitException;

		public Dispatcher ShellDispatcher;
		public Runspace ShellRunspace;
	}

	public class Presentation
	{
		public static Window Window { get; set; }
		public static Dispatcher Dispatcher { get; set; }

		// internal method to start the window thread
		public static WindowDispatcherAsyncResult Start(XmlNode xaml, SetWindowProperties initialize)
		{
			var ShowDialogHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
			WindowArgs Args = new WindowArgs()
			{
				WindowXaml = xaml,
				AsyncResult = new WindowDispatcherAsyncResult(new EventWaitHandle(false, EventResetMode.ManualReset)),
				InitHandle = new EventWaitHandle(false, EventResetMode.ManualReset),
				Initialize = initialize,
				ShellDispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher,
				ShellRunspace = Runspace.DefaultRunspace
			};

			Thread WindowThread = new Thread(new ParameterizedThreadStart(WindowProc))
			{
				Name = "Presentation.Thread"
			};

			WindowThread.SetApartmentState(ApartmentState.STA);
			WindowThread.Start(Args);

			Args.InitHandle.WaitOne();

			if (Args.InitException != null) throw Args.InitException;

			return Args.AsyncResult;
		}

		// public stop method to retrieve the dialog result 
		public static PresentationResult Stop(WindowDispatcherAsyncResult asyncResult)
		{
			if (asyncResult == null)
				throw new ArgumentNullException("asyncResult");

			asyncResult.AsyncWaitHandle.WaitOne();
			return asyncResult.Result;
		}

		// procedure for the window thread
		private static void WindowProc(object obj)
		{
			WindowArgs Args = (WindowArgs)obj;
			Window LocalWindow;
			PresentationResult FinalResult = new PresentationResult();

			Runspace.DefaultRunspace = Args.ShellRunspace;
			// trap initialization errors
			try
			{
				object XamlObject = null;
				// initialize from xaml
				if (Args.WindowXaml != null)
				{
					XamlObject = System.Windows.Markup.XamlReader.Load(
						 new System.Xml.XmlNodeReader((System.Xml.XmlNode)Args.WindowXaml));
				}

				if (!(XamlObject is Window))
				{
					LocalWindow = new Window
					{
						WindowStyle = WindowStyle.ToolWindow,
						SizeToContent = SizeToContent.WidthAndHeight
						// Content = XamlHelper.NewItemsControl()
					};
					// _xamlUI.PopoutWindows.Add(_window);
					// _window.Show();
				}
				else
				{
					LocalWindow = (Window)XamlObject;
				}
			}
			catch (Exception ex)
			{
				// set initialization exception
				Args.InitException = ex;
				Args.InitHandle.Set();
				return;
			}

			// initialization complete
			// set the dispatcher
			Args.AsyncResult.SetWindow(LocalWindow);

			Args.InitHandle.Set();

			if (Args.Initialize != null)
			{
				Args.Initialize(LocalWindow);
			}

			// trap runtime exceptions
			try
			{
				LoadTemplates(LocalWindow);
				FinalResult.DialogResult = (bool)LocalWindow.ShowDialog();
				FinalResult.WindowTag = LocalWindow.Tag;
			}
			catch (Exception ex)
			{
				// set the runtime exception to be rethrown by the stop method
				Args.AsyncResult.SetException(ex);
				return;
			}
			// set the dialog result
			Args.AsyncResult.SetComplete(FinalResult);
		}

      internal static void LoadTemplates(Window window)
      {
         LoadTemplates(window, new[] {
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WindowsPowerShell"),
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell\\v1.0")
         });

      }

      internal static void LoadTemplates(Window window, string[] sourceFolders)
		{
			LoadTemplates(window, sourceFolders, new[] { "DataTemplates.xaml" });
		}

      internal static void LoadTemplates(Window window, string[] sourceFolders, string[] templates)
		{
			window.Dispatcher.BeginInvoke((Action)(() =>
			{
				foreach (string template in templates)
				{
               foreach (string root in sourceFolders)
               {
                  string templatePath = System.IO.Path.Combine(root, template);

                  if (System.IO.File.Exists(templatePath))
                  {
                     ResourceDictionary resources;
                     ErrorRecord error;
                     System.IO.FileInfo startup = new System.IO.FileInfo(templatePath);
                     // Application.ResourceAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                     if (startup.TryLoadXaml(out resources, out error))
                     {
                        //Application.Current.Resources.MergedDictionaries.Add(resources);
                        window.Resources.MergedDictionaries.Add(resources);
                     }
                     else
                     {
                        throw error.Exception;
                     }
                  }
               }
				}
			}));

		}
	}
}