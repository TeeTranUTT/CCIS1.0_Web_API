using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ES.CCIS.Host.Models.HopDong
{
    public class ContractExtendedInfoModel
    {
        public int ContractId { get; set; }
        public int PointId { get; set; }
        public VsipContractExtend Data { get; set; }
    }
    public class Cap1
    {
        public Obj5051 Obj5051 { get; set; }
        public Obj5051N Obj5051N { get; set; }
    }

    public class Cap2
    {
        public Obj5051 Obj5051 { get; set; }
        public Obj5051N Obj5051N { get; set; }
    }

    public class Obj5051
    {
        public string DongDien { get; set; }
        public string ThoiGian { get; set; }
        public string DoThiBaoVe { get; set; }
    }

    public class Obj5051N
    {
        public string DongDien { get; set; }
        public string ThoiGian { get; set; }
        public string DoThiBaoVe { get; set; }
    }

    public class VsipContractExtend
    {
        public string PointCode { get; set; }
        public int PointId { get; set; }
        public Cap1 Cap1 { get; set; }
        public Cap2 Cap2 { get; set; }
    }

    public class ContractMoreInfo
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}