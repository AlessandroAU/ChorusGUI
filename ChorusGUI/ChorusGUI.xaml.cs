//BUG: causing exception coz it wants to speak laptime without being started on startup ?
//TODO: calculate best race for pilot collection
//TODO: qualification choose between best run or best of all
//TODO: code weirdo racing system
//TODO: maybe: delay pilot starts?
//TODO: maybe: doubleclick for datagrid for information window
//TODO: search for TODO

using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Ports;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Timers;
using System.Xml.Serialization;
using System.Speech.Synthesis;
using System.Collections.Specialized;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace chorusgui
{
    internal static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        public static extern uint SetThreadExecutionState(uint esFlags);
        public const uint ES_CONTINUOUS = 0x80000000;
        public const uint ES_SYSTEM_REQUIRED = 0x00000001;
    }

    [Serializable]
    public class PilotCollection : ObservableCollection<Pilot>
    {
    }
    [Serializable]
    public class RaceCollection : ObservableCollection<Race>
    {
    }

    [Serializable]
    public class QualificationCollection : ObservableCollection<Race>
    {
    }

    [Serializable]
    public class HeatCollection : ObservableCollection<Race>
    {
    }
 
    public class ChorusDeviceClass
    {
        public ComboBox Frequency;
        public CheckBox SoundState;
        public CheckBox SkipFirstLap;
        public CheckBox Calibrated;
        public int CalibrationTime;
        public Label CalibrationTimeLabel;
        public int MinimalLapTime;
        public Label MinimalLapTimeLabel;
        public CheckBox Configured;
        public CheckBox RaceActive;
        public int CurrentRSSIValue;
        public Label CurrentRSSIValueLabel;
        public int CurrentTreshold;
        public TextBox CurrentTresholdTextBox;
        public CheckBox RSSIMonitoringActive;
        public ListView LapTimes;
        public double CurrentVoltage;
        public double BatteryVoltageAdjustment;
        public Label CurrentVoltageLabel;
        public Grid grid;
        public int APIVersion;
    }

    [Serializable]
    public class Pilot
    {
        private string _guid;
        public string guid
        {
            get
            {
                if (_guid == null)
                {
                    _guid = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Substring(0, 22).Replace('+', '-').Replace('/', '_'); ;
                }
                return _guid;
            }
            set
            {
                _guid = value;
            }
        }
        public string Ranking { get; set; }
        public string Name { get; set; }
        public int BestLap { get; set; }
        public string BestRace { get; set; }
        public int overtime { get; set; }
        public int totallaps { get; set; }

    }

    public class _Lap
    {
        public Race race;
        public string this[int index]
        {
            get
            {
                if (race == null)
                {
                    return null;
                }
                if (race.laps == null)
                {
                    return null;
                }
                string[] laps = race.laps.Split(';');
                if (laps.Length - 2 < index)
                {
                    return null;
                }
                var pos = laps[index].IndexOf(":");
                if (pos == -1)
                {
                    return null;
                }
                return laps[index].Substring(pos + 1);
            }
            set { }
        }
    }


    [Serializable]
    public class Race
    {
        _Lap _lap = new _Lap();
        public string guid { get; set; }
        public Pilot pilot { get; set; }
        public int Device { get; set; }
        public string RFChannel
        {
            get
            {
                //TODO: PRETTIFY ME
                return Device.ToString();
            }
            set { /*not needed*/ }
        }
        public string laps { get; set; }
        public int BestLap { get; set; }
        public int Heat { get; set; }
        public int overtime { get; set; }
        public int totallaps { get; set; }
        public Boolean finished { get; set; }
        public _Lap lap
        {
            get
            {
                return _lap;
            }
            set
            {
                lap = value;
            }
        }
        public string Result { get; set; }
    }

    [Serializable]
    public class Settings
    {
        public int SerialBaud { get; set; }
        public string SerialPortName { get; set; }
        public int SerialBaudIndex { get; set; }
        public Boolean VoltageMonitoring { get; set; }
        public Boolean LapSpeaking { get; set; }
        public int VoltageMonitorDevice { get; set; }
        public string Voice { get; set; }
        public List<string> RecentFiles = new List<string>();
    }

    public partial class ChorusGUI : Window
    {
        System.Timers.Timer aTimer = new System.Timers.Timer();
        System.Timers.Timer VoltageMonitorTimer = new System.Timers.Timer();
        public SerialPort mySerialPort = null;
        int TimerCalibration = 1000;
        private SpeechSynthesizer synthesizer = null;
        public int NumberOfDevices;
        private uint m_previousExecutionState;

        EventClass Event;

        private HeatCollection Heat;
        public Settings settings = new Settings();

        ChorusDeviceClass[] ChorusDevices;

        #region WINDOW

        //CLASS INIT
        public ChorusGUI()
        {
            m_previousExecutionState = NativeMethods.SetThreadExecutionState(
                NativeMethods.ES_CONTINUOUS | NativeMethods.ES_SYSTEM_REQUIRED);
            if (0 == m_previousExecutionState)
            {
                MessageBox.Show("Call to SetThreadExecutionState failed unexpectedly.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            try {
                Event = new EventClass();
                Event.gui = this;
                InitializeComponent();
                Event.pilots = (PilotCollection)Resources["PilotCollection"];
                Event.qualifications = (QualificationCollection)Resources["QualificationCollection"];
                Event.races = (RaceCollection)Resources["RaceCollection"];
                Heat = (HeatCollection)Resources["HeatCollection"];
                LoadSettings();
                if (settings.RecentFiles.Count == 0)
                {
                    settings.RecentFiles.Add("currentevent.xml");
                    UpdateRecentFileList("");
                }
                else
                {
                    UpdateRecentFileList("");
                    Event.LoadEvent(settings.RecentFiles[0]);
                }
                Title = "Chorus Lap Timer @ " + settings.SerialPortName + "(" + settings.SerialBaud + " Baud) -=] " + Event.name + " [=-"; 
                aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
                VoltageMonitorTimer.Elapsed += new ElapsedEventHandler(VoltageMonitorTimerEvent);
                QualificationRunsLabel.Content = Event.QualificationRaces;
                synthesizer = new SpeechSynthesizer();
                var voices = synthesizer.GetInstalledVoices();
                foreach (var voice in voices)
                {
                    cbSpeechVoice.Items.Add(voice.VoiceInfo.Name);
                    if (settings.Voice == voice.VoiceInfo.Name)
                    {
                        cbSpeechVoice.SelectedItem = settings.Voice;
                    }
                }
                if (cbSpeechVoice.SelectedItem == null)
                {
                    cbSpeechVoice.SelectedIndex = 0;
                }
                Event.pilots.CollectionChanged += Pilots_CollectionChanged;
                cbEnableLapSpeaking.IsChecked = settings.LapSpeaking;
#if DEBUG
                debugtab.Visibility = Visibility.Visible;
#endif
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabControl tabcontrol = (TabControl)sender;
            if (tabcontrol.SelectedIndex == 0)
            {
                if (!Event.IsRaceActive)
                {
                    if (Event.EliminationSystem != ((ComboBoxItem)cbElimination.SelectedItem).Tag.ToString())
                    {
                        BuildEventTables();
                    }
                }
            }
        }

        private void cbElimination_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem cbItem = (ComboBoxItem)cbElimination.SelectedItem;
            Event.EliminationSystem = cbItem.Tag.ToString();
            if (!Event.IsRaceActive)
            {
                BuildEventTables();
            }
        }

        private void cbQualificationAddResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem cbItem = (ComboBoxItem)cbQualificationAddResults.SelectedItem;
            Event.QualificationAddResults = cbItem.Tag.ToString();
        }

        private void Pilots_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!Event.IsRaceActive)
            {
                BuildEventTables();
            }
        }

        //WINDOW CLOSING
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Event.SaveEvent(settings.RecentFiles[0]);
            XmlSerializer serializer = new XmlSerializer(typeof(Settings));
            using (FileStream stream = new FileStream("settings.xml", FileMode.Create))
            {
                serializer.Serialize(stream, settings);
            }
            NativeMethods.SetThreadExecutionState(m_previousExecutionState);
            if (mySerialPort != null)
            {
                mySerialPort.Close();
            }
            Application.Current.Shutdown();
        }

        //WINDOW LOADED
        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                mySerialPort = new SerialPort(settings.SerialPortName, settings.SerialBaud, Parity.None, 8, StopBits.One);
                mySerialPort.ReadBufferSize = 20000;
                mySerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                mySerialPort.WriteBufferSize = 16;
                mySerialPort.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "FATAL ERROR - SerialPort.Open", MessageBoxButton.OK,MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
            SendData("N0");
        }
        #endregion

        #region Recieving
        private delegate void UpdateUiTextDelegate(string text);
        //DATA RECEIVING
        private void DataReceivedHandler(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            try
            {
                string recieved_data;
                do
                {
                    recieved_data = mySerialPort.ReadLine();
                    Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(ReadData), recieved_data);
                } while (mySerialPort.BytesToRead != 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "FATAL ERROR - SerialPort.ReadLine", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        //DATA SENDING
        private void SendData(string outdata)
        {
            try
            {
                mySerialPort.Write(outdata + "\n");
#if DEBUG
                listBox.Items.Add("[TX " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff") + "] " + outdata);
#endif
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "FATAL ERROR - SerialPort.Write", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        //MAIN PARSER
        private void ReadData(string readbuffer)
        {
            try
            {
#if DEBUG
                listBox.Items.Add("[RX " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff") + "] " + readbuffer);
#endif
                if ((NumberOfDevices > 0) || (readbuffer[0] == 'N'))
                {
                    switch (readbuffer[0])
                    {
                        case 'N': //ENUMERATE DEVICES
                                  //TODO: VoltageMonitoring
                            if (ChorusDevices != null)
                            {
                                break;
                            }
                            if (readbuffer.Length < 2)
                            {
                                break;
                            }
                            NumberOfDevices = readbuffer[1] - '0';
                            if (NumberOfDevices == 0)
                            {
                                break;
                            }
                            if ((NumberOfDevices < Event.NumberOfContendersForQualification) || (NumberOfDevices < Event.NumberOfContendersForRace))
                            {
                                btnRace.IsEnabled = false;
                                MessageBox.Show("You dont have enough Devices to continue this event", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                            else
                            {
                                btnRace.IsEnabled = true;
                            }
                            contender_slider1.Maximum = NumberOfDevices;
                            contender_slider2.Maximum = NumberOfDevices;
                            int tmp = NumberOfDevices;
                            if ((tmp % 2) == 1)
                            {
                                tmp--;
                            }
                            ChorusDevices = new ChorusDeviceClass[NumberOfDevices];
                            for (int ii = 0; ii < NumberOfDevices; ii++)
                            {
                                cbVoltageMonitoring.Items.Add("Device " + ii);
                                ChorusDevices[ii] = new ChorusDeviceClass();
                                ChorusDevices[ii].BatteryVoltageAdjustment = 1;
                                ChorusDevices[ii].APIVersion = 0;
                                Grid grid = new Grid();
                                CheckBox checkbox = new CheckBox();
                                checkbox.Content = "Device is configured";
                                checkbox.Name = "ID" + ii + "P";
                                checkbox.Margin = new Thickness(10, 10, 0, 0);
                                checkbox.IsEnabled = false;
                                grid.Children.Add(checkbox);
                                ChorusDevices[ii].Configured = checkbox;

                                checkbox = new CheckBox();
                                checkbox.Content = "Enable Device Sounds";
                                checkbox.Name = "ID" + ii + "D";
                                checkbox.Margin = new Thickness(200, 10, 0, 0);
                                checkbox.Click += device_cbClicked;
                                grid.Children.Add(checkbox);
                                ChorusDevices[ii].SoundState = checkbox;

                                checkbox = new CheckBox();
                                checkbox.Content = "Device is calibrated";
                                checkbox.Name = "ID" + ii + "i";
                                checkbox.IsEnabled = false;
                                checkbox.Margin = new Thickness(10, 30, 0, 0);
                                grid.Children.Add(checkbox);
                                ChorusDevices[ii].Calibrated = checkbox;

                                Label label = new Label();
                                label.Content = "Calibration Time: 0";
                                label.Name = "ID" + ii + "I";
                                label.Margin = new Thickness(200, 25, 0, 0);
                                grid.Children.Add(label);
                                ChorusDevices[ii].CalibrationTimeLabel = label;

                                checkbox = new CheckBox();
                                checkbox.Content = "Race is Active";
                                checkbox.Name = "ID" + ii + "R";
                                checkbox.IsEnabled = false;
                                checkbox.Margin = new Thickness(10, 50, 0, 0);
                                grid.Children.Add(checkbox);
                                ChorusDevices[ii].RaceActive = checkbox;

                                label = new Label();
                                label.Content = "Minimal Lap time: 0 seconds";
                                label.Name = "ID" + ii + "M";
                                label.Margin = new Thickness(200, 45, 0, 0);
                                grid.Children.Add(label);
                                ChorusDevices[ii].MinimalLapTimeLabel = label;

                                checkbox = new CheckBox();
                                checkbox.Content = "Skip First Lap";
                                checkbox.Name = "ID" + ii + "F";
                                checkbox.IsEnabled = false;
                                checkbox.Margin = new Thickness(10, 70, 0, 0);
                                grid.Children.Add(checkbox);
                                ChorusDevices[ii].SkipFirstLap = checkbox;

                                label = new Label();
                                label.Content = "Freqency: ";
                                label.Name = "ID" + ii + "FREQ";
                                label.Margin = new Thickness(10, 87, 0, 0);
                                grid.Children.Add(label);

                                ComboBox combobox = new ComboBox();
                                combobox.Items.Add(new ComboBoxItem { Content = "5180, Connex", Tag = "5180" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5200, Connex", Tag = "5200" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5220, Connex", Tag = "5220" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5240, Connex", Tag = "5240" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5645, Band E/Channel 1", Tag = "5645" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5658, RaceBand/Channel 1", Tag = "5658" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5665, Band E/Channel 2", Tag = "5665" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5685, Band E/Channel 3", Tag = "5685" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5695, RaceBand/Channel 2", Tag = "5695" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5705, Band E/Channel 4", Tag = "5705" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5725, Band A/Channel 1", Tag = "5725" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5732, RaceBand/Channel 3", Tag = "5732" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5733, Band B/Channel 1", Tag = "5733" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5740, Band F(IRC)/Channel 1", Tag = "5740" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5745, Band A/Channel 2 + Connex", Tag = "5745" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5752, Band B/Channel 2", Tag = "5752" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5760, Band F(IRC)/Channel 2", Tag = "5760" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5765, Band A/Channel 3 + Connex", Tag = "5765" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5769, RaceBand/Channel 4", Tag = "5769" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5771, Band B/Channel 3", Tag = "5771" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5780, Band F(IRC)/Channel 3", Tag = "5780" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5785, Band A/Channel 4 + Connex", Tag = "5785" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5790, Band B/Channel 4", Tag = "5790" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5800, Band F(IRC)/Channel 4", Tag = "5800" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5805, Band A/Channel 5 + Connex", Tag = "5805" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5806, RaceBand/Channel 5", Tag = "5806" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5809, Band B/Channel 5", Tag = "5809" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5820, Band F(IRC)/Channel 5", Tag = "5820" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5825, Band A/Channel 6 + Connex", Tag = "5825" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5828, Band B/Channel 6", Tag = "5828" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5840, Band F(IRC)/Channel 6", Tag = "5840" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5843, RaceBand/Channel 6", Tag = "5843" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5845, Band A/Channel 7 + Connex", Tag = "5845" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5847, Band B/Channel 7", Tag = "5847" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5860, Band F(IRC)/Channel 7", Tag = "5860" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5865, Band A/Channel 8", Tag = "5865" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5866, Band B/Channel 8", Tag = "5866" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5880, RaceBand/Channel 7 + Band F(IRC)/Channel 8", Tag = "5880" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5885, Band E/Channel 5", Tag = "5885" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5905, Band E/Channel 6", Tag = "5905" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5917, RaceBand/Channel 8", Tag = "5917" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5925, Band E/Channel 7", Tag = "5925" });
                                combobox.Items.Add(new ComboBoxItem { Content = "5945, Band E/Channel 8", Tag = "5945" });

                                combobox.HorizontalAlignment = HorizontalAlignment.Left;
                                combobox.VerticalAlignment = VerticalAlignment.Top;
                                combobox.SelectedIndex = 0;
                                combobox.Name = "ID"+ii+"F";
                                combobox.Tag = ii;
                                combobox.Margin = new Thickness(80, 90, 10, 0);
                                combobox.Height = 20;
                                combobox.Width = 330;
                                combobox.IsEditable = true;
                                switch (ii)
                                {
                                    default:
                                    case 0:
                                        combobox.SelectedIndex = 5;
                                        break;
                                    case 1:
                                        combobox.SelectedIndex = 8;
                                        break;
                                    case 2:
                                        combobox.SelectedIndex = 11;
                                        break;
                                    case 3:
                                        combobox.SelectedIndex = 18;
                                        break;
                                    case 4:
                                        combobox.SelectedIndex = 25;
                                        break;
                                    case 5:
                                        combobox.SelectedIndex = 31;
                                        break;
                                    case 6:
                                        combobox.SelectedIndex = 37;
                                        break;
                                    case 7:
                                        combobox.SelectedIndex = 40;
                                        break;
                                }

                                combobox.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent, new System.Windows.Controls.TextChangedEventHandler(device_textChange));
                                combobox.SelectionChanged += device_cbSelChange;
                                grid.Children.Add(combobox);
                                ChorusDevices[ii].Frequency = combobox;

                                checkbox = new CheckBox();
                                checkbox.Content = "RSSI Monitoring is Active";
                                checkbox.Name = "ID" + ii + "V";
                                checkbox.Margin = new Thickness(10, 115, 0, 0);
                                checkbox.Click += device_cbClicked;
                                grid.Children.Add(checkbox);
                                ChorusDevices[ii].RSSIMonitoringActive = checkbox;

                                label = new Label();
                                label.Content = "RSSI Value: 0";
                                label.Name = "ID" + ii + "S";
                                label.Margin = new Thickness(200, 110, 0, 0);
                                grid.Children.Add(label);
                                ChorusDevices[ii].CurrentRSSIValueLabel = label;

                                label = new Label();
                                label.Content = "Current RSSI Treshold:";
                                label.Name = "ID" + ii + "T";
                                label.Margin = new Thickness(10, 132, 0, 0);
                                grid.Children.Add(label);

                                TextBox textbox = new TextBox();
                                textbox.Name = "ID" + ii + "Tb";
                                textbox.Margin = new Thickness(135, 135, 0, 0);
                                textbox.HorizontalAlignment = HorizontalAlignment.Left;
                                textbox.VerticalAlignment = VerticalAlignment.Top;
                                textbox.TextWrapping = TextWrapping.NoWrap;
                                textbox.MaxLines = 1;
                                textbox.Height = 20;
                                textbox.Width = 60;
                                textbox.TextChanged += txt_RssiTreshold_TextChanged;
                                grid.Children.Add(textbox);
                                ChorusDevices[ii].CurrentTresholdTextBox = textbox;

                                Button button = new Button();
                                button.Name = "ID" + ii + "Td";
                                button.Content = "-";
                                button.Margin = new Thickness(200, 135, 0, 0);
                                button.Height = 20;
                                button.Width = 20;
                                button.HorizontalAlignment = HorizontalAlignment.Left;
                                button.VerticalAlignment = VerticalAlignment.Top;
                                button.Click += device_btnClick;
                                grid.Children.Add(button);

                                button = new Button();
                                button.Name = "ID" + ii + "Ts";
                                button.Content = "Current";
                                button.Margin = new Thickness(230, 135, 0, 0);
                                button.Height = 20;
                                button.Width = 50;
                                button.HorizontalAlignment = HorizontalAlignment.Left;
                                button.VerticalAlignment = VerticalAlignment.Top;
                                button.Click += device_btnClick;
                                grid.Children.Add(button);

                                button = new Button();
                                button.Name = "ID" + ii + "Ti";
                                button.Content = "+";
                                button.Margin = new Thickness(290, 135, 0, 0);
                                button.Height = 20;
                                button.Width = 20;
                                button.HorizontalAlignment = HorizontalAlignment.Left;
                                button.VerticalAlignment = VerticalAlignment.Top;
                                button.Click += device_btnClick;
                                grid.Children.Add(button);

                                label = new Label();
                                label.Content = "Current Voltage: X.XX";
                                label.Name = "ID" + ii + "Y";
                                label.Margin = new Thickness(10, 162, 0, 0);
                                grid.Children.Add(label);
                                ChorusDevices[ii].CurrentVoltageLabel = label;

                                button = new Button();
                                button.Name = "ID" + ii + "Yd";
                                button.Content = "-";
                                button.Margin = new Thickness(200, 165, 0, 0);
                                button.Height = 20;
                                button.Width = 20;
                                button.HorizontalAlignment = HorizontalAlignment.Left;
                                button.VerticalAlignment = VerticalAlignment.Top;
                                button.Click += device_btnClick;
                                grid.Children.Add(button);

                                button = new Button();
                                button.Name = "ID" + ii + "Yi";
                                button.Content = "+";
                                button.Margin = new Thickness(290, 165, 0, 0);
                                button.Height = 20;
                                button.Width = 20;
                                button.HorizontalAlignment = HorizontalAlignment.Left;
                                button.VerticalAlignment = VerticalAlignment.Top;
                                button.Click += device_btnClick;
                                grid.Children.Add(button);

                                label = new Label();
                                label.Content = "Laps since last Race:";
                                label.Name = "ID" + ii + "Llabel";
                                label.Margin = new Thickness(10, 185, 0, 0);
                                grid.Children.Add(label);

                                ListView listview = new ListView();
                                listview.Name = "ID" + ii + "L";
                                listview.Margin = new Thickness(10, 210, 10, 10);

                                GridView myGridView = new GridView();
                                myGridView.AllowsColumnReorder = true;
                                GridViewColumn gvc1 = new GridViewColumn();
                                gvc1.DisplayMemberBinding = new Binding("Lap");
                                gvc1.Header = "Lap";
                                gvc1.Width = 50;
                                myGridView.Columns.Add(gvc1);
                                GridViewColumn gvc2 = new GridViewColumn();
                                gvc2.DisplayMemberBinding = new Binding("Time");
                                gvc2.Header = "Milliseconds";
                                gvc2.Width = 300;
                                myGridView.Columns.Add(gvc2);
                                listview.View = myGridView;
                                grid.Children.Add(listview);
                                ChorusDevices[ii].LapTimes = listview;

                                TabItem ti = new TabItem();
                                ti.Header = "Device " + ii;
                                ti.Name = "DEVICE" + ii;
                                ti.Content = grid;
                                Settings_TabControl.Items.Add(ti);
                                if (Event.IsRaceActive)
                                {
                                    grid.IsEnabled = false;
                                }
                                ChorusDevices[ii].grid = grid;
                            }
                            cbVoltageMonitoring.SelectedIndex = 0;
                            UpdateHeatTable();
                            SendData("R*A");
                            break;
                        case 'S':
                            if (readbuffer.Length < 4)
                            {
                                break;
                            }
                            int device = readbuffer[1] - '0';
                            switch (readbuffer[2])
                            {
                                case 'Q': //set frequency
                                    var freq = int.Parse(readbuffer.Substring(3), System.Globalization.NumberStyles.HexNumber);
                                    if (ChorusDevices[device].Frequency.SelectedItem == null)
                                    {
                                        if (ChorusDevices[device].Frequency.Text != freq.ToString())
                                        {
                                            SendData("R" + device + "Q" + Convert.ToInt32(ChorusDevices[device].Frequency.Text).ToString("X4"));
                                        }
                                    }
                                    else
                                    {
                                        ComboBoxItem cbitem = (ComboBoxItem)ChorusDevices[device].Frequency.SelectedItem;
                                        if (cbitem.Tag.ToString() != freq.ToString())
                                        {
                                            SendData("R" + device + "Q" + int.Parse(cbitem.Tag.ToString()).ToString("X4"));
                                        }
                                    }
                                    break;
                                case 'D': //Sound State (half-byte; 1 = On, 0 = Off)
                                    if (readbuffer[3] == '0')
                                    {
                                        ChorusDevices[device].SoundState.IsChecked = false;
                                    }
                                    else
                                    {
                                        ChorusDevices[device].SoundState.IsChecked = true;
                                    }
                                    break;
                                case 'F': //First Lap State (half-byte; 1 = Skip, 0 = Count)
                                    if (readbuffer[3] == '0')
                                    {
                                        ChorusDevices[device].SkipFirstLap.IsChecked = false;
                                    }
                                    else
                                    {
                                        ChorusDevices[device].SkipFirstLap.IsChecked = true;
                                    }
                                    if (cbSkipFirstLap.IsChecked.Value != ChorusDevices[device].SkipFirstLap.IsChecked.Value)
                                    {
                                        SendData("R" + device + "F");
                                    }
                                    break;
                                case 'i': //Calibration State (half-byte, 1 = Calibrated, 0 = Not Calibrated)
                                    if (readbuffer[3] == '0')
                                    {
                                        ChorusDevices[device].Calibrated.IsChecked = false;
                                    }
                                    else
                                    {
                                        ChorusDevices[device].Calibrated.IsChecked = true;
                                    }
                                    break;
                                case 'I': //Calibration Time (4 bytes)
                                          //TODO weird results???
                                    ChorusDevices[device].CalibrationTime = int.Parse(readbuffer.Substring(3), System.Globalization.NumberStyles.HexNumber);
                                    //SendData("C"+device+(ChorusDevices[device].CalibrationTime - TimerCalibration).ToString("X8"));
                                    //ChorusDevices[device].CalibrationTime = TimerCalibration;
                                    SendData("C" + device + "0");
                                    ChorusDevices[device].CalibrationTimeLabel.Content = "Calibration Time: " + ChorusDevices[device].CalibrationTime + " ms for " + TimerCalibration + " ms";
                                    break;
                                case 'L': //Lap Time; last Lap Time is automatically sent in race mode when drone passes the gate; All Lap Times sent as a response to Bulk Device State (see below); Format: (1 byte: lap number + 4 bytes: lap time)
                                    TriggerLap(device, int.Parse(readbuffer.Substring(3, 2), System.Globalization.NumberStyles.HexNumber), int.Parse(readbuffer.Substring(5), System.Globalization.NumberStyles.HexNumber));
                                    break;
                                case 'M': //Minimal Lap Time (1 byte, in seconds)
                                    ChorusDevices[device].MinimalLapTime = int.Parse(readbuffer.Substring(3), System.Globalization.NumberStyles.HexNumber);
                                    ChorusDevices[device].MinimalLapTimeLabel.Content = "Minimal Lap time: " + ChorusDevices[device].MinimalLapTime + " seconds";
                                    if (ChorusDevices[device].MinimalLapTime != Event.MinimalLapTime)
                                    {
                                        SendData("R" + device + "L" + Event.MinimalLapTime.ToString("X2"));
                                    }
                                    break;
                                case 'P': //Device ist configured (half-byte, 1 = yes, 0 = no)
                                    if (readbuffer[3] == '0')
                                    {
                                        ChorusDevices[device].Configured.IsChecked = false;
                                    }
                                    else
                                    {
                                        ChorusDevices[device].Configured.IsChecked = true;
                                    }
                                    break;
                                case 'R': //Race Status (half-byte; 1 = On, 0 = Off)
                                    if (readbuffer[3] == '0')
                                    {
                                        ChorusDevices[device].RaceActive.IsChecked = false;
                                    }
                                    else
                                    {
                                        ChorusDevices[device].RaceActive.IsChecked = true;
                                        ChorusDevices[device].LapTimes.Items.Clear();
                                    }
                                    break;
                                case 'S': //Current RSSI Value; sent each 100ms when RSSI Monitor is On (2 bytes)
                                    ChorusDevices[device].CurrentRSSIValue = int.Parse(readbuffer.Substring(3), System.Globalization.NumberStyles.HexNumber);
                                    ChorusDevices[device].CurrentRSSIValueLabel.Content = "RSSI Value: " + ChorusDevices[device].CurrentRSSIValue;
                                    break;
                                case 'T': //Current Threshold (2 bytes)
                                    ChorusDevices[device].CurrentTreshold = int.Parse(readbuffer.Substring(3), System.Globalization.NumberStyles.HexNumber);
                                    ChorusDevices[device].CurrentTresholdTextBox.Text = ChorusDevices[device].CurrentTreshold.ToString(); ;
                                    break;
                                case 'V': //RSSI Monitor State (half-byte; 1 = On, 0 = Off)
                                    if (readbuffer[3] == '0')
                                    {
                                        ChorusDevices[device].RSSIMonitoringActive.IsChecked = false;
                                    }
                                    else
                                    {
                                        ChorusDevices[device].RSSIMonitoringActive.IsChecked = true;
                                    }
                                    break;
                                case 'Y': //Current Voltage (2 bytes)
                                    ChorusDevices[device].CurrentVoltage = int.Parse(readbuffer.Substring(3), System.Globalization.NumberStyles.HexNumber);
                                    double batteryVoltage = (double)ChorusDevices[device].CurrentVoltage * 11 * 5 * (((double)ChorusDevices[device].BatteryVoltageAdjustment + 1000) / 1000) / 1024;
                                    int cellsCount = (int)(batteryVoltage / 3.4);
                                    double cellVoltage = batteryVoltage / cellsCount;
                                    ChorusDevices[device].CurrentVoltageLabel.Content = "Current Cell Voltage: " + cellVoltage.ToString("0.00") + " Volt";
                                    if (device == cbVoltageMonitoring.SelectedIndex)
                                    {
                                        if (cbEnableVoltageMonitoring.IsChecked == true)
                                        {
                                            Title = "Chorus Lap Timer @ " + settings.SerialPortName + "(" + settings.SerialBaud + " Baud) Cell Voltage @ Device " + device + " :" + cellVoltage.ToString("0.00") + " Volt -=] " + Event.name + " [=-";
                                        }
                                    }
                                    break;
                                case '#': //APIVERSION
                                    ChorusDevices[device].APIVersion = int.Parse(readbuffer.Substring(3), System.Globalization.NumberStyles.HexNumber);
                                    break;
                                case 'X': //All states corresponding to specified letters (see above) plus 'X' meaning the end of state transmission for each device
                                    if (ChorusDevices[device].APIVersion < 1)
                                    {
                                        MessageBox.Show("WARNING! THIS VERSION NEEDS AT LEAST API VERSION. PLEASE UPDATE YOUR CHORUS-RF VERSION", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                                        Application.Current.Shutdown();
                                    }
                                    if (device == 0)
                                    {
                                        aTimer.Interval = 1000;
                                        SendData("R*I");
                                        aTimer.Enabled = true;
                                    }
                                    break;
                            }
                            break;

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //GET VOLTAGE MONITOR VALUE
        private void SendVoltageMonitorRequest(string outdata)
        {
            if (!Event.IsRaceActive)
            {
                SendData("R" + cbVoltageMonitoring.SelectedIndex + "Y");
            }
        }

        //CALIBRATION TIMER
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(SendData), "R*i");
            aTimer.Stop();
        }

        #endregion

        #region Settings
        //LOAD SETTINGS
        void LoadSettings()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Settings));
            try
            {
                using (FileStream stream = new FileStream("settings.xml", FileMode.Open))
                {
                    settings = (Settings)serializer.Deserialize(stream);
                }
            }
            catch (FileNotFoundException)
            {
                settings.SerialBaudIndex = 2;
            }
        }
        
        private void Settings_TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabControl tabcontrol = (TabControl)sender;
            if ((tabcontrol.SelectedIndex - 2) >= 0)
            {
                if (!Event.IsRaceActive)
                {
                    SendData("R" + (tabcontrol.SelectedIndex - 2) + "Y");
                }
            }
        }
        
        #region Settings_Race
        //RACEMODE
        void RaceMode_Checked(object sender, RoutedEventArgs e)
        {
            Event.RaceMode = cbRaceMode1.IsChecked.Value;
        }

        void txtRaceMode_TextChanged(object sender, TextChangedEventArgs e)
        {
            int value;
            try
            {
                value = Convert.ToInt32(txtRaceMode.Text);
            }
            catch (FormatException)
            {
                value = 0;
            }
            if (value < 1)
            {
                value = 1;
                txtRaceMode.Text = "1";
            }
            if (value > 1000)
            {
                value = 1000;
                txtRaceMode.Text = "1000";
            }
            Event.NumberofTimeForHeat = value;
        }

        //MINIMAL LAP TIME
        private void btn_MinimalLapTime(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            if (button.Name[0] == 'D')
            {
                if (Event.MinimalLapTime > 0)
                {
                    Event.MinimalLapTime--;
                }
            }
            else if (button.Name[0] == 'I')
            {
                if (Event.MinimalLapTime < 250)
                {
                    Event.MinimalLapTime++;
                }
            }
            SendData("R*L" + Event.MinimalLapTime.ToString("X2"));
            MinimalLapTimeLabel.Content = Event.MinimalLapTime + " seconds";
        }

        //SKIP FIRST LAP
        private void SkipFirstLap_Click(object sender, RoutedEventArgs e)
        {
            Event.SkipFirstLap = cbSkipFirstLap.IsChecked.Value;
            if (cbSkipFirstLap.IsChecked.Value)
            {
                SendData("R*F");
            }
            else
            {
                SendData("R*F");
            }
        }

        //NUMBER OF CONTENDERS FOR QUALIFICATION RUNS
        private void contender_slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!Event.IsRaceActive)
            {
                if (contenders1 != null)
                {
                    contenders1.Text = e.NewValue.ToString();
                }
                if ((NumberOfDevices > 0) && ((NumberOfDevices < Event.NumberOfContendersForQualification) || (NumberOfDevices < Event.NumberOfContendersForRace)))
                {
                    btnRace.IsEnabled = false;
                    MessageBox.Show("You dont have enough Devices to continue this event", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    btnRace.IsEnabled = true;
                }
                Event.NumberOfContendersForQualification = Convert.ToInt32(e.NewValue);
                BuildEventTables();
            }
        }

        //NUMBER OF QUALIFICATION RUNS
        private void btn_QualificationRuns(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            if (!Event.IsRaceActive)
            {
                if (button.Name[0] == 'D')
                {
                    if (Event.QualificationRaces > 0)
                    {
                        Event.QualificationRaces--;
                    }
                }
                else if (button.Name[0] == 'I')
                {
                    if (Event.QualificationRaces < 100)
                    {
                        Event.QualificationRaces++;
                    }
                }
                QualificationRunsLabel.Content = Event.QualificationRaces;
                BuildEventTables();
            }
        }

        //NUMBER OF CONTENDERS FOR ELEMINATION RUNS
        private void contender_slider2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (contenders2 != null)
            {
                contenders2.Text = e.NewValue.ToString();
            }
            Event.NumberOfContendersForRace = Convert.ToInt32(e.NewValue);
            if ((NumberOfDevices > 0) && ((NumberOfDevices < Event.NumberOfContendersForQualification) || (NumberOfDevices < Event.NumberOfContendersForRace)))
            {
                btnRace.IsEnabled = false;
                MessageBox.Show("You dont have enough Devices to continue this event", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                btnRace.IsEnabled = true;
            }
        }

        //ENABLE VOLTAGE MONITORING
        private void cbVoltageMonitoring_SelChange(object sender, SelectionChangedEventArgs e)
        {
            settings.VoltageMonitorDevice = cbVoltageMonitoring.SelectedIndex;
            SendVoltageMonitorRequest("");
        }

        private void LapSpeaking_Click(object sender, RoutedEventArgs e)
        {
            settings.LapSpeaking = cbEnableLapSpeaking.IsChecked.Value;
        }

        private void VoltageMonitoring_Click(object sender, RoutedEventArgs e)
        {
            settings.VoltageMonitoring = cbEnableVoltageMonitoring.IsChecked.Value;
            if (cbEnableVoltageMonitoring.IsChecked.Value)
            {
                cbVoltageMonitoring.IsEnabled = true;
                VoltageMonitorTimer.Interval = 10000;
                VoltageMonitorTimer.Enabled = true;
                SendVoltageMonitorRequest("");
            }
            else
            {
                cbVoltageMonitoring.IsEnabled = false;
                VoltageMonitorTimer.Enabled = false;
                Title = "Chorus Lap Timer @ " + settings.SerialPortName + "(" + settings.SerialBaud + " Baud) -=] " + Event.name + " [=-";
            }
        }
        private void VoltageMonitorTimerEvent(object source, ElapsedEventArgs e)
        {
            if (!Event.IsRaceActive)
            {
                Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(SendVoltageMonitorRequest), "");
            }
        }
        #endregion

        #region Settings_Speech

        private void cbSpeechVoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            synthesizer.SelectVoice(cbSpeechVoice.SelectedItem.ToString());
            settings.Voice = cbSpeechVoice.SelectedItem.ToString();
            if (window.Visibility == Visibility.Visible)
            {
                synthesizer.SpeakAsync("Hello. " + cbSpeechVoice.SelectedItem + "selected. Im happy to assist you.");
            }
        }
        #endregion

        #region Settings_Devices
        private void device_btnClick(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            var device = button.Name[2] - '0';
            switch (button.Name[3])
            {
                case 'T':
                    if (button.Name[4] == 'i')
                    {
                        SendData("R" + device + "T");
                    }
                    else if (button.Name[4] == 'd')
                    {
                        SendData("R" + device + "t");
                    }
                    else if (button.Name[4] == 's')
                    {
                        SendData("R" + device + "S");
                    }
                    break;
                case 'Y':
                    if (button.Name[4] == 'i')
                    {
                        ChorusDevices[device].BatteryVoltageAdjustment++;
                        SendData("R" + device + "Y");
                    }
                    else if (button.Name[4] == 'd')
                    {
                        ChorusDevices[device].BatteryVoltageAdjustment--;
                        SendData("R" + device + "Y");
                    }
                    break;
            }
        }

        private void device_cbClicked(object sender, RoutedEventArgs e)
        {
            CheckBox checkbox = (CheckBox)sender;
            var device = checkbox.Name[2] - '0';
            switch (checkbox.Name[3])
            {
                case 'D':
                    SendData("R" + device + "D");
                    break;
                case 'V':
                    if (checkbox.IsChecked.Value)
                    {
                        SendData("R" + device + "V");
                    }
                    else
                    {
                        SendData("R" + device + "v");
                    }
                    break;
            }
        }

        private void device_cbSelChange(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combobox = (ComboBox)sender;
            ComboBoxItem cbitem = (ComboBoxItem)combobox.SelectedItem;
            if (cbitem != null)
            {
                SendData("R" + combobox.Tag + "Q" + int.Parse(cbitem.Tag.ToString()).ToString("X4"));
            }
        }

        void device_textChange(object sender, TextChangedEventArgs e)
        {
            ComboBox combobox = (ComboBox)sender;
            if (combobox.SelectedItem == null)
            {
                int value;
                try
                {
                    value = Convert.ToInt32(combobox.Text);
                }
                catch (FormatException)
                {
                    if (combobox.Text != "")
                    {
                        combobox.SelectedIndex = 0;
                        return;
                    }
                    else
                    {
                        value = 0;
                    }
                }
                if ((value <= 6000) && (value >= 5180))
                {
                    SendData("R" + combobox.Tag + "Q" + value.ToString("X4"));
                }
            }
        }

        #endregion

        #region Settings_Debug
        private void textBox_OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                SendData(textBox.Text);
                textBox.Text = "";
            }
        }
        #endregion

        #endregion

        #region EventLoading
        private void IDM_LOAD_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Do you want to save the current Event?", "WARNING", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Extensible Markup Language File (*.xml)|*.xml|Any File (*.*)|*.*";
                if (saveFileDialog.ShowDialog() != true)
                {
                    return;
                }
                Event.SaveEvent(saveFileDialog.FileName);
            }
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Extensible Markup Language File (*.xml)|*.xml|Any File (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                Event.LoadEvent(openFileDialog.FileName);
                UpdateHeatTable();
                if (Event.IsRaceActive)
                {
                    for (int i = 0; i < NumberOfDevices; i++)
                    {
                        ChorusDevices[i].grid.IsEnabled = false;
                    }
                    Pilots_dataGrid.IsEnabled = false;
                    RaceSettingsGrid.IsEnabled = false;

                }
                else
                {
                    for (int i = 0; i < NumberOfDevices; i++)
                    {
                        ChorusDevices[i].grid.IsEnabled = true;
                    }
                    Pilots_dataGrid.IsEnabled = true;
                    RaceSettingsGrid.IsEnabled = true;
                }
            }
        }
        private void IDM_SAVEAS_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Extensible Markup Language File (*.xml)|*.xml|Any File (*.*)|*.*";
            if (saveFileDialog.ShowDialog() != true)
            {
                return;
            }
            Event.SaveEvent(saveFileDialog.FileName);
        }
        private void IDM_NEW_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Do you want to save the current Event?", "WARNING", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Extensible Markup Language File (*.xml)|*.xml|Any File (*.*)|*.*";
                if (saveFileDialog.ShowDialog() != true)
                {
                    return;
                }
                Event.SaveEvent(saveFileDialog.FileName);
            }
            Event.races.Clear();
            Event.qualifications.Clear();
            Event.pilots.Clear();
            Heat.Clear();
            Event.CurrentHeat = 0;
            Event.IsRaceActive = false;
            for (int i = 0; i < NumberOfDevices; i++)
            {
                ChorusDevices[i].grid.IsEnabled = true;
            }
            Pilots_dataGrid.IsEnabled = true;
            RaceSettingsGrid.IsEnabled = true;
            btnRace.Content = "Start Heat";
            UpdateHeatTable();
            UpdateRecentFileList("newevent-"+ ((Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds).ToString("X") +".xml");
        }

        private void txtEventName_TextChanged(object sender, TextChangedEventArgs e)
        {
            Event.name = txtEventName.Text;
            Title = "Chorus Lap Timer @ " + settings.SerialPortName + "(" + settings.SerialBaud + " Baud) -=] " + Event.name + " [=-";
        }

        private void txtContenders_TextChanged(object sender, TextChangedEventArgs e)
        {
            int value;
            try
            {
                value = Convert.ToInt32(txtContenders.Text);
            }
            catch (FormatException)
            {
                value = 0;
            }
            if (value < 1)
            {
                value = 1;
                txtContenders.Text = "1";
            }
            if (value > 1000)
            {
                value = 1000;
                txtContenders.Text = "1000";
            }
            Event.Contenders = value;
            if (!Event.IsRaceActive)
            {
                BuildEventTables();
            }
        }


        private void IDM_HELP_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("help? why? its open source!", "TODO: IDM_HELP_Click");
        }

        private void IDM_LOAD_ClickExisting(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            if (MessageBox.Show("Do you want to save the current Event?", "WARNING", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Extensible Markup Language File (*.xml)|*.xml|Any File (*.*)|*.*";
                if (saveFileDialog.ShowDialog() != true)
                {
                    return;
                }
                Event.SaveEvent(saveFileDialog.FileName);
            }
            Event.LoadEvent((string)menuItem.Tag);
            UpdateHeatTable();
            if (Event.IsRaceActive)
            {
                for (int i = 0; i < NumberOfDevices; i++)
                {
                    ChorusDevices[i].grid.IsEnabled = false;
                }
                Pilots_dataGrid.IsEnabled = false;
                RaceSettingsGrid.IsEnabled = false;

            }
            else
            {
                for (int i = 0; i < NumberOfDevices; i++)
                {
                    ChorusDevices[i].grid.IsEnabled = true;
                }
                Pilots_dataGrid.IsEnabled = true;
                RaceSettingsGrid.IsEnabled = true;
            }
        }

        public void UpdateRecentFileList(string filename)
        {
            if (filename != "")
            {
                var index = settings.RecentFiles.IndexOf(filename);
                if (index == -1)
                {
                    settings.RecentFiles.Insert(0, filename);
                }
                else
                {
                    settings.RecentFiles.RemoveAt(index);
                    settings.RecentFiles.Insert(0, filename);
                }
                while (settings.RecentFiles.Count > 10)
                {
                    settings.RecentFiles.RemoveAt(settings.RecentFiles.Count);
                }
            }
            if (settings.RecentFiles.Count > 0)
            {
                IDM_LOAD.Items.Clear();
                MenuItem item = new MenuItem();
                item.Name = "IDM_LOADOPEN";
                item.Header = "Load Event";
                item.Click += new RoutedEventHandler(IDM_LOAD_Click);
                IDM_LOAD.Items.Add(item);
                IDM_LOAD.Items.Add(new Separator());
                foreach (string file in settings.RecentFiles)
                {
                    item = new MenuItem();
                    item.Name = "IDM_LOADOPEN";
                    item.Header = file;
                    item.Tag = file;
                    item.Click += new RoutedEventHandler(IDM_LOAD_ClickExisting);
                    IDM_LOAD.Items.Add(item);
                }
            }
        }
        #endregion

        #region Raceing
        private void UpdateHeatTable()
        {
            Heat.Clear();
            if (Event.CurrentHeat >= (int)Math.Ceiling((double)Event.pilots.Count / Event.NumberOfContendersForQualification) * Event.QualificationRaces)
            {
                labelCurrentHeat.Content = "Elimination Heat :" + Event.CurrentHeat;
                foreach (Race race in Event.races)
                {
                    if (race.Heat == Event.CurrentHeat)
                    {
                        Heat.Add(race);
                    }
                }
            }
            else
            {
                labelCurrentHeat.Content = "Qualification Heat :" + Event.CurrentHeat;
                foreach (Race race in Event.qualifications)
                {
                    if (race.Heat == Event.CurrentHeat)
                    {
                        Heat.Add(race);
                    }
                }
            }
            UpdateGridViews();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (btnRace.Content.ToString() == "Start Heat")
            {
                Event.IsRaceActive = true;
                for (int i = 0; i < NumberOfDevices; i++)
                {
                    ChorusDevices[i].grid.IsEnabled = false;
                }
                Pilots_dataGrid.IsEnabled = false;
                RaceSettingsGrid.IsEnabled = false;
                btnRace.Content = "Stop Heat";
                SendData("R*v");
                Console.Beep(1000, 750);
                System.Threading.Thread.Sleep(250);
                Console.Beep(1000, 750);
                System.Threading.Thread.Sleep(250);
                Console.Beep(1000, 750);
                System.Threading.Thread.Sleep(250);
                SendData("R*R");
                new Thread(() => Console.Beep(1600,1500)).Start();
                //TODO: allow delaying pilot start
            }
            else if (btnRace.Content.ToString() == "Stop Heat")
            {
                SendData("R*r");
                btnRace.Content = "Verify Results";
                synthesizer.SpeakAsync("Heat finished");
                foreach (Race race in Heat)
                {
                    if (race.laps==null)
                    {
                        race.laps = "";
                    }
                    CalculateResults(race);
                }
                UpdateGridViews();
            }
            else if (btnRace.Content.ToString() == "Verify Results")
            {
                Event.CurrentHeat++;
                if (Event.CurrentHeat == (int)Math.Ceiling((double)Event.pilots.Count / Event.NumberOfContendersForQualification) * Event.QualificationRaces) {
                    tabControl1.SelectedIndex = 1; //select EliminationTab here
                    UpdateEliminationTable(true);
                }
                if (Event.CurrentHeat > (int)Math.Ceiling((double)Event.pilots.Count / Event.NumberOfContendersForQualification) * Event.QualificationRaces)
                {
                    UpdateEliminationTable(false);
                }
                if (!Event.IsRaceComplete)
                {
                    btnRace.Content = "Start Heat";
                    UpdateHeatTable();
                }
                else
                {
                    btnRace.Content = "Race Complete";
                    btnRace.IsEnabled = false;
                }
                Event.SaveEvent(settings.RecentFiles[0]);
            }
        }

        public void TriggerLap(int device, int lap, int milliseconds)
        {
            ChorusDevices[device].LapTimes.Items.Add(new { Lap = lap.ToString(), Time = milliseconds.ToString() });
            if (btnRace.Content.ToString() == "Stop Heat")
            {
                if (Event.CurrentHeat >= (int)Math.Ceiling((double)Event.pilots.Count / Event.NumberOfContendersForQualification) * Event.QualificationRaces)
                {
                    foreach (Race race in Event.races)
                    {
                        HandleLap(race, device, lap, milliseconds);
                    }
                }
                else
                {
                    foreach (Race race in Event.qualifications)
                    {
                        HandleLap(race, device, lap, milliseconds);
                    }
                }
            }
            UpdateGridViews();
        }

        void UpdateGridViews()
        {
            //MSDN gotta be kidding me. wtf @ this way of updating cells?
            var bleh1 = dgCurrentHeat.ItemsSource;
            dgCurrentHeat.ItemsSource = null;
            dgCurrentHeat.ItemsSource = bleh1;
            var bleh2 = dgQualification.ItemsSource;
            dgQualification.ItemsSource = null;
            dgQualification.ItemsSource = bleh2;
            var bleh3 = dgElemination.ItemsSource;
            dgElemination.ItemsSource = null;
            dgElemination.ItemsSource = bleh3;
        }

        public void HandleLap(Race race, int device, int lap, int milliseconds)
        {
            if ((race.Device == device) && (race.Heat == Event.CurrentHeat))
            {
                if (lap > 0)
                {
                    if ((milliseconds < race.BestLap) || (race.BestLap == 0))
                    {
                        race.BestLap = milliseconds;
                    }
                    if ((milliseconds < race.pilot.BestLap) || (race.pilot.BestLap == 0))
                    {
                        race.pilot.BestLap = milliseconds;
                    }
                }
                race.laps += lap + ":" + milliseconds + ";";
                CalculateResults(race);
                if (settings.LapSpeaking)
                {
                    /*TODO BETA TEST THIS ONE*/
                    synthesizer.SpeakAsync(race.pilot.Name + ", lap " + lap + ", " + milliseconds + " milliseconds");
                }
            }
        }

        public void CalculateResults(Race race)
        {
            int lapnum = 0;
            int time = 0;
            int totaltime = 0;
            if (race.laps != null)
            {
                string[] laps = race.laps.Split(';');
                int pos;
                foreach (string lap in laps)
                {
                    pos = lap.IndexOf(":");
                    if (pos == -1)
                    {
                        continue;
                    }
                    lapnum = int.Parse(lap.Substring(0, pos));
                    time = int.Parse(lap.Substring(pos + 1));
                    if ((lapnum == 0) && (!Event.SkipFirstLap))
                    {
                        totaltime += time;
                    }
                    if (lapnum > 0)
                    {
                        totaltime += time;
                        if ((time < race.BestLap) || (race.BestLap == 0))
                        {
                            race.BestLap = time;
                        }
                        if ((time < race.pilot.BestLap) || (race.pilot.BestLap == 0))
                        {
                            race.pilot.BestLap = time;
                        }
                    }
                    if (Event.RaceMode)
                    {
                        //laps to finish
                        if (lapnum == Event.NumberofTimeForHeat)
                        {
                            /*TODO BETA TEST THIS ONE*/synthesizer.SpeakAsync(race.pilot.Name + " heat complete");
                            break;
                        }
                    }
                    else
                    {
                        //time to race
                        if (totaltime >= Event.NumberofTimeForHeat * 1000)
                        {
                            /*TODO BETA TEST THIS ONE*/synthesizer.SpeakAsync(race.pilot.Name + " heat complete");
                            break;
                        }
                    }
                }
                race.finished = true;
                if (Event.RaceMode)
                {
                    //laps to finish
                    race.Result = "ms: " + totaltime;
                    race.overtime = totaltime;
                    if (lapnum < Event.NumberofTimeForHeat)
                    {
                        race.Result = race.Result + " DNF";
                        race.finished = false;
                    }
                    else
                    {
                        if (race.pilot.BestLap > race.BestLap)
                        {
                            race.pilot.BestLap = race.BestLap;
                            foreach (Pilot pilot in Event.pilots)
                            {
                                if (pilot.guid == race.pilot.guid)
                                {
                                    if (pilot.BestLap > race.BestLap)
                                    {
                                        pilot.BestLap = race.BestLap;
                                    }
                                    //TODO: check if best race for Pilot collection
                                    break;
                                }
                            }
                        }
                        
                    }
                }
                else
                {
                    //time to race
                    race.Result = "Laps: " + (lapnum - 1) + " Overtime: " + time;
                    race.totallaps = lapnum - 1;
                    race.overtime = time;
                    if (totaltime < Event.NumberofTimeForHeat * 1000)
                    {
                        race.Result = race.Result + " DNF";
                        race.finished = false;
                    }
                    else
                    {
                        if (race.pilot.BestLap > race.BestLap)
                        {
                            race.pilot.BestLap = race.BestLap;
                            foreach (Pilot pilot in Event.pilots)
                            {
                                if (pilot.guid == race.pilot.guid)
                                {
                                    if (pilot.BestLap > race.BestLap)
                                    {
                                        pilot.BestLap = race.BestLap;
                                    }
                                    //TODO: check if best race for Pilot collection
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void dgCurrentHeat_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (btnRace.Content.ToString() == "Verify Results")
            {
                if (dgCurrentHeat.SelectedCells.Count == 1)
                {
                    if (dgCurrentHeat.SelectedCells[0].Column.Header.ToString().Substring(0, 3) == "Lap")
                    {
                        var lap = int.Parse(dgCurrentHeat.SelectedCells[0].Column.Header.ToString().Substring(3));
                        Race race = dgCurrentHeat.SelectedCells[0].Item as Race;
                        if (race.lap[lap] != null)
                        {
                            ContextMenu contextMenu1 = new ContextMenu();
                            MenuItem menuItem1 = new MenuItem();
                            menuItem1.Header = "Delete Lap";
                            menuItem1.Tag = new { lap=lap, race=race };
                            menuItem1.Click += IDM_DELETELAP;
                            contextMenu1.Items.Add(menuItem1);
                            dgCurrentHeat.ContextMenu = contextMenu1;
                            return;
                        }
                    }
                }
            }
            dgCurrentHeat.ContextMenu = null;
        }

        private void IDM_DELETELAP(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Do you really want to delete this lap?", "Delete Lap", MessageBoxButton.YesNo,MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                MenuItem menuItem1 = sender as MenuItem;
                object o = menuItem1.Tag;
                int lapnum = (int)o?.GetType().GetProperty("lap")?.GetValue(o, null);
                Race race = (Race)o?.GetType().GetProperty("race")?.GetValue(o, null);
                string[] laps = race.laps.Split(';');
                int newlap=0;
                int newtime=0;
                race.laps = "";
                foreach (string lap in laps)
                {
                    string[] lapinfo = lap.Split(':');
                    if (lapinfo.Length == 2)
                    {
                        newtime += int.Parse(lapinfo[1]);
                        if (lapnum != int.Parse(lapinfo[0]))
                        {
                            race.laps += newlap + ":" + newtime + ";";
                            newlap++;
                            newtime = 0;
                        }
                    }
                }
                CalculateResults(race);
                UpdateGridViews();
            }
        }

        public void BuildEventTables()
        {
            //sanitycheck
            if (Event.races == null)
            {
                return;
            }
            Event.qualifications.Clear();
            Heat.Clear();
            int i = 0, ii = 0;
            for (int iii = 1; iii <= Event.QualificationRaces; iii++)
            {
                foreach (Pilot pilot in Event.pilots)
                {
                    Race race = new Race();
                    race.lap.race = race;
                    race.guid = pilot.guid;
                    race.pilot = pilot;
                    race.Heat = i;
                    race.Device = ii;
                    Event.qualifications.Add(race);
                    ii++;
                    if (ii == Event.NumberOfContendersForQualification)
                    {
                        ii = 0;
                        i++;
                    }
                }
                if (ii != 0)
                {
                    i++;
                    ii = 0;
                }
            }
            switch (Event.EliminationSystem)
            {
                case "doubleout":
                    BuildDoubleOutTable();
                    break;
                case "singleout":
                default:
                    BuildSingleOutTable();
                    break;
            }
            for (int k = 1; k < Event.races.Count; k++)
            {
                int j = k;
                while (j > 0)
                {
                    int result;
                    result = Event.races[j - 1].Heat - Event.races[j].Heat;
                    if (result == 0)
                        result = Event.races[j - 1].Device - Event.races[j].Device;

                    if (result > 0)
                    {
                        var temp = Event.races[j - 1];
                        Event.races[j - 1] = Event.races[j];
                        Event.races[j] = temp;
                        j--;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            UpdateHeatTable();
        }

        private int InsertSort1(Race race1, Race race2)
        {
            //race1 > race2
            //laps to finish
            return race1.overtime - race2.overtime;
        }
        private int InsertSort2(Race race1, Race race2)
        {
            //time to race
            var tmp = race1.totallaps - race2.totallaps;
            if (tmp != 0)
                return tmp;
            return race2.overtime - race1.overtime;
        }

        private void UpdateEliminationTable(Boolean firstCall)
        {
            if (firstCall)
            {
                if (Event.QualificationAddResults == "bestofall")
                {
                    //TODO
                }
                else if (Event.QualificationAddResults == "bestoftwo")
                {
                    //TODO
                }
                else //(Event.QualificationAddResults == "bestrun")
                {
                    List<Race> racehelper = new List<Race>();
                    foreach (Race race in Event.qualifications)
                    {
                        racehelper.Add(race);
                    }
                    for (int i = 1; i < racehelper.Count; i++)
                    {
                        int j = i;
                        while (j > 0)
                        {
                            int result;
                            if (Event.RaceMode)
                            {
                                //laps to finish
                                result = InsertSort1(racehelper[j - 1], racehelper[j]);
                            }
                            else
                            {
                                //time to race
                                result = InsertSort2(racehelper[j - 1], racehelper[j]);
                            }

                            if (result > 0)
                            {
                                var temp = racehelper[j - 1];
                                racehelper[j - 1] = racehelper[j];
                                racehelper[j] = temp;
                                j--;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    for (int i = racehelper.Count - 1; i > 0; i--)
                    {
                        for (int j = 0; j < i; j++)
                        {
                            if (racehelper[j].guid == racehelper[i].guid)
                            {
                                racehelper.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }
                //TODO: completly fill stage one of the elimination table
                //TODO: fill ranking in pilots area for pilots who are out
            }
            else
            {
                //TODO: fill results from last heat to elimination table
                //TODO: fill ranking in pilots area for pilots who are out
            }
            UpdateGridViews();
        }

        #endregion

        void txt_RssiTreshold_TextChanged(object sender, TextChangedEventArgs e) 
        {
            int value;
            TextBox textbox = (TextBox)sender;
            var device = textbox.Name[2] - '0';
            try
            {
                value = Convert.ToInt32(textbox.Text);
                SendData("R" + device + "S" + value.ToString("X4"));
            }
            catch (Exception)
            {

            }
        }

        private void dgRace_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //TODO
        }

        private void dgCurrentHeat_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //TODO
        }

        private void dgPilots_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //TODO
        }
    }
}
