using CCIS_DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ES.CCIS.Host.Models
{
    public class SearchInfoModel
    {
        public SearchInfoModel()
        {
            Search_CustomerInfoModel = new List<Search_CustomerInfoModel>();
            Search_EquipmentModel = new List<Search_EquipmentModel>();
            Search_ImposedPriceModel = new List<Search_ImposedPriceModel>();
            Search_PointDetailModel = new List<Search_PointDetailModel>();
            Search_BillDetailModel = new List<Search_BillDetailModel>();
            Search_BillAdjustDetailModel = new List<Search_BillAdjustDetailModel>();
            Search_TrackDebtModel = new List<Search_TrackDebtModel>();
        }

        public List<Search_CustomerInfoModel> Search_CustomerInfoModel { get; set; }
        public List<Search_EquipmentModel> Search_EquipmentModel { get; set; }
        public List<Search_ImposedPriceModel> Search_ImposedPriceModel { get; set; }
        public List<Search_PointDetailModel> Search_PointDetailModel { get; set; }
        public List<Search_BillDetailModel> Search_BillDetailModel { get; set; }
        public List<Search_BillAdjustDetailModel> Search_BillAdjustDetailModel { get; set; }
        public List<Search_TrackDebtModel> Search_TrackDebtModel { get; set; }
    }

    public class Search_CustomerInfoModel : Concus_Customer
    {
        public string ContractCode { get; set; }
        public string SaveDate { get; set; }
        public string FormOfPayment { get; set; }
        public int ContractId { get; set; }
    }

    public class Search_EquipmentModel : Index_Value
    {
        public string PointCode { get; set; }
        public string ElectricityMeterCodeDDN { get; set; }
        public string ElectricityMeterCodeDUP { get; set; }
        public decimal ValueDDN { get; set; }
        public decimal ValueDUP { get; set; }
        public decimal CoefficientDDN { get; set; }
        public decimal CoefficientDUP { get; set; }

    }

    public class Search_ImposedPriceModel : Concus_ImposedPrice
    {
        public string Description { get; set; }
        public string PointCode { get; set; }
        public decimal Price { get; set; }
        public string PotentialName { get; set; }
    }

    public class Search_PointDetailModel : Concus_ServicePoint
    {
        public string PointType { get; set; }
    }

    public class Search_BillDetailModel : Bill_ElectricityBillDetail
    {
        public string CustomerName { get; set; }
        public decimal VAT { get; set; }
        public decimal AllTotal { get; set; }
    }

    public class Search_BillAdjustDetailModel : Bill_ElectricityBillAdjustmentDetail
    {
        public string CustomerName { get; set; }
        public decimal VAT { get; set; }
        public decimal AllTotal { get; set; }
        public string AdjustmentType { get; set; }
    }

    public class Search_TrackDebtModel : Bill_ElectricityBillDetail
    {
        public string CustomerName { get; set; }
        public decimal VAT { get; set; }
        public decimal AllTotal { get; set; }
    }
}