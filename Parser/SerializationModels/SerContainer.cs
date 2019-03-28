using ParserMilsetones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserMilestones.SerializationModels
{
    [Serializable]
    public class SerContainer
    {
        public List<SerMilestone> Milestones { get; set; }
        public List<SerRoute> Routes { get; set; }
        public List<RawError> Errors { get; set; }

        public SerContainer()
        {
            Milestones = new List<SerMilestone>();
            Routes = new List<SerRoute>();
            Errors = new List<RawError>();
        }
    }
}
