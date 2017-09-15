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
            if (Event.races == null)
            {
                return;
            }
            Event.races.Clear();
            int heats = 0;
            int contenders = Event.Contenders;
            if (contenders > Event.pilots.Count())
            {
                contenders = Event.pilots.Count();
            }
            int device = 0;
            int heat = (int)Math.Ceiling((double)Event.pilots.Count / Event.NumberOfContendersForQualification) * Event.QualificationRaces;
            heats = (int)Math.Ceiling((double)contenders / Event.NumberOfContendersForRace) + heat; 
            for (int i = 0 ; i < contenders; i++)
            {
                Race race = new Race();
                race.guid = "*g" + i;
                race.Heat = heat;
                race.Device = device;
                race.pilot = new Pilot();
                race.pilot.Name = "Qualification Rank " + (i+1);
                Event.races.Add(race);
                heat++;
                if (heat == heats)
                {
                    heat = (int)Math.Ceiling((double)Event.pilots.Count / Event.NumberOfContendersForQualification) * Event.QualificationRaces;
                    device++;
                }
            }
            //TODO: Build tree
        }
    }
}
