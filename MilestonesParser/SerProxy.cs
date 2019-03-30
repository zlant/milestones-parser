using Newtonsoft.Json;
using ParserMilestones.SerializationModels;
using ParserMilsetones;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ParserMilestones
{
    public class SerProxy
    {
        public List<Milestone> milestones = new List<Milestone>();
        public List<RouteRel> routes = new List<RouteRel>();

        public SerContainer container = new SerContainer();

        public SerProxy(Parser parser)
        {
            milestones = parser.milestones;
            routes = parser.routes;
            container.Errors = parser.errors;
        }

        public void OptimizationBeforeConvert()
        {
            // привязка по ref столбов

            var refMiles = milestones.Where(x => !string.IsNullOrEmpty(x.Ref));

            foreach (var mile in refMiles)
            {
                if (mile.RelRoutes.Any(x => x.Ref == mile.Ref))
                {
                    mile.RelRoutes.RemoveAll(x => x.Ref != mile.Ref);
                    mile.WayRoute.Dublicate = true;
                }
                else if (mile.WayRoute.Ref == mile.Ref)
                {
                    mile.RelRoutes.RemoveAll(x => true);
                }
                else if (routes.Any(x => x.Ref == mile.Ref))
                {
                    mile.RelRoutes.RemoveAll(x => x.Ref != mile.Ref);
                    mile.RelRoutes.AddRange(routes.Where(x => x.Ref == mile.Ref));
                    mile.WayRoute.Dublicate = true;
                }
                else if (milestones.Any(x => x.WayRoute.Ref == mile.Ref))
                {
                    mile.RelRoutes.RemoveAll(x => x.Ref != mile.Ref);
                    mile.WayRoute = milestones.First(x => x.WayRoute.Ref == mile.Ref).WayRoute;
                }
            }

            // удаление столбов с пустыми линиями и отношениями

            milestones.RemoveAll(x => x.WayRoute.Empty() && x.RelRoutes.Count > 0 && x.RelRoutes.All(y => y.Empty()));
            milestones.RemoveAll(x => x.WayRoute.Empty() && x.RelRoutes.Count == 0);

            // удаление пустых отношений

            milestones.ForEach(x => x.RelRoutes.RemoveAll(z => z.Empty()));
            routes.RemoveAll(x => x.Empty());

            // удаление дубликатов линий

            var wayGroupsByRef = milestones
                .Where(x => !x.WayRoute.Dublicate)
                .Where(x => !x.WayRoute.EmptyRef())
                .GroupBy(x => x.WayRoute.Ref)
                .Where(x => x.Count() > 1);

            DeleteDublicatesWay(wayGroupsByRef);

            var wayGroupsByName = milestones
                .Where(x => !x.WayRoute.Dublicate)
                .Where(x => !x.WayRoute.EmptyName())
                .GroupBy(x => x.WayRoute.Name)
                .Where(x => x.Count() > 1);

            DeleteDublicatesWay(wayGroupsByName);
        }

        private static void DeleteDublicatesWay(IEnumerable<IGrouping<string, Milestone>> wayGroups)
        {
            foreach (var groupRef in wayGroups)
            {
                foreach (var mile in groupRef)
                {
                    mile.WayRoute = groupRef.ElementAt(0).WayRoute;
                }
            }
        }

        public void OptimizationAfterConvert()
        {

            var groubByRef = container.Routes
                .Where(x => !string.IsNullOrEmpty(x.Ref))
                .GroupBy(x => x.Ref)
                .Where(x => x.Count() > 1);

            foreach (var routes in groubByRef)
            {
                var sortRoutes = routes
                    .OrderByDescending(x => x.Relation)
                    .ThenByDescending(x => x.Milestones.Count);

                var mainRoute = sortRoutes.First();

                foreach (var route in routes)
                {
                    if (route == mainRoute)
                        continue;

                    if (route.Relation)
                        continue;

                    foreach (var mileId in route.Milestones)
                        if (!mainRoute.Milestones.Contains(mileId))
                        {
                            mainRoute.Milestones.Add(mileId);
                            var milestone = container.Milestones.Find(x => x.OsmId == mileId);
                            milestone.Routes
                                .RemoveAll(x => x.Relation == route.Relation && x.OsmId == route.OsmId);
                            milestone.Routes
                                .Add(new SerRouteLink { Relation = mainRoute.Relation, OsmId = mainRoute.OsmId });
                        }
                        else
                        {
                            var milestone = container.Milestones.Find(x => x.OsmId == mileId);
                            milestone.Routes
                                .RemoveAll(x => x.Relation == route.Relation && x.OsmId == route.OsmId);
                        }

                    container.Routes.Remove(route);
                }
            }

        }

        public bool ValidateResult()
        {
            bool result = true;

            foreach (var mile in container.Milestones)
            {
                foreach (var route in mile.Routes)
                {
                    if (!container.Routes.Any(x => x.Relation == route.Relation && x.OsmId == route.OsmId))
                        result = false;
                }
            }

            foreach (var route in container.Routes)
            {
                foreach (var mile in route.Milestones)
                {
                    if (!container.Milestones.Any(x => x.OsmId == mile))
                        result = false;
                }
            }

            return result;
        }

        public void Convert()
        {
            Console.WriteLine();
            Console.WriteLine("Clear");

            OptimizationBeforeConvert();

            Console.WriteLine("Convert");

            container.Milestones = milestones.Select(x => new SerMilestone(x)).ToList();
            container.Routes = routes.Select(x => new SerRoute(x)).ToList();

            //получение списка id столбов относящиеся к маршруту
            foreach (var route in container.Routes)
            {
                var routeMils = new List<long>();

                if (route.Relation)
                    routeMils.AddRange(milestones
                      .Where(x => x.RelRoutes.Any(z => z.Id == route.OsmId))
                      .Select(x => x.Id));

                route.Milestones = routeMils;
            }

            var wayRoutes = milestones
                .Select(x => x.WayRoute)
                .Where(x => !x.Dublicate)
                .Distinct();

            foreach (var wroute in wayRoutes)
            {
                if (container.Routes.Any(x => !x.Relation && x.OsmId == wroute.Id))
                    continue;

                var miles = milestones
                    .Where(x => x.WayRoute == wroute)
                    .Select(x => x.Id);

                var serRoute = new SerRoute(wroute);
                serRoute.Milestones = miles.ToList();
                container.Routes.Add(serRoute);

                foreach (var mileId in miles)
                {
                    container.Milestones.First(x => x.OsmId == mileId).Routes
                        .Add(new SerRouteLink
                        {
                            Relation = false,
                            OsmId = serRoute.OsmId
                        });
                }
            }

            ValidateResult();
            OptimizationAfterConvert();


            var refs = container.Routes.Select(x => x.Ref).ToList();
            refs.Sort();
            return;
        }

        public void Serialization()
        {
            Console.WriteLine("Serialization");

            File.WriteAllText("milestones.json", JsonConvert.SerializeObject(this.container));
            /*XmlSerializer formatter = new XmlSerializer( typeof( SerContainer ) );
            using ( FileStream fs = new FileStream( @"D:\serNewMils.xml", FileMode.Create ) )
            {
                formatter.Serialize( fs, container );
            }*/
        }
    }
}
