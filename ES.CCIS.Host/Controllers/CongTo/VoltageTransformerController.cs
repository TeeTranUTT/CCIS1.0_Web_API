using CCIS_BusinessLogic;
using CCIS_DataAccess;
using CCIS_DataAccess.ViewModels;
using ES.CCIS.Host.Helpers;
using PagedList;
using System;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Web.Configuration;
using System.Web.Http;

namespace ES.CCIS.Host.Controllers.CongTo
{
    [Authorize]
    [RoutePrefix("api/VoltageTransformer")]
    public class VoltageTransformerController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly CCISContext _dbContext;
        private readonly Business_EquipmentVT_VoltageTransformer business_EquipmentVT_VoltageTransformer = new Business_EquipmentVT_VoltageTransformer();
        private readonly Business_EquipmentVT_Testing quipmentVtTesting = new Business_EquipmentVT_Testing();
        private readonly Business_EquipmentVT_Testing_Log quipmentVtTestingLog = new Business_EquipmentVT_Testing_Log();

        public VoltageTransformerController()
        {
            _dbContext = new CCISContext();
        }

        [HttpGet]
        [Route("VoltageTransformer_Manager")]
        public HttpResponseMessage VoltageTransformer_Manager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search, [DefaultValue("")] string ActionCode)
        {
            try
            {
                var query = _dbContext.EquipmentVT_VoltageTransformer.Select(item => new EquipmentVT_VoltageTransformerModel
                {
                    VoltageTransformerId = item.VoltageTransformerId,
                    DepartmentId = item.DepartmentId,
                    VTCode = item.VTCode,
                    VTNumber = item.VTNumber,
                    VTTypeId = item.VTTypeId,
                    Possesive = item.Possesive,
                    ManufactureYear = item.ManufactureYear,
                    ActionCode = item.ActionCode,
                    ActionDate = item.ActionDate,
                    CreateDate = item.CreateDate,
                    CreateUser = item.CreateUser,
                    ReasonId = item.ReasonId,
                    TypeCode = item.Category_VoltageTransformerType.TypeCode,
                    TypeName = item.Category_VoltageTransformerType.TypeName,
                    TestingStatus = _dbContext.EquipmentVT_Testing.Where(i => i.VoltageTransformerId == item.VoltageTransformerId).Select(i => i.Status).FirstOrDefault()
                });

                if (!string.IsNullOrEmpty(ActionCode))
                {
                    query = query.Where(item => item.ActionCode.Contains(ActionCode));
                }

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(item => item.VTCode.Contains(search) || item.VTNumber.Contains(search));
                }

                var paged = (IPagedList<EquipmentVT_VoltageTransformerModel>)query.OrderBy(p => p.VoltageTransformerId).ToPagedList(pageNumber, pageSize);

                var response = new
                {
                    paged.PageNumber,
                    paged.PageSize,
                    paged.TotalItemCount,
                    paged.PageCount,
                    paged.HasNextPage,
                    paged.HasPreviousPage,
                    VoltageTransformers = paged.ToList()
                };
                respone.Status = 1;
                respone.Message = "Lấy danh sách thành công.";
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

        //Cập nhật lý do thanh lý
        [HttpPost]
        [Route("LiquidationTU")]
        public HttpResponseMessage LiquidationTU(string VoltageTransformerTU, string ReasonId)
        {
            try
            {
                business_EquipmentVT_VoltageTransformer.UpdateLiquidationTU(VoltageTransformerTU, ReasonId, _dbContext);

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
        [Route("AddVoltageTransformer")]
        public HttpResponseMessage AddVoltageTransformer(EquipmentVT_VoltageTransforme_StockReport_Testing model)
        {
            try
            {
                //Kiểm tra mã TU trước khi thêm mới
                if (business_EquipmentVT_VoltageTransformer.CheckExistVoltageTransformerCode(
                                model.VoltageTransformerModel.VTTypeId, model.VoltageTransformerModel.ManufactureYear,
                                model.VoltageTransformerModel.VTNumber))
                {
                    throw new ArgumentException("Mã TU đã tồn tại.");
                }
                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var userId = TokenHelper.GetUserIdFromToken();

                using (var dbContextTransaction = _dbContext.Database.BeginTransaction())
                {
                    try
                    {
                        model.VoltageTransformerModel.CreateUser = userId;
                        model.VoltageTransformerModel.CreateDate = DateTime.Now;
                        model.VoltageTransformerModel.DepartmentId = departmentId;
                        model.VoltageTransformerModel.ActionCode = "A";
                        var typeV = _dbContext.Category_VoltageTransformerType.Where(item => item.VTTypeId == model.VoltageTransformerModel.VTTypeId).FirstOrDefault();
                        model.VoltageTransformerModel.VTCode = typeV.TypeCode +
                                                               model.VoltageTransformerModel.ManufactureYear +
                                                               model.VoltageTransformerModel.VTNumber;
                        model.VoltageTransformerModel.TestingDate = model.TestingModel.TestingDate;
                        model.VoltageTransformerModel.EndTestingDate = model.TestingModel.TestingDate.Value.AddMonths(typeV.TestingDay);
                        model.VoltageTransformerModel.EndTestingDate = new DateTime(model.VoltageTransformerModel.EndTestingDate.Value.Year, model.VoltageTransformerModel.EndTestingDate.Value.Month, 1);
                        int voltageTransformerId = business_EquipmentVT_VoltageTransformer.AddEquipmentVT_VoltageTransformer(model.VoltageTransformerModel, _dbContext);

                        // lấy ra VoltageTransformerId insert vào bảng EquipmentVT_Testing
                        model.TestingModel.VoltageTransformerId = voltageTransformerId; // Id TU
                        model.TestingModel.CreateDate = DateTime.Now;
                        model.TestingModel.CreateUser = userId;
                        quipmentVtTesting.AddEquipmentVT_Testing(model.TestingModel, _dbContext);
                        _dbContext.SaveChanges();
                        dbContextTransaction.Commit();

                        respone.Status = 1;
                        respone.Message = "Thêm mới TU thành công.";
                        respone.Data = voltageTransformerId;
                        return createResponse();
                    }
                    catch (Exception ex)
                    {
                        dbContextTransaction.Rollback();
                        throw new ArgumentException("Thêm mới TU không thành công.");
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

        //Kiểm định TU
        [HttpGet]
        [Route("Testing_VoltageTransformer")]
        public HttpResponseMessage Testing_VoltageTransformer(int voltageTransformerId)
        {
            try
            {
                var VoltageTransforme =
                    _dbContext.EquipmentVT_VoltageTransformer.Where(item => item.VoltageTransformerId.Equals(voltageTransformerId))
                        .Select(item => new EquipmentVT_VoltageTransformerModel
                        {
                            VoltageTransformerId = item.VoltageTransformerId,
                            DepartmentId = item.DepartmentId,
                            VTCode = item.VTCode,
                            VTNumber = item.VTNumber,
                            VTTypeId = item.VTTypeId,
                            Possesive = item.Possesive,
                            ManufactureYear = item.ManufactureYear,
                            ActionCode = item.ActionCode,
                            ActionDate = item.ActionDate,
                            CreateDate = item.CreateDate,
                            CreateUser = item.CreateUser,
                            ReasonId = item.ReasonId,
                            TypeName = item.Category_VoltageTransformerType.TypeName
                        }).FirstOrDefault();
                EquipmentVT_VoltageTransforme_StockReport_Testing model = new EquipmentVT_VoltageTransforme_StockReport_Testing();
                model.VoltageTransformerModel = VoltageTransforme;

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

        [HttpPost]
        [Route("Testing_VoltageTransformer")]
        public HttpResponseMessage Testing_VoltageTransformer(EquipmentVT_VoltageTransforme_StockReport_Testing model)
        {
            using (var dbContextTransaction = _dbContext.Database.BeginTransaction())
            {
                try
                {

                    var userId = TokenHelper.GetUserIdFromToken();
                    EquipmentVT_TestingModel TestingVT = model.TestingModel;
                    TestingVT.CreateDate = DateTime.Now;
                    TestingVT.CreateUser = userId;
                    if (model.TestingModel.TestingStatus)
                        model.TestingModel.Status = 1;
                    if (!model.TestingModel.TestingStatus)
                        model.TestingModel.Status = 2;
                    TestingVT.VoltageTransformerId = model.VoltageTransformerModel.VoltageTransformerId;
                    
                    quipmentVtTesting.EditEquipmentVT_Testing(TestingVT, _dbContext);

                    dbContextTransaction.Commit();

                    respone.Status = 1;
                    respone.Message = "Kiểm định TU thành công.";
                    respone.Data = null;
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

        #region Chuẩn hóa thông tin TU
        [HttpGet]
        [Route("EditVoltageTransformer")]
        public HttpResponseMessage EditVoltageTransformer(int voltageTransformerId)
        {
            try
            {
                EquipmentVT_VoltageTransforme_StockReport_Testing model = new EquipmentVT_VoltageTransforme_StockReport_Testing();

                // danh sách thông tin TU
                var listTU = _dbContext.EquipmentVT_VoltageTransformer.Where(item => item.VoltageTransformerId.Equals(voltageTransformerId)).Select(item => new EquipmentVT_VoltageTransformerModel()
                {
                    VoltageTransformerId = item.VoltageTransformerId,
                    VTCode = item.VTCode, // mã Tu
                    VTNumber = item.VTNumber, // số Tu
                    VTTypeId = item.VTTypeId, // chủng loại
                    Possesive = item.Possesive,// sở hữu
                    ManufactureYear = item.ManufactureYear, // năm
                    ActionCode = item.ActionCode
                }).FirstOrDefault();

                model.VoltageTransformerModel = listTU;
                // kiểm tra tình trạng kiểm định
                var ds = _dbContext.EquipmentVT_Testing.Where(item => item.VoltageTransformerId.Equals(voltageTransformerId)).Select(item => new
                {
                    TestingStatus = item.Status,
                    TestingDate = item.TestingDate
                }).ToList().FirstOrDefault();
                if (ds.TestingStatus == 0 || ds.TestingStatus == 2)
                {
                    var ListTU_Testing = _dbContext.EquipmentVT_Testing.Where(item => item.VoltageTransformerId.Equals(voltageTransformerId)).Select(item => new EquipmentVT_TestingModel()
                    {
                        VoltageTransformerId = item.VoltageTransformerId,
                        ReportNumber = item.ReportNumber, // biên bản kiểm định
                        TestingDate = item.TestingDate, // ngày kiểm định
                        TestingDepartmentId = item.TestingDepartmentId, // id đơn vị kiểm định
                        TestingEmployee = item.TestingEmployee, // nhân viên kiểm định
                        PliersCode = item.PliersCode, // mã kìm
                        LeadQuantity = item.LeadQuantity, // số viên chì
                        VignetteCode = item.VignetteCode, // mã tem
                        Serial = item.Serial, // seri
                        TestingStatus = false,
                    }).FirstOrDefault();
                    model.TestingModel = ListTU_Testing;
                }
                else
                {
                    var ListTU_Testing = _dbContext.EquipmentVT_Testing.Where(item => item.VoltageTransformerId.Equals(voltageTransformerId)).Select(item => new EquipmentVT_TestingModel()
                    {
                        VoltageTransformerId = item.VoltageTransformerId,
                        ReportNumber = item.ReportNumber, // biên bản kiểm định
                        TestingDate = item.TestingDate, // ngày kiểm định
                        TestingDepartmentId = item.TestingDepartmentId, // đơn vị kiểm định
                        TestingEmployee = item.TestingEmployee, // nhân viên kiểm định
                        PliersCode = item.PliersCode, // mã kìm
                        LeadQuantity = item.LeadQuantity, // số viên chì
                        VignetteCode = item.VignetteCode, // mã tem
                        Serial = item.Serial, // seri
                        TestingStatus = true
                    }).FirstOrDefault();
                    model.TestingModel = ListTU_Testing;
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

        [HttpPost]
        [Route("EditVoltageTransformer")]
        public HttpResponseMessage EditVoltageTransformer(EquipmentVT_VoltageTransforme_StockReport_Testing model)
        {
            using (var dbContextTransaction = _dbContext.Database.BeginTransaction())
            {
                try
                {
                    //kiểm tra mã Tu
                    if (business_EquipmentVT_VoltageTransformer.CheckExistVoltageTransformer(model.VoltageTransformerModel.VTTypeId, model.VoltageTransformerModel.ManufactureYear, model.VoltageTransformerModel.VTNumber, model.VoltageTransformerModel.VoltageTransformerId))
                    {
                        throw new ArgumentException("Mã TU đã tồn tại.");                        
                    }

                    var typeCode = _dbContext.Category_VoltageTransformerType.Where(
                            item => item.VTTypeId == model.VoltageTransformerModel.VTTypeId)
                            //.Select(item => item.TypeCode)
                            .FirstOrDefault(); // mã chủng loại
                    model.VoltageTransformerModel.VTCode = typeCode.TypeCode
                                                         + model.VoltageTransformerModel.ManufactureYear
                                                         + model.VoltageTransformerModel.VTNumber;
                    // lưu thông tin TU vào db.
                    model.VoltageTransformerModel.TestingDate = model.TestingModel.TestingDate;
                    model.VoltageTransformerModel.EndTestingDate = model.TestingModel.TestingDate.Value.AddMonths(typeCode.TestingDay);
                    model.VoltageTransformerModel.EndTestingDate = new DateTime(model.VoltageTransformerModel.EndTestingDate.Value.Year, model.VoltageTransformerModel.EndTestingDate.Value.Month, 1);
                    business_EquipmentVT_VoltageTransformer.EditVoltageTransformerTU(model.VoltageTransformerModel, _dbContext);
                    // thông tin kiểm định
                    model.TestingModel.VoltageTransformerId = model.VoltageTransformerModel.VoltageTransformerId;
                    if (model.TestingModel.TestingStatus)
                        model.TestingModel.Status = 1;
                    if (!model.TestingModel.TestingStatus)
                        model.TestingModel.Status = 2;
                    // lưu thông tin chuẩn hóa kiểm định vào db
                    quipmentVtTesting.EditEquipmentVT_TestingTU(model.TestingModel, _dbContext);

                    _dbContext.SaveChanges();
                    dbContextTransaction.Commit();

                    respone.Status = 1;
                    respone.Message = "Chuẩn hóa thông tin TU thành công.";
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
        }
        #endregion

        #region Thanh lý TU
        [HttpPost]
        [Route("DeleteTU")]
        public HttpResponseMessage DeleteTU(int voltageTransformerId)
        {
            try
            {
                var target = _dbContext.EquipmentVT_VoltageTransformer.Where(item => item.VoltageTransformerId == voltageTransformerId).FirstOrDefault();

                var testingEquipment = _dbContext.EquipmentVT_Testing.Where(item => item.VoltageTransformerId == voltageTransformerId).ToList().LastOrDefault();
                if (target != null)
                {
                    if (target.ActionCode == "B")
                    {
                        throw new ArgumentException("TU đang treo, không thể thanh lý!");                        
                    }
                    else
                    {
                        target.ActionCode = "F";
                        testingEquipment.Status = 2;
                        _dbContext.SaveChanges();

                        respone.Status = 1;
                        respone.Message = "Thanh lý TU thành công.";
                        respone.Data = null;
                        return createResponse();                       
                    }
                }
                else
                {
                    throw new ArgumentException($"Không có TU: {target.VTNumber}");                    
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
    }
}
