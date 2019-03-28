using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserMilsetones
{
    [Serializable]
    public class Milestone : IComparable<Milestone>
    {
        public int? Distance { get; set; }
        public int? DistanceBack { get; set; }
        public long Id { get; set; }

        public string Ref { get; set; }

        public WayRoute WayRoute { get; set; }
        public List<RouteRel> RelRoutes { get; set; }

        public double Lat { get; set; }
        public double Lon { get; set; }

        public bool EmptyRoutes()
        {
            if (!WayRoute.Empty())
                return false;

            foreach (var route in RelRoutes)
                if (!route.Empty())
                    return false;

            return true;
        }

        public Milestone()
        {
            RelRoutes = new List<RouteRel>();
        }

        public int CompareTo(Milestone m)
        {
            return Id.CompareTo(m.Id);
        }

        public int CompareTo(long id)
        {
            return Id.CompareTo(id);
        }
    }
}
