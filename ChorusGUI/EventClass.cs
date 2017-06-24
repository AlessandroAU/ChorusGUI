using System;
using System.Xml;

namespace chorusgui
{
    public partial class EventClass
    {
        public ChorusGUI gui;
        public String name;
        public int MinimalLapTime = 5;
        public int TimeToPrepare = 5;
        public Boolean SkipFirstLap = true;
        public Boolean DoubleOut = true;
        public Boolean RaceMode = true;
        public int NumberOfContendersForQualification = 2;
        public int QualificationRaces = 1;
        public int NumberOfContendersForRace = 2;
        public int CurrentHeat = 0;
        public Boolean IsRaceActive = false;
        public int NumberofTimeForHeat = 0;

        public PilotCollection pilots;
        public QualificationCollection qualifications;
        public RaceCollection races;

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
                        case "doubleout":
                            if (xmlNode.InnerText.ToLower() == "true")
                            {
                                DoubleOut = true;
                                gui.cbDoubleOut.IsChecked = true;
                            }
                            else
                            {
                                DoubleOut = false;
                                gui.cbDoubleOut.IsChecked = false;
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
                        case "israceactive":
                            if (xmlNode.InnerText.ToLower() == "true")
                            {
                                IsRaceActive = true;
                            }
                            else
                            {
                                IsRaceActive = false;
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
                        case "timetoprepare":
                            TimeToPrepare = Int32.Parse(xmlNode.InnerText);
                            gui.TimeToPrepareLabel.Content = TimeToPrepare + " seconds";
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
                                            pilot.BestLap = xmlPilot.InnerText;
                                            break;
                                        case "bestrace":
                                            pilot.BestRace = xmlPilot.InnerText;
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
                                foreach (XmlNode xmlRace in xmlQualification)
                                {
                                    switch (xmlRace.Name.ToLower())
                                    {
                                        case "guid":
                                            race.guid = xmlRace.InnerText;
                                            foreach (Pilot pilot in pilots)
                                            {
                                                if (pilot.guid == race.guid)
                                                    race.pilot = pilot;
                                            }
                                            if (race.pilot == null)
                                            {
                                                //TODO: WTF???
                                            }
                                            break;
                                        case "heat":
                                            race.Heat = Int32.Parse(xmlRace.InnerText);
                                            break;
                                        case "device":
                                            race.Device = Int32.Parse(xmlRace.InnerText);
                                            break;
                                        case "laps":
                                            race.Laps = xmlRace.InnerText;
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
                                foreach (XmlNode xmlRace in xmlRaces)
                                {
                                    switch (xmlRace.Name.ToLower())
                                    {
                                        case "guid":
                                            race.guid = xmlRace.InnerText;
                                            foreach (Pilot pilot in pilots)
                                            {
                                                if (pilot.guid == race.guid)
                                                    race.pilot = pilot;
                                            }
                                            if (race.pilot == null)
                                            {
                                                //TODO: WTF???
                                            }
                                            break;
                                        case "heat":
                                            race.Heat = Int32.Parse(xmlRace.InnerText);
                                            break;
                                        case "device":
                                            race.Device = Int32.Parse(xmlRace.InnerText);
                                            break;
                                        case "laps":
                                            race.Laps = xmlRace.InnerText;
                                            break;
                                    }
                                }
                                races.Add(race);
                            }
                            break;
                    }
                }
                gui.UpdateRecentFileList(filename);
            }
            catch (Exception ex)
            {
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

            xmlItem = xmlDoc.CreateElement("doubleout");
            xmlText = xmlDoc.CreateTextNode(this.DoubleOut.ToString());
            xmlItem.AppendChild(xmlText);
            rootNode.AppendChild(xmlItem);

            xmlItem = xmlDoc.CreateElement("racemode");
            xmlText = xmlDoc.CreateTextNode(this.RaceMode.ToString());
            xmlItem.AppendChild(xmlText);
            rootNode.AppendChild(xmlItem);

            xmlItem = xmlDoc.CreateElement("israceactive");
            xmlText = xmlDoc.CreateTextNode(this.IsRaceActive.ToString());
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

            xmlItem = xmlDoc.CreateElement("timetoprepare");
            xmlText = xmlDoc.CreateTextNode(this.TimeToPrepare.ToString());
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
                if (pilot.BestLap != null)
                {
                    xmlchild = xmlDoc.CreateElement("bestlap");
                    xmlText = xmlDoc.CreateTextNode(pilot.BestLap);
                    xmlchild.AppendChild(xmlText);
                    xmlpilot.AppendChild(xmlchild);
                }
                if (pilot.BestRace != null)
                {
                    xmlchild = xmlDoc.CreateElement("bestrace");
                    xmlText = xmlDoc.CreateTextNode(pilot.BestRace);
                    xmlchild.AppendChild(xmlText);
                    xmlpilot.AppendChild(xmlchild);
                }
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

                if (race.Laps != null)
                {
                    xmlchild = xmlDoc.CreateElement("bestrace");
                    xmlText = xmlDoc.CreateTextNode(race.Laps);
                    xmlchild.AppendChild(xmlText);
                    xmlrace.AppendChild(xmlchild);
                }
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

                xmlchild = xmlDoc.CreateElement("head");
                xmlText = xmlDoc.CreateTextNode(race.Heat.ToString());
                xmlchild.AppendChild(xmlText);
                xmlrace.AppendChild(xmlchild);

                xmlchild = xmlDoc.CreateElement("device");
                xmlText = xmlDoc.CreateTextNode(race.Device.ToString());
                xmlchild.AppendChild(xmlText);
                xmlrace.AppendChild(xmlchild);

                if (race.Laps != null)
                {
                    xmlchild = xmlDoc.CreateElement("bestrace");
                    xmlText = xmlDoc.CreateTextNode(race.Laps);
                    xmlchild.AppendChild(xmlText);
                    xmlrace.AppendChild(xmlchild);
                }

            }
            rootNode.AppendChild(xmlItem);
            xmlDoc.Save(filename);
            gui.UpdateRecentFileList(filename);
            return true;
        }

    }
}