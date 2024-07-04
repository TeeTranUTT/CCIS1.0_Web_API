using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ES.CCIS.Host.Models
{
    public class ChangePriceModel
    {
        public string OccupationsGroupCode { get; set; }
        public string PotentialCode { get; set; }
        public decimal Price { get; set; }
        public string PriceGroupCode { get; set; }
        public string Time { get; set; }
        public string ActiveDate { get; set; }
        public string OccupationsGroupName { get; set; }
    }

    public class GenerateCustomerCodeInput
    {
        public int TeamId { get; set; }
        public string CodeCustom { get; set; }
        public int RegionId { get; set; }
        public string ServiceType { get; set; }
    }    
}