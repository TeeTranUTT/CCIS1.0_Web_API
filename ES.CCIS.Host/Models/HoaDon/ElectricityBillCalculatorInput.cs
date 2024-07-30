using CCIS_BusinessLogic.CustomBusiness.BillEdit_TaxInvoice;
using CCIS_DataAccess;
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

    public class CreateReTaxInvoiceInput
    {
        public ReBillAdjustment ReBillAdjustment { get; set; }
        public List<BillTaxAdjustDetail> ListBillTaxAdjust { get; set; }
    }

    public class pElectricityBillCalculatorInput
    {
        public List<Bill_Index_CalendarOfSaveIndexModel> ListBill { get; set; }
        public string FilterDateSearch { get; set; }
        public string TermSearch { get; set; }
        public string FigureBookIdSearch { get; set; }
        public string SaveDateSearch { get; set; }
    }

    public class TaxInvoiceCalculatorInput
    {
        public int FigureBookId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }
}