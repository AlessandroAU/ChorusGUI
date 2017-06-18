using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO.Ports;

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
                Button newBtn = new Button();
                newBtn.Content = "Select Port: " + port;
                newBtn.Name = port;
                newBtn.FontSize = 20;
                newBtn.Click += SelectPort;
                sp.Children.Add(newBtn);
            }
            if ((GUI.settings.SerialBaudIndex < 0) && (GUI.settings.SerialBaudIndex > comboBox.Items.Count))
                GUI.settings.SerialBaudIndex = 2;
            comboBox.SelectedIndex = GUI.settings.SerialBaudIndex;
            
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


