using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ES.CCIS.Host.Models.HoaDon.HoaDonNuoc
{
    public class WaterBillAsyncDTO
    {
        public WaterBillAsyncDTO()
        {
            lstFigurebook = new List<WaterMonthBookModel>();
        }

        public List<WaterMonthBookModel> lstFigurebook { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int figurebookId { get; set; }
        public string ResMonthString { get; set; }
        public string ResTitle { get; set; }
    }
    public class WaterMonthBookModel
    {
        public int Id { get; set; }
        public string BookCode { get; set; }
        public string BookName { get; set; }
        public int? Number { get; set; }
        public int Status { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Term { get; set; }
        public decimal? Total { get; set; }
        public DateTime CreatedDate { get; set; }
        public int FigureBookId { get; set; }
    }
}