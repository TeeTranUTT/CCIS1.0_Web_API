using CCIS_BusinessLogic;
using CCIS_DataAccess;
using ES.CCIS.Host.Helpers;
using ES.CCIS.Host.Models;
using ES.CCIS.Host.Models.CongTo;
using ES.CCIS.Host.Models.EnumMethods;
using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Web.Configuration;
using System.Web.Http;


namespace ES.CCIS.Host.Controllers.CongTo
{
    [Authorize]
    [RoutePrefix("api/ElectricMeter")]
    public class ElectricMeterController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_Administrator_Parameter vParameters = new Business_Administrator_Parameter();

        [HttpGet]
        [Route("EquipmentMT_ElectricityMeterManager")]
        public HttpResponseMessage EquipmentMT_ElectricityMeterManager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search, [DefaultValue("")] string ActionCode, [DefaultValue(4)] int Status)
        {
            try
            {
                var userInfo = TokenHelper.GetUserInfoFromRequest();
                var lstDepartmentIds = DepartmentHelper.GetChildDepIdsByUser(userInfo.UserName);

                using (var db = new CCISContext())
                {
                    string strQlyCto = vParameters.GetParameterValue("QLYCTO", "RIENG", lstDepartmentIds.FirstOrDefault());

                    if (strQlyCto == "CHUNG")
                    {
                        var departmentID = db.Administrator_Parameter.Where(x => x.ParameterName == "QLYCTO" && lstDepartmentIds.Contains(x.DepartmentId)).Select(x => x.ParameterDescribe).FirstOrDefault();
                        var lstDepartment = db.Administrator_Department.Where(x => departmentID.Contains(x.DepartmentId.ToString())).Select(x => x.DepartmentId).ToList();
                        lstDepartmentIds.AddRange(lstDepartment);
                    }

                    var query = from a in db.EquipmentMT_ElectricityMeter
                                join b in db.EquipmentMT_Testing
                                  on a.ElectricityMeterId equals b.ElectricityMeterId
                                where lstDepartmentIds.Contains(a.DepartmentId) && a.IsRoot == false && (b.Status == Status || Status == 4)
                                select new EquipmentMT_ElectricityMeterModel
                                {
                                    ElectricityMeterId = a.ElectricityMeterId,
                                    ElectricityMeterCode = a.ElectricityMeterCode,
                                    ElectricityMeterNumber = a.ElectricityMeterNumber,
                                    TypeName = a.Category_ElectricityMeterType.TypeName,
                                    CreateDate = a.CreateDate,
                                    TypeCode = a.Category_ElectricityMeterType.TypeCode,
                                    ActionCode = a.ActionCode,
                                    TestingStatus = b.Status
                                };

                    if (!string.IsNullOrEmpty(search))
                    {
                        query = (IQueryable<EquipmentMT_ElectricityMeterModel>)query.Where(item => item.ElectricityMeterCode.Contains(search) || item.ElectricityMeterNumber.Contains(search));
                    }

                    if (!string.IsNullOrEmpty(ActionCode))
                    {
                        query = (IQueryable<EquipmentMT_ElectricityMeterModel>)query.Where(item => item.ActionCode.Contains(ActionCode));
                    }

                    var paged = (IPagedList<EquipmentMT_ElectricityMeterModel>)query.OrderBy(p => p.CreateDate).ToPagedList(pageNumber, pageSize);

                    var response = new
                    {
                        paged.PageNumber,
                        paged.PageSize,
                        paged.TotalItemCount,
                        paged.PageCount,
                        paged.HasNextPage,
                        paged.HasPreviousPage,
                        ElectricityMeters = paged.ToList()
                    };

                    respone.Status = 1;
                    respone.Message = "OK";
                    respone.Data = response;
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

        //Quản lý công tơ đầu nguồn
        [HttpGet]
        [Route("EquipmentMT_RootElectricityMeterManager")]
        public HttpResponseMessage EquipmentMT_RootElectricityMeterManager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search, [DefaultValue("")] string ActionCode)
        {
            try
            {
                var userInfo = TokenHelper.GetUserInfoFromRequest();

                var lstDepartmentIds = DepartmentHelper.GetChildDepIdsByUser(userInfo.UserName);

                using (var db = new CCISContext())
                {
                    var query = db.EquipmentMT_ElectricityMeter.Where(item => lstDepartmentIds.Contains(item.DepartmentId) && item.IsRoot == true)
                        .Select(item => new EquipmentMT_ElectricityMeterModel
                        {
                            ElectricityMeterId = item.ElectricityMeterId,
                            ElectricityMeterCode = item.ElectricityMeterCode,
                            ElectricityMeterNumber = item.ElectricityMeterNumber,
                            TypeName = item.Category_ElectricityMeterType.TypeName,
                            CreateDate = item.CreateDate,
                            TypeCode = item.Category_ElectricityMeterType.TypeCode,
                            ActionCode = item.ActionCode,
                            TestingStatus = db.EquipmentMT_Testing.Where(i => i.ElectricityMeterId == item.ElectricityMeterId).Select(i => i.Status).FirstOrDefault()
                        });

                    if (!string.IsNullOrEmpty(search))
                    {
                        query = (IQueryable<EquipmentMT_ElectricityMeterModel>)query.Where(item => item.ElectricityMeterNumber.Contains(search) || item.ElectricityMeterCode.Contains(search));
                    }

                    if (!string.IsNullOrEmpty(ActionCode))
                    {
                        query = (IQueryable<EquipmentMT_ElectricityMeterModel>)query.Where(item => item.ActionCode.Contains(ActionCode));
                    }

                    var paged = (IPagedList<EquipmentMT_ElectricityMeterModel>)query.OrderBy(p => p.CreateDate).ToPagedList(pageNumber, pageSize);

                    var response = new
                    {
                        paged.PageNumber,
                        paged.PageSize,
                        paged.TotalItemCount,
                        paged.PageCount,
                        paged.HasNextPage,
                        paged.HasPreviousPage,
                        ElectricityMeters = paged.ToList()
                    };

                    respone.Status = 1;
                    respone.Message = "OK";
                    respone.Data = response;
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

        //Cập nhật lý do thanh lý
        [HttpPost]
        [Route("LiquidationMeter")]
        public HttpResponseMessage LiquidationMeter(LiquidationMeterInput input)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    var target = db.EquipmentMT_ElectricityMeter.Where(item => item.ElectricityMeterId == input.ElectricMeter).FirstOrDefault();

                    var testingEquipment = db.EquipmentMT_Testing.Where(item => item.ElectricityMeterId == input.ElectricMeter).ToList().LastOrDefault();
                    if (target != null)
                    {
                        if (target.ActionCode == TreoThaoActionCode.TrenLuoi)
                        {
                            throw new ArgumentException("Điểm đo đang treo công tơ, không thể thanh lý.");                            
                        }
                        else
                        {
                            target.ReasonId = Convert.ToInt32(input.ReasonId);
                            target.ActionCode = TreoThaoActionCode.ThanhLy;
                            target.LiquidationDate = DateTime.Now;
                            testingEquipment.Status = 2;
                            db.SaveChanges();

                            respone.Status = 1;
                            respone.Message = "Thanh lý công tơ thành công.";
                            respone.Data = null;
                            return createResponse();                            
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"Không có công tơ: {target.ElectricityMeterNumber}");                        
                    }
                }                
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"{ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }
    }
}
