using OsmSharp;
using OsmSharp.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ParserMilsetones
{
    public class Parser
    {
        public List<Milestone> milestones = new List<Milestone>();
        public List<RouteRel> routes = new List<RouteRel>();
        public List<RawError> errors = new List<RawError>();

        List<long> nids;
        List<long> mems;

        bool firstWay = false;
        bool firstRel = false;
        int i = 0;

        public void ReadPbfFile(string path)
        {
            FileInfo pbfile = new FileInfo(path);

            using (FileStream fileStream = pbfile.OpenRead())
            using (PBFOsmStreamSource reader = new PBFOsmStreamSource(fileStream))
            {
                foreach (var geo in reader)
                {
                    if (geo.Id.HasValue)
                    {
                        if (i++ == 1000000)
                        {
                            Console.Write(100 * fileStream.Position / fileStream.Length + " % ");
                            Console.CursorLeft = 0;
                            i = 0;
                        }

                        ParseOsmGeo(geo);
                    }
                }
            }


            Validate();
        }

        private void Validate()
        {
            errors.AddRange(routes
                .Where(x => !x.EmptyName() && x.Name == x.Description)
                .Select(x => new RawError(ErrorType.SameNameAndDescription, x, x.Name)));

            errors.AddRange(routes
                .Where(x => x.EmptyRef() && (!x.EmptyName() || !x.EmptyDesc()))
                .Select(x => new RawError(ErrorType.EmptyRef, x, x.EmptyName() ? x.Description : x.Name)));

            errors.AddRange(routes
                .Where(x => !x.EmptyRef() && x.EmptyName() && x.EmptyDesc())
                .Select(x => new RawError(ErrorType.EmptyNameAndDescription, x, x.Ref)));

            errors.AddRange(routes
                .Where(x => x.Empty())
                .Select(x => new RawError(ErrorType.EmptyRefAndNameAndDescription, x)));

            errors.AddRange(routes
                .Where(x => !x.EmptyRef())
                .GroupBy(x => x.Ref)
                .Where(x => x.Count() > 1)
                .SelectMany(x => x)
                .Select(x => new RawError(ErrorType.SameRef, x, x.Ref)));

            errors.AddRange(milestones
                .Where(x => x.WayRoute.Empty() && x.RelRoutes.Count == 0)
                .Select(x => new RawError(ErrorType.OnUnnamedRoad, x)));

            errors.AddRange(milestones
                .Where(x => x.WayRoute.Empty() && x.RelRoutes.Count > 0 && x.RelRoutes.All(y => y.Empty()))
                .Select(x => new RawError(ErrorType.OnUnnamedRoadAndUnnamedRoutes, x)));

            foreach (var r in routes)
            {
                var forw = milestones
                    .Where(x => x.RelRoutes.Contains(r) && x.Distance != null)
                    .GroupBy(x => x.Distance)
                    .Where(x => x.Count() > 1)
                    .SelectMany(x => x);

                var back = milestones
                    .Where(x => x.RelRoutes.Contains(r) && x.DistanceBack != null)
                    .GroupBy(x => x.DistanceBack)
                    .Where(x => x.Count() > 1)
                    .SelectMany(x => x);

                errors.AddRange(forw.Union(back)
                    .Select(x => new RawError(ErrorType.SameDistanceInRoute, x, r.ToShortString())));

                var temperror = new List<RawError>();

                var miles = milestones
                    .Where(x => x.RelRoutes.Contains(r) && x.Distance != null)
                    .GroupBy(x => x.Distance)
                    .Where(x => x.Count() == 1)
                    .SelectMany(x => x)
                    .OrderBy(x => x.Distance);

                var prev = miles.FirstOrDefault();
                foreach (var m in miles)
                {
                    if (m.Distance - prev.Distance != 1)
                        continue;

                    if (Distance(m, prev) > 1400.0)
                    {
                        temperror.Add(new RawError(ErrorType.DistantNeighbor, prev));
                        temperror.Add(new RawError(ErrorType.DistantNeighbor, m));
                    }
                    prev = m;
                }

                miles = milestones
                    .Where(x => x.RelRoutes.Contains(r) && x.DistanceBack != null)
                    .GroupBy(x => x.DistanceBack)
                    .Where(x => x.Count() == 1)
                    .SelectMany(x => x)
                    .OrderBy(x => x.DistanceBack);

                prev = miles.FirstOrDefault();
                foreach (var m in miles)
                {
                    if (m.DistanceBack - prev.DistanceBack != 1)
                        continue;

                    if (Distance(m, prev) > 1400.0)
                    {
                        temperror.Add(new RawError(ErrorType.DistantNeighbor, prev));
                        temperror.Add(new RawError(ErrorType.DistantNeighbor, m));
                    }
                    prev = m;
                }

                errors.AddRange(temperror
                    .GroupBy(x => x.Id)
                    .Select(x => x.First()));
            }
        }

        double Distance(Milestone m1, Milestone m2)
        {
            return Distance(m1.Lat, m1.Lon, m2.Lat, m2.Lon);
        }

        double Distance(double lat1, double lon1, double lat2, double lon2)
        {
            return Rad2Km(DistanceRad(
                Math.PI * lat1 / 180,
                Math.PI * lon1 / 180,
                Math.PI * lat2 / 180,
                Math.PI * lon2 / 180));
        }

        double DistanceRad(double lat1, double lon1, double lat2, double lon2)
        {
            return 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin((lat1 - lat2) / 2), 2) +
                Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin((lon1 - lon2) / 2), 2))
                );
        }

        double Rad2Km(double rad)
        {
            return rad * 6378.137 * 1000;
        }

        private void ParseOsmGeo(OsmGeo element)
        {
            if (element.Type == OsmGeoType.Node)
                ParseNode(element as Node);

            if (element.Type == OsmGeoType.Way)
                ParseWay(element as Way);

            if (element.Type == OsmGeoType.Relation)
                ParseRel(element as Relation);
        }

        private void ParseNode(Node element)
        {
            if (!element.Tags.Contains("highway", "milestone"))
                return;

            string tagDistance;
            if (!element.Tags.TryGetValue("distance", out tagDistance))
                element.Tags.TryGetValue("pk", out tagDistance);

            string tagDistanceBack;
            if (!element.Tags.TryGetValue("distance:backward", out tagDistanceBack))
                element.Tags.TryGetValue("pk:backward", out tagDistanceBack);

            if (string.IsNullOrEmpty(tagDistance) && string.IsNullOrEmpty(tagDistanceBack))
            {
                errors.Add(new RawError
                {
                    Type = ErrorType.NotDistanceTag,
                    OsmType = OsmGeoType.Node,
                    Id = (long)element.Id,
                    Desc = element.Id.Value.ToString()
                });
                return;
            }

            string tagRef;
            element.Tags.TryGetValue("ref", out tagRef);

            int? distance = ParseDistance(element, tagDistance);
            int? distanceBack = ParseDistance(element, tagDistanceBack);

            Milestone ms = new Milestone()
            {
                Distance = distance,
                DistanceBack = distanceBack,
                Id = (long)element.Id,
                Ref = tagRef,
                Lat = (double)element.Latitude,
                Lon = (double)element.Longitude
            };

            milestones.Add(ms);
        }

        private int? ParseDistance(Node element, string tagDistance)
        {
            if (string.IsNullOrEmpty(tagDistance))
                return null;

            string trimDistance = ClearDistance(tagDistance);
            int distance;

            if (!int.TryParse(trimDistance, out distance))
            {
                errors.Add(new RawError
                {
                    Type = ErrorType.DistanceUnparse,
                    OsmType = OsmGeoType.Node,
                    Id = (long)element.Id,
                    Desc = tagDistance
                });
                return null;
            }

            return distance;
        }

        private string ClearDistance(string value)
        {
            if (Regex.IsMatch(value, @"^\d+$"))
                return value;
            if (Regex.IsMatch(value, @"^\d+\.\d$"))
                return Regex.Match(value, @"\d+").Value;
            if (Regex.IsMatch(value, @"^\d+.+$"))
                return Regex.Match(value, @"\d+").Value;

            return value;
            /*
            string dist = "";
            foreach ( var symb in value )
                if ( Char.IsDigit( symb ) )
                    dist += symb;
                else
                    break;
            return dist;*/
        }

        private void ParseWay(Way element)
        {
            if (!firstWay)
            {
                nids = milestones.Select(x => x.Id).ToList();
                firstWay = true;
            }

            string taghighway;
            if (!element.Tags.TryGetValue("highway", out taghighway))
                return;

            Highway tagHighwayEnum;
            if (!Highway.TryParse(taghighway, out tagHighwayEnum))
            {
                if (!taghighway.Contains("_"))
                    return;

                var split = taghighway.Split('_');
                if (!(split.Length == 2
                    && split[1] == "link"
                    && Highway.TryParse(split[0], out tagHighwayEnum)))
                    return;
            }


            var intersect = Intersect(nids, element.Nodes.ToList());

            if (intersect.Count == 0)
                return;

            WayRoute route;

            string tagref;
            element.Tags.TryGetValue("ref", out tagref);

            string tagname;
            element.Tags.TryGetValue("name", out tagname);

            string tagdescription;
            element.Tags.TryGetValue("description", out tagdescription);

            if (tagname != "" && tagname == tagdescription)
            {
                errors.Add(new RawError
                {
                    Type = ErrorType.SameNameAndDescription,
                    OsmType = OsmGeoType.Way,
                    Id = (long)element.Id,
                    Desc = tagname
                });
            }

            route = new WayRoute
            {
                Id = (long)element.Id,
                Ref = tagref,
                Name = tagname,
                Description = tagdescription,
                Highway = tagHighwayEnum
            };

            // привязование линии к точке
            foreach (var id in intersect)
            {
                var index = milestones.BinarySearch(new Milestone { Id = id });
                var milestone = milestones[index];
                if (milestone.WayRoute == null)
                    milestone.WayRoute = route;
                else if (milestone.WayRoute.Highway < route.Highway)
                    milestone.WayRoute = route;
            }
        }

        private void ParseRel(Relation element)
        {
            if (!firstRel)
            {
                errors.AddRange(milestones
                    .Where(x => x.WayRoute == null)
                    .Select(x => new RawError(ErrorType.NotOnRoad, x)));

                milestones.RemoveAll(x => x.WayRoute == null);
                mems = milestones.Select(x => x.WayRoute.Id).Distinct().ToList();
                mems.Sort();
                firstRel = true;
                Console.WriteLine("Rel");
            }

            if (!(element.Tags.Contains("type", "route") &&
                element.Tags.Contains("route", "road")))
                return;

            if (element.Tags.Contains("network", "e-road") ||
                element.Tags.Contains("network", "AsianHighway"))
                return;

            var wayMems = element.Members
                .Where(x => x.Type == OsmGeoType.Way)
                .Select(x => (long)x.Id)
                .ToList();
            //.Intersect( mems );

            var intersect = Intersect(mems, wayMems);

            if (intersect.Count() == 0)
                return;

            string tagref = "";
            element.Tags.TryGetValue("ref", out tagref);
            string tagname;
            element.Tags.TryGetValue("name", out tagname);
            string tagdescription;
            element.Tags.TryGetValue("description", out tagdescription);

            RouteRel route = new RouteRel
            {
                Id = (long)element.Id,
                Ref = tagref,
                Name = tagname,
                Description = tagdescription
            };

            routes.Add(route);

            // привязывание отношения к точке
            foreach (var id in intersect)
            {
                var mils = milestones.FindAll(x => x.WayRoute.Id == id);
                foreach (var mil in mils)
                {
                    mil.RelRoutes.Add(route);
                    if (!string.IsNullOrEmpty(mil.WayRoute.Ref))
                    {
                        if (mil.WayRoute.Ref == route.Ref)
                            mil.WayRoute.Dublicate = true;
                        else if (mil.WayRoute.Ref.Contains(";"))
                        {
                            foreach (var wayref in mil.WayRoute.Ref.Split(';'))
                                if (wayref.Trim() == route.Ref)
                                    mil.WayRoute.Dublicate = true;
                        }
                    }
                }
            }
        }

        public void Serialization()
        {
            var arr = milestones.ToArray();

            XmlSerializer formatter = new XmlSerializer(typeof(Milestone[]));
            using (FileStream fs = new FileStream(@"D:\smilestones.xml", FileMode.Create))
            {
                formatter.Serialize(fs, arr);
            }
        }

        List<long> Intersect(List<long> longList, List<long> shortList)
        {
            List<long> result = new List<long>();
            foreach (var item in shortList)
            {
                var index = longList.BinarySearch(item);
                if (index >= 0)
                    result.Add(longList[index]);
            }
            return result;
        }
    }
}
