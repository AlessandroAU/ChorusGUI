using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace chorusgui
{
    public partial class ChorusGUI : Window
    {
        private void BuildSingleOutTable()
        {
            //TODO: we dont need that racehelper, build the table from scrath and fill with "winner heat xxx, looser heat xxx"
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
            Event.races.Clear();
            int heats = (int)Math.Ceiling((double)Event.pilots.Count / Event.NumberOfContendersForRace);
            int heat = 0;
            int device = 0;
            /*REMOVEME*/
            foreach (Race race in racehelper)
            {
                Race newrace = new Race();
                newrace.guid = race.guid;
                newrace.pilot = race.pilot;
                newrace.Device = device;
                newrace.Heat = heat + Event.CurrentHeat;
                Event.races.Add(newrace);
                heat++;
                if (heat == heats)
                {
                    device++;
                    heat = 0;
                }
            }
            //TODO BUILD TREE!!!
            //TODO build elemination table
            heat = heats;
            int pilots = racehelper.Count;
            int winners;
            int winnersheat = heats;
            int loosers;
            int loosersheat = heats;
            int newwinnerheats;
            int newlooserheats;

            {
                winners = heats * Event.NumberOfContendersForRace / 2;
                newwinnerheats = (int)Math.Ceiling((double)winners / Event.NumberOfContendersForRace);
                loosers = pilots - winners;
                newlooserheats = (int)Math.Ceiling((double)loosers / Event.NumberOfContendersForRace);
                //TODO

            }
            /*
            for (int i=0;i< count*2;i++)
            {
                Race newrace = new Race();
                newrace.Heat = heat + Event.CurrentHeat + i;
                newrace.Device = device;
                newrace.pilot = new Pilot();
                newrace.pilot.Name = "Winner Heat " + (Event.CurrentHeat + i);
                newrace.pilot.guid = "*" + newrace.pilot.Name;

                Event.races.Add(newrace);
            }
            */
            UpdateGridViews();
            //TODO: sort eliminationgridview by heat + device!
        }
    }
}
