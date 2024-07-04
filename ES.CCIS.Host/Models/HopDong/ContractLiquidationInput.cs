using CCIS_DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ES.CCIS.Host.Models.HopDong
{
    public class ContractLiquidationInput
    {
        public Concus_ContractModel Contract { get; set; }
        public string Liquidation { get; set; }
        public int ReasonId { get; set; }
    }
    public class ContractExtensionInput
    {
        public Concus_ContractModel Contract { get; set; }
        public string Extend { get; set; }
    }
    public class EditConcus_CustomerInput
    {
        public Customer_ContractModel ConCusConTract { get; set; }
        public string Gender { get; set; }
        public string OccupationsGroupName { get; set; }
    }

    public class ContractManagerViewerModel : Concus_ContractModel
    {
        public int FigureBookId { get; set; }
        public int NumberOfPhases { get; set; }
        public int Day { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string Address { get; set; }
        public string InvoiceAddress { get; set; }
        public string Fax { get; set; }
        public string Gender { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string TaxCode { get; set; }
        public string BankAccount { get; set; }
        public string BankName { get; set; }
        public string PhoneCustomerCare { get; set; }
        public decimal HouseholdNumber { get; set; }
        public string PersonalId { get; set; }
        public string PDay { get; set; }
        public string PMonth { get; set; }
        public string PYear { get; set; }
        public string PArea { get; set; }

    }

    public class AddConcus_ServicePointJSModel
    {
        public List<Concus_ServicePointModelDTO> HandOnTableObject { get; set; }
        public List<string> HandOnTableHeader { get; set; }
        public bool ManualColumnResize { get; set; }
    }

    public class Concus_ServicePointModelDTO
    {
        public string PointCode { get; set; }
        public string Address { get; set; }
        public int ServicePointType { get; set; }
        public string PotentialCode { get; set; }
        public decimal Power { get; set; }
        public decimal HouseholdNumber { get; set; }
        public int NumberOfPhases { get; set; }
        public int StationId { get; set; }
        public int FigureBookId { get; set; }
        public int Index { get; set; }
        public string PillarNumber { get; set; }
        public int? RegionId { get; set; }
        public string StationCode { get; set; }
        public string BookCode { get; set; }
    }

}