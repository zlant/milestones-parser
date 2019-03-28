using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserMilestones.SerializationModels
{
    [Serializable]
    public class SerRouteLink
    {
        public bool Relation { get; set; }
        public long OsmId { get; set; }

        public SerRouteLink()
        { }
    }

    [Serializable]
    public class SerMilestone
    {
        public long Id { get; set; }
        public long OsmId { get; set; }
        public int? Distance { get; set; }
        public int? DistanceBack { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }

        public List<SerRouteLink> Routes { get; set; }

        public SerMilestone()
        {
            Routes = new List<SerRouteLink>();
        }

        public SerMilestone( ParserMilsetones.Milestone mile )
        {
            OsmId = mile.Id;
            Distance = mile.Distance;
            DistanceBack = mile.DistanceBack;
            Lat = mile.Lat;
            Lon = mile.Lon;

            Routes = mile.RelRoutes
                .Select( x => new SerRouteLink
                {
                    Relation = true,
                    OsmId = x.Id
                } )
                .ToList();

            if ( !mile.WayRoute.Dublicate )
                Routes.Add( new SerRouteLink
                {
                    Relation = false,
                    OsmId = mile.WayRoute.Id
                } );
        }
    }
}
