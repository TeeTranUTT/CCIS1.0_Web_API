using CCIS_BusinessLogic;
using CCIS_DataAccess;
using CCIS_DataAccess.ViewModels;
using ES.CCIS.Host.Helpers;
using ES.CCIS.Host.Models;
using ES.CCIS.Host.Models.CongTo;
using ES.CCIS.Host.Models.EnumMethods;
using Microsoft.Ajax.Utilities;
using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
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
        private readonly Business_EquipmentMT_Testing bussinessEquipmentMtTesting = new Business_EquipmentMT_Testing();
        private readonly Business_EquipmentMT_ElectricityMeter bussinessEquipmentMtElectricityMeter = new Business_EquipmentMT_ElectricityMeter();
        private readonly Business_Administrator_Department businessDepartment = new Business_Administrator_Department();
        private readonly Business_EquipmentMT_StockReport equipmentMT_StockReport = new Business_EquipmentMT_StockReport();

        private readonly Business_EquipmentMT_OperationReport businessEquipmentMTOperationReport = new Business_EquipmentMT_OperationReport();
        private readonly Business_EquipmentMT_OperationDetail businessEquipmentMTOperationDetail = new Business_EquipmentMT_OperationDetail();
        private readonly Business_Index_Value businessIndexValue = new Business_Index_Value();
        private readonly Business_EquipmentMT_ElectricityMeter businessEquipmentMTElectricityMeter = new Business_EquipmentMT_ElectricityMeter();
        private readonly Business_EquipmentCT_OperationDetail businessEquipmentCTOperationDetail = new Business_EquipmentCT_OperationDetail();
        private readonly Business_EquipmentCT_CurrentTransformer businessEquipmentCTCurrentTransformer = new Business_EquipmentCT_CurrentTransformer();
        private readonly Business_EquipmentVT_OperationDetail businessEquipmentVTOperationDetail = new Business_EquipmentVT_OperationDetail();
        private readonly Business_EquipmentVT_VoltageTransformer businessEquipmentVTVoltageTransformer = new CCIS_BusinessLogic.Business_EquipmentVT_VoltageTransformer();
        private readonly Business_EquipmentVT_Testing businessEquipmentVTTesting = new Business_EquipmentVT_Testing();
        private readonly Business_EquipmentCT_Testing businessEquipmentCTTesting = new Business_EquipmentCT_Testing();

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

        #region Thêm mới công tơ
        [HttpPost]
        [Route("AddEquipmentMT_ElectricityMeter")]
        public HttpResponseMessage AddEquipmentMT_ElectricityMeter(EquipmentMT_ElectricityMeterViewModel model)
        {
            using (var db = new CCISContext())
            {
                using (var dbContextTransaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var departmentId = TokenHelper.GetDepartmentIdFromToken();
                        if (bussinessEquipmentMtElectricityMeter.CheckExistElectricityMeterCode(
                                model.ElectricityMeter.ElectricityMeterTypeId, model.ElectricityMeter.ManufactureYear,
                                model.ElectricityMeter.ElectricityMeterNumber, db))
                        {
                            throw new ArgumentException("Mã công tơ đã tồn tại.");
                        }
                        var webEmployeeId = businessDepartment.UserId(User.Identity.Name);
                        model.ElectricityMeter.DepartmentId = departmentId;
                        model.ElectricityMeter.CreateDate = DateTime.Now;
                        model.ElectricityMeter.ActionCode = TreoThaoActionCode.TrongKho;
                        model.ElectricityMeter.ActionDate = null;
                        model.ElectricityMeter.CreateUser = webEmployeeId;

                        var typeCode = db.Category_ElectricityMeterType.Where(item => item.ElectricityMeterTypeId.Equals(model.ElectricityMeter.ElectricityMeterTypeId))
                                .FirstOrDefault();
                        model.ElectricityMeter.ElectricityMeterCode = typeCode.TypeCode +
                                                                      model.ElectricityMeter.ManufactureYear
                                                                          .ToStringInvariant() +
                                                                      model.ElectricityMeter.ElectricityMeterNumber;

                        model.ElectricityMeter.TestingDate = model.Testing.TestingDate;
                        model.ElectricityMeter.EndTestingDate = model.Testing.TestingDate.Value.AddMonths(typeCode.TestingDay);
                        model.ElectricityMeter.EndTestingDate = new DateTime(model.ElectricityMeter.EndTestingDate.Year, model.ElectricityMeter.EndTestingDate.Month, 1);
                        int electricityMeterId = bussinessEquipmentMtElectricityMeter.AddEquipmentMT_ElectricityMeter(model.ElectricityMeter, false, db);

                        //Thông tin cập nhật thông tin kiểm định công tơ
                        if (model.Testing.ListTimeOfUse != null && model.Testing.ListTimeOfUse.Count() > 0)
                        {
                            for (int i = 0; i < model.Testing.ListTimeOfUse.Count(); i++)
                            {
                                if (i == 0)
                                    model.Testing.TimeOfUse += model.Testing.ListTimeOfUse[i].ToString();
                                else
                                    model.Testing.TimeOfUse += "," + model.Testing.ListTimeOfUse[i].ToString();
                            }
                        }
                        model.Testing.ElectricityMeterId = electricityMeterId;
                        model.Testing.CreateDate = DateTime.Now;
                        model.Testing.CreateUser = webEmployeeId;
                        model.Testing.Status = 1;
                        bussinessEquipmentMtTesting.AddEquipmentMT_Testing(model.Testing, db);

                        dbContextTransaction.Commit();

                        respone.Status = 1;
                        respone.Message = "Thêm mới công tơ thành công.";
                        respone.Data = electricityMeterId;
                        return createResponse();
                    }
                    catch (Exception ex)
                    {
                        dbContextTransaction.Rollback();
                        respone.Status = 0;
                        respone.Message = $"Lỗi: {ex.Message.ToString()}";
                        respone.Data = null;
                        return createResponse();
                    }
                }
            }
        }

        //Todo: api thêm mới công tơ bằng file chưa được viết
        [HttpPost]
        [Route("AddEquipmentMT_RootElectricityMeter")]
        public HttpResponseMessage AddEquipmentMT_RootElectricityMeter(EquipmentMT_ElectricityMeterViewModel model)
        {
            using (var db = new CCISContext())
            {
                using (var dbContextTransaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var departmentId = TokenHelper.GetDepartmentIdFromToken();
                        // Thêm mới công tơ
                        if (bussinessEquipmentMtElectricityMeter.CheckExistElectricityMeterCode(
                                model.ElectricityMeter.ElectricityMeterTypeId, model.ElectricityMeter.ManufactureYear,
                                model.ElectricityMeter.ElectricityMeterNumber, db))
                        {
                            throw new ArgumentException("Mã công tơ đã tồn tại.");
                        }

                        var webEmployeeId = businessDepartment.UserId(User.Identity.Name);
                        model.ElectricityMeter.DepartmentId = departmentId;
                        model.ElectricityMeter.CreateDate = DateTime.Now;
                        model.ElectricityMeter.ActionCode = TreoThaoActionCode.TrongKho;
                        model.ElectricityMeter.ActionDate = null;
                        model.ElectricityMeter.CreateUser = webEmployeeId;

                        var typeCode = db.Category_ElectricityMeterType.Where(item => item.ElectricityMeterTypeId.Equals(model.ElectricityMeter.ElectricityMeterTypeId))
                                .FirstOrDefault();
                        model.ElectricityMeter.ElectricityMeterCode = typeCode.TypeCode + model.ElectricityMeter.ManufactureYear.ToStringInvariant() +
                                                                      model.ElectricityMeter.ElectricityMeterNumber;
                        model.ElectricityMeter.TestingDate = model.Testing.TestingDate;
                        model.ElectricityMeter.EndTestingDate = model.Testing.TestingDate.Value.AddMonths(typeCode.TestingDay);
                        model.ElectricityMeter.EndTestingDate = new DateTime(model.ElectricityMeter.EndTestingDate.Year, model.ElectricityMeter.EndTestingDate.Month, 1);
                        int electricityMeterId = bussinessEquipmentMtElectricityMeter.AddEquipmentMT_ElectricityMeter(model.ElectricityMeter, true, db);

                        //Thông tin cập nhật thông tin kiểm định công tơ
                        if (model.Testing.ListTimeOfUse != null && model.Testing.ListTimeOfUse.Count() > 0)
                        {
                            for (int i = 0; i < model.Testing.ListTimeOfUse.Count(); i++)
                            {
                                if (i == 0)
                                    model.Testing.TimeOfUse += model.Testing.ListTimeOfUse[i].ToString();
                                else
                                    model.Testing.TimeOfUse += "," + model.Testing.ListTimeOfUse[i].ToString();
                            }
                        }
                        model.Testing.ElectricityMeterId = electricityMeterId;
                        model.Testing.CreateDate = DateTime.Now;
                        model.Testing.CreateUser = webEmployeeId;

                        bussinessEquipmentMtTesting.AddEquipmentMT_Testing(model.Testing, db);
                        dbContextTransaction.Commit();

                        respone.Status = 1;
                        respone.Message = "Lấy danh sách khách hàng thành công.";
                        respone.Data = electricityMeterId;
                        return createResponse();
                    }
                    catch (Exception ex)
                    {
                        respone.Status = 0;
                        respone.Message = $"Lỗi: {ex.Message.ToString()}";
                        respone.Data = null;
                        return createResponse();
                    }
                }
            }
        }
        #endregion

        //Lấy danh sách BCS theo chủng loại công tơ
        [HttpGet]
        [Route("GetTimeOfUse")]
        public HttpResponseMessage GetTimeOfUse(int electricityMeterTypeId)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    var type = "";
                    var check = db.Category_ElectricityMeterType.Any(item => item.ElectricityMeterTypeId == electricityMeterTypeId);
                    if (!check)
                    {
                        throw new ArgumentException($"Không tồn tại công tơ có ElectricityMeterTypeId {electricityMeterTypeId}");
                    }
                    type = db.Category_ElectricityMeterType.First(item => item.ElectricityMeterTypeId == electricityMeterTypeId).Type;
                    var listTimeOfUse = new List<string>();
                    switch (type.Trim())
                    {
                        case "DT":
                            listTimeOfUse.Add("BT");
                            listTimeOfUse.Add("CD");
                            listTimeOfUse.Add("TD");
                            listTimeOfUse.Add("SG");
                            listTimeOfUse.Add("VC");
                            //20201025_XuanDT_bỏ sung chiều nhận
                            //listTimeOfUse.Add("BN");
                            //listTimeOfUse.Add("CN");
                            //listTimeOfUse.Add("TN");
                            //listTimeOfUse.Add("SN");
                            //listTimeOfUse.Add("VN");
                            break;
                        case "D1":
                            listTimeOfUse.Add("KT");
                            listTimeOfUse.Add("VC");
                            break;
                        case "HC":
                            listTimeOfUse.Add("KT");
                            break;
                        default:
                            listTimeOfUse.Add("VC");
                            break;
                    }

                    respone.Status = 1;
                    respone.Message = "Lấy danh sách khách hàng thành công.";
                    respone.Data = listTimeOfUse;
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

        #region Kiểm định công tơ
        [HttpGet]
        [Route("GetElectricityMeterById")]
        public HttpResponseMessage GetElectricityMeterById(int electricityMeterId)
        {
            try
            {
                if (electricityMeterId < 0 || electricityMeterId == 0)
                {
                    throw new ArgumentException($"ElectricityMeterId {electricityMeterId} không hợp lệ.");
                }
                using (var db = new CCISContext())
                {
                    var equipmentMTElectricityMeterModel = db.EquipmentMT_ElectricityMeter.Where(item => item.ElectricityMeterId.Equals(electricityMeterId)).Select(item => new EquipmentMT_ElectricityMeterModel()
                    {
                        ElectricityMeterId = item.ElectricityMeterId,
                        ElectricityMeterCode = item.ElectricityMeterCode,
                        Possesive = item.Possesive,
                        ElectricityMeterNumber = item.ElectricityMeterNumber,
                        ManufactureYear = item.ManufactureYear,
                        TypeName = item.Category_ElectricityMeterType.TypeName
                    }).FirstOrDefault();

                    EquipmentMT_ElectricityMeterViewModel model = new EquipmentMT_ElectricityMeterViewModel();
                    model.ElectricityMeter = equipmentMTElectricityMeterModel;
                    var viewdulieu = db.EquipmentMT_Testing.Where(item => item.ElectricityMeterId.Equals(electricityMeterId)).Select(item => new EquipmentMT_TestingModel
                    {

                        TestingEmployee = item.TestingEmployee,
                        TaiLeadCode = item.TaiLeadCode,
                        TaiLeadQuantity = item.TaiLeadQuantity,
                        TestingLeadCode = item.TestingLeadCode,
                        VignetteCode = item.VignetteCode,
                        Serial = item.Serial,
                        OpticalGate = item.OpticalGate,
                        DevIndex = item.DevIndex,
                        VoltageRatio = item.VoltageRatio,
                        CurrentRatio = item.CurrentRatio,
                        DataError = item.DataError,
                        TestingLeadQuantity = item.TestingLeadQuantity,
                        K_Multiplication = item.K_Multiplication
                    }).FirstOrDefault();
                    model.Testing = viewdulieu;

                    respone.Status = 1;
                    respone.Message = "Lấy thông tin công tơ thành công.";
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

        [HttpPost]
        [Route("TestingElectricMeter")]
        public HttpResponseMessage TestingElectricMeter(EquipmentMT_ElectricityMeterViewModel model)
        {
            try
            {
                var userId = TokenHelper.GetUserIdFromToken();
                model.Testing.CreateDate = DateTime.Now;
                model.Testing.CreateUser = userId;
                model.Testing.ElectricityMeterId = model.ElectricityMeter.ElectricityMeterId;
                if (model.Testing.TestingStatus == true)
                    model.Testing.Status = 1;
                if (model.Testing.TestingStatus == false)
                    model.Testing.Status = 2;

                if (model.Testing.ListTimeOfUse != null && model.Testing.ListTimeOfUse.Count() > 0)
                {
                    for (int i = 0; i < model.Testing.ListTimeOfUse.Count(); i++)
                    {
                        if (i == 0)
                            model.Testing.TimeOfUse += model.Testing.ListTimeOfUse[i].ToString();
                        else
                            model.Testing.TimeOfUse += "," + model.Testing.ListTimeOfUse[i].ToString();
                    }
                }

                bussinessEquipmentMtTesting.EditEquipmentMT_Testing(model.Testing);

                respone.Status = 1;
                respone.Message = "Cập nhật kiểm định thành công.";
                respone.Data = model.ElectricityMeter.ElectricityMeterId;
                return createResponse();
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }

        [HttpPost]
        [Route("TestingElectricMeterByFile")]
        public HttpResponseMessage TestingElectricMeterByFile(List<EquipmentTestingViewModel> model)
        {
            using (var db = new CCISContext())
            {
                using (var dbContextTransaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var equipmentMT_TestingModel = new EquipmentMT_Testing();
                        if (model != null)
                        {
                            foreach (var item in model)
                            {
                                // Thêm mới công tơ
                                var electricityMeter = db.EquipmentMT_ElectricityMeter.Where(it => it.ElectricityMeterCode == item.ElectricityMeterCode).FirstOrDefault();
                                var testingDeparmentId = db.Category_TestingDepartment.Where(it => it.TestingDepartmentCode == item.TestingDepartmentCode).Select(it => it.TestingDepartmentId).FirstOrDefault();
                                if (item.K_Multiplication == null
                                    || item.Day == null || item.Month == null || item.Year == null || item.TestingDepartmentCode == null
                                    || item.ElectricityMeterCode == null || item.ReportNumber == null || item.ListTimeOfUse == null)
                                {
                                    dbContextTransaction.Rollback();

                                    throw new ArgumentException("Công tơ thiếu dữ liệu, xin nhập lại.");

                                }
                                if (electricityMeter == null)
                                {
                                    dbContextTransaction.Rollback();

                                    throw new ArgumentException($"Không có mã công tơ: {item.ElectricityMeterCode}.");
                                }
                                else if (electricityMeter.ActionCode == TreoThaoActionCode.TrenLuoi)
                                {
                                    dbContextTransaction.Rollback();

                                    throw new ArgumentException($"Công tơ chưa được tháo xuống vui lòng tháo trước khi kiểm định lại: {item.ElectricityMeterCode}.");
                                }
                                else
                                {
                                    var userId = TokenHelper.GetUserIdFromToken();
                                    equipmentMT_TestingModel.ElectricityMeterId = electricityMeter.ElectricityMeterId;
                                    equipmentMT_TestingModel.TimeOfUse = item.ListTimeOfUse;
                                    equipmentMT_TestingModel.ReportNumber = item.ReportNumber;
                                    string testingDate = item.Year.Value + "-" + item.Month.Value + "-" + item.Day.Value;
                                    equipmentMT_TestingModel.TestingDate = DateTime.Parse(testingDate);
                                    equipmentMT_TestingModel.K_Multiplication = item.K_Multiplication.Value;
                                    equipmentMT_TestingModel.TestingDepartmentId = testingDeparmentId;
                                    equipmentMT_TestingModel.TestingEmployee = item.TestingEmployee;
                                    equipmentMT_TestingModel.TaiLeadCode = item.TaiLeadCode;
                                    equipmentMT_TestingModel.TaiLeadQuantity = item.TaiLeadQuantity.Value;
                                    equipmentMT_TestingModel.TestingLeadCode = item.TestingLeadCode;
                                    equipmentMT_TestingModel.VignetteCode = item.VignetteCode;
                                    equipmentMT_TestingModel.Serial = item.Serial;
                                    equipmentMT_TestingModel.Description = "";
                                    equipmentMT_TestingModel.DevIndex = 0;
                                    equipmentMT_TestingModel.DevDate = DateTime.Now;
                                    equipmentMT_TestingModel.K_Complement = 1;
                                    equipmentMT_TestingModel.VoltageRatio = "1";
                                    equipmentMT_TestingModel.CurrentRatio = "1";
                                    equipmentMT_TestingModel.PliersCode = "1";
                                    equipmentMT_TestingModel.Status = 1;
                                    equipmentMT_TestingModel.SendDate = DateTime.Now;
                                    equipmentMT_TestingModel.OpticalGate = null;
                                    equipmentMT_TestingModel.CreateDate = DateTime.Now;
                                    equipmentMT_TestingModel.CreateUser = userId;
                                    equipmentMT_TestingModel.DataError = "";
                                    bussinessEquipmentMtTesting.EditEquipmentMT_Testing(equipmentMT_TestingModel);
                                }
                            }

                            dbContextTransaction.Commit();

                            respone.Status = 1;
                            respone.Message = "Kiểm định công tơ thành công.";
                            respone.Data = null;
                            return createResponse();
                        }
                        else
                        {
                            dbContextTransaction.Rollback();
                            throw new ArgumentException("Không có nhập công tơ.");
                        }

                    }
                    catch (Exception ex)
                    {
                        dbContextTransaction.Rollback();
                        respone.Status = 0;
                        respone.Message = $"{ex.Message.ToString()}";
                        respone.Data = null;
                        return createResponse();
                    }
                }
            }
        }
        #endregion

        #region Chuyển kho công tơ
        // Hàm lấy thông tin công tơ in ra lưới chuyển kho
        [HttpPost]
        [Route("GetMeterInfor")]
        public HttpResponseMessage GetMeterInfor(string MeterCode, string MeterNumber)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    var electricityMeter =
                        db.EquipmentMT_ElectricityMeter.Where(
                                item =>
                                    (item.ElectricityMeterCode.Equals(MeterCode) ||
                                    item.ElectricityMeterNumber.Equals(MeterNumber)) && item.ActionCode.Equals(TreoThaoActionCode.TrongKho))// kiểm tra mã công tơ thỏa mãn tồn tại trong kho
                            .Select(item => new EquipmentMT_ElectricityMeterModel
                            {
                                ElectricityMeterCode = item.ElectricityMeterCode,
                                ManufactureYear = item.ManufactureYear,
                                ElectricityMeterTypeId = item.ElectricityMeterTypeId,
                                ElectricityMeterId = item.ElectricityMeterId
                            }).FirstOrDefault();

                    var electricityMeterType =
                        db.Category_ElectricityMeterType.Where(
                                item => item.ElectricityMeterTypeId.Equals(electricityMeter.ElectricityMeterTypeId))
                            .Select(item => new Category_ElectricityMeterTypeModel
                            {
                                TypeCode = item.TypeCode,
                                TypeName = item.TypeName
                            })
                            .FirstOrDefault();

                    var result = new
                    {
                        electricityMeter.ElectricityMeterCode,
                        electricityMeter.ManufactureYear,
                        electricityMeter.ElectricityMeterId,
                        ElectricityMeterType = electricityMeterType.TypeCode + "-" + electricityMeterType.TypeName
                    };

                    respone.Status = 1;
                    respone.Message = "Lấy danh sách khách hàng thành công.";
                    respone.Data = result;
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

        [HttpGet]
        [Route("HistoryOfStock")]
        public HttpResponseMessage HistoryOfStock([DefaultValue(1)] int pageNumber, [DefaultValue(10)] int pageSize,
            [DefaultValue("")] string search, [DefaultValue("")] string electMeterCode,
            [DefaultValue("")] string electMeterNum)
        {
            try
            {
                var userInfo = TokenHelper.GetUserInfoFromRequest();
                var departmentId = businessDepartment.GetIddv(userInfo.UserName);

                using (var db = new CCISContext())
                {
                    var query = db.EquipmentMT_StockReport.Where(
                                item => item.DepartmentId.Equals(departmentId))
                            .Select(item => new EquipmentMT_StockReportModel
                            {
                                CreateDate = item.CreateDate,
                                CreateUser = item.CreateUser,
                                Deliverer = item.Deliverer,
                                DeliveringDate = item.DeliveringDate,
                                DepartmentId = item.DepartmentId,
                                ReasonId = item.ReasonId,
                                ReceivingDate = item.ReceivingDate,
                                Recipient = item.Recipient,
                                ReportCode = item.ReportCode,
                                ReportId = item.ReportId,
                                StockId = item.StockId,
                                DelivererName =
                                    db.Category_Employee.FirstOrDefault(
                                        i => i.EmployeeId == item.Deliverer && i.Type == 1).FullName,
                                RecipientName =
                                    db.Category_Employee.FirstOrDefault(
                                        i => i.EmployeeId == item.Recipient && i.Type == 1).FullName,
                                ReasonName =
                                    db.Category_Reason.FirstOrDefault(i => i.ReasonId == item.ReasonId && i.Group == 2)
                                        .ReasonName,
                                StockName = db.Category_Stock.FirstOrDefault(i => i.StockId == item.StockId).Description,
                            });

                    if (!string.IsNullOrEmpty(search))
                    {
                        query = (IQueryable<EquipmentMT_StockReportModel>)query.Where(item => item.ReportCode.Contains(search));
                    }

                    var paged = (IPagedList<EquipmentMT_StockReportModel>)query.OrderBy(p => p.ReportCode).ToPagedList(pageNumber, pageSize);

                    var response = new
                    {
                        paged.PageNumber,
                        paged.PageSize,
                        paged.TotalItemCount,
                        paged.PageCount,
                        paged.HasNextPage,
                        paged.HasPreviousPage,
                        StockReports = paged.ToList()
                    };
                    respone.Status = 1;
                    respone.Message = "Lấy danh sách biên bản chuyển kho thành công.";
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

        //Cập nhật biên bản chuyển kho
        [HttpPost]
        [Route("StockReportUpdate")]
        public HttpResponseMessage StockReportUpdate(List<EquipmentMT_StockReportModel> myArray)
        {
            try
            {
                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                var userId = TokenHelper.GetUserIdFromToken();

                equipmentMT_StockReport.AddListEquipmentMT_StockReport(myArray, departmentId, userId);

                respone.Status = 1;
                respone.Message = "Cập nhật biên bản chuyển kho thành công.";
                respone.Data = null;
                return createResponse();
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }
        #endregion

        #region Treo tháo công tơ
        [HttpGet]
        [Route("OperationReportManager")]
        public HttpResponseMessage OperationReportManager([DefaultValue(0)] int figureBookId, [DefaultValue("")] string pointCode)
        {
            try
            {
                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var listDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);

                using (var db = new CCISContext())
                {
                    var query = db.Concus_ServicePoint.Where(item => item.Status == true)
                    .Select(item => new Concus_ServicePointModel()
                    {
                        PointId = item.PointId,
                        PointCode = item.PointCode,
                        CustomerName = item.Concus_Contract.Concus_Customer.Name,
                        CustomerCode = item.Concus_Contract.Concus_Customer.CustomerCode,
                        Index = item.Index,
                        Address = item.Address,
                        ElectricityMeterNumber = db.EquipmentMT_OperationDetail.Where(i => i.PointId == item.PointId && i.Status == 1).OrderByDescending(i => i.DetailId).FirstOrDefault().EquipmentMT_ElectricityMeter.ElectricityMeterNumber != null ? db.EquipmentMT_OperationDetail.Where(i => i.PointId == item.PointId && i.Status == 1).OrderByDescending(i => i.DetailId).FirstOrDefault().EquipmentMT_ElectricityMeter.ElectricityMeterNumber : "",
                        K_Multiplication = db.EquipmentMT_OperationDetail.Where(i => i.PointId == item.PointId && i.Status == 1).OrderByDescending(i => i.DetailId).FirstOrDefault().K_Multiplication != null ? db.EquipmentMT_OperationDetail.Where(i => i.PointId == item.PointId && i.Status == 1).OrderByDescending(i => i.DetailId).FirstOrDefault().K_Multiplication : 0//todo: hieulv: lấy hệ số nhân hiện tại
                        ,
                        CanEditInfoEquipment = db.EquipmentMT_OperationReport.Any(opertions => opertions.PointId.Equals(item.PointId))
                    });

                    if (!string.IsNullOrEmpty(pointCode))
                    {
                        query = (IQueryable<Concus_ServicePointModel>)query.Where(item => item.PointCode.Trim() == pointCode.Trim());
                    }

                    if (figureBookId > 0)
                    {
                        query = (IQueryable<Concus_ServicePointModel>)query.Where(item => item.FigureBookId == figureBookId);
                    }

                    var paged = query.OrderBy(p => p.CustomerCode).ToList();

                    respone.Status = 1;
                    respone.Message = "OK";
                    respone.Data = paged;
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

        //In biên bản treo tháo đầu nguồn
        [HttpGet]
        [Route("Root_OperationReportManager")]
        public HttpResponseMessage Root_OperationReportManager(int figureBookId = 0, string pointCode = "")
        {
            try
            {
                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                var listDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);

                using (var db = new CCISContext())
                {
                    var model = db.Concus_ServicePoint.Where(item => ((figureBookId > 0 && item.FigureBookId == figureBookId)
                                                            || (pointCode.Trim() != "" && item.PointCode.Trim() == pointCode.Trim()))
                                                        && item.IsRootPoint == true && item.Status == true)
                    .Select(item => new Concus_ServicePointModel()
                    {
                        PointId = item.PointId,
                        PointCode = item.PointCode,
                        Index = item.Index,
                        Address = item.Address,
                        ElectricityMeterNumber = "",
                        K_Multiplication = 0
                    }).OrderBy(item => item.Index).ToList();
                    if (model != null)
                    {
                        foreach (var vSP in model)
                        {
                            var bdong = db.EquipmentMT_OperationDetail.Where(i => i.PointId == vSP.PointId && i.Status == 1).OrderByDescending(i => i.DetailId).FirstOrDefault();
                            if (bdong != null)
                            {
                                vSP.ElectricityMeterNumber = bdong.EquipmentMT_ElectricityMeter.ElectricityMeterNumber;
                                vSP.K_Multiplication = bdong.K_Multiplication;
                            }
                        }
                    }

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

        //Chi tiết biên bản
        [HttpGet]
        [Route("EquipmentMT_OperationReport")]
        public HttpResponseMessage EquipmentMT_OperationReport(int pointId)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    EquipmentMT_OperationReportViewModel model = MT_OperationReport(pointId);

                    var lstCodeReason = new List<int> { LyDoTreoThaoGroup.DinhKy, LyDoTreoThaoGroup.LapTrinhLaiCto, LyDoTreoThaoGroup.ThaoThanhLy, LyDoTreoThaoGroup.TreoMoi };

                    var reasonList = db.Category_Reason.Where(item => lstCodeReason.Contains(item.Group)).ToList();

                    model.LstReason = reasonList;

                    respone.Status = 1;
                    respone.Message = "Lấy danh sách khách hàng thành công.";
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

        //Chi tiết biên bản đầu nguồn
        [HttpGet]
        [Route("Root_EquipmentMT_OperationReport")]
        public HttpResponseMessage Root_EquipmentMT_OperationReport(int pointId)
        {
            try
            {
                EquipmentMT_OperationReportViewModel model = MT_OperationReport(pointId);

                using (var db = new CCISContext())
                {
                    var daGCSTrongThang = db.Index_Value.Any(o => o.Month == DateTime.Now.Month && o.Year == DateTime.Now.Year && o.PointId == pointId && o.IndexType == "DDK");
                    if (daGCSTrongThang)
                    {
                        throw new ArgumentException("Treo tháo không thành công, điểm đo đã ghi chỉ số định kỳ.");
                    }
                }

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = model;
                return createResponse();
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }

        private EquipmentMT_OperationReportViewModel MT_OperationReport(int editing)
        {
            EquipmentMT_OperationReportViewModel model = new EquipmentMT_OperationReportViewModel();
            using (var db = new CCISContext())
            {

                //Lấy thông tin điểm đo
                Concus_ServicePointModel servicePointModel = new Concus_ServicePointModel();
                servicePointModel = db.Concus_ServicePoint.Where(item => item.PointId.Equals(editing))
                    .Select(item => new Concus_ServicePointModel()
                    {
                        PointId = item.PointId,
                        PointCode = item.PointCode,
                        IsRootPoint = item.IsRootPoint,
                        Address = item.Address,
                        ServicePointType = item.ServicePointType,
                        NumberOfPhases = item.NumberOfPhases,
                        ActiveDate = item.ActiveDate,
                        ContractId = item.ContractId,
                        DepartmentId = item.DepartmentId
                    }).FirstOrDefault();
                if (!servicePointModel.IsRootPoint)
                {
                    var cusID = db.Concus_Contract.Where(o => o.ContractId == servicePointModel.ContractId).FirstOrDefault();
                    servicePointModel.ActiveDate = servicePointModel.ActiveDate > cusID.ActiveDate ? servicePointModel.ActiveDate : cusID.ActiveDate;
                    var cusinf = db.Concus_Customer.Where(o => o.CustomerId == cusID.CustomerId).FirstOrDefault();
                    servicePointModel.CustomerName = cusinf.Name;
                }

                //Lấy thông tin thiết đang treo (để tháo)
                #region Công tơ tháo
                var detail = db.EquipmentMT_OperationDetail.Where(o3 => o3.PointId == editing).ToList();
                if (detail.Count > 0)
                {
                    var dReportIdMax = detail.Max(o4 => o4.ReportId);
                    var operationDetailE = db.EquipmentMT_OperationDetail.Where(item => item.ReportId.Equals(dReportIdMax) && item.Status == 1)
                        .Select(item => new EquipmentMT_OperationDetailModel()
                        {
                            PointId = item.PointId,
                            ElectricityMeterCode = item.EquipmentMT_ElectricityMeter.ElectricityMeterCode,
                            ElectricityMeterNumber = item.EquipmentMT_ElectricityMeter.ElectricityMeterNumber,
                            ElectricityMeterId = item.ElectricityMeterId,
                            Status = 0, //Gán mặc định là tháo
                            K_Multiplication = item.K_Multiplication
                        }).FirstOrDefault();

                    model.OperationDetailE = operationDetailE;

                    //Thông tin chỉ số tháo
                    if (operationDetailE != null)
                    {
                        //Lấy chỉ số cũ
                        var listIndexValue = db.Index_Value.Where(item => item.PointId == servicePointModel.PointId && item.ElectricityMeterId == operationDetailE.ElectricityMeterId).OrderByDescending(item => item.IndexId).Take(10).ToList();

                        //Lấy thông tin bộ chỉ số treo từ bảng kiểm định nếu tìm thấy công tơ                        
                        var timeOfUseB = db.EquipmentMT_Testing.Where(item => item.ElectricityMeterId == operationDetailE.ElectricityMeterId).FirstOrDefault();
                        model.IndexValueE = new List<Index_ValueModel>();
                        if (timeOfUseB != null && timeOfUseB.TimeOfUse != null)
                        {
                            string[] arrTimeOfUseB = timeOfUseB.TimeOfUse.Split(',');

                            foreach (var item in arrTimeOfUseB)
                            {
                                Index_ValueModel vm = new Index_ValueModel();
                                vm.IndexType = "DDN";
                                vm.ElectricityMeterId = operationDetailE.ElectricityMeterId;
                                vm.PointId = servicePointModel.PointId;
                                vm.TimeOfUse = item;
                                vm.ElectricityMeterCode = operationDetailE.ElectricityMeterCode;
                                vm.ElectricityMeterNumber = operationDetailE.ElectricityMeterNumber;
                                // Đoạn này cần check xem bộ chỉ số ở Index Value và ở bảng EquipmentMT_Testing có giống nhau không, hiện đang bị lỗi này rất nhiều.
                                try
                                {
                                    vm.Coefficient = listIndexValue.Count == 0 ? 0 : listIndexValue.OrderByDescending(i => i.IndexId).FirstOrDefault().Coefficient;
                                    vm.OldValue = listIndexValue.Count == 0 ? 0 : listIndexValue.Where(i => i.TimeOfUse.Trim() == item.Trim()).OrderByDescending(i => i.IndexId).FirstOrDefault().NewValue;
                                }
                                catch
                                {
                                    throw new Exception("Bộ chỉ số ghi tháng trước đó không khớp với bộ chỉ số treo tháo hiện tại vui lòng kiểm tra lại");
                                }
                                model.IndexValueE.Add(vm);
                            }
                        }
                        else
                        {
                            throw new Exception($"Không xác định được bộ chỉ số của công tơ cần tháo có số công tơ là {operationDetailE.ElectricityMeterNumber}");
                        }
                    }
                }
                #endregion

                #region TI tháo --> lấy tất cả các TI đang treo
                var listCTDetailId = (from c in db.EquipmentCT_OperationDetail
                                      where c.PointId == editing
                                      group c by c.CurrentTransformerId into g
                                      select g.Max(a => a.DetailId)
                                   ).ToList();
                if (listCTDetailId != null && listCTDetailId.Count > 0)
                {
                    var listCTOperationEDetail = db.EquipmentCT_OperationDetail
                        .Where(o3 => o3.PointId == editing && listCTDetailId.Contains(o3.DetailId) && o3.Status == 1)
                        .Select(item => new EquipmentCT_OperationDetailModel()
                        {
                            PointId = item.PointId,
                            CurrentTransformerId = item.CurrentTransformerId,
                            CTCode = item.EquipmentCT_CurrentTransformer.CTCode,
                            CTNumber = item.EquipmentCT_CurrentTransformer.CTNumber,
                            TypeCode = item.EquipmentCT_CurrentTransformer.Category_CurrentTransformerType.TypeCode,
                            NumberOfPhases = item.EquipmentCT_CurrentTransformer.Category_CurrentTransformerType.NumberOfPhases,
                            IsEnd = false,
                            ConnectionRatio = item.ConnectionRatio
                        }).ToList();
                    model.CTOperationDetailE = listCTOperationEDetail;
                }
                #endregion

                #region TU tháo
                var listVTDetailId = (from c in db.EquipmentVT_OperationDetail
                                      where c.PointId == editing
                                      group c by c.VoltageTransformerId into g
                                      select g.Max(a => a.DetailId)
                                   ).ToList();
                if (listVTDetailId != null && listVTDetailId.Count > 0)
                {
                    var listVTOperationEDetail = db.EquipmentVT_OperationDetail
                        .Where(o3 => o3.PointId == editing && listVTDetailId.Contains(o3.DetailId) && o3.Status == 1)
                        .Select(item => new EquipmentVT_OperationDetailModel()
                        {
                            PointId = item.PointId,
                            VoltageTransformerId = item.VoltageTransformerId,
                            VTCode = item.EquipmentVT_VoltageTransformer.VTCode,
                            VTNumber = item.EquipmentVT_VoltageTransformer.VTNumber,
                            TypeCode = item.EquipmentVT_VoltageTransformer.Category_VoltageTransformerType.TypeCode,
                            NumberOfPhases = item.EquipmentVT_VoltageTransformer.Category_VoltageTransformerType.NumberOfPhases,
                            IsEnd = false,
                            ConnectionRatio = item.ConnectionRatio
                        }).ToList();
                    model.VTOperationDetailE = listVTOperationEDetail;
                }
                #endregion


                model.ServicePoint = servicePointModel;
                model.IndexValueB = new List<Index_ValueModel>();
                #region Lấy thêm thông tin ngay bien dong gan nhat để kiểm tra
                DateTime activeDate2;
                try
                {
                    activeDate2 = db.Concus_Contract.Where(c => c.ContractId == model.ServicePoint.ContractId && c.DepartmentId == model.ServicePoint.DepartmentId).Max(c => c.ActiveDate);
                    servicePointModel.ActiveDate = servicePointModel.ActiveDate > activeDate2 ? servicePointModel.ActiveDate : activeDate2;
                }
                catch (Exception ex1)
                {
                }

                #endregion
                return model;
            }
        }

        [HttpPost]
        [Route("EquipmentMT_OperationReport")]
        public HttpResponseMessage EquipmentMT_OperationReport(EquipmentMT_OperationReportViewModel model, string saveWork, [DefaultValue(0)] int ReasonId, [DefaultValue(true)] bool isRoot)
        {
            using (var db = new CCISContext())
            {
                using (var dbContextTransaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        #region Thông tin chung
                        if (ReasonId == 0)
                        {
                            var lstCodeReason = new List<int> { LyDoTreoThaoGroup.DinhKy, LyDoTreoThaoGroup.LapTrinhLaiCto, LyDoTreoThaoGroup.ThaoThanhLy, LyDoTreoThaoGroup.TreoMoi };
                            var reasonList = db.Category_Reason.Where(item => lstCodeReason.Contains(item.Group)).ToList();
                            model.LstReason = reasonList;
                        }
                        else
                        {
                            model.LstReason = db.Category_Reason.Where(item => item.ReasonId == ReasonId).ToList();
                        }
                        #endregion

                        //kiểm tra xem trong ngày đã có treo tháo chưa, nếu có rồi thì không cho làm nữa
                        if (model.OperationReport.OperationDate.Value < model.ServicePoint.ActiveDate)
                        {
                            throw new ArgumentException($"Kiểm tra: Ngày treo tháo phải từ ngày ký hợp đồng hoặc thay đổi thông tin gần ({model.ServicePoint.ActiveDate.ToString("dd/MM/yyyy")})");                            
                        }

                        DateTime activeDate2;
                        activeDate2 = db.Index_Value.Where(c => c.PointId == model.ServicePoint.PointId && c.DepartmentId == model.ServicePoint.DepartmentId).Max(c => c.EndDate);
                        if (model.OperationReport.OperationDate.Value <= activeDate2)
                        {
                            throw new ArgumentException($"Kiểm tra: Ngày treo tháo phải sau ngày có biến động chỉ số gần nhất ({activeDate2.ToString("dd/MM/yyyy")})");                            
                        }

                        activeDate2 = db.EquipmentMT_OperationReport.Where(c => c.PointId == model.ServicePoint.PointId && c.DepartmentId == model.ServicePoint.DepartmentId).Max(c => c.OperationDate.Value);
                        if (model.OperationReport.OperationDate.Value <= activeDate2)
                        {
                            throw new ArgumentException($"Kiểm tra: Ngày treo tháo phải sau ngày có biến động treo tháo gần nhất ({activeDate2.ToString("dd/MM/yyyy")})");                            
                        }

                        var departmentId = db.Concus_ServicePoint.Where(item => item.PointId == model.ServicePoint.PointId).FirstOrDefault().DepartmentId;
                        var userId = TokenHelper.GetUserIdFromToken();

                        if (saveWork.Trim() == "Treo công tơ")
                        {
                            #region Tìm công tơ treo

                            //Nếu là treo tháo lập trình lại công tơ gán thông tin công tơ treo = tháo
                            if (model.OperationType == LoaiTreoThao.LapTrinhLaiCto)
                            {
                                model.OperationDetailB.BOOCCode = model.OperationDetailE.BOOCCode;
                                model.OperationDetailB.BOOCQuantity = model.OperationDetailE.BOOCQuantity;
                                model.OperationDetailB.BoxAddress = model.OperationDetailE.BoxAddress;
                                model.OperationDetailB.BoxLeadCode = model.OperationDetailE.BoxLeadCode;
                                model.OperationDetailB.BoxLeadQuantity = model.OperationDetailE.BoxLeadQuantity;
                                model.OperationDetailB.CreateDate = model.OperationDetailE.CreateDate;
                                model.OperationDetailB.CreateUser = model.OperationDetailE.CreateUser;
                                model.OperationDetailB.ElectricityMeterCode = model.OperationDetailE.ElectricityMeterCode;
                                model.OperationDetailB.ElectricityMeterId = model.OperationDetailE.ElectricityMeterId;
                                model.OperationDetailB.ElectricityMeterNumber = model.OperationDetailE.ElectricityMeterNumber;
                                model.OperationDetailB.ElectricityMeterType = model.OperationDetailE.ElectricityMeterType;
                                model.OperationDetailB.FigureBookId = model.OperationDetailE.FigureBookId;
                                model.OperationDetailB.Id = model.OperationDetailE.Id;
                                model.OperationDetailB.Index = model.OperationDetailE.Index;
                                model.OperationDetailB.ManuFatureYear = model.OperationDetailE.ManuFatureYear;
                                model.OperationDetailB.NewValue = model.OperationDetailE.NewValue;
                                model.OperationDetailB.OldValue = model.OperationDetailE.OldValue;
                                model.OperationDetailB.OperationDate = model.OperationDetailE.OperationDate;
                                model.OperationDetailB.PointCode = model.OperationDetailE.PointCode;
                                model.OperationDetailB.PointId = model.OperationDetailE.PointId;
                                model.OperationDetailB.PositionId = model.OperationDetailE.PositionId;
                                model.OperationDetailB.ReportId = model.OperationDetailE.ReportId;
                                model.OperationDetailB.Status = model.OperationDetailE.Status;
                                model.OperationDetailB.TimeOfUse = model.OperationDetailE.TimeOfUse;
                                model.OperationDetailB.TimeOfUses = model.OperationDetailE.TimeOfUses;

                                model.IndexValueB = model.IndexValueE;
                            }

                            //if (model.OperationType == 1 && model.OperationDetailB.ElectricityMeterId == null)
                            if (model.OperationType == LoaiTreoThao.DinhKy)
                            {
                                //Tìm kiếm công tơ treo - điều kiện mã biến động = A

                                var electricMeterB = db.EquipmentMT_ElectricityMeter.Where(item => item.DepartmentId == departmentId && item.ElectricityMeterCode == model.OperationDetailB.ElectricityMeterCode.Trim() && item.ActionCode != TreoThaoActionCode.TrenLuoi)
                                    .Select(item => new EquipmentMT_OperationDetailModel()
                                    {
                                        PointId = model.ServicePoint.PointId,
                                        ElectricityMeterCode = item.ElectricityMeterCode,
                                        ElectricityMeterNumber = item.ElectricityMeterNumber,
                                        ElectricityMeterId = item.ElectricityMeterId,
                                        Status = 1, //Gán mặc định là treo
                                        K_Multiplication = model.OperationDetailB.K_Multiplication,
                                        NumberOfPhases = item.Category_ElectricityMeterType.NumberOfPhases,
                                        Type = item.Category_ElectricityMeterType.Type,
                                        ActionCode = item.ActionCode
                                    }).FirstOrDefault();
                                //nếu không tìm thấy công tơ trong đơn vị thì tìm toàn đơn vị
                                if (electricMeterB == null)
                                {
                                    //Kiểm tra chế độ, có phải là chạy công tơ chung toàn các đơn vị không (như DNC)
                                    Business_Administrator_Parameter vParameters = new Business_Administrator_Parameter();
                                    string strQlyCto = vParameters.GetParameterValue("QLYCTO", "RIENG");
                                    if (strQlyCto == "CHUNG")
                                    {
                                        var Cto = db.EquipmentMT_ElectricityMeter.Where(item => item.ElectricityMeterCode == model.OperationDetailB.ElectricityMeterCode.Trim() && item.ActionCode != TreoThaoActionCode.TrenLuoi).FirstOrDefault();
                                        if (Cto != null)
                                        {
                                            //chuyển đơn vị luôn
                                            Cto.DepartmentId = departmentId;
                                            db.SaveChanges();
                                            electricMeterB = new EquipmentMT_OperationDetailModel()
                                            {
                                                PointId = model.ServicePoint.PointId,
                                                ElectricityMeterCode = Cto.ElectricityMeterCode,
                                                ElectricityMeterNumber = Cto.ElectricityMeterNumber,
                                                ElectricityMeterId = Cto.ElectricityMeterId,
                                                Status = 1, //Gán mặc định là treo
                                                K_Multiplication = model.OperationDetailB.K_Multiplication,
                                                NumberOfPhases = Cto.Category_ElectricityMeterType.NumberOfPhases,
                                                Type = Cto.Category_ElectricityMeterType.Type
                                            };
                                        }
                                    }
                                }

                                //Thông báo nếu không tìm thấy công tơ
                                if (electricMeterB == null)
                                {
                                    throw new ArgumentException($"Không tìm thấy công tơ {model.OperationDetailB.ElectricityMeterCode}");                                    
                                }

                                if (electricMeterB.ActionCode == TreoThaoActionCode.TrenLuoi)
                                {
                                    throw new ArgumentException($"Công tơ này đang được treo cho một điểm đo khác vui lòng không treo công tơ này.");                                    
                                }

                                var pointType = model.ServicePoint.ServicePointType;

                                if (pointType == 1 && electricMeterB.Type == "DT")
                                {
                                    throw new ArgumentException("Điểm đo loại 1 không thể treo công tơ điện tử nhiều giá.");                                   
                                }
                                if (pointType == 8 && electricMeterB.Type == "HC")
                                {
                                    throw new ArgumentException("Điểm đo loại 8 không thể treo công tơ ĐT nhiều giá.");                                    
                                }
                                if (model.ServicePoint.NumberOfPhases != electricMeterB.NumberOfPhases)
                                {
                                    throw new ArgumentException("Số pha điểm đo khác số pha công tơ.");                                    
                                }

                                model.OperationDetailB = electricMeterB;

                                //Lấy thông tin bộ chỉ số treo từ bảng kiểm định nếu tìm thấy công tơ                        
                                var timeOfUseB = db.EquipmentMT_Testing.Where(item => item.ElectricityMeterId == electricMeterB.ElectricityMeterId).FirstOrDefault();
                                model.IndexValueB = new List<Index_ValueModel>();
                                if (timeOfUseB.TimeOfUse == null)
                                {
                                    throw new ArgumentException("Chưa cấu hình bộ chỉ số của công tơ treo, vui lòng kiểm tra lại công tơ.");                                    
                                }
                                if (timeOfUseB != null && timeOfUseB.TimeOfUse != null)
                                {
                                    string[] arrTimeOfUseB = timeOfUseB.TimeOfUse.Split(',');

                                    foreach (var item in arrTimeOfUseB)
                                    {
                                        Index_ValueModel vm = new Index_ValueModel();
                                        vm.IndexType = "DUP";
                                        vm.ElectricityMeterId = electricMeterB.ElectricityMeterId;
                                        vm.PointId = model.ServicePoint.PointId;
                                        vm.TimeOfUse = item;
                                        vm.ElectricityMeterCode = electricMeterB.ElectricityMeterCode;
                                        vm.ElectricityMeterNumber = electricMeterB.ElectricityMeterNumber;
                                        model.IndexValueB.Add(vm);
                                    }
                                }                                
                            }

                            #endregion
                            respone.Status = 1;
                            respone.Message = "Treo công tơ thành công.";
                            respone.Data = model;
                            return createResponse();                            
                        }

                        if (saveWork.Trim() == "Treo TI")
                        {
                            #region Tìm TI treo
                            //Tìm kiếm TI treo - điều kiện mã biến động = A
                            var currentTransformerB = db.EquipmentCT_CurrentTransformer.Where(item => item.DepartmentId == departmentId && item.CTCode == model.CTCode.Trim() && item.ActionCode != TreoThaoActionCode.TrenLuoi)
                                .Select(item => new EquipmentCT_OperationDetailModel()
                                {
                                    CurrentTransformerId = item.CurrentTransformerId,
                                    PointId = model.ServicePoint.PointId,
                                    CTCode = item.CTCode,
                                    CTNumber = item.CTNumber,
                                    TypeCode = item.Category_CurrentTransformerType.TypeCode,
                                    NumberOfPhases = item.Category_CurrentTransformerType.NumberOfPhases,
                                    ConnectionRatio = model.CTConnectionRatio
                                }).FirstOrDefault();

                            if (currentTransformerB != null)
                            {
                                model.CTCode = null;
                                model.CTConnectionRatio = null;

                                ModelState.Clear();
                                if (model.CTOperationDetailB == null)
                                {
                                    model.CTOperationDetailB = new List<EquipmentCT_OperationDetailModel>();
                                }
                                model.CTOperationDetailB.Add(currentTransformerB);
                            }
                            //Thông báo nếu không tìm thấy TI
                            else
                            {
                                throw new ArgumentException("Không tìm thấy TI.");                               
                            }
                            #endregion
                            respone.Status = 1;
                            respone.Message = "Treo TI thành công.";
                            respone.Data = model;
                            return createResponse();
                        }

                        if (saveWork.Trim() == "Treo TU")
                        {
                            #region Tìm TU treo
                            //Tìm kiếm TU treo - điều kiện mã biến động = A
                            var voltageTransformerB = db.EquipmentVT_VoltageTransformer.Where(item => item.DepartmentId == departmentId && item.VTCode == model.VTCode.Trim() && item.ActionCode != TreoThaoActionCode.TrenLuoi)
                                .Select(item => new EquipmentVT_OperationDetailModel()
                                {
                                    VoltageTransformerId = item.VoltageTransformerId,
                                    PointId = model.ServicePoint.PointId,
                                    VTCode = item.VTCode,
                                    VTNumber = item.VTNumber,
                                    TypeCode = item.Category_VoltageTransformerType.TypeCode,
                                    NumberOfPhases = item.Category_VoltageTransformerType.NumberOfPhases,
                                    ConnectionRatio = model.VTConnectionRatio
                                }).FirstOrDefault();

                            if (voltageTransformerB != null)
                            {
                                model.VTCode = null;
                                model.VTConnectionRatio = null;

                                ModelState.Clear();
                                if (model.VTOperationDetailB == null)
                                {
                                    model.VTOperationDetailB = new List<EquipmentVT_OperationDetailModel>();
                                }
                                model.VTOperationDetailB.Add(voltageTransformerB);
                            }
                            //Thông báo nếu không tìm thấy TU
                            else
                            {
                                throw new ArgumentException("Không tìm thấy TU.");                                
                            }
                            #endregion
                            respone.Status = 1;
                            respone.Message = "Treo TU thành công.";
                            respone.Data = model;
                            return createResponse();
                        }

                        if (saveWork.Trim() == "Cập nhật")
                        {
                            //Nếu là treo tháo định kỳ thì bắt buộc nhập công tơ treo
                            if (model.OperationDetailB == null)
                            {                                
                                dbContextTransaction.Rollback();
                                throw new ArgumentException("Chưa nhập thông tin công tơ treo.");
                            }

                            //Nếu là treo tháo lập trình lại công tơ gán thông tin công tơ treo = tháo
                            if (model.OperationType == LoaiTreoThao.LapTrinhLaiCto)
                            {
                                model.OperationDetailB.BOOCCode = model.OperationDetailE.BOOCCode;
                                model.OperationDetailB.BOOCQuantity = model.OperationDetailE.BOOCQuantity;
                                model.OperationDetailB.BoxAddress = model.OperationDetailE.BoxAddress;
                                model.OperationDetailB.BoxLeadCode = model.OperationDetailE.BoxLeadCode;
                                model.OperationDetailB.BoxLeadQuantity = model.OperationDetailE.BoxLeadQuantity;
                                model.OperationDetailB.CreateDate = model.OperationDetailE.CreateDate;
                                model.OperationDetailB.CreateUser = model.OperationDetailE.CreateUser;
                                model.OperationDetailB.ElectricityMeterCode = model.OperationDetailE.ElectricityMeterCode;
                                model.OperationDetailB.ElectricityMeterId = model.OperationDetailE.ElectricityMeterId;
                                model.OperationDetailB.ElectricityMeterNumber = model.OperationDetailE.ElectricityMeterNumber;
                                model.OperationDetailB.ElectricityMeterType = model.OperationDetailE.ElectricityMeterType;
                                model.OperationDetailB.FigureBookId = model.OperationDetailE.FigureBookId;
                                model.OperationDetailB.Id = model.OperationDetailE.Id;
                                model.OperationDetailB.Index = model.OperationDetailE.Index;
                                model.OperationDetailB.ManuFatureYear = model.OperationDetailE.ManuFatureYear;
                                model.OperationDetailB.NewValue = model.OperationDetailE.NewValue;
                                model.OperationDetailB.OldValue = model.OperationDetailE.OldValue;
                                model.OperationDetailB.OperationDate = model.OperationDetailE.OperationDate;
                                model.OperationDetailB.PointCode = model.OperationDetailE.PointCode;
                                model.OperationDetailB.PointId = model.OperationDetailE.PointId;
                                model.OperationDetailB.PositionId = model.OperationDetailE.PositionId;
                                model.OperationDetailB.ReportId = model.OperationDetailE.ReportId;
                                model.OperationDetailB.Status = model.OperationDetailE.Status;
                                model.OperationDetailB.TimeOfUse = model.OperationDetailE.TimeOfUse;
                                model.OperationDetailB.TimeOfUses = model.OperationDetailE.TimeOfUses;

                                //model.IndexValueB = model.IndexValueE;
                            }

                            //Bắt buộc nhập hệ số nhân
                            if (model.OperationDetailB.K_Multiplication == null && model.OperationType != LoaiTreoThao.ThaoThanhLy)
                            {                                                                
                                dbContextTransaction.Rollback();
                                throw new ArgumentException("Chưa nhập hệ số nhân.");
                            }

                            #region Save thông tin treo tháo công tơ

                            //Id sổ ghi chỉ số
                            var servicePoint = db.Concus_ServicePoint.Where(item => item.PointId.Equals(model.ServicePoint.PointId)).FirstOrDefault();
                            int figureBookId = servicePoint.FigureBookId;

                            //Check ngày treo tháo trong kỳ
                            DateTime operationDate = model.OperationReport.OperationDate.Value;
                            DateTime activeDateIndex;
                            try
                            {
                                activeDateIndex = (from c in db.Index_Value
                                                   where c.DepartmentId == departmentId && c.PointId == model.ServicePoint.PointId
                                                   select c).Max(c => c.EndDate);
                            }
                            catch (Exception)
                            {
                                activeDateIndex = servicePoint.ActiveDate;
                            }

                            var calendarOfSaveIndex = db.Index_CalendarOfSaveIndex.FirstOrDefault(item => item.FigureBookId == figureBookId
                               && DbFunctions.TruncateTime(item.StartDate) <= DbFunctions.TruncateTime(operationDate) && DbFunctions.TruncateTime(item.EndDate) >= DbFunctions.TruncateTime(operationDate)
                               && (item.Status == 1 || item.Status == 3));

                            //Nếu không tìm thấy ngày treo tháo thì thông báo
                            if (calendarOfSaveIndex == null)
                            {                                
                                dbContextTransaction.Rollback();
                                throw new ArgumentException($"Không tìm thấy lịch ghi chỉ số tương ứng với ngày treo tháo {operationDate.ToString("dd/MM/yyyy")} để thực hiện cập nhật, Vui lòng treo tháo trong kỳ lập lịch ghi chỉ số.");
                            }

                            //Ngày treo tháo không được lớn hơn ngày hiện tại
                            if (operationDate > DateTime.Now)
                            {                                
                                dbContextTransaction.Rollback();
                                throw new ArgumentException("Sai ngày treo tháo - phải lớn hơn ngày hiện tại.");
                            }


                            //Lưu thông tin biên bản
                            model.OperationReport.PointId = model.ServicePoint.PointId;
                            model.OperationReport.Status = true;//Gán mặc định là true
                            model.OperationReport.ReasonId = ReasonId;
                            decimal reportId = businessEquipmentMTOperationReport.AddEquipmentMT_OperationReport(model.OperationReport, db, departmentId, userId);

                            if (model.OperationDetailE != null)
                            {
                                //Lưu thông tin chi tiết công tơ tháo
                                model.OperationDetailE.PointId = model.ServicePoint.PointId;
                                model.OperationDetailE.Status = 0;//Gán mặc định là 1

                                businessEquipmentMTOperationDetail.AddEquipmentMT_OperationDetail(model.OperationDetailE, db, reportId, userId);

                                //Cập nhật trạng thái công tơ tháo thành E
                                businessEquipmentMTElectricityMeter.UpdateActionCode(model.OperationDetailE.ElectricityMeterId, TreoThaoActionCode.DuoiLuoi, db);

                                //Cập nhật trạng thái kiểm định thành 0 (chưa kiểm định) với TH treo tháo định kỳ
                                if (model.OperationType == LoaiTreoThao.DinhKy || model.OperationType == LoaiTreoThao.ThaoThanhLy)
                                    bussinessEquipmentMtTesting.UpdateStatus(model.OperationDetailE.ElectricityMeterId, 0, db);

                                //Xác định ngày đầu kỳ với trường hợp tháo
                                DateTime startDateDDN = new DateTime();
                                List<string> listIndexType = new List<string>(new string[] { "DDK", "DUP", "CSC" });
                                //lấy ngày chỉ số mới nhất
                                var MaxDeateIndex = db.Index_Value.Where(item => item.PointId == servicePoint.PointId && listIndexType.Contains(item.IndexType))
                                    .Select(o => o.EndDate).Max();

                                var listIndex = db.Index_Value.Where(item => item.PointId == servicePoint.PointId
                                        && listIndexType.Contains(item.IndexType) && item.EndDate == MaxDeateIndex).ToList();
                                if (listIndex != null && listIndex.Count > 0)
                                {
                                    startDateDDN = listIndex.FirstOrDefault().EndDate.AddDays(1);
                                }
                                else
                                {
                                    startDateDDN = (DateTime)calendarOfSaveIndex.StartDate;
                                }

                                //Lưu chỉ số tháo
                                if (model.IndexValueE != null)
                                {
                                    foreach (var item in model.IndexValueE)
                                    {
                                        //Kiểm tra chỉ số tháo
                                        if (item.NewValue == null || item.NewValue < item.OldValue)
                                        {                                            
                                            dbContextTransaction.Rollback();
                                            throw new ArgumentException("Sai chỉ số tháo. hoặc chỉ số tháo nhỏ hơn chỉ số cũ.");
                                        }
                                        item.DepartmentId = servicePoint.DepartmentId;
                                        item.PointId = servicePoint.PointId;
                                        if (servicePoint.Concus_Contract != null)
                                            item.CustomerId = servicePoint.Concus_Contract.CustomerId;
                                        //item.Coefficient = model.OperationDetailE.K_Multiplication;
                                        item.Term = calendarOfSaveIndex.Term;
                                        item.Month = calendarOfSaveIndex.Month;
                                        item.Year = calendarOfSaveIndex.Year;
                                        item.IndexType = "DDN";
                                        item.StartDate = startDateDDN;
                                        item.EndDate = operationDate;
                                        item.CreateUser = userId;
                                        businessIndexValue.AddIndex_Value(item, db);
                                    }
                                }

                            }

                            //Lưu thông tin chi tiết công tơ treo (4 = tháo thanh lý)
                            if (model.OperationType != LoaiTreoThao.ThaoThanhLy && model.IndexValueB != null)
                            {
                                model.OperationDetailB.PointId = model.ServicePoint.PointId;
                                model.OperationDetailB.Status = 1;//Gán mặc định là 1
                                businessEquipmentMTOperationDetail.AddEquipmentMT_OperationDetail(model.OperationDetailB, db, reportId, userId);

                                //Cập nhật trạng thái công tơ treo thành B
                                businessEquipmentMTElectricityMeter.UpdateActionCode(model.OperationDetailB.ElectricityMeterId, TreoThaoActionCode.TrenLuoi, db);

                                //Lưu chỉ số treo
                                foreach (var item in model.IndexValueB)
                                {
                                    item.DepartmentId = servicePoint.DepartmentId;
                                    item.PointId = servicePoint.PointId;
                                    if (servicePoint.Concus_Contract != null)
                                        item.CustomerId = servicePoint.Concus_Contract.Concus_Customer.CustomerId;
                                    item.Coefficient = model.OperationDetailB.K_Multiplication;
                                    item.Term = calendarOfSaveIndex.Term;
                                    item.Month = calendarOfSaveIndex.Month;
                                    item.Year = calendarOfSaveIndex.Year;
                                    item.IndexType = "DUP";
                                    item.OldValue = item.NewValue;
                                    item.StartDate = operationDate;
                                    item.EndDate = operationDate;
                                    item.CreateUser = userId;
                                    businessIndexValue.AddIndex_Value(item, db);
                                }
                            }
                            #endregion

                            #region Save thông tin treo tháo TI

                            //Lưu thông tin tháo TI
                            if (model.CTOperationDetailE != null && model.CTOperationDetailE.Count > 0)
                            {
                                foreach (var item in model.CTOperationDetailE)
                                {
                                    //Nếu tích chọn tháo -> save vào db trạng thái = 0
                                    if (item.IsEnd)
                                    {
                                        item.PointId = servicePoint.PointId;
                                        item.ReportId = reportId;
                                        item.Status = 0;
                                        item.CreateUser = userId;
                                        businessEquipmentCTOperationDetail.AddEquipmentCT_OperationDetail(item, db);

                                        //Cập nhật trạng thái TI thành E
                                        businessEquipmentCTCurrentTransformer.UpdateActionCode(item.CurrentTransformerId, TreoThaoActionCode.DuoiLuoi, db);

                                        //Cập nhật trạng thái kiểm định = 0 (chưa kiểm định)
                                        businessEquipmentCTTesting.UpdateStatus(item.CurrentTransformerId, 0, db);
                                    }                                    
                                }
                            }

                            //Lưu thông tin treo TI trạng thái = 1
                            if (model.CTOperationDetailB != null && model.CTOperationDetailB.Count > 0)
                            {
                                foreach (var item in model.CTOperationDetailB)
                                {
                                    item.PointId = servicePoint.PointId;
                                    item.ReportId = reportId;
                                    item.Status = 1;
                                    item.CreateUser = userId;
                                    businessEquipmentCTOperationDetail.AddEquipmentCT_OperationDetail(item, db);

                                    //Cập nhật trạng thái TI thành B
                                    businessEquipmentCTCurrentTransformer.UpdateActionCode(item.CurrentTransformerId, TreoThaoActionCode.TrenLuoi, db);
                                }
                            }
                            #endregion

                            #region Save thông tin treo tháo TU

                            //Lưu thông tin tháo TU
                            if (model.VTOperationDetailE != null && model.VTOperationDetailE.Count > 0)
                            {
                                foreach (var item in model.VTOperationDetailE)
                                {
                                    //Nếu tích chọn tháo -> save vào db trạng thái = 0
                                    if (item.IsEnd)
                                    {
                                        item.PointId = servicePoint.PointId;
                                        item.ReportId = reportId;
                                        item.Status = 0;
                                        item.CreateUser = userId;
                                        businessEquipmentVTOperationDetail.AddEquipmentVT_OperationDetail(item, db);

                                        //Cập nhật trạng thái TU thành E
                                        businessEquipmentVTVoltageTransformer.UpdateActionCode(item.VoltageTransformerId, TreoThaoActionCode.DuoiLuoi, db);

                                        //Cập nhật trạng thái kiểm định = 0 (chưa kiểm định)
                                        businessEquipmentVTTesting.UpdateStatus(item.VoltageTransformerId, 0, db);
                                    }                                    
                                }
                            }

                            //Lưu thông tin treo TU trạng thái = 1
                            if (model.VTOperationDetailB != null && model.VTOperationDetailB.Count > 0)
                            {
                                foreach (var item in model.VTOperationDetailB)
                                {
                                    item.PointId = servicePoint.PointId;
                                    item.ReportId = reportId;
                                    item.Status = 1;
                                    item.CreateUser = userId;
                                    businessEquipmentVTOperationDetail.AddEquipmentVT_OperationDetail(item, db);

                                    //Cập nhật trạng thái TU thành B
                                    businessEquipmentVTVoltageTransformer.UpdateActionCode(item.VoltageTransformerId, TreoThaoActionCode.TrenLuoi, db);
                                }
                            }
                            #endregion                            
                        }
                        dbContextTransaction.Commit();
                        respone.Status = 1;
                        respone.Message = "OK";
                        respone.Data = null;
                        return createResponse();
                    }
                    catch (Exception ex)
                    {
                        dbContextTransaction.Rollback();
                        respone.Status = 0;
                        respone.Message = $"{ex.Message.ToString()}";
                        respone.Data = null;
                        return createResponse();
                    }
                }
            }
        }


        #endregion


    }
}
