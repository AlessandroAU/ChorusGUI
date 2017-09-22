using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO.Ports;
using System.Management;

namespace chorusgui
{
    public partial class MainWindow : Window
    {
        ChorusGUI GUI = new ChorusGUI();

        Boolean closedByUser = true;
        public MainWindow()
        {
            InitializeComponent();

            string[] ports = SerialPort.GetPortNames();
            if (ports.Count() == 0)
            {
                MessageBox.Show("NO SERIAL PORTS FOUND", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
            foreach (string port in ports)
            {
                try
                {
                    //yay @ drinking beer and listening to bassdrive.com while coding w00h0000 ((.)(.))
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity where Caption like '%"+port+"%'");
                    ManagementObjectCollection queryCollection = searcher.Get();
                    ManagementObject mo = queryCollection.OfType<ManagementObject>().FirstOrDefault();
                    Button newBtn = new Button();
                    newBtn.Name = port;
                    newBtn.FontSize = 12;
                    newBtn.Width = 320;
                    if (mo == null)
                    {
                        newBtn.Content = port + " (Unable to detect PnPDevice)";
                    }
                    else
                    {
                        newBtn.Content = mo["Caption"].ToString();
                    }
                    newBtn.Click += SelectPort;
                    sp.Children.Add(newBtn);
                }
                catch (Exception ex)
                {
                }
            }
            if ((GUI.settings.SerialBaudIndex < 0) && (GUI.settings.SerialBaudIndex > comboBox.Items.Count))
                GUI.settings.SerialBaudIndex = 2;
            comboBox.SelectedIndex = GUI.settings.SerialBaudIndex;
            Title = "ChorusGUI, Startup, v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }
        private void SelectPort(object sender, RoutedEventArgs e)
        {
            string[] bauds = comboBox.Text.Split(' ');
            Button Btn = (Button)sender;
            GUI.settings.SerialBaud = int.Parse(bauds[0]);
            GUI.settings.SerialPortName = Btn.Name;
            GUI.Show();
            closedByUser = false;
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (closedByUser)
                Application.Current.Shutdown();
        }
    }
}


