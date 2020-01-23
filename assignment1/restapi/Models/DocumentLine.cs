using System;

namespace restapi.Models
{
    public class DocumentLine
    {
        public DocumentLine() { }

        public int Week { get; set; }

        public int Year { get; set; }

        public DayOfWeek Day { get; set; }

        public float Hours { get; set; }

        public string Project { get; set; }

        //[MN] ADDED Employee attribute
        public int Employee { get; set; }
    }

    //[MN] ADDED DocumentLinePatch
    public class DocumentLinePatch
    {

        public int? Week { get; set; }

        public int? Year { get; set; }

        public DayOfWeek? Day { get; set; }

        public float? Hours { get; set; }

        public string Project { get; set; }

        //ADDED Employee attribute
        public int Employee { get; set; }
    }

}