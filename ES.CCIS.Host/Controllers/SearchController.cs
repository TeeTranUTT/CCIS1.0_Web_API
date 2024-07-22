using CCIS_DataAccess;
using ES.CCIS.Host.Models;
using ES.CCIS.Host.Models.EnumMethods;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

namespace ES.CCIS.Host.Controllers
{
    [Authorize]
    [RoutePrefix("api/Search")]
    public class SearchController : ApiBaseController
    {
        [HttpGet]
        [Route("")]
        public HttpResponseMessage Index(string search, [DefaultValue(0)] int pointid)
        {
            try
            {
                using (var DB = new CCISContext())
                {
                    SearchInfoModel model = new SearchInfoModel();
                    List<int> lstCusId = new List<int>();
                    List<int> lstPointId = new List<int>();

                    if (pointid == 0)
                    {
                        var lstCustomer = DB.Concus_Customer.Where(x => x.CustomerCode == search || x.Name == search).ToList();

                        var lstId = lstCustomer.Select(x => x.CustomerId).ToList();

                        var lstPoint = DB.Concus_ServicePoint.Where(x => lstId.Contains(x.Concus_Contract.CustomerId)).Select(x => x.PointId).ToList();

                        lstCusId.AddRange(lstId);
                        lstPointId.AddRange(lstPoint);
                    }
                    else
                    {
                        var lstContract = DB.Concus_ServicePoint.Where(x => x.PointId == pointid).Select(x => x.ContractId).ToList();

                        var lstCustomer = DB.Concus_Contract.Where(x => lstContract.Contains(x.ContractId)).Select(x => x.Concus_Customer.CustomerId).ToList();

                        lstCusId.AddRange(lstCustomer);
                        lstPointId.Add(pointid);
                    }

                    model.Search_CustomerInfoModel = GetCustomerInfo(lstCusId);
                    model.Search_EquipmentModel = GetEquipmentHistory(lstPointId);
                    model.Search_ImposedPriceModel = GetImposedPrice(lstPointId);
                    model.Search_PointDetailModel = GetPointDetail(lstPointId);
                    model.Search_BillDetailModel = GetBillDetail(lstCusId);

                    respone.Status = 1;
                    respone.Message = "OK";
                    respone.Data = model;
                    return createResponse();
                }
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }
        private List<Search_CustomerInfoModel> GetCustomerInfo(List<int> lstCusId)
        {
            using (var DB = new CCISContext())
            {
                var lstView = DB.Concus_Contract.Where(x => lstCusId.Contains(x.CustomerId))
                            .Select(x => new Search_CustomerInfoModel
                            {
                                ContractId = x.ContractId,
                                CustomerId = x.CustomerId,
                                CustomerCode = x.Concus_Customer.CustomerCode,
                                Name = x.Concus_Customer.Name,
                                PhoneNumber = x.Concus_Customer.PhoneNumber,
                                Address = x.Concus_Customer.Address,
                                BankAccount = x.Concus_Customer.BankAccount,
                                BankName = x.Concus_Customer.BankName,
                                FormOfPayment = DB.Bill_ElectricityBill.Where(o => o.CustomerId == x.CustomerId).OrderByDescending(o => o.BillId).Take(1).FirstOrDefault().FormOfPayment,
                                ContractCode = x.ContractCode,
                                SaveDate = DB.Index_CalendarOfSaveIndex
                                           .Where(o => o.FigureBookId == DB.Concus_ServicePoint.Where(i => i.ContractId == x.ContractId).FirstOrDefault().FigureBookId)
                                           .OrderByDescending(o => o.CalendarOfSaveIndexId).Take(1).FirstOrDefault().SaveDate
                            })
                            .ToList();

                return lstView;
            }            
        }

        private List<Search_PointDetailModel> GetPointDetail(List<int> lstPointId)
        {
            using (var DB = new CCISContext())
            {
                var lstPoint = DB.Concus_ServicePoint.Where(x => lstPointId.Contains(x.PointId))
                             .Select(x => new Search_PointDetailModel
                             {
                                 PointCode = x.PointCode,
                                 ActiveDate = x.ActiveDate,
                                 HouseholdNumber = x.HouseholdNumber,
                                 Address = x.Address,
                                 NumberOfPhases = x.NumberOfPhases,
                                 ReactivePower = x.ReactivePower,
                                 Power = x.Power,
                                 PointType = DB.Category_ServicePointType.Where(o => o.ServicePointTypeId == x.ServicePointType).FirstOrDefault().Description
                             })
                             .ToList();
                return lstPoint;
            }            
        }

        private List<Search_EquipmentModel> GetEquipmentHistory(List<int> lstPointId)
        {
            using (var DB = new CCISContext())
            {
                var lstEquip = DB.Index_Value.Where(x => lstPointId.Contains(x.PointId) && x.IndexType == EnumMethod.LoaiChiSo.DDN)
                            .Select(x => new Search_EquipmentModel
                            {
                                PointCode = x.Concus_ServicePoint.PointCode,
                                EndDate = x.EndDate,
                                ElectricityMeterCodeDDN = DB.EquipmentMT_ElectricityMeter.Where(o => o.ElectricityMeterId == x.ElectricityMeterId).FirstOrDefault().ElectricityMeterCode,
                                ElectricityMeterCodeDUP = DB.EquipmentMT_ElectricityMeter
                                                            .Where(o => o.ElectricityMeterId == DB.Index_Value.Where(i => i.PointId == x.PointId && i.IndexType == EnumMethod.LoaiChiSo.DUP
                                                            && i.Term == x.Term && i.Month == x.Month && i.Year == x.Year).FirstOrDefault().ElectricityMeterId)
                                                            .FirstOrDefault().ElectricityMeterCode,
                                CoefficientDDN = x.Coefficient,
                                CoefficientDUP = DB.Index_Value.Where(o => o.PointId == x.PointId && o.IndexType == EnumMethod.LoaiChiSo.DUP && o.Term == x.Term && o.Month == x.Month && o.Year == x.Year)
                                                .FirstOrDefault().Coefficient,
                                ValueDDN = x.NewValue,
                                ValueDUP = DB.Index_Value.Where(o => o.PointId == x.PointId && o.IndexType == EnumMethod.LoaiChiSo.DUP && o.Term == x.Term && o.Month == x.Month && o.Year == x.Year)
                                                .FirstOrDefault().NewValue,
                            })
                            .ToList();

                return lstEquip;
            }            
        }

        public List<Search_ImposedPriceModel> GetImposedPrice(List<int> lstPointId)
        {
            using (var DB = new CCISContext())
            {
                var lstImposed = DB.Concus_ImposedPrice.Where(x => lstPointId.Contains(x.PointId))
                                .Select(x => new Search_ImposedPriceModel
                                {
                                    PointCode = DB.Concus_ServicePoint.Where(o => o.PointId == x.PointId).FirstOrDefault().PointCode,
                                    OccupationsGroupCode = x.OccupationsGroupCode,
                                    GroupCode = x.GroupCode,
                                    TimeOfSale = x.TimeOfSale,
                                    Description = DB.Category_Price
                                                    .Where(o => o.OccupationsGroupCode == x.OccupationsGroupCode
                                                    && o.PriceGroupCode == x.GroupCode && o.PotentialSpace == x.PotentialCode && o.Time == x.TimeOfSale)
                                                    .FirstOrDefault().Description,
                                    Price = DB.Category_Price
                                                    .Where(o => o.OccupationsGroupCode == x.OccupationsGroupCode
                                                    && o.PriceGroupCode == x.GroupCode && o.PotentialSpace == x.PotentialCode && o.Time == x.TimeOfSale)
                                                    .FirstOrDefault().Price,
                                    ActiveDate = x.ActiveDate,
                                    PotentialName = DB.Category_Potential.Where(o => o.PotentialCode == x.PotentialCode).FirstOrDefault().PotentialName
                                })
                                .ToList();

                return lstImposed;
            }            
        }

        public List<Search_BillDetailModel> GetBillDetail(List<int> lstCusId)
        {
            using (var DB = new CCISContext())
            {
                var lstBill = DB.Bill_ElectricityBillDetail.Where(x => lstCusId.Contains(x.CustomerId))
                            .Select(x => new Search_BillDetailModel
                            {
                                BillId = x.BillId,
                                CustomerName = DB.Bill_ElectricityBill.Where(o => o.BillId == x.BillId).FirstOrDefault().CustomerName,
                                Term = x.Term,
                                Month = x.Month,
                                Year = x.Year,
                                ElectricityIndex = x.ElectricityIndex,
                                TimeOfUse = x.TimeOfUse,
                                OccupationsGroupCode = x.OccupationsGroupCode,
                                Price = x.Price,
                                Total = x.Total,
                                VAT = DB.Bill_ElectricityBill.Where(o => o.BillId == x.BillId).FirstOrDefault().VAT,
                                AllTotal = DB.Bill_ElectricityBill.Where(o => o.BillId == x.BillId).FirstOrDefault().Total
                            })
                            .ToList();
                return lstBill;
            }            
        }
    }
}
