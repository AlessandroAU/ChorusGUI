﻿using System;
using System.Xml;
using System.Windows.Controls;
using System.Windows;

namespace chorusgui
{
    public class ChorusDeviceSettings
    {
        public int Frequency;
        public int CurrentThreshold;
    }

    public partial class EventClass
    {
        public ChorusGUI gui;
        public String name;
        public int MinimalLapTime = 5;
        public Boolean SkipFirstLap = true;
        public string EliminationSystem;
        public Boolean RaceMode = true;
        public int NumberOfContendersForQualification = 1;
        public int QualificationRaces = 5;
        public int NumberOfContendersForRace = 1;
        public int CurrentHeat = 0;
        public Boolean IsRaceActive = false;
        public int NumberofTimeForHeat = 0;
        public int Contenders=0;
        public string QualificationAddResults;

        public PilotCollection pilots;
        public QualificationCollection qualifications;
        public RaceCollection races;
        public Boolean IsRaceComplete = false;
        public ChorusDeviceSettings[] ChorusDeviceSettings;

        public EventClass()
        {
            //constructor
        }

        public Boolean LoadEvent(string filename)
        {
            pilots.Clear();
            qualifications.Clear();
            races.Clear();
            CurrentHeat = 0;
            IsRaceActive = false;
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(filename);
                foreach (XmlNode xmlNode in xmlDoc.DocumentElement)
                {
                    switch (xmlNode.Name.ToLower())
                    {
                        case "name":
                            name = xmlNode.InnerText;
                            gui.txtEventName.Text = name;
                            break;
                        case "skipfirstlap":
                            if (xmlNode.InnerText.ToLower() == "true")
                            {
                                SkipFirstLap = true;
                                gui.cbSkipFirstLap.IsChecked = true;
                            }
                            else
                            {
                                SkipFirstLap = false;
                                gui.cbSkipFirstLap.IsChecked = false;
                            }
                            break;
                        case "isracecomplete":
                            if (xmlNode.InnerText.ToLower() == "true")
                            {
                                IsRaceComplete = true;
                            }
                            else
                            {
                                IsRaceComplete = false;
                            }
                            break;
                        case "eliminationsystem":
                            gui.cbElimination.SelectedIndex = 1;
                            EliminationSystem = xmlNode.InnerText.ToLower();
                            foreach (ComboBoxItem item in gui.cbElimination.Items)
                            {
                                if (item.Tag.Equals(EliminationSystem))
                                {
                                    gui.cbElimination.SelectedItem = item;
                                    break;
                                }
                            }
                            break;
                        case "racemode":
                            if (xmlNode.InnerText.ToLower() == "true")
                            {
                                RaceMode = true;
                                gui.cbRaceMode2.IsChecked = false;
                                gui.cbRaceMode1.IsChecked = true;
                            }
                            else
                            {
                                RaceMode = false;
                                gui.cbRaceMode1.IsChecked = false;
                                gui.cbRaceMode2.IsChecked = true;
                            }
                            break;
                        case "qualificationaddresults":
                            QualificationAddResults = xmlNode.InnerText.ToLower();
                            gui.cbQualificationAddResults.SelectedIndex = 0;
                            foreach (ComboBoxItem item in gui.cbQualificationAddResults.Items)
                            {
                                if (item.Tag.Equals(QualificationAddResults))
                                {
                                    gui.cbQualificationAddResults.SelectedItem = item;
                                    break;
                                }
                            }
                            break;
                        case "israceactive":
                            if (xmlNode.InnerText.ToLower() == "true")
                            {
                                IsRaceActive = true;
                                gui.Pilots_dataGrid.IsEnabled = false;
                                gui.RaceSettingsGrid.IsEnabled = false;
                            }
                            else
                            {
                                IsRaceActive = false;
                                gui.Pilots_dataGrid.IsEnabled = true;
                                gui.RaceSettingsGrid.IsEnabled = true;
                            }
                            break;
                        case "numberoftimeforheat":
                            NumberofTimeForHeat = Int32.Parse(xmlNode.InnerText);
                            gui.txtRaceMode.Text = NumberofTimeForHeat.ToString();
                            break;
                        case "minimallaptime":
                            MinimalLapTime = Int32.Parse(xmlNode.InnerText);
                            gui.MinimalLapTimeLabel.Content = MinimalLapTime + " seconds";
                            break;
                        case "numberofcontendersforqualification":
                            NumberOfContendersForQualification = Int32.Parse(xmlNode.InnerText);
                            gui.contender_slider1.Value = NumberOfContendersForQualification;
                            break;
                        case "qualificationraces":
                            QualificationRaces = Int32.Parse(xmlNode.InnerText);
                            gui.QualificationRunsLabel.Content = QualificationRaces;
                            break;
                        case "numberofcontendersforrace":
                            NumberOfContendersForRace = Int32.Parse(xmlNode.InnerText);
                            gui.contender_slider2.Value = NumberOfContendersForRace;
                            break;
                        case "contenders":
                            Contenders = Int32.Parse(xmlNode.InnerText);
                            gui.txtContenders.Text = Contenders.ToString();
                            break;
                        case "currentheat":
                            CurrentHeat = Int32.Parse(xmlNode.InnerText);
                            break;
                        case "pilots":
                            foreach (XmlNode xmlPilots in xmlNode)
                            {
                                Pilot pilot = new Pilot();
                                foreach (XmlNode xmlPilot in xmlPilots)
                                {
                                    switch (xmlPilot.Name.ToLower())
                                    {
                                        case "guid":
                                            pilot.guid = xmlPilot.InnerText;
                                            break;
                                        case "name":
                                            pilot.Name = xmlPilot.InnerText;
                                            break;
                                        case "ranking":
                                            pilot.Ranking = xmlPilot.InnerText;
                                            break;
                                        case "bestlap":
                                            pilot.BestLap = int.Parse(xmlPilot.InnerText);
                                            break;
                                        case "overtime":
                                            pilot.overtime = int.Parse(xmlPilot.InnerText);
                                            break;
                                        case "totallaps":
                                            pilot.totallaps = int.Parse(xmlPilot.InnerText);
                                            break;
                                        case "firstentry":
                                            if (xmlNode.InnerText.ToLower() == "true")
                                            {
                                                pilot.firstentry = true;
                                            }
                                            else
                                            {
                                                pilot.firstentry = false;
                                            }
                                            break;
                                    }
                                }
                                pilots.Add(pilot);
                            }
                            break;
                        case "qualifications":
                            foreach (XmlNode xmlQualification in xmlNode)
                            {
                                Race race = new Race();
                                race.lap.race = race;
                                foreach (XmlNode xmlRace in xmlQualification)
                                {
                                    switch (xmlRace.Name.ToLower())
                                    {
                                        case "guid":
                                            race.guid = xmlRace.InnerText;
                                            foreach (Pilot pilot in pilots)
                                            {
                                                if (pilot.guid == race.guid)
                                                {
                                                    race.pilot = pilot;
                                                    break;
                                                }
                                            }
                                            //TODO: WTF???
                                            break;
                                        case "heat":
                                            race.Heat = Int32.Parse(xmlRace.InnerText);
                                            break;
                                        case "device":
                                            race.Device = Int32.Parse(xmlRace.InnerText);
                                            break;
                                        case "laps":
                                            race.laps = xmlRace.InnerText;
                                            break;
                                        case "bestlap":
                                            race.BestLap = int.Parse(xmlRace.InnerText);
                                            break;
                                        case "overtime":
                                            race.overtime = int.Parse(xmlRace.InnerText);
                                            break;
                                        case "totallaps":
                                            race.totallaps = int.Parse(xmlRace.InnerText);
                                            break;
                                        case "finished":
                                            if (xmlNode.InnerText.ToLower() == "true")
                                            {
                                                race.finished = true;
                                            }
                                            else
                                            {
                                                race.finished = false;
                                            }
                                            break;
                                    }
                                }
                                qualifications.Add(race);
                            }
                            break;
                        case "races":
                            foreach (XmlNode xmlRaces in xmlNode)
                            {
                                Race race = new Race();
                                race.lap.race = race;
                                foreach (XmlNode xmlRace in xmlRaces)
                                {
                                    switch (xmlRace.Name.ToLower())
                                    {
                                        case "guid":
                                            race.guid = xmlRace.InnerText;
                                            if (race.guid[0] == '*')
                                            {
                                                //TODO: prettyfy names
                                                race.pilot = new Pilot();
                                                race.pilot.Name = race.guid;
                                            }
                                            else
                                            {
                                                foreach (Pilot pilot in pilots)
                                                {
                                                    if (pilot.guid == race.guid)
                                                        race.pilot = pilot;
                                                }
                                            }
                                            if (race.pilot == null)
                                            {
                                                //WTF???
                                            }
                                            break;
                                        case "heat":
                                            race.Heat = Int32.Parse(xmlRace.InnerText);
                                            break;
                                        case "device":
                                            race.Device = Int32.Parse(xmlRace.InnerText);
                                            break;
                                        case "laps":
                                            race.laps = xmlRace.InnerText;
                                            break;
                                        case "bestlap":
                                            race.BestLap = int.Parse(xmlRace.InnerText);
                                            break;
                                        case "overtime":
                                            race.overtime = int.Parse(xmlRace.InnerText);
                                            break;
                                        case "totallaps":
                                            race.totallaps = int.Parse(xmlRace.InnerText);
                                            break;
                                        case "finished":
                                            if (xmlNode.InnerText.ToLower() == "true")
                                            {
                                                race.finished = true;
                                            }
                                            else
                                            {
                                                race.finished = false;
                                            }
                                            break;
                                    }
                                }
                                races.Add(race);
                            }
                            break;
                        case "devices":
                            int devicecount = xmlNode.SelectNodes("device").Count;
                            if (devicecount == 0)
                            {
                                break;
                            }
                            ChorusDeviceSettings = new ChorusDeviceSettings[devicecount];
                            int i = 0;
                            foreach (XmlNode xmlDevices in xmlNode)
                            {
                                ChorusDeviceSettings[i] = new ChorusDeviceSettings();
                                foreach (XmlNode xmlDevice in xmlDevices)
                                {
                                    switch (xmlDevice.Name.ToLower())
                                    {
                                        case "rssithreshold":
                                            ChorusDeviceSettings[i].CurrentThreshold = Int32.Parse(xmlDevice.InnerText);
                                            break;
                                        case "frequency":
                                            ChorusDeviceSettings[i].Frequency = Int32.Parse(xmlDevice.InnerText);
                                            break;
                                    }
                                }
                                i++;
                            }
                            break;
                    }
                }
                gui.UpdateRecentFileList(filename);
                gui.BuildEventTables();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            if (gui.NumberOfDevices > 0)
            {
                if ((gui.NumberOfDevices < NumberOfContendersForQualification) || (gui.NumberOfDevices < NumberOfContendersForRace))
                {
                    gui.btnRace.IsEnabled = false;
                    MessageBox.Show("You dont have enough Devices to continue this event", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    gui.btnRace.IsEnabled = true;
                }
            }
            return true;
        }

        public Boolean SaveEvent(string filename)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", null, null);
            XmlElement root = xmlDoc.DocumentElement;
            xmlDoc.InsertBefore(xmlDeclaration, root);

            XmlNode rootNode = xmlDoc.CreateElement("event");
            XmlAttribute nsAttribute = xmlDoc.CreateAttribute("xmlns", "xsi", "http://www.w3.org/2000/xmlns/");
            nsAttribute.Value = "http://www.w3.org/2001/XMLSchema-instance";
            rootNode.Attributes.Append(nsAttribute);
            nsAttribute = xmlDoc.CreateAttribute("xmlns", "xsd", "http://www.w3.org/2000/xmlns/");
            nsAttribute.Value = "http://www.w3.org/2001/XMLSchema";
            rootNode.Attributes.Append(nsAttribute);
            xmlDoc.AppendChild(rootNode);

            XmlNode xmlItem = xmlDoc.CreateElement("name");
            XmlText xmlText = xmlDoc.CreateTextNode(this.name);
            xmlItem.AppendChild(xmlText);
            rootNode.AppendChild(xmlItem);

            xmlItem = xmlDoc.CreateElement("skipfirstlap");
            xmlText = xmlDoc.CreateTextNode(this.SkipFirstLap.ToString());
            xmlItem.AppendChild(xmlText);
            rootNode.AppendChild(xmlItem);

            xmlItem = xmlDoc.CreateElement("eliminationsystem");
            xmlText = xmlDoc.CreateTextNode(this.EliminationSystem);
            xmlItem.AppendChild(xmlText);
            rootNode.AppendChild(xmlItem);

            xmlItem = xmlDoc.CreateElement("racemode");
            xmlText = xmlDoc.CreateTextNode(this.RaceMode.ToString());
            xmlItem.AppendChild(xmlText);
            rootNode.AppendChild(xmlItem);

            xmlItem = xmlDoc.CreateElement("isracecomplete");
            xmlText = xmlDoc.CreateTextNode(this.IsRaceComplete.ToString());
            xmlItem.AppendChild(xmlText);
            rootNode.AppendChild(xmlItem);

            xmlItem = xmlDoc.CreateElement("israceactive");
            xmlText = xmlDoc.CreateTextNode(this.IsRaceActive.ToString());
            xmlItem.AppendChild(xmlText);
            rootNode.AppendChild(xmlItem);

            xmlItem = xmlDoc.CreateElement("qualificationaddresults");
            xmlText = xmlDoc.CreateTextNode(this.QualificationAddResults);
            xmlItem.AppendChild(xmlText);
            rootNode.AppendChild(xmlItem);

            xmlItem = xmlDoc.CreateElement("numberoftimeforheat");
            xmlText = xmlDoc.CreateTextNode(this.NumberofTimeForHeat.ToString());
            xmlItem.AppendChild(xmlText);
            rootNode.AppendChild(xmlItem);

            xmlItem = xmlDoc.CreateElement("minimallaptime");
            xmlText = xmlDoc.CreateTextNode(this.MinimalLapTime.ToString());
            xmlItem.AppendChild(xmlText);
            rootNode.AppendChild(xmlItem);

            xmlItem = xmlDoc.CreateElement("numberofcontendersforqualification");
            xmlText = xmlDoc.CreateTextNode(this.NumberOfContendersForQualification.ToString());
            xmlItem.AppendChild(xmlText);
            rootNode.AppendChild(xmlItem);

            xmlItem = xmlDoc.CreateElement("qualificationraces");
            xmlText = xmlDoc.CreateTextNode(this.QualificationRaces.ToString());
            xmlItem.AppendChild(xmlText);
            rootNode.AppendChild(xmlItem);

            xmlItem = xmlDoc.CreateElement("numberofcontendersforrace");
            xmlText = xmlDoc.CreateTextNode(this.NumberOfContendersForRace.ToString());
            xmlItem.AppendChild(xmlText);
            rootNode.AppendChild(xmlItem);

            xmlItem = xmlDoc.CreateElement("contenders");
            xmlText = xmlDoc.CreateTextNode(this.Contenders.ToString());
            xmlItem.AppendChild(xmlText);
            rootNode.AppendChild(xmlItem);

            xmlItem = xmlDoc.CreateElement("currentheat");
            xmlText = xmlDoc.CreateTextNode(this.CurrentHeat.ToString());
            xmlItem.AppendChild(xmlText);
            rootNode.AppendChild(xmlItem);

            xmlItem = xmlDoc.CreateElement("pilots");
            foreach (Pilot pilot in pilots)
            {
                XmlNode xmlpilot = xmlDoc.CreateElement("pilot");
                xmlItem.AppendChild(xmlpilot);

                XmlNode xmlchild = xmlDoc.CreateElement("guid");
                xmlText = xmlDoc.CreateTextNode(pilot.guid);
                xmlchild.AppendChild(xmlText);
                xmlpilot.AppendChild(xmlchild);

                xmlchild = xmlDoc.CreateElement("name");
                xmlText = xmlDoc.CreateTextNode(pilot.Name);
                xmlchild.AppendChild(xmlText);
                xmlpilot.AppendChild(xmlchild);

                if (pilot.Ranking != null)
                {
                    xmlchild = xmlDoc.CreateElement("ranking");
                    xmlText = xmlDoc.CreateTextNode(pilot.Ranking);
                    xmlchild.AppendChild(xmlText);
                    xmlpilot.AppendChild(xmlchild);
                }

                xmlchild = xmlDoc.CreateElement("bestlap");
                xmlText = xmlDoc.CreateTextNode(pilot.BestLap.ToString());
                xmlchild.AppendChild(xmlText);
                xmlpilot.AppendChild(xmlchild);

                xmlchild = xmlDoc.CreateElement("overtime");
                xmlText = xmlDoc.CreateTextNode(pilot.overtime.ToString());
                xmlchild.AppendChild(xmlText);
                xmlpilot.AppendChild(xmlchild);

                xmlchild = xmlDoc.CreateElement("totallaps");
                xmlText = xmlDoc.CreateTextNode(pilot.totallaps.ToString());
                xmlchild.AppendChild(xmlText);
                xmlpilot.AppendChild(xmlchild);

                xmlchild = xmlDoc.CreateElement("firstentry");
                xmlText = xmlDoc.CreateTextNode(pilot.firstentry.ToString());
                xmlchild.AppendChild(xmlText);
                xmlpilot.AppendChild(xmlchild);
            }
            rootNode.AppendChild(xmlItem);

            xmlItem = xmlDoc.CreateElement("qualifications");
            foreach (Race race in qualifications)
            {
                XmlNode xmlrace = xmlDoc.CreateElement("race");
                xmlItem.AppendChild(xmlrace);

                XmlNode xmlchild = xmlDoc.CreateElement("guid");
                xmlText = xmlDoc.CreateTextNode(race.guid);
                xmlchild.AppendChild(xmlText);
                xmlrace.AppendChild(xmlchild);

                xmlchild = xmlDoc.CreateElement("heat");
                xmlText = xmlDoc.CreateTextNode(race.Heat.ToString());
                xmlchild.AppendChild(xmlText);
                xmlrace.AppendChild(xmlchild);

                xmlchild = xmlDoc.CreateElement("device");
                xmlText = xmlDoc.CreateTextNode(race.Device.ToString());
                xmlchild.AppendChild(xmlText);
                xmlrace.AppendChild(xmlchild);

                if (race.laps != null)
                {
                    xmlchild = xmlDoc.CreateElement("laps");
                    xmlText = xmlDoc.CreateTextNode(race.laps);
                    xmlchild.AppendChild(xmlText);
                    xmlrace.AppendChild(xmlchild);
                }

                xmlchild = xmlDoc.CreateElement("bestlap");
                xmlText = xmlDoc.CreateTextNode(race.BestLap.ToString());
                xmlchild.AppendChild(xmlText);
                xmlrace.AppendChild(xmlchild);

                xmlchild = xmlDoc.CreateElement("overtime");
                xmlText = xmlDoc.CreateTextNode(race.overtime.ToString());
                xmlchild.AppendChild(xmlText);
                xmlrace.AppendChild(xmlchild);

                xmlchild = xmlDoc.CreateElement("totallaps");
                xmlText = xmlDoc.CreateTextNode(race.totallaps.ToString());
                xmlchild.AppendChild(xmlText);
                xmlrace.AppendChild(xmlchild);

                xmlchild = xmlDoc.CreateElement("finished");
                xmlText = xmlDoc.CreateTextNode(race.finished.ToString());
                xmlchild.AppendChild(xmlText);
                xmlrace.AppendChild(xmlchild);
            }
            rootNode.AppendChild(xmlItem);

            xmlItem = xmlDoc.CreateElement("races");
            foreach (Race race in races)
            {
                XmlNode xmlrace = xmlDoc.CreateElement("race");
                xmlItem.AppendChild(xmlrace);

                XmlNode xmlchild = xmlDoc.CreateElement("guid");
                xmlText = xmlDoc.CreateTextNode(race.guid);
                xmlchild.AppendChild(xmlText);
                xmlrace.AppendChild(xmlchild);

                xmlchild = xmlDoc.CreateElement("heat");
                xmlText = xmlDoc.CreateTextNode(race.Heat.ToString());
                xmlchild.AppendChild(xmlText);
                xmlrace.AppendChild(xmlchild);

                xmlchild = xmlDoc.CreateElement("device");
                xmlText = xmlDoc.CreateTextNode(race.Device.ToString());
                xmlchild.AppendChild(xmlText);
                xmlrace.AppendChild(xmlchild);

                if (race.laps != null)
                {
                    xmlchild = xmlDoc.CreateElement("laps");
                    xmlText = xmlDoc.CreateTextNode(race.laps);
                    xmlchild.AppendChild(xmlText);
                    xmlrace.AppendChild(xmlchild);
                }

                xmlchild = xmlDoc.CreateElement("bestlap");
                xmlText = xmlDoc.CreateTextNode(race.BestLap.ToString());
                xmlchild.AppendChild(xmlText);
                xmlrace.AppendChild(xmlchild);

                xmlchild = xmlDoc.CreateElement("overtime");
                xmlText = xmlDoc.CreateTextNode(race.overtime.ToString());
                xmlchild.AppendChild(xmlText);
                xmlrace.AppendChild(xmlchild);

                xmlchild = xmlDoc.CreateElement("totallaps");
                xmlText = xmlDoc.CreateTextNode(race.totallaps.ToString());
                xmlchild.AppendChild(xmlText);
                xmlrace.AppendChild(xmlchild);

                xmlchild = xmlDoc.CreateElement("finished");
                xmlText = xmlDoc.CreateTextNode(race.finished.ToString());
                xmlchild.AppendChild(xmlText);
                xmlrace.AppendChild(xmlchild);
            }

            rootNode.AppendChild(xmlItem);

            if ((gui.ChorusDevices != null) && (gui.ChorusDevices.Length > 0))
            {
                xmlItem = xmlDoc.CreateElement("devices");
                for (int i = 0; i < gui.ChorusDevices.Length; i++)
                {
                    XmlNode xmlDevice = xmlDoc.CreateElement("device");

                    XmlNode xmlchild = xmlDoc.CreateElement("rssithreshold");
                    xmlText = xmlDoc.CreateTextNode(gui.ChorusDevices[i].CurrentThreshold.ToString());
                    xmlchild.AppendChild(xmlText);
                    xmlDevice.AppendChild(xmlchild);

                    xmlchild = xmlDoc.CreateElement("frequency");
                    ComboBoxItem cbitem = (ComboBoxItem)gui.ChorusDevices[i].Frequency.SelectedItem;
                    if (cbitem != null)
                    {
                        xmlText = xmlDoc.CreateTextNode(cbitem.Tag.ToString());
                    }
                    else
                    {
                        xmlText = xmlDoc.CreateTextNode("5658");
                    }
                    xmlchild.AppendChild(xmlText);
                    xmlDevice.AppendChild(xmlchild);

                    xmlItem.AppendChild(xmlDevice);
                }
                rootNode.AppendChild(xmlItem);
            }
            xmlDoc.Save(filename);
            gui.UpdateRecentFileList(filename);
            return true;
        }

    }
}