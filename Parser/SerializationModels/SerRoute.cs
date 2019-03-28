using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserMilestones.SerializationModels
{
    [Serializable]
    public class SerRoute
    {
        public long Id { get; set; }
        public bool Dublicate { get; set; }
        public bool Relation { get; set; }
        public long OsmId { get; set; }
        public string Ref { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<long> Milestones { get; set; }

        public SerRoute()
        {
            Milestones = new List<long>();
        }

        public SerRoute(ParserMilsetones.RouteRel route)
        {
            OsmId = route.Id;
            Relation = true;
            Dublicate = false;

            Ref = route.Ref;
            Name = route.Name;
            Description = route.Description;
        }

        public SerRoute( ParserMilsetones.WayRoute route )
        {
            OsmId = route.Id;
            Relation = false;
            Dublicate = route.Dublicate;

            Ref = route.Ref;
            Name = route.Name;
            Description = route.Description;
        }

    }
}
