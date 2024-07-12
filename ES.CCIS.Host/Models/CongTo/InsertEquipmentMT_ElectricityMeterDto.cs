using CCIS_BusinessLogic.CustomBusiness.Models;
using CCIS_DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ES.CCIS.Host.Models.CongTo
{
    public class InsertEquipmentMT_ElectricityMeterDto
    {        
           public List<InsertEquipmentMT_ElectricityMeterModelItem> LstData { get; set; }
    }

    public class EquipmentStatusSearchingDTO
    {
        public string TypeCode { get; set; }
        public string EquipmentCode { get; set; }
        public int NumberOfPhase { get; set; }
        public string StockCode { get; set; }
        public EquipmentMT_ElectricityMeterModel equipmentMT_ElectricityMeter { get; set; }
        public EquipmentMT_TestingModel equipmentMT_Testing { get; set; }


    }





}