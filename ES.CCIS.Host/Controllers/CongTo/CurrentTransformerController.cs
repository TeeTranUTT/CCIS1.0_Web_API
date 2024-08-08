using CCIS_BusinessLogic;
using CCIS_DataAccess;
using CCIS_DataAccess.ViewModels;
using ES.CCIS.Host.Helpers;
using ES.CCIS.Host.Models;
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
    [RoutePrefix("api/CurrentTransformer")]
    public class CurrentTransformerController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_EquipmentCT_CurrentTransformer business_EquipmentCT_CurrentTransformer = new Business_EquipmentCT_CurrentTransformer();
        private readonly Business_Administrator_Department administratorDepartment = new Business_Administrator_Department();
        private readonly Business_EquipmentCT_Testing equipmentCTTesting = new Business_EquipmentCT_Testing();
        private readonly Business_EquipmentCT_Testing_Log equipmentCTTestingLog = new Business_EquipmentCT_Testing_Log();
        private readonly CCISContext _dbContext;

        public CurrentTransformerController()
        {
            _dbContext = new CCISContext();
        }

        [HttpGet]
        [Route("CurrentTransformer_StockDetail_Testing")]
        public HttpResponseMessage CurrentTransformer_StockDetail_Testing([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search)
        {
            try
            {
                var query = _dbContext.EquipmentCT_CurrentTransformer
                    .Select(item => new EquipmentCT_CurrentTransformerModel
                    {
                        CurrentTransformerId = item.CurrentTransformerId,
                        DepartmentId = item.DepartmentId,
                        CTCode = item.CTCode,
                        CTNumber = item.CTNumber,
                        CTTypeId = item.CTTypeId,
                        Possesive = item.Possesive,
                        StockId = item.StockId,
                        ManufactureYear = item.ManufactureYear,
                        ActionCode = item.ActionCode,
                        ActionDate = item.ActionDate,
                        CreateDate = item.CreateDate,
                        CreateUser = item.CreateUser,
                        ReasonId = item.ReasonId,
                    });

                if (!string.IsNullOrEmpty(search))
                {
                    query = (IQueryable<EquipmentCT_CurrentTransformerModel>)query.Where(item => item.CTCode.Contains(search) || item.CTNumber.Contains(search));
                }

                var paged = (IPagedList<EquipmentCT_CurrentTransformerModel>)query.OrderBy(p => p.CurrentTransformerId).ToPagedList(pageNumber, pageSize);
                var response = new
                {
                    paged.PageNumber,
                    paged.PageSize,
                    paged.TotalItemCount,
                    paged.PageCount,
                    paged.HasNextPage,
                    paged.HasPreviousPage,
                    EquipmentCT_CurrentTransformers = paged.ToList()
                };

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = response;
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

        #region Quản lý TI
        [HttpGet]
        [Route("EquipmentCT_CurrentTransformerManager")]
        public HttpResponseMessage EquipmentCT_CurrentTransformerManager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search, [DefaultValue("")] string ActionCode)
        {
            try
            {
                var userInfo = TokenHelper.GetUserInfoFromRequest();

                var lstDepartmentIds = DepartmentHelper.GetChildDepIdsByUser(userInfo.UserName);

                var query = _dbContext.EquipmentCT_CurrentTransformer.Where(item => lstDepartmentIds.Contains(item.DepartmentId))
                    .Select(item => new EquipmentCT_CurrentTransformerModel
                    {
                        CurrentTransformerId = item.CurrentTransformerId,
                        CTCode = item.CTCode,
                        CTNumber = item.CTNumber,
                        TypeCode = item.Category_CurrentTransformerType.TypeCode,
                        TypeName = item.Category_CurrentTransformerType.TypeName,
                        CreateDate = item.CreateDate,
                        ActionCode = item.ActionCode,
                        TestingStatus = _dbContext.EquipmentCT_Testing.Where(i => i.CurrentTransformerId == item.CurrentTransformerId).Select(i => i.Status).FirstOrDefault()
                    });

                if (!string.IsNullOrEmpty(search))
                {
                    query = (IQueryable<EquipmentCT_CurrentTransformerModel>)query.Where(item => item.CTCode.Contains(search) || item.CTNumber.Contains(search));
                }

                if (!string.IsNullOrEmpty(ActionCode))
                {
                    query = (IQueryable<EquipmentCT_CurrentTransformerModel>)query.Where(item => item.ActionCode.Contains(ActionCode));
                }

                var paged = (IPagedList<EquipmentCT_CurrentTransformerModel>)query.OrderBy(p => p.CreateDate).ToPagedList(pageNumber, pageSize);

                var response = new
                {
                    paged.PageNumber,
                    paged.PageSize,
                    paged.TotalItemCount,
                    paged.PageCount,
                    paged.HasNextPage,
                    paged.HasPreviousPage,
                    EquipmentCTCurrentTransformer = paged.ToList()
                };
                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = response;
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

        // cap nhat ly do thanh ly
        [HttpPost]
        [Route("LiquidationTI")]
        public HttpResponseMessage LiquidationTI(string CurrentTransformerTI, string ReasonId)
        {
            try
            {
                business_EquipmentCT_CurrentTransformer.UpdateLiquidationTI(CurrentTransformerTI, ReasonId, _dbContext);

                respone.Status = 1;
                respone.Message = "Cập nhật lý do thanh lý thành công.";
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

        [HttpPost]
        [Route("AddEquipmentCT_CurrentTransformer")]
        public HttpResponseMessage AddEquipmentCT_CurrentTransformer(EquipmentCT_Transformer_StockReport_Testing model)
        {

            try
            {
                using (var _dbContextContextTransaction = _dbContext.Database.BeginTransaction())
                {
                    try
                    {
                        //Kiểm tra mã TI trước khi thêm mới
                        if (business_EquipmentCT_CurrentTransformer.CheckExistCurrentTransformerCode(
                                model.CurrentTransformerModel.CTTypeId, model.CurrentTransformerModel.ManufactureYear,
                                model.CurrentTransformerModel.CTNumber))
                        {
                            throw new ArgumentException("Mã TI đã tồn tại.");
                        }

                        //EquipmentCT_CurrentTransformer
                        int userId = administratorDepartment.UserId(User.Identity.Name);
                        model.CurrentTransformerModel.CreateUser = userId;
                        model.CurrentTransformerModel.CreateDate = DateTime.Now;
                        model.CurrentTransformerModel.DepartmentId = administratorDepartment.GetIddv(User.Identity.Name);

                        var typeCode = _dbContext.Category_CurrentTransformerType.Where(item => item.CTTypeId == model.CurrentTransformerModel.CTTypeId).FirstOrDefault();
                        model.CurrentTransformerModel.CTCode = typeCode.TypeCode +
                                                               model.CurrentTransformerModel.ManufactureYear +
                                                               model.CurrentTransformerModel.CTNumber;
                        //Mã biến động gán mặc định là A
                        model.CurrentTransformerModel.ActionCode = "A";

                        model.CurrentTransformerModel.TestingDate = model.EquipmentCtTesting.TestingDate;
                        model.CurrentTransformerModel.EndTestingDate = model.EquipmentCtTesting.TestingDate.Value.AddMonths(typeCode.TestingDay);
                        model.CurrentTransformerModel.EndTestingDate = new DateTime(model.CurrentTransformerModel.EndTestingDate.Value.Year, model.CurrentTransformerModel.EndTestingDate.Value.Month, 1);

                        int currentTransformerId =
                            business_EquipmentCT_CurrentTransformer.AddEquipmentCT_CurrentTransformer(model.CurrentTransformerModel, _dbContext);
                        //Lấy ra CurrentTransformerId insert vào bảng  EquipmentCT_Testing

                        model.EquipmentCtTesting.CurrentTransformerId = currentTransformerId; // Id TI
                        model.EquipmentCtTesting.CreateDate = DateTime.Now;
                        model.EquipmentCtTesting.CreateUser = userId;

                        equipmentCTTesting.AddEquipmentCT_Testing(model.EquipmentCtTesting, _dbContext);

                        _dbContext.SaveChanges();
                        _dbContextContextTransaction.Commit();

                        respone.Status = 1;
                        respone.Message = "Thêm mới TI thành công.";
                        respone.Data = null;
                        return createResponse();
                    }
                    catch
                    {
                        _dbContextContextTransaction.Rollback();
                        throw new ArgumentException("Thêm mới TI không thành công.");
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

        [HttpPost]
        [Route("EditEquipmentCT_Testing")]
        public HttpResponseMessage EditEquipmentCT_Testing(EquipmentCT_Transformer_StockReport_Testing model)
        {
            try
            {
                var userId = TokenHelper.GetUserIdFromToken();

                using (var _dbContextContextTransaction = _dbContext.Database.BeginTransaction())
                {
                    try
                    {
                        model.EquipmentCtTesting.CurrentTransformerId = model.CurrentTransformerModel.CurrentTransformerId;
                        model.EquipmentCtTesting.CreateUser = userId;
                        if (model.EquipmentCtTesting.TestingStatus)
                            model.EquipmentCtTesting.Status = 1;
                        if (!model.EquipmentCtTesting.TestingStatus)
                            model.EquipmentCtTesting.Status = 2;

                        equipmentCTTesting.EditEquipmentCT_Testing(model.EquipmentCtTesting, _dbContext);
                        _dbContextContextTransaction.Commit();

                        respone.Status = 1;
                        respone.Message = "Kiểm định TI thành công.";
                        respone.Data = model.EquipmentCtTesting.CurrentTransformerId;
                        return createResponse();
                    }
                    catch
                    {
                        _dbContextContextTransaction.Rollback();
                        throw new ArgumentException("Kiểm định TI không thành công.");
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
        #endregion

        #region Chuẩn hóa thông tin TI
        [HttpGet]
        [Route("EditCurrentTransformer")]
        public HttpResponseMessage EditCurrentTransformer(int editing)
        {
            try
            {
                EquipmentCT_Transformer_StockReport_Testing model = new EquipmentCT_Transformer_StockReport_Testing();
                //Lấy thông tin TI
                var currentTransformer = _dbContext.EquipmentCT_CurrentTransformer.Where(item => item.CurrentTransformerId.Equals(editing))
                    .Select(item => new EquipmentCT_CurrentTransformerModel()
                    {
                        CurrentTransformerId = item.CurrentTransformerId,
                        Possesive = item.Possesive,
                        CTNumber = item.CTNumber,
                        CTCode = item.CTCode,
                        ManufactureYear = item.ManufactureYear,
                        CTTypeId = item.CTTypeId,
                        TypeName = item.Category_CurrentTransformerType.TypeName,
                        ActionCode = item.ActionCode
                    }).FirstOrDefault();

                model.CurrentTransformerModel = currentTransformer;

                //Kiểm tra tình trạng kiểm định
                var ds = _dbContext.EquipmentCT_Testing.Where(item => item.CurrentTransformerId.Equals(editing)).Select(item => new EquipmentCT_CurrentTransformerModel()
                {
                    TestingStatus = item.Status,
                    TestingDate = item.TestingDate
                }).OrderByDescending(item => item.TestingDate).FirstOrDefault();
                // kiểm tra tình trạng chất lượng kiểm định
                if (ds.TestingStatus == 2 || ds.TestingStatus == 0)
                {
                    var currentTransformerTesting = _dbContext.EquipmentCT_Testing.Where(item => item.CurrentTransformerId.Equals(editing)).Select(item => new EquipmentCT_TestingModel()
                    {
                        CurrentTransformerId = item.CurrentTransformerId,
                        ReportNumber = item.ReportNumber,
                        TestingDate = item.TestingDate,
                        PliersCode = item.PliersCode, // mã kìm
                        LeadQuantity = item.LeadQuantity, // số viên chì
                        VignetteCode = item.VignetteCode, // mã tem
                        Serial = item.Serial, // seri
                        TestingStatus = false,
                        TestingEmployee = item.TestingEmployee // nhan vien kiem dinh
                    }).OrderByDescending(item => item.TestingDate).FirstOrDefault();
                    model.EquipmentCtTesting = currentTransformerTesting;
                }
                else
                {
                    var currentTransformerTesting = _dbContext.EquipmentCT_Testing.Where(item => item.CurrentTransformerId.Equals(editing)).Select(item => new EquipmentCT_TestingModel()
                    {
                        CurrentTransformerId = item.CurrentTransformerId,
                        ReportNumber = item.ReportNumber,
                        TestingDate = item.TestingDate,
                        PliersCode = item.PliersCode, // mã kìm
                        LeadQuantity = item.LeadQuantity, // số viên chì
                        VignetteCode = item.VignetteCode, // mã tem
                        Serial = item.Serial, // seri
                        TestingStatus = true,
                        TestingEmployee = item.TestingEmployee // nhan vien kiem dinh
                    }).OrderByDescending(item => item.TestingDate).FirstOrDefault();
                    model.EquipmentCtTesting = currentTransformerTesting;
                }

                respone.Status = 1;
                respone.Message = "Lấy thông tin TI thành công.";
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
        [HttpPost]
        [Route("EditCurrentTransformer")]
        public HttpResponseMessage EditCurrentTransformer(EquipmentCT_Transformer_StockReport_Testing model, int possesiveId)
        {
            try
            {
                using (var _dbContextContextTransaction = _dbContext.Database.BeginTransaction())
                {
                    //kiểm tra mã Ti
                    if (business_EquipmentCT_CurrentTransformer.CheckExistCurrentTransformerIdTi(model.CurrentTransformerModel.CTTypeId, model.CurrentTransformerModel.ManufactureYear, model.CurrentTransformerModel.CTNumber, model.CurrentTransformerModel.CurrentTransformerId))
                    {
                        throw new ArgumentException("Mã TI đã tồn tại.");
                    }

                    try
                    {
                        //thông tin Ti
                        model.CurrentTransformerModel.Possesive = possesiveId; // gán giá trị mới cho sở hữu
                        var typeCode =
                            _dbContext.Category_CurrentTransformerType.Where(
                                item => item.CTTypeId == model.CurrentTransformerModel.CTTypeId)
                                //.Select(item => item.TypeCode)
                                .FirstOrDefault(); // mã chủng loại
                        model.CurrentTransformerModel.CTCode = typeCode.TypeCode +
                                                               model.CurrentTransformerModel.ManufactureYear +
                                                               model.CurrentTransformerModel.CTNumber; // mã Ti
                        model.CurrentTransformerModel.TestingDate = model.EquipmentCtTesting.TestingDate;
                        model.CurrentTransformerModel.EndTestingDate = model.CurrentTransformerModel.TestingDate.Value.AddMonths(typeCode.TestingDay);
                        model.CurrentTransformerModel.EndTestingDate = new DateTime(model.CurrentTransformerModel.EndTestingDate.Value.Year, model.CurrentTransformerModel.EndTestingDate.Value.Month, 1);
                        business_EquipmentCT_CurrentTransformer.EditEquipmentCT_CurrentTransformer(model.CurrentTransformerModel, _dbContext);
                        //thông tin kiểm định
                        model.EquipmentCtTesting.CurrentTransformerId = model.CurrentTransformerModel.CurrentTransformerId;
                        if (model.EquipmentCtTesting.TestingStatus)
                            model.EquipmentCtTesting.Status = 1;
                        if (!model.EquipmentCtTesting.TestingStatus)
                            model.EquipmentCtTesting.Status = 2;
                        //chuẩn hóa thông tin kiểm định
                        equipmentCTTesting.EditEquipmentCT_TestingTi(model.EquipmentCtTesting, _dbContext);

                        _dbContext.SaveChanges();
                        _dbContextContextTransaction.Commit();

                        respone.Status = 1;
                        respone.Message = "Chuẩn hóa thông tin TI thành công.";
                        respone.Data = model.EquipmentCtTesting.CurrentTransformerId;
                        return createResponse();
                    }
                    catch
                    {
                        _dbContextContextTransaction.Rollback();
                        throw new ArgumentException("Chuẩn hóa thông tin TI thất bại.");
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
        #endregion


        #region Thanh lý TI
        [HttpPost]
        [Route("DeleteTI")]
        public HttpResponseMessage DeleteTI(int currentTransformerId)
        {
            try
            {
                var target = _dbContext.EquipmentCT_CurrentTransformer.Where(item => item.CurrentTransformerId == currentTransformerId).FirstOrDefault();

                var testingEquipment = _dbContext.EquipmentCT_Testing.Where(item => item.CurrentTransformerId == currentTransformerId).ToList().LastOrDefault();
                if (target != null)
                {
                    if (target.ActionCode == "B")
                    {
                        throw new ArgumentException("TI đang treo, không thể thanh lý!");
                    }
                    else
                    {
                        target.ActionCode = "F";
                        testingEquipment.Status = 2;
                        _dbContext.SaveChanges();

                        respone.Status = 1;
                        respone.Message = "Thanh lý TI thành công.";
                        respone.Data = null;
                        return createResponse();
                    }
                }
                else
                {
                    throw new ArgumentException($"Không có TI: {target.CTNumber}");
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
        #endregion
    }
}
