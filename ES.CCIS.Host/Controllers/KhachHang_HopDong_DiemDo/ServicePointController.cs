using CCIS_BusinessLogic;
using CCIS_DataAccess;
using ES.CCIS.Host.Helpers;
using ES.CCIS.Host.Models;
using ES.CCIS.Host.Models.EnumMethods;
using ES.CCIS.Host.Models.HopDong;
using Newtonsoft.Json;
using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Web.Configuration;
using System.Web.Http;

namespace ES.CCIS.Host.Controllers.KhachHang_HopDong_DiemDo
{
    [Authorize]
    [RoutePrefix("api/DiemDo")]
    public class ServicePointController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_Concus_ServicePoint business_Concus_ServicePoint = new Business_Concus_ServicePoint();

        #region Quản lý điểm đo
        [HttpGet]
        [Route("ServicePointManager")]
        public HttpResponseMessage ServicePointManager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search, [DefaultValue(0)] int? figureBookId, [DefaultValue(0)] int? regionId, [DefaultValue(0)] int departmentId)
        {
            try
            {
                //Thong tin user from token                
                var userInfo = TokenHelper.GetUserInfoFromRequest();
                if (departmentId == 0)
                    departmentId = TokenHelper.GetDepartmentIdFromToken();
                //list đơn vị con của đơn vị được search
                var listDepartments = DepartmentHelper.GetChildDepIds(departmentId);

                using (var db = new CCISContext())
                {
                    var searchByElectricityMeterNumberPointId = (from detailequi in db.EquipmentMT_OperationDetail
                                                                 join equi in db.EquipmentMT_ElectricityMeter on detailequi.ElectricityMeterId equals equi.ElectricityMeterId
                                                                 where equi.ElectricityMeterNumber == search
                                                                 select detailequi).OrderByDescending(i => i.DetailId).FirstOrDefault();

                    var pointID = searchByElectricityMeterNumberPointId == null ? 0 : searchByElectricityMeterNumberPointId.Status == 1 ? searchByElectricityMeterNumberPointId.PointId : 0;

                    var query = (from item in db.Concus_ServicePoint.Where(item => listDepartments.Contains(item.DepartmentId) && item.IsRootPoint == false)
                                 select new Concus_ServicePointModel
                                 {
                                     PointId = item.PointId,
                                     PointCode = item.PointCode,
                                     DepartmentId = item.DepartmentId,
                                     PotentialCode = item.PotentialCode,
                                     PotentialName = item.Category_Potential.PotentialName,
                                     Address = item.Address == null ? "" : item.Address,
                                     Status = item.Status,
                                     StationId = item.StationId,
                                     StationName = item.Category_Satiton.StationName,
                                     ServicePointType = item.ServicePointType,
                                     ContractId = item.ContractId,
                                     ContractCode = item.Concus_Contract.ContractCode,
                                     CustomerCode = item.Concus_Contract.Concus_Customer.CustomerCode,
                                     Check_Price = db.Concus_ImposedPrice.Where(a => a.PointId.Equals(item.PointId)).Select(a => a.PointId).Any() ? 1 : 0,
                                     CustomerName = item.Concus_Contract.Concus_Customer.Name,
                                     ElectricityMeterNumber = item.Status ? db.EquipmentMT_OperationDetail.Where(i => i.PointId == item.PointId && i.Status == 1)
                                                                .OrderByDescending(i => i.DetailId).FirstOrDefault().EquipmentMT_ElectricityMeter.ElectricityMeterNumber != null
                                                                ? db.EquipmentMT_OperationDetail.Where(i => i.PointId == item.PointId && i.Status == 1).OrderByDescending(i => i.DetailId)
                                                                .FirstOrDefault().EquipmentMT_ElectricityMeter.ElectricityMeterNumber : "" : ""
                                 });
                    if (figureBookId != 0)
                    {
                        query = (IQueryable<Concus_ServicePointModel>)query.Where(x => x.FigureBookId == figureBookId);
                    }

                    if (regionId != 0)
                    {
                        query = (IQueryable<Concus_ServicePointModel>)query.Where(x => x.RegionId == regionId);
                    }

                    if (!string.IsNullOrEmpty(search))
                    {
                        query = (IQueryable<Concus_ServicePointModel>)query.Where(item => item.PointCode.Contains(search) || item.ContractCode.Contains(search) || item.Address.Contains(search) || item.CustomerCode.Contains(search) || item.CustomerName.Contains(search) || item.PointId == pointID);
                    }

                    var pagedServicePoint = (IPagedList<Concus_ServicePointModel>)query.OrderBy(p => p.CustomerCode).ToPagedList(pageNumber, pageSize);

                    var response = new
                    {
                        pagedServicePoint.PageNumber,
                        pagedServicePoint.PageSize,
                        pagedServicePoint.TotalItemCount,
                        pagedServicePoint.PageCount,
                        pagedServicePoint.HasNextPage,
                        pagedServicePoint.HasPreviousPage,
                        ServicePoint = pagedServicePoint.ToList()
                    };
                    respone.Status = 1;
                    respone.Message = "Lấy danh sách điểm đo thành công.";
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

        [HttpGet]
        [Route("")]
        public HttpResponseMessage GetServicePointById(int servicePointId)
        {
            try
            {
                if (servicePointId < 0 || servicePointId == 0)
                {
                    throw new ArgumentException($"ServicePointId {servicePointId} không hợp lệ.");
                }
                using (var dbContext = new CCISContext())
                {
                    var route = dbContext.Concus_ServicePoint.Where(p => p.ServicePointType == servicePointId).Select(item => new Concus_ServicePointModel
                    {
                        PointId = item.PointId,
                        PointCode = item.PointCode,
                        DepartmentId = item.DepartmentId,
                        ContractId = item.ContractId,
                        Address = item.Address,
                        PotentialCode = item.PotentialCode,
                        ReactivePower = item.ReactivePower,
                        Power = item.Power,
                        NumberOfPhases = item.NumberOfPhases,
                        ActiveDate = item.ActiveDate,
                        Status = item.Status,
                        CreateDate = item.CreateDate,
                        CreateUser = item.CreateUser,
                        HouseholdNumber = item.HouseholdNumber,
                        StationId = item.StationId,
                        RouteId = item.RouteId,
                        TeamId = item.TeamId,
                        BoxNumber = item.BoxNumber,

                        PillarNumber = item.PillarNumber,
                        FigureBookId = item.FigureBookId,
                        Index = item.Index,
                        ServicePointType = item.ServicePointType,
                        GroupReactivePower = item.GroupReactivePower,
                        PrimaryPointId = item.PrimaryPointId,
                        RegionId = item.RegionId,
                    });

                    if (route?.Any() == true)
                    {
                        var response = route.FirstOrDefault();
                        if (response.Status)
                        {
                            respone.Status = 1;
                            respone.Message = "Lấy thông tin điểm đo thành công.";
                            respone.Data = response;
                            return createResponse();
                        }
                        else
                        {
                            throw new ArgumentException($"Điểm đo {response.ContractCode} đã bị vô hiệu.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"Điểm đo có ServicePointId {servicePointId} không tồn tại.");
                    }
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
        [Route("")]
        public HttpResponseMessage AddConcus_ServicePoint(Concus_ServicePointModel model)
        {
            try
            {
                var userId = TokenHelper.GetUserIdFromToken();
                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                if (business_Concus_ServicePoint.CheckExistPointCode(model.PointCode))
                {
                    throw new ArgumentException("Mã điểm đo đã tồn tại.");
                }
                else
                {
                    model.DepartmentId = departmentId;
                    model.CreateUser = userId;
                    try
                    {
                        using (var db = new CCISContext())
                        {
                            var ContractInfor = db.Concus_Contract.Where(item => item.ContractId.Equals(model.ContractId)).FirstOrDefault();
                            model.ActiveDate = ContractInfor.ActiveDate;
                        }
                    }
                    catch
                    {
                        model.ActiveDate = DateTime.Now;
                    }
                    model.CreateDate = DateTime.Now;
                    model.IsRootPoint = false;
                    business_Concus_ServicePoint.AddConcus_ServicePoint(model);

                    using (var dbContext = new CCISContext())
                    {
                        var diemDo = dbContext.Concus_ServicePoint.Where(p => p.PointCode == model.ContractCode).FirstOrDefault();
                        if (diemDo != null)
                        {
                            respone.Status = 1;
                            respone.Message = "Thêm mới điểm đo thành công.";
                            respone.Data = diemDo.PointId;
                            return createResponse();
                        }
                        else
                        {
                            respone.Status = 0;
                            respone.Message = "Thêm mới điểm đo không thành công.";
                            respone.Data = null;
                            return createResponse();
                        }
                    }
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
        [Route("AddOrEditExtendedInfo")]
        public HttpResponseMessage AddOrEditExtendedInfo(ContractExtendedInfoModel req)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    var contractExtendedInfo = db.AdditionInfos.Where(x => x.ObjectId == req.ContractId && x.ObjectCode == VSIPKeys.ObjectCode).ToList();
                    if (contractExtendedInfo.Count > 0)
                    {
                        bool isExist = false; // Biến này dùng để check xem danh sách có điểm đo đấy chưa nếu chưa có thì thêm mới vào
                        contractExtendedInfo.ForEach(x =>
                        {
                            var item = JsonConvert.DeserializeObject<VsipContractExtend>(x.Value);
                            if (item.PointId == req.PointId)
                            {
                                isExist = true;
                                x.Value = JsonConvert.SerializeObject(req.Data);
                            }
                        });
                        if (!isExist)
                        {
                            db.AdditionInfos.Add(new AdditionInfos
                            {
                                FieldCode = VSIPKeys.FieldCode,
                                ObjectCode = VSIPKeys.ObjectCode,
                                ObjectId = req.ContractId,
                                Value = JsonConvert.SerializeObject(req.Data)
                            });
                        }
                    }
                    else
                    {
                        db.AdditionInfos.Add(new AdditionInfos
                        {
                            FieldCode = VSIPKeys.FieldCode,
                            ObjectCode = VSIPKeys.ObjectCode,
                            ObjectId = req.ContractId,
                            Value = JsonConvert.SerializeObject(req.Data)
                        });
                    }
                    db.SaveChanges();
                }

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = req.PointId;
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
        [Route("EditServicePoint")]
        public HttpResponseMessage EditServicePoint(Concus_ServicePointModel model)
        {
            try
            {
                Business_Concus_ServicePoint business = new Business_Concus_ServicePoint();
                if (business.CheckExistPointCode_Edit(model.PointCode, model.PointId))
                {
                    model.IsRootPoint = false;
                    business.EditConcus_ServicePoint(model);

                    respone.Status = 1;
                    respone.Message = "Chỉnh sửa điểm đo thành công.";
                    respone.Data = model.PointId;
                    return createResponse();
                }
                else
                {
                    throw new ArgumentException("Chỉnh sửa điểm đo không thành công, mã điểm đo đã tồn tại.");
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
        [Route("DeleteServicePoint")]
        public HttpResponseMessage DeleteServicePoint(int pointId)
        {
            try
            {
                bool isRootPoint = false;
                using (var db = new CCISContext())
                {
                    var target = db.Concus_ServicePoint.Where(item => item.PointId == pointId).FirstOrDefault();
                    isRootPoint = target.IsRootPoint;
                    if (target != null)
                    {
                        var index = db.Index_Value.Where(item => item.PointId == pointId).OrderByDescending(r => r.IndexId).FirstOrDefault();
                        if (index != null)
                        {
                            if (index.IndexType != "DDN")
                            {
                                throw new ArgumentException("Điểm đo đang treo công tơ, không thể thanh lý!");
                            }
                            else
                            {
                                target.Status = false;
                                db.SaveChanges();

                                respone.Status = 1;
                                respone.Message = "Thanh lý điểm đo thành công.";
                                respone.Data = null;
                                return createResponse();
                            }
                        }
                        else
                        {
                            target.Status = false;
                            db.SaveChanges();
                            respone.Status = 1;
                            respone.Message = "Thanh lý điểm đo thành công.";
                            respone.Data = null;
                            return createResponse();
                        }

                    }
                    else
                    {
                        throw new ArgumentException("Điểm đo không thể thanh lý!");
                    }
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
        #endregion

        #region Quản lý điểm đo đầu nguồn
        [HttpGet]
        [Route("RootServicePointManager")]
        public HttpResponseMessage RootServicePointManager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search, [DefaultValue(0)] int? figureBookId, [DefaultValue(0)] int departmentId)
        {
            try
            {
                if (departmentId == 0)
                    departmentId = TokenHelper.GetDepartmentIdFromToken();
                //list đơn vị con của đơn vị được search
                var listDepartments = DepartmentHelper.GetChildDepIds(departmentId);

                using (var db = new CCISContext())
                {
                    var query = (from item in db.Concus_ServicePoint.Where(item => listDepartments.Contains(item.DepartmentId) && item.IsRootPoint == true)
                                 select new Concus_ServicePointModel
                                 {
                                     PointId = item.PointId,
                                     PointCode = item.PointCode,
                                     DepartmentId = item.DepartmentId,
                                     PotentialCode = item.PotentialCode,
                                     PotentialName = item.Category_Potential.PotentialName,
                                     Address = item.Address == null ? "" : item.Address,
                                     Status = item.Status,
                                     StationId = item.StationId,
                                     StationName = item.Category_Satiton.StationName,
                                     ServicePointType = item.ServicePointType,
                                     ContractId = item.ContractId,
                                     ContractCode = db.Concus_Contract.Where(a => a.ContractId.Equals(item.ContractId)).Select(a => a.ContractCode).FirstOrDefault(),
                                     CustomerCode = "",
                                     Check_Price = db.Concus_ImposedPrice.Where(a => a.PointId.Equals(item.PointId)).Select(a => a.PointId).Any() ? 1 : 0,
                                     CustomerName = "",
                                     ElectricityMeterNumber = db.EquipmentMT_OperationDetail.Where(i => i.PointId == item.PointId && i.Status == 1)
                                                                .OrderByDescending(i => i.DetailId).FirstOrDefault().EquipmentMT_ElectricityMeter.ElectricityMeterNumber != null
                                                                ? db.EquipmentMT_OperationDetail.Where(i => i.PointId == item.PointId && i.Status == 1).OrderByDescending(i => i.DetailId)
                                                                .FirstOrDefault().EquipmentMT_ElectricityMeter.ElectricityMeterNumber : ""
                                 });

                    if (figureBookId != 0)
                    {
                        query = (IQueryable<Concus_ServicePointModel>)query.Where(item => item.FigureBookId == figureBookId);
                    }

                    if (!string.IsNullOrEmpty(search))
                    {
                        query = (IQueryable<Concus_ServicePointModel>)query.Where(item => item.PointCode.Contains(search));
                    }

                    var paged = (IPagedList<Concus_ServicePointModel>)query.OrderBy(p => p.ContractId).ToPagedList(pageNumber, pageSize);

                    var response = new
                    {
                        paged.PageNumber,
                        paged.PageSize,
                        paged.TotalItemCount,
                        paged.PageCount,
                        paged.HasNextPage,
                        paged.HasPreviousPage,
                        ServicePoint = paged.ToList()
                    };
                    respone.Status = 1;
                    respone.Message = "Lấy danh sách điểm đo thành công.";
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

        [HttpPost]
        [Route("AddRootServicePoint")]
        public HttpResponseMessage AddRootServicePoint(Concus_ServicePointModel model)
        {
            try
            {
                var userId = TokenHelper.GetUserIdFromToken();
                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                if (business_Concus_ServicePoint.CheckExistPointCode(model.PointCode))
                {
                    throw new ArgumentException("Mã điểm đo đã tồn tại.");                    
                }
                else
                {
                    model.DepartmentId = departmentId;
                    model.CreateUser = userId;
                    if (model.ContractId > 0)
                    {
                        using (var db = new CCISContext())
                        {
                            var contract = db.Concus_Contract.Where(o => o.DepartmentId == departmentId && o.ContractId == model.ContractId).FirstOrDefault();
                            model.ActiveDate = contract.ActiveDate;
                        }
                    }
                    else
                    {
                        model.ActiveDate = DateTime.Now.AddMonths(-3);
                    }
                    model.CreateDate = DateTime.Now;
                    model.IsRootPoint = true;
                    business_Concus_ServicePoint.AddConcus_ServicePoint(model);

                    using (var dbContext = new CCISContext())
                    {
                        var diemDo = dbContext.Concus_ServicePoint.Where(p => p.PointCode == model.ContractCode).FirstOrDefault();
                        if (diemDo != null)
                        {
                            respone.Status = 1;
                            respone.Message = "Thêm mới điểm đo thành công.";
                            respone.Data = diemDo.PointId;
                            return createResponse();
                        }
                        else
                        {
                            respone.Status = 0;
                            respone.Message = "Thêm mới điểm đo không thành công.";
                            respone.Data = null;
                            return createResponse();
                        }
                    }                    
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
        [Route("EditRootServicePoint")]
        public HttpResponseMessage EditRootServicePoint(Concus_ServicePointModel model)
        {
            try
            {
                if (business_Concus_ServicePoint.CheckExistPointCode_Edit(model.PointCode, model.PointId))
                {
                    model.IsRootPoint = true;
                    business_Concus_ServicePoint.EditConcus_ServicePoint(model);

                    respone.Status = 1;
                    respone.Message = "Chỉnh sửa điểm đo thành công.";
                    respone.Data = model.PointId;
                    return createResponse();                    
                }
                else
                {
                    throw new ArgumentException("Chỉnh sửa điểm đo không thành công, mã điểm đo đã tồn tại.");                    
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
        [Route("GetData_Edit_ValueJS")]
        public HttpResponseMessage GetData_Edit_ValueJS([DefaultValue(0)] int figureBookId)
        {
            try
            {
                var model = new AddConcus_ServicePointJSModel();
                List<Concus_ServicePointModelDTO> list = new List<Concus_ServicePointModelDTO>();

                using (var db = new CCISContext())
                {
                    if (figureBookId == 0)
                        figureBookId = db.Category_FigureBook.Select(item => item.FigureBookId).FirstOrDefault();

                    model.HandOnTableHeader = new List<string> { "Mã điểm đo", "Địa chỉ", "Loại điểm đo", "Cấp điện áp", "Công suất", "Số pha", "Trạm", "Sổ GCS", "Số hộ", "STT sổ GCS", "Số cột", "Khu vực" };

                    bool isRootBook = db.Category_FigureBook.Where(item => item.FigureBookId == figureBookId).FirstOrDefault().IsRootBook;

                    var departmentId = TokenHelper.GetDepartmentIdFromToken();
                    // danh sách điểm đo lấy theo sổ ghi chỉ số, ứng với 
                    // lấy ra danh sash điểm đo
                    if (isRootBook)
                    {
                        list = db.Concus_ServicePoint.OrderBy(item => item.Index).Where(item => item.FigureBookId.Equals(figureBookId) && item.Status == true).Select(item => new Concus_ServicePointModelDTO
                        {
                            ServicePointType = item.ServicePointType,
                            PointCode = item.PointCode,
                            StationId = item.StationId,
                            PotentialCode = item.PotentialCode,
                            Index = item.Index,
                            NumberOfPhases = item.NumberOfPhases,
                            PillarNumber = item.PillarNumber,
                            HouseholdNumber = item.HouseholdNumber,
                            Power = item.Power,
                            RegionId = item.RegionId,
                            Address = item.Address

                        }).OrderBy(item => item.Address).ToList();
                    }
                    else
                    {
                        list = db.Concus_ServicePoint.OrderBy(item => item.Index).Where(item => item.FigureBookId.Equals(figureBookId) && item.Status == true).Select(item => new Concus_ServicePointModelDTO
                        {
                            ServicePointType = item.ServicePointType,
                            PointCode = item.PointCode,
                            StationId = item.StationId,
                            PotentialCode = item.PotentialCode,
                            Index = item.Index,
                            NumberOfPhases = item.NumberOfPhases,
                            PillarNumber = item.PillarNumber,
                            HouseholdNumber = item.HouseholdNumber,
                            Power = item.Power,
                            RegionId = item.RegionId,
                            Address = item.Address
                        }).OrderBy(item => item.Address).ToList();
                    }
                    // check trang thai hien thi form = 1 với 3 thì shown lên
                    //list.OrderBy(item => item.Index).ThenBy(item => item.PointId);
                    model.HandOnTableObject = list;

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

        //Còn thiếu api SaveConcus_ServicePointJS và api ExcelExport
        #endregion
    }
}
