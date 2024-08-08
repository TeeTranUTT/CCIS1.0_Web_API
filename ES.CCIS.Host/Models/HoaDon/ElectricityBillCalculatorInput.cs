using CCIS_BusinessLogic.CustomBusiness.BillEdit_TaxInvoice;
using CCIS_BusinessLogic.CustomBusiness.Models;
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

    #region EditQLVHBill   
    public class QLVHBillDTO
    {
        public QLVHBillDTO()
        {
            LstData = new List<QLVHBill_Item>();
            SlsSearchFigureBookId = new List<SlsSearchFigureBook>();
        }
        public DateTime? Month { get; set; }
        public int Term { get; set; }
        public int FigureBookId { get; set; }
        public string SearchCustomerCode { get; set; }
        public List<SlsSearchFigureBook> SlsSearchFigureBookId { get; set; }
        public List<QLVHBill_Item> LstData { get; set; }
        public QLVHBill_Item Bill_Item { get; set; }

    }

    public class SlsSearchFigureBook
    {
        public string Text { get; set; }
        public string Value { get; set; }
    }

    public class UpdateBillTaxInVoiceDTO
    {
        public UpdateBillTaxInVoiceDTO()
        {
            LstData = new List<Bill_TaxInvoice_Item>();
            SlsSearchFigureBookId = new List<SlsSearchFigureBook>();
        }
        public DateTime? Month { get; set; }
        public int FigureBookId { get; set; }
        public string SearchCustomerCode { get; set; }
        public List<SlsSearchFigureBook> SlsSearchFigureBookId { get; set; }
        public List<Bill_TaxInvoice_Item> LstData { get; set; }

    }

    public class UpdateElectricityEstimate
    {
        public UpdateElectricityEstimate()
        {
            LstData = new List<Bill_ElectricityEstimateModel>();
            SlsSearchFigureBookId = new List<SlsSearchFigureBook>();
        }
        public int FigureBookId { get; set; }
        public string SearchPointCode { get; set; }
        public List<SlsSearchFigureBook> SlsSearchFigureBookId { get; set; }
        public List<Bill_ElectricityEstimateModel> LstData { get; set; }

    }
    public class Bill_ElectricityEstimateModel
    {
        public decimal ElectricityEstimateId { get; set; }
        public int DepartmentId { get; set; }
        public int PointId { get; set; }
        public decimal? ElectricityIndex { get; set; }
        public DateTime CreateDate { get; set; }
        public int CreateUser { get; set; }
        public string PointCode { get; set; }
        public string CustomerName { get; set; }
    }
    #endregion
}