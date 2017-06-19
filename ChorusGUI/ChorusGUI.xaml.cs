/*
Renn management:
Beim Starten des Races,sollte ein Countdown runterlaufen und ein Startsignal ausgeben (Irgendeinen Sound)
Am ersten Gate wird dann der Countdown gestartet. So machen wir es immer, dass wir als erstes durch das Startgate fliegen
und bei jeder Runde sollte Ein Sound ertönen. 
Wenn jemand seine Runden X (sagen wir 4  Runden) voll hat, sollte ein Soundfile abgespielt werden "Pilot 1 finish" oderso
Gibt es keine Sprachbibliothek um auch den Nickname ansagen zu lassen?
*/

//TODO: search for TODO
//TODO: if(settings.IsRaceActive) disable controlls!!!!

using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Timers;
using System.Xml.Serialization;
using System.IO;
using System.Speech.Synthesis;
using System.Collections.Specialized;
using System.Windows.Markup;

namespace chorusgui
{
    /// <summary>
    /// Interaction logic for ChorusGUI.xaml
    /// </summary>

    public class ChorusDeviceClass
    {
        public ComboBox Band;
        public ComboBox Channel;
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
        public Label CurrentTresholdLabel;
        public CheckBox RSSIMonitoringActive;
        public ListView LapTimes;
        public double CurrentVoltage;
        public double BatteryVoltageAdjustment;
        public Label CurrentVoltageLabel;
        public Grid grid;
    }

    [Serializable]
    public class Pilot
    {
        private string _guid;
        public string guid {
            get {
                if (_guid == null)
                    _guid = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Substring(0, 22).Replace('+', '-').Replace('/', '_'); ;
                return _guid;
            }
            set {
                _guid = guid;
            }
        }
        public string Ranking { get; set; }
        public string Name { get; set; }
        public string BestLap { get; set; }
        public string BestRace { get; set; }
    }

    [Serializable]
    public class Race
    {
        public int[] Lap { get; set; }
        public string Name { get; set; }
        public string guid { get; set; }
        public int Device { get; set; }
        public string RFChannel { get; set; }
        public string Seconds { get; set; }
        public string Laps { get; set; }
        public string BestLap { get; set; }
        public string Heat { get; set; }
        public string Result
        {
            get
            {
                if (Laps != null)
                    return "Laps: " + Laps + " seconds: " + Seconds;
                else if (Seconds != null)
                    return "Seconds: "+ Seconds;
                return null;
            }
            set { }
        }
    }

    [Serializable]
    public class Settings
    {
        public int SerialBaud { get; set; }
        public string SerialPortName { get; set; }
        public int SerialBaudIndex { get; set; }
        public int MinimalLapTime { get; set; }
        public int TimeToPrepare { get; set; }
        public Boolean SkipFirstLap { get; set; }
        public Boolean DoubleOut { get; set; }
        public Boolean RaceMode { get; set; }
        public int NumberofTime { get; set; }
        public Boolean VoltageMonitoring { get; set; }
        public int VoltageMonitorDevice { get; set; }
        public int NumberOfContendersForQualification { get; set; }
        public int QualificationRaces { get; set; }
        public int NumberOfContendersForRace { get; set; }
        public string Voice { get; set; }
        public int Heat { get; set; }
        public Boolean IsRaceActive { get; set; }
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

    public partial class ChorusGUI : Window
    {
        System.Timers.Timer aTimer = new System.Timers.Timer();
        System.Timers.Timer VoltageMonitorTimer = new System.Timers.Timer();
        SerialPort mySerialPort;
        string readbuffer;
        int TimerCalibration = 1000;
        int DeviceCount;
        private SpeechSynthesizer synthesizer;
        Boolean QualificationNeedsUpdate;

        private PilotCollection Pilots;
        private RaceCollection Races;
        private QualificationCollection Qualifications;
        private HeatCollection Heat;
        public Settings settings = new Settings();

        ChorusDeviceClass[] ChorusDevices;

        #region WINDOW

        //CLASS INIT
        public ChorusGUI()
        {
            InitializeComponent();
            LoadSettings();
            Title = "Chorus Lap Timer @ " + settings.SerialPortName + "(" + settings.SerialBaud + " Baud)";
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            VoltageMonitorTimer.Elapsed += new ElapsedEventHandler(VoltageMonitorTimerEvent);
            QualificationRunsLabel.Content = settings.QualificationRaces;
            Heat = (HeatCollection)Resources["HeatCollection"];
            Qualifications = (QualificationCollection)Resources["QualificationCollection"];
            Races = (RaceCollection)Resources["RaceCollection"];
            Pilots = (PilotCollection)Resources["PilotCollection"];
            XmlSerializer serializer = new XmlSerializer(typeof(PilotCollection));
            try {
                using (FileStream stream = new FileStream("pilots.xml", FileMode.Open))
                {
                    IEnumerable<Pilot> PilotData = (IEnumerable<Pilot>)serializer.Deserialize(stream);
                    foreach (Pilot p in PilotData)
                    {
                        Pilots.Add(p);
                    }
                }
            }
            catch (Exception ex) { }

            serializer = new XmlSerializer(typeof(QualificationCollection));
            try
            {
                using (FileStream stream = new FileStream("qualification.xml", FileMode.Open))
                {
                    IEnumerable<Race> RaceData = (IEnumerable<Race>)serializer.Deserialize(stream);
                    foreach (Race p in RaceData)
                    {
                        Races.Add(p);
                    }
                }
            }
            catch (Exception ex) { }

            XmlSerializer serializer3 = new XmlSerializer(typeof(RaceCollection));
            try
            {
                using (FileStream stream = new FileStream("race.xml", FileMode.Open))
                {
                    IEnumerable<Race> RaceData = (IEnumerable<Race>)serializer.Deserialize(stream);
                    foreach (Race p in RaceData)
                    {
                        Qualifications.Add(p);
                    }
                }
            }
            catch (Exception ex) { }

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
                cbSpeechVoice.SelectedIndex = 0;
            Pilots.CollectionChanged += Pilots_CollectionChanged;
        }
        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabControl tabcontrol = (TabControl)sender;
            if (tabcontrol.SelectedIndex == 0)
                UpdateQualificationTable();
        }
        private void Pilots_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            QualificationNeedsUpdate = true;
        }

        private void Pilots_dataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            QualificationNeedsUpdate = true;
        }

        //WINDOW CLOSING
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(PilotCollection));
            using (FileStream stream = new FileStream("pilots.xml", FileMode.Create))
            {
                serializer.Serialize(stream, Pilots);
            }

            serializer = new XmlSerializer(typeof(Settings));
            using (FileStream stream = new FileStream("settings.xml", FileMode.Create))
            {
                serializer.Serialize(stream, settings);
            }

            serializer = new XmlSerializer(typeof(RaceCollection));
            using (FileStream stream = new FileStream("race.xml", FileMode.Create))
            {
                serializer.Serialize(stream, Races);
            }

            serializer = new XmlSerializer(typeof(QualificationCollection));
            using (FileStream stream = new FileStream("qualification.xml", FileMode.Create))
            {
                serializer.Serialize(stream, Qualifications);
            }
        }

        //WINDOW LOADED
        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mySerialPort = new SerialPort(settings.SerialPortName, settings.SerialBaud, 0, 8, StopBits.One);
            mySerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            mySerialPort.Open();
            SendData("N0");
        }
        #endregion

        #region Recieving
        private delegate void UpdateUiTextDelegate(string text);
        //DATA RECEIVING
        private void DataReceivedHandler(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            string recieved_data = mySerialPort.ReadExisting();
            Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(ReadData), recieved_data);
        }

        //DATA SENDING
        private void SendData(string outdata)
        {
            mySerialPort.Write(outdata + "\n");
            listBox.Items.Add("[TX " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff") + "] " + outdata);
            //TODO AUTOSCROLL???
        }

        //MAIN PARSER
        private void ReadData(string indata)
        {
            for (int i = 0; i < indata.Length; i++)
            {
                if (indata[i] == '\n')
                {
                    if (readbuffer.Length == 0)
                        return;
                    listBox.Items.Add("[RX " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff") + "] " + readbuffer);
                    //TODO AUTOSCROLL???
                    switch (readbuffer[0])
                    {
                        case 'N': //ENUMERATE DEVICES
                            //TODO VERIFY SETTINGS!!!
                            //TODO: VoltageMonitoring
                            if (readbuffer.Length < 2)
                                break;
                            DeviceCount = readbuffer[1] - '0';
                            if (DeviceCount == 0)
                                break;
                            contender_slider1.Maximum = DeviceCount;
                            contender_slider2.Maximum = DeviceCount;
                            int tmp = DeviceCount;
                            if ((tmp % 2) == 1)
                                tmp--;
                            if (settings.NumberOfContendersForQualification > DeviceCount)
                                settings.NumberOfContendersForQualification = tmp;
                            if (settings.NumberOfContendersForRace > DeviceCount)
                                settings.NumberOfContendersForRace = tmp;
                            ChorusDevices = new ChorusDeviceClass[DeviceCount];
                            for (int ii = 0; ii < DeviceCount; ii++)
                            {
                                cbVoltageMonitoring.Items.Add("Device " + ii);
                                ChorusDevices[ii] = new ChorusDeviceClass();
                                ChorusDevices[ii].BatteryVoltageAdjustment = 1;
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

                                ComboBox combobox = new ComboBox();
                                combobox.Items.Add("0, Raceband");
                                combobox.Items.Add("1, Band A");
                                combobox.Items.Add("2, Band B");
                                combobox.Items.Add("3, Band E");
                                combobox.Items.Add("4, Band F(Airwave)");
                                combobox.Items.Add("5, Band D (5.3)");
                                combobox.HorizontalAlignment = HorizontalAlignment.Left;
                                combobox.VerticalAlignment = VerticalAlignment.Top;
                                combobox.SelectedIndex = 0;
                                combobox.Name = "ID" + ii + "B";
                                combobox.Margin = new Thickness(10, 90, 10, 0);
                                combobox.Height = 20;
                                combobox.Width = 150;
                                combobox.SelectionChanged += device_cbSelChange;
                                grid.Children.Add(combobox);
                                ChorusDevices[ii].Band = combobox;

                                combobox = new ComboBox();
                                combobox.Items.Add("Channel 1");
                                combobox.Items.Add("Channel 2");
                                combobox.Items.Add("Channel 3");
                                combobox.Items.Add("Channel 4");
                                combobox.Items.Add("Channel 5");
                                combobox.Items.Add("Channel 6");
                                combobox.Items.Add("Channel 7");
                                combobox.Items.Add("Channel 8");
                                combobox.HorizontalAlignment = HorizontalAlignment.Left;
                                combobox.VerticalAlignment = VerticalAlignment.Top;
                                combobox.SelectedIndex = 0;
                                combobox.Name = "ID" + ii + "C";
                                combobox.Margin = new Thickness(200, 90, 10, 0);
                                combobox.Height = 20;
                                combobox.Width = 150;
                                combobox.SelectionChanged += device_cbSelChange;
                                if (ii < 8)
                                    combobox.SelectedIndex = ii;
                                grid.Children.Add(combobox);
                                ChorusDevices[ii].Channel = combobox;

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
                                label.Content = "Current RSSI Treshold: 0";
                                label.Name = "ID" + ii + "T";
                                label.Margin = new Thickness(10, 132, 0, 0);
                                grid.Children.Add(label);
                                ChorusDevices[ii].CurrentTresholdLabel = label;

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
                                button.Content = "Set";
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
                                ChorusDevices[ii].grid = grid;
                            }
                            cbVoltageMonitoring.SelectedIndex = 0;
                            QualificationNeedsUpdate = true;
                            UpdateQualificationTable();
                            SendData("R*A");
                            break;
                        case 'S':
                            if (readbuffer.Length < 4)
                                break;
                            int device = readbuffer[1] - '0';
                            switch (readbuffer[2])
                            {
                                case 'B': //Current Band (half-byte; 0 - 5)
                                    if ((readbuffer[3] - '0') != ChorusDevices[device].Band.SelectedIndex)
                                    {
                                        SendData("R" + device + "N" + ChorusDevices[device].Band.SelectedIndex);
                                    }
                                    else
                                    {
                                        ChorusDevices[device].Band.SelectedIndex = readbuffer[3] - '0';
                                    }
                                    QualificationNeedsUpdate = true;
                                    break;
                                case 'C': //Current Channel (half-byte; 0 - 7)
                                    if ((readbuffer[3] - '0') != ChorusDevices[device].Channel.SelectedIndex)
                                    {
                                        SendData("R" + device + "H" + ChorusDevices[device].Channel.SelectedIndex);
                                    }
                                    else
                                    {
                                        ChorusDevices[device].Channel.SelectedIndex = readbuffer[3] - '0';
                                    }
                                    QualificationNeedsUpdate = true;
                                    break;
                                case 'D': //Sound State (half-byte; 1 = On, 0 = Off)
                                    if (readbuffer[3] == '0')
                                        ChorusDevices[device].SoundState.IsChecked = false;
                                    else
                                        ChorusDevices[device].SoundState.IsChecked = true;
                                    break;
                                case 'F': //First Lap State (half-byte; 1 = Skip, 0 = Count)
                                    if (readbuffer[3] == '0')
                                        ChorusDevices[device].SkipFirstLap.IsChecked = false;
                                    else
                                        ChorusDevices[device].SkipFirstLap.IsChecked = true;
                                    if (cbSkipFirstLap.IsChecked.Value != ChorusDevices[device].SkipFirstLap.IsChecked.Value)
                                    {
                                        SendData("R"+device+"F");
                                    }
                                    break;
                                case 'i': //Calibration State (half-byte, 1 = Calibrated, 0 = Not Calibrated)
                                    if (readbuffer[3] == '0')
                                        ChorusDevices[device].Calibrated.IsChecked = false;
                                    else
                                    {
                                        ChorusDevices[device].Calibrated.IsChecked = true;
                                        if (device<8)
                                            SendData("R" + device + "H" + device);
                                    }
                                    break;
                                case 'I': //Calibration Time (4 bytes)
                                    //TODO WEIRD RESULTS???
                                    //ChorusDevices[device].CalibrationTime = int.Parse(readbuffer.Substring(3), System.Globalization.NumberStyles.HexNumber);
                                    //SendData("C"+device+(ChorusDevices[device].CalibrationTime - TimerCalibration).ToString("X1"));
                                    ChorusDevices[device].CalibrationTime = TimerCalibration;
                                    SendData("C" + device + "0");
                                    ChorusDevices[device].CalibrationTimeLabel.Content = "Calibration Time: " + ChorusDevices[device].CalibrationTime + " ms for " + TimerCalibration + " ms";
                                    break;
                                case 'L': //Lap Time; last Lap Time is automatically sent in race mode when drone passes the gate; All Lap Times sent as a response to Bulk Device State (see below); Format: (1 byte: lap number + 4 bytes: lap time)
                                    TriggerLap(device, int.Parse(readbuffer.Substring(3, 2), System.Globalization.NumberStyles.HexNumber), int.Parse(readbuffer.Substring(5), System.Globalization.NumberStyles.HexNumber));
                                    break;
                                case 'M': //Minimal Lap Time (1 byte, in seconds)
                                    ChorusDevices[device].MinimalLapTime = int.Parse(readbuffer.Substring(3), System.Globalization.NumberStyles.HexNumber);
                                    ChorusDevices[device].MinimalLapTimeLabel.Content = "Minimal Lap time: " + ChorusDevices[device].MinimalLapTime + " seconds";
                                    if (ChorusDevices[device].MinimalLapTime != settings.MinimalLapTime)
                                    {
                                        SendData("R" + device + "L" + settings.MinimalLapTime.ToString("X2"));
                                    }
                                    break;
                                case 'P': //Device ist configured (half-byte, 1 = yes, 0 = no)
                                    if (readbuffer[3] == '0')
                                        ChorusDevices[device].Configured.IsChecked = false;
                                    else
                                        ChorusDevices[device].Configured.IsChecked = true;
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
                                    ChorusDevices[device].CurrentTresholdLabel.Content = "Current RSSI Treshold: " + ChorusDevices[device].CurrentTreshold;
                                    break;
                                case 'V': //RSSI Monitor State (half-byte; 1 = On, 0 = Off)
                                    if (readbuffer[3] == '0')
                                        ChorusDevices[device].RSSIMonitoringActive.IsChecked = false;
                                    else
                                        ChorusDevices[device].RSSIMonitoringActive.IsChecked = true;
                                    break;
                                case 'Y': //Current Voltage (2 bytes)
                                    ChorusDevices[device].CurrentVoltage = int.Parse(readbuffer.Substring(3), System.Globalization.NumberStyles.HexNumber);
                                    double batteryVoltage = (double)ChorusDevices[device].CurrentVoltage * 11 * 5 * (((double)ChorusDevices[device].BatteryVoltageAdjustment + 1000) / 1000) / 1024;
                                    int cellsCount = (int)(batteryVoltage / 3.4);
                                    double cellVoltage = batteryVoltage / cellsCount;
                                    ChorusDevices[device].CurrentVoltageLabel.Content = "Current Cell Voltage: " + cellVoltage.ToString("0.00") + " Volt";
                                    if (device == cbVoltageMonitoring.SelectedIndex) 
                                        if (cbEnableVoltageMonitoring.IsChecked == true)
                                            Title = "Chorus Lap Timer @ " + settings.SerialPortName + "(" + settings.SerialBaud + " Baud) Cell Voltage @ Device "+device+" :"+ cellVoltage.ToString("0.00") + " Volt";
                                    break;
                                case 'X': //All states corresponding to specified letters (see above) plus 'X' meaning the end of state transmission for each device
                                    if (device == 0)
                                    {
                                        aTimer.Interval = TimerCalibration;
                                        aTimer.Enabled = true;
                                        SendData("R*I");
                                    }
                                    break;
                            }
                            break;

                    }
                    readbuffer = "";
                }
                else
                {
                    readbuffer += indata[i];
                }
            }
        }
        
        //GET VOLTAGE MONITOR VALUE
        private void SendVoltageMonitorRequest(string outdata)
        {
            if (!settings.IsRaceActive)
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
                settings.MinimalLapTime = 5;
                settings.QualificationRaces = 1;
                settings.TimeToPrepare = 5;
            }
            DeviceCount = 0;
            readbuffer = "";
            contender_slider1.Value = settings.NumberOfContendersForQualification;
            contender_slider2.Value = settings.NumberOfContendersForRace;
            MinimalLapTimeLabel.Content = settings.MinimalLapTime + " seconds";
            TimeToPrepareLabel.Content = settings.TimeToPrepare + " seconds";
            settings.IsRaceActive = false;
        }
        
        private void Settings_TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabControl tabcontrol = (TabControl)sender;
            if ((tabcontrol.SelectedIndex - 2) >= 0)
            {
                if (!settings.IsRaceActive)
                    SendData("R" + (tabcontrol.SelectedIndex - 2) + "Y");
            }
        }
        
        #region Settings_Race
        //RACEMODE
        void RaceMode_Checked(object sender, RoutedEventArgs e)
        {
            settings.RaceMode = cbRaceMode1.IsChecked.Value;
            QualificationNeedsUpdate = true;
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
            settings.NumberofTime = value;
            QualificationNeedsUpdate = true;
        }

        //MINIMAL LAP TIME
        private void btn_MinimalLapTime(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            if (button.Name[0] == 'D')
            {
                if (settings.MinimalLapTime > 0)
                    settings.MinimalLapTime--;
            }
            else if (button.Name[0] == 'I')
            {
                if (settings.MinimalLapTime < 250)
                    settings.MinimalLapTime++;
            }
            SendData("R*L" + settings.MinimalLapTime.ToString("X2"));
            MinimalLapTimeLabel.Content = settings.MinimalLapTime + " seconds";
        }

        //TIME TO PREPARE
        private void btn_TimeToPrepare(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            if (button.Name[0] == 'D')
            {
                if (settings.TimeToPrepare > 0)
                    settings.TimeToPrepare--;
            }
            else if (button.Name[0] == 'I')
            {
                if (settings.TimeToPrepare < 120)
                    settings.TimeToPrepare++;
            }
            TimeToPrepareLabel.Content = settings.TimeToPrepare + " seconds";
        }

        //SKIP FIRST LAP
        private void SkipFirstLap_CLick(object sender, RoutedEventArgs e)
        {
            settings.SkipFirstLap = cbSkipFirstLap.IsChecked.Value;
            if (cbSkipFirstLap.IsChecked.Value)
            {
                SendData("R*F");
            }
            else
            {
                SendData("R*F");
            }
        }

        //USE DOUBLE OUT
        private void DoubleOut_Click(object sender, RoutedEventArgs e)
        {
            settings.DoubleOut = cbDoubleOut.IsChecked.Value;
        }

        //NUMBER OF CONTENDERS FOR QUALIFICATION RUNS
        private void contender_slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (contenders1 != null)
                contenders1.Text = e.NewValue.ToString();
            settings.NumberOfContendersForQualification = Convert.ToInt32(e.NewValue);
            QualificationNeedsUpdate = true;
        }

        //NUMBER OF QUALIFICATION RUNS
        private void btn_QualificationRuns(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            if (button.Name[0] == 'D')
            {
                if (settings.QualificationRaces > 1)
                    settings.QualificationRaces--;
            }
            else if (button.Name[0] == 'I')
            {
                if (settings.QualificationRaces < 10)
                    settings.QualificationRaces++;
            }
            QualificationRunsLabel.Content = settings.QualificationRaces;
            QualificationNeedsUpdate = true;
        }

        //NUMBER OF CONTENDERS FOR ELEMINATION RUNS
        private void contender_slider2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (contenders2 != null)
                contenders2.Text = e.NewValue.ToString();
            settings.NumberOfContendersForRace = Convert.ToInt32(e.NewValue);
        }

        //ENABLE VOLTAGE MONITORING
        private void cbVoltageMonitoring_SelChange(object sender, SelectionChangedEventArgs e)
        {
            settings.VoltageMonitorDevice = cbVoltageMonitoring.SelectedIndex;
            SendVoltageMonitorRequest("");
        }

        private void VoltageMonitoring_CLick(object sender, RoutedEventArgs e)
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
                Title = "Chorus Lap Timer @ " + settings.SerialPortName + "(" + settings.SerialBaud + " Baud)";
            }
        }
        private void VoltageMonitorTimerEvent(object source, ElapsedEventArgs e)
        {
            if (!settings.IsRaceActive)
                Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(SendVoltageMonitorRequest), "");
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
                        SendData("R" + device + "V");
                    else
                        SendData("R" + device + "v");
                    break;
            }
        }

        private void device_cbSelChange(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combobox = (ComboBox)sender;
            var device = combobox.Name[2] - '0';
            switch (combobox.Name[3])
            {
                case 'B':
                    SendData("R" + device + "N" + combobox.SelectedIndex);
                    break;
                case 'C':
                    SendData("R" + device + "H" + combobox.SelectedIndex);
                    break;
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

        #region Raceing

        private void UpdateQualificationTable()
        {
            if (QualificationNeedsUpdate && (ChorusDevices != null))
            {
                if (!settings.IsRaceActive)
                {
                    settings.Heat = 0;
                    QualificationNeedsUpdate = false;
                    Qualifications.Clear();
                    Heat.Clear();
                    int numberofheats = (int)Math.Ceiling((double)Pilots.Count / settings.NumberOfContendersForQualification);
                    int i = 1, ii = 0;
                    labelCurrentHeat.Content = "Qualification Run 1, Heat 1:";
                    for (int iii = 1; iii <= settings.QualificationRaces; iii++)
                    {
                        foreach (Pilot pilot in Pilots)
                        {
                            Race race = new Race();
                            race.guid = pilot.guid;
                            race.Name = pilot.Name;
                            race.Heat = i.ToString();
                            race.Device = ii;
                            race.RFChannel = ChorusDevices[ii].Band.Text + ", " + ChorusDevices[ii].Channel.Text;
                            Qualifications.Add(race);
                            if (i == 1)
                            {
                                Heat.Add(race);
                            }
                            ii++;
                            if (ii == settings.NumberOfContendersForQualification)
                            {
                                ii = 0;
                                i++;
                            }
                        }
                        ii = 0;
                        i++;
                    }
                }
                else
                {
                    //race is active, fill heat!!!
                }
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            /*
                k so heres an idea:
                the tool autogenerates a list for qualification pilots & heats.
                pilots for first qualification run will be autoselected.
                on press on "start heat" its going take the lap times and the button will turn into "stop heat"
                if "stop heat" is clicked you will be allowed to modify the table.
                after that you can click "add results" and its going to add the results to the qualification table and going to load the new heat.

                once all qualification heats are done its going to fill the pilots for elimination phase
                starting heat works on the same way.

                well might remove "add results" and use the same button im using for start and stop.
            */

            //TODO
            if (settings.IsRaceActive)
            {
                SendData("R*r");
                btnRace.Content = "Start Race";
                settings.IsRaceActive = false;
                for (int i = 0; i < DeviceCount; i++)
                    ChorusDevices[i].grid.IsEnabled = true;
                Pilots_dataGrid.IsEnabled = true;
                RaceSettingsGrid.IsEnabled = true;
                textBox.IsEnabled = true;
                synthesizer.SpeakAsync("Race finished");
            }
            else
            {
                SendData("R*v");
                SendData("R*R");
                btnRace.Content = "Stop Race";
                settings.IsRaceActive = true;
                for (int i = 0; i < DeviceCount; i++)
                    ChorusDevices[i].grid.IsEnabled = false;
                Pilots_dataGrid.IsEnabled = false;
                RaceSettingsGrid.IsEnabled = false;
                textBox.IsEnabled = false;
                synthesizer.SpeakAsync("Race started");
            }

        }

        public void TriggerLap(int device, int lap, int milliseconds)
        {
            ChorusDevices[device].LapTimes.Items.Add(new { Lap = lap.ToString(), Time = milliseconds.ToString() });
            //TODO
        }

        #endregion
    }
}
