using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ES.CCIS.Host.Models.HoaDon
{
    public class ElectricityBillCalculatorInput
    {
        public string FilterDate { get; set; }
        public int Term { get; set; }
        public int FigureBookId { get; set; }
        public string SaveDate { get; set; }
    }

    public class CancelBillCalculatorInput
    {
        public int FigureBookId { get; set; }
        public int Term { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }
}