using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserMilsetones
{
    [Serializable]
    public class RouteRel : IComparable
    {
        public long Id { get; set; }
        public string Ref { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public RouteRel()
        { }


        public int CompareTo(RouteRel route)
        {
            return Id.CompareTo(route.Id);
        }

        public int CompareTo(object obj)
        {
            return CompareTo((RouteRel)obj);
        }


        public override int GetHashCode()
        {
            return (int)this.Id;
        }

        public override bool Equals(object obj)
        {
            if (Object.ReferenceEquals(this, obj))
                return true;

            if (Object.ReferenceEquals(obj, null))
                return false;

            if (obj.GetType() != this.GetType())
                return false;

            return EqualsHelper(this, obj as RouteRel);
        }

        protected static bool EqualsHelper(RouteRel r1, RouteRel r2)
        {
            return r1.Id == r2.Id || r1.Ref == r2.Ref;
        }

        public override string ToString()
        {
            if (Empty())
                return "";

            string result;

            result = String.Format(" [ {0}", Ref);
            if (!String.IsNullOrEmpty(Name))
                result += String.Format(" | {0}", Name);
            if (!String.IsNullOrEmpty(Description) && Name != Description)
                result += String.Format(" | {0}", Description);
            result += String.Format(" ]");

            return result;
        }

        public string ToShortString()
        {
            if (!String.IsNullOrEmpty(Ref))
                return Ref;
            if (!String.IsNullOrEmpty(Name))
                return Name;
            if (!String.IsNullOrEmpty(Description))
                return Description;
            return Id.ToString();
        }

        public bool Empty()
        {
            return EmptyRef() && EmptyName() && EmptyDesc();
        }

        public bool EmptyRefName()
        {
            return EmptyRef() && EmptyName();
        }

        public bool EmptyRef()
        {
            return String.IsNullOrEmpty(Ref);
        }

        public bool EmptyName()
        {
            return String.IsNullOrEmpty(Name);
        }

        public bool EmptyDesc()
        {
            return String.IsNullOrEmpty(Description);
        }
    }

    public enum Highway
    {
        proposed = 1,
        construction,
        residential,
        unclassified,
        tertiary,
        secondary,
        primary,
        trunk,
        motorway,
    }

    [Serializable]
    public class WayRoute : RouteRel
    {
        public bool Dublicate { get; set; }
        public Highway Highway { get; set; }

        public WayRoute()
        { }
    }
}
