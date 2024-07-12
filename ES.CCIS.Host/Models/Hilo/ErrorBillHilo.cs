using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ES.CCIS.Host.Models.Hilo
{
    public class ErrorBillHilo
    {
        public decimal BillId { get; set; }
        public string CustomerName { get; set; }
        public string FigureBook { get; set; }
        public int Ky { get; set; }
        public int Thang { get; set; }
        public int Nam { get; set; }
        public string ErrorMessage { get; set; }
        public int DepartmentId { get; set; }
    }

    public class PushHiloBillInput
    {
        public string[] BillId { get; set; }
        public int DepartmentId { get; set; }
    }
}