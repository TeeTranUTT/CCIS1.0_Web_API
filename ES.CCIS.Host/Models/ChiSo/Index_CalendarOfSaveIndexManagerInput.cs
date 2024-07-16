using CCIS_DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ES.CCIS.Host.Models.ChiSo
{
    public class Index_CalendarOfSaveIndexManagerInput
    {

    }

    public class Update_RouteModel
    {
        public List<Index_ValueModel> ListIndexValue { get; set; }
        public List<Index_Value> ListIndex { get; set; }
        public Concus_ServicePointModel ServicePoint { get; set; }
    }

    public class Update_RouteInput
    {
        public Concus_ServicePointModel ServicePointModel { get; set; }
        public int Term { get; set; }
        public DateTime SaveDate { get; set; }
        public DateTime DateChange { get; set; }
        public int NewRouteId { get; set; }
        public List<Loss_IndexModel> ListIndex { get; set; }
    }

    public class AddIndex_ValueJSModel
    {
        public List<Concus_CustomerModel_Index_ValueModelDTO> HandOnTableObject { get; set; }
        public List<string> HandOnTableHeader { get; set; }
        public bool ManualColumnResize { get; set; }
    }

    public class Concus_CustomerModel_Index_ValueModelDTO
    {
        public int IndexOfList { get; set; }
        public int Index { get; set; }
        public string PointCode { get; set; }

        public string Name { get; set; }
        public string Address { get; set; }
        //TruongVM>>
        public string ElectricityMeterNumber { get; set; }
        public string SPAddress { get; set; }
        public string TimeOfUse { get; set; }
        public decimal OldValue { get; set; }
        public decimal NewValue { get; set; }
        public decimal AdjustPower { get; set; }
        public decimal Coefficient { get; set; }
        public decimal Consume { get; set; }//sản lượng điện tiêu thụ. HieuLV said!
                                            //<<TruongVM
        public string IndexType { get; set; }
        public int Term { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int FigureBookId { get; set; }
        public int PointId { get; set; }
        public string CustomerCode { get; set; }
        public string Date { get; set; }
        public int CustomerId { get; set; }

    }

    public class IndexServicePointContractModel
    {
        public Concus_ServicePoint Concus_ServicePoint { get; set; }
        public int CustomerId { get; set; }
    }
}