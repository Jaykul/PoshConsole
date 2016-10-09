using Fluent;
using PoshCode.Controls;
using PoshCode.PowerShell;
using System;
using System.Management.Automation.Runspaces;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Button = System.Windows.Controls.Button;

namespace PoshConsole.Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            PoshConsole.ProgressPanel = Progress;
        }

        private void ZoomSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TextOptions.SetTextFormattingMode(this, e.NewValue > 1.0 ? TextFormattingMode.Ideal : TextFormattingMode.Display);
        }

        protected override void OnInitialized(EventArgs e)
        {
            //// This is here just to make sure we can run commands in this event handler!
            // PoshConsole.InvokeAsync("Write-Output $PSVersionTable");
            base.OnInitialized(e);
        }

        private void Console_PromptForChoice(object sender, PromptForChoiceEventArgs e)
        {
            // Ensure this is invoked synchronously on the UI thread
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(DispatcherPriority.Render, (Action) (() =>
                {
                    Console_PromptForChoice(sender, e);
                }));
                return;
            }

            // Disable the console ...
            PoshConsole.CommandBox.IsEnabled = false;

            #region PromptForChoiceWindowCouldBeInXaml
            // Create a window with a stack panel inside
            var content = new Grid {Margin = new Thickness(6)};

            content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50)});
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto});

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right};
            content.Children.Add(buttons);
            buttons.SetValue(Grid.RowProperty, 1);

            var dialog = new Window
            {
                SizeToContent = SizeToContent.WidthAndHeight,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Title = e.Caption,
                Content = content,
                MinHeight = 100,
                MinWidth = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Tag = -1 // We must initialize the tag
            };

            // Make buttons for each choice
            var index = 0;
            foreach (var choice in e.Choices)
            {
                var item = new Button
                {
                    Content = choice.Label.Replace('&', '_'),
                    ToolTip = choice.HelpMessage,
                    IsDefault = e.SelectedIndex == index,
                    Padding = new Thickness(10,4,10,4),
                    Margin = new Thickness(4),
                    Tag = index // set the button Tag to it's index
                };

                // when the button is clicked, set the window tag to the button's index, and close the window.
                item.Click += (o, args) =>
                {
                    dialog.Tag = (args.OriginalSource as FrameworkElement)?.Tag;
                    dialog.Close();
                };
                buttons.Children.Add(item);
                index++;
            }

            // Handle the Caption and Message
            if (string.IsNullOrWhiteSpace(e.Caption))
            {
                e.Caption = e.Message;
            }
            if (!string.IsNullOrWhiteSpace(e.Message))
            {
                content.Children.Insert(0, new TextBlock
                {
                    Text = e.Message,
                    FontSize = 16,
                    FontWeight = FontWeight.FromOpenTypeWeight(700),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Colors.CadetBlue,
                        Direction = 0,
                        ShadowDepth = 0,
                        BlurRadius = 5
                    }
                });
            }
            #endregion CouldBeInXaml

            dialog.ShowDialog();
            e.SelectedIndex = (int)dialog.Tag;

            // Reenable the console
            PoshConsole.CommandBox.IsEnabled = true;

        }
        /*
        private void Console_PromptForObject(object sender, PromptForObjectEventArgs e)
        {

            var results = new Dictionary<string, PSObject>();
            foreach (var fieldDescription in e.Descriptions)
            {
                var type = Type.GetType(fieldDescription.ParameterAssemblyFullName);
                var prompt = string.IsNullOrEmpty(fieldDescription.Label) ? fieldDescription.Name : fieldDescription.Label;
                if (type != null && type.IsArray)
                {
                    type = type.GetElementType();
                    var output = new List<PSObject>();
                    int count = 0;
                    do
                    {
                        var single = GetSingle(e.Caption, e.Message, $"{prompt}[{count++}]", fieldDescription.HelpMessage, fieldDescription.DefaultValue, type);
                        if (single == null) break;

                        if (!(single.BaseObject is string) || ((string)single.BaseObject).Length > 0)
                        {
                            output.Add(single);
                        }
                        else break;
                    } while (true);

                    results[fieldDescription.Name] = PSObject.AsPSObject(output.ToArray());
                }
                else
                {
                    results[fieldDescription.Name] = GetSingle(e.Caption, e.Message, prompt, fieldDescription.HelpMessage, fieldDescription.DefaultValue, type);
                }

            }
            e.Results = results;
        }

        private PSObject GetSingle(string caption, string message, string prompt, string help, PSObject psDefault, Type type)
        {
            if (null != type && type == typeof(PSCredential))
            {
                return PSObject.AsPSObject(CredentialUI.Prompt(caption, message));
            }

            while (true)
            {
                // TODO: Only show the help message if they type '?' as their entry something, in which case show help and re-prompt.
                if (!String.IsNullOrEmpty(help))
                    Write(help + "\n");

                Write($"{prompt}: ");

                if (null != type && typeof(SecureString) == type)
                {
                    var userData = ReadLineAsSecureString() ?? new SecureString();
                    return PSObject.AsPSObject(userData);
                } // Note: This doesn't look the way it does in PowerShell, but it should work :)
                else
                {
                    if (psDefault != null && psDefault.ToString().Length > 0)
                    {
                        if (Dispatcher.CheckAccess())
                        {
                            CurrentCommand = psDefault.ToString();
                            _commandBox.SelectAll();
                        }
                        else
                        {
                            Dispatcher.BeginInvoke(DispatcherPriority.Input, (Action<string>)(def =>
                            {
                                CurrentCommand = def;
                                _commandBox.SelectAll();
                            }), psDefault.ToString());
                        }
                    }

                    var userData = ReadLine();

                    if (type != null && userData.Length > 0)
                    {
                        object output;
                        var ice = TryConvertTo(type, userData, out output);
                        // Special exceptions that happen when casting to numbers and such ...
                        if (ice == null)
                        {
                            return PSObject.AsPSObject(output);
                        }
                        if ((ice.InnerException is FormatException) || (ice.InnerException is OverflowException))
                        {
                            Write(ConsoleBrushes.ErrorForeground, ConsoleBrushes.ErrorBackground,
                                $@"Cannot recognize ""{userData}"" as a {type.FullName} due to a format error.\n");
                        }
                        else
                        {
                            return PSObject.AsPSObject(String.Empty);
                        }
                    }
                    else if (userData.Length == 0)
                    {
                        return PSObject.AsPSObject(String.Empty);
                    }
                    else return PSObject.AsPSObject(userData);
                }
            }
        }
        */

        async void Capture_Click(object sender, RoutedEventArgs e)
        {
            var files = await PoshConsole.InvokeAsync(new Command("Get-ChildItem"));
            Dispatcher.Invoke(() => MainContent.DataContext = files.Output);
        }

        async void Secret_Click(object sender, RoutedEventArgs e)
        {
            var processes = await PoshConsole.InvokeAsync("Get-Process | Select -First 25", output: ConsoleOutput.None);
            Dispatcher.Invoke(() => MainContent.DataContext = processes.Output);
        }

        void Console_Click(object sender, RoutedEventArgs e)
        {
            PoshConsole.Invoke("Write-Output $PSVersionTable");
        }

        async void Error_Click(object sender, RoutedEventArgs e)
        {
            var files = await PoshConsole.InvokeAsync("Get-ChildItem NoSuchFile");
            if (files.State == PipelineState.Failed)
            {
                Dispatcher.Invoke(() => MainContent.DataContext = "Failed Command");
            }
            else if (!files.HadErrors)
            {
                Dispatcher.Invoke(() => MainContent.DataContext = files.Output);
            }
        }

        async void Exception_Click(object sender, RoutedEventArgs e)
        {
            var files = await PoshConsole.InvokeAsync("throw 'whatever'");
            if (files.State == PipelineState.Failed)
            {
                Dispatcher.Invoke(() => MainContent.DataContext = "Failed Command");
            }
        }

        async void Input_Click(object sender, RoutedEventArgs e)
        {
            // this is not part of the test/demo ... we just need something to use as input
            // You should _not_ do this in lieu of using the pipeline
            var processes = await PoshConsole.InvokeAsync("Get-Process | Sort CPU -Descending | Select -First 10", output: ConsoleOutput.None);

            // Now invoke a command with pipeline input:
            await PoshConsole.InvokeAsync("Format-Table", isScript: false, input: processes.Output);


            Dispatcher.Invoke(() => MainContent.DataContext = processes.Output);

        }

        async void Pipeline_Click(object sender, RoutedEventArgs e)
        {
            // When you want to accept user input for parameters, you should always build your pipeline using Commands
            var ps = new Command("Get-Process");
            var sort = new Command("Sort-Object");

            // That way, you can pass the user input to a specific parameter, and avoid code injection:
            sort.Parameters.Add("Property", ProcessSort.SelectedValue);
            // Switch parameters...
            if (ProcessDescending.IsChecked == true)
            {
                sort.Parameters.Add("Descending");
            }

            // Note that PowerShell is still dynamic, so there's no validation going on here
            // But these parameter values are not script, so the user can't use $(tricks) to execute code
            var select = new Command("Select-Object");
            select.Parameters.Add("First", (int)ProcessCount.Value);

            // Now pass them in order to InvokeAsync:
            var processes = await PoshConsole.InvokeAsync( new [] { ps, sort, select });
        }

        private void Invoke_Click(object sender, RoutedEventArgs e)
        {
            if(CommandInput.Text.Length > 0)
            // Oh man, please don't do anything like this with user inputs in the real world ...
            // But if you do, at least have the sense to make sure that isScript:false
            PoshConsole.InvokeAsync(Command.Text, isScript:false, input: CommandInput.Text.Split(' '));
        }
    }
}
