using CCIS_DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ES.CCIS.Host.Models
{
    public class ResponseModel
    {
        public int Status { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public string Code { get; set; } = "00";
    }

    public class Category_AllocateFigureBookModelInput
    {
        public List<Category_AllocateFigureBookModel> ListUser { get; set; }
        public int FigureBookId { get; set; }
    }
}