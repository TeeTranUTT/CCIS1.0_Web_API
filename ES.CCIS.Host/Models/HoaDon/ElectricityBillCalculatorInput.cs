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

    public class GetAllValueInput
    {
        public List<ListBill_ElectricityBillAdjustmentModel> MyArrayCustomer { get; set; }
        public List<List_ElectricityBillAdjustmentDetailModel> MyArrayBillDetail { get; set; }
        public string AdjustmentType { get; set; }
        public string ReportNumber { get; set; }
        public DateTime CreateDate { get; set; }
        public int ReasonId { get; set; }
        public int TermAdjustment { get; set; }
        public List<Bill_ElectricityBillAdjustmentServicesModel> BillAdjustmentServicesModel { get; set; }
    }

    public class GetAllValue_HBInput
    {
        public string PhoneNumber { get; set; }
        public int BillAdjustmentId { get; set; }
        public string AdjustmentType { get; set; }
        public string ReportNumber { get; set; }
        public DateTime CreateDate { get; set; }
        public int ReasonId { get; set; }
        public int TermAdjustment { get; set; }
    }

    public class ElectricityBill_HBInput
    {
        public string ReportNumber { get; set; }
        public decimal BillAdjustmentId { get; set; }
        public int TermAdjustment { get; set; }
        public DateTime CreateDate { get; set; }
        public int ReasonId { get; set; }
    }

    public class Category_ServiceModelDTO
    {
        public int ServiceId { get; set; }
        public int DepartmentId { get; set; }
        public bool IsDelete { get; set; }
        public string ServiceName { get; set; }
        public string Unit { get; set; }
        public int? Quantity { get; set; }
        public int? Price { get; set; }
        public int? Total { get; set; }
    }
}