using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ES.CCIS.Host.Models.HoaDon.HoaDonGTGT
{
    public class TaxInvoiceCalculator_EMBInput
    {
        public int FigureBookId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }

    public class CancelTaxInvoiceCalculatorInput
    {
        public int FigureBookId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string RedirectToActionLink { get; set; }
        public List<int> ServiceTypeIds { get; set; }
    }

    public class ConfirmTaxInvoiceInput
    {
        public int FigureBookId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string RedirectToActionLink { get; set; }
    }

    public class ConfirmCancelTaxInvoiceInput
    {
        public int DepartmentId { get; set; }
        public int FigureBookId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string RedirectToActionLink { get; set; }
    }
}