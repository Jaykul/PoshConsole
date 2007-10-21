using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Huddled.Interop.Hotkeys;


namespace GlobalHotkeys
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>

    public partial class DemoWindow : System.Windows.Window
    {

        public DemoWindow()
        {
            InitializeComponent();
            //new System.Windows.Input.ExecutedRoutedEventHandler(
            ApplicationCommands.Stop.CanExecute(null, this);

        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            // The hotkeys are set in this event 
            base.OnSourceInitialized(e);

            // so now we can ask which keys are still unregistered.
            HotkeyManager hk = HotkeyManager.GetHotkeyManager(this);
            int k = -1;
            while (++k < hk.UnregisteredKeys.Count)
            {
                KeyBinding key = hk.UnregisteredKeys[k];
                // hypothetically, you would show them a GUI for changing the hotkeys... 

                // but you could try modifying them yourself ...
                ModifierKeys mk = HotkeyManager.FindUnsetModifier(key.Modifiers);
                if (mk != ModifierKeys.None)
                {
                    MessageBox.Show(string.Format("Can't register hotkey: {0}+{1} \nfor {2}\n\nWe'll try registering it as {3}+{0}+{1}.", key.Modifiers, key.Key, key.Command, mk));
                    key.Modifiers |= mk;
                    hk.Add(key);
                }
                else
                {
                    MessageBox.Show(string.Format("Can't register hotkey: {0}+{1} \nfor {2}.", key.Modifiers, key.Key, key.Command, mk));
                    //key.Modifiers |= mk;
                    //hk.Add(key);
                }
            }
        }
    }
}