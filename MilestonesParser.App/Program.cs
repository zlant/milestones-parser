using ParserMilestones;
using ParserMilsetones;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MilestonesParser.App
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = args[0];
            Parser parser = new Parser();
            parser.ReadPbfFile(path);
            var ser = new SerProxy(parser);
            ser.Convert();
            ser.Serialization();
            ser.ValidateResult();
        }


        static private ParserMilsetones.Parser Deserialization()
        {
            List<ParserMilsetones.Milestone> milestones;

            //using ( FileStream fs = new FileStream( Server.MapPath( "~/smilestones.xml" ), FileMode.Open ) )
            using (MemoryStream fs = new MemoryStream(Encoding.UTF8.GetBytes(System.IO.File.ReadAllText(@"D:\smilestonest.xml"/*Server.MapPath( "~/smilestones.xml" )*/ ))))
            {
                XmlSerializer formatter = new XmlSerializer(typeof(ParserMilsetones.Milestone[]));
                ParserMilsetones.Milestone[] arr = (ParserMilsetones.Milestone[])formatter.Deserialize(fs);
                milestones = arr.ToList();
            }

            List<RouteRel> routes = new List<RouteRel>();
            foreach (var listroutes in milestones.Select(x => x.RelRoutes))
                routes.AddRange(listroutes);

            var routes2 = routes.Distinct().ToList();
            Parser parser = new Parser
            {
                milestones = milestones,
                routes = routes
            };

            return parser;
        }
    }
}
