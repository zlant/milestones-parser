using OsmSharp.Osm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserMilsetones
{
    [Serializable]
    public enum ErrorType
    {
        NotDistanceTag,
        DistanceUnparse,
        NotOnRoad,
        OnUnnamedRoad,
        OnUnnamedRoadAndUnnamedRoutes,
        SameRef,
        SameNameAndDescription,
        EmptyRef,
        EmptyNameAndDescription,
        EmptyRefAndNameAndDescription,
        SameDistanceInRoute,
        DistantNeighbor
    }

    [Serializable]
    public class RawError
    {
        public OsmGeoType OsmType { get; set; }
        public long Id { get; set; }
        public string Desc { get; set; }
        public ErrorType Type { get; set; }

        public RawError() { }

        public RawError(ErrorType type, RouteRel route)
            : this(type, route, "[route]") { }

        public RawError(ErrorType type, RouteRel route, string desc)
        {
            OsmType = OsmGeoType.Relation;
            Id = route.Id;
            Desc = desc;
            Type = type;
        }

        public RawError(ErrorType type, Milestone mile)
            : this(type, mile, "[milestone]") { }

        public RawError(ErrorType type, Milestone mile, string desc)
        {
            OsmType = OsmGeoType.Node;
            Id = mile.Id;
            Desc = desc;
            Type = type;
        }
    }

    class RawErrors
    {
        public List<RawError> BadNodes { get; }
        public List<RawError> BadWays { get; }
        public List<RawError> BadRels { get; }

        public RawErrors()
        {
            BadNodes = new List<RawError>();
            BadWays = new List<RawError>();
            BadRels = new List<RawError>();
        }

        public void AddManyNode(IEnumerable<long> ids, string d)
        {
            foreach (var id in ids)
                AddNode(id, d);
        }

        public void AddNode(long id, string d)
        {
            Add(BadNodes, id, d);
        }

        public void AddWay(long id, string d)
        {
            Add(BadWays, id, d);
        }

        public void AddManyRel(IEnumerable<long> ids, string d)
        {
            foreach (var id in ids)
                AddRel(id, d);
        }

        public void AddManyRel(IEnumerable<RawError> errs)
        {
            foreach (var err in errs)
                AddRel(err.Id, err.Desc);
        }

        public void AddRel(long id, string d)
        {
            Add(BadRels, id, d);
        }

        void Add(List<RawError> list, long id, string d)
        {
            list.Add(new ParserMilsetones.RawError { Id = id, Desc = d });
        }

        public void WriteReportHtml()
        {
            using (StreamWriter wr = new StreamWriter("report.html"))
            {
                wr.WriteLine(CreateJosmAllLink(BadNodes, "n"));
                wr.WriteLine(CreateTable(BadNodes, "node"));

                wr.WriteLine(CreateJosmAllLink(BadWays, "w"));
                wr.WriteLine(CreateTable(BadWays, "way"));

                wr.WriteLine(CreateJosmAllLink(BadRels, "r"));
                wr.WriteLine(CreateTable(BadRels, "relation"));
            }
        }

        private string CreateJosmAllLink(List<RawError> list, string type)
        {
            string josm = @"<a href=""http://127.0.0.1:8111/load_object?objects=";
            foreach (var milestone in list)
                josm += type + milestone.Id + ",";
            josm = josm.TrimEnd(',') + @""">Josm all</a>";
            return josm;
        }

        private string CreateTable(List<RawError> list, string type)
        {
            StringBuilder tbl = new StringBuilder();
            tbl.AppendLine("<table>");

            foreach (var milestone in list)
                tbl.AppendFormat(@"<tr><td><a href=""http://www.openstreetmap.org/{1}/{0}"">{0} | {2}</a></td></tr>",
                    milestone.Id,
                    type,
                    milestone.Desc
                    );

            tbl.AppendLine("</table>");
            return tbl.ToString();
        }
    }
}
