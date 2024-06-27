using CCIS_BusinessLogic;
using CCIS_DataAccess;
using ES.CCIS.Host.Helpers;
using PagedList;
using System;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Web.Configuration;
using System.Web.Http;

namespace ES.CCIS.Host.Controllers.DanhMuc
{
    [Authorize]
    [RoutePrefix("api/DanhMuc/ChungLoaiTU")]
    public class Category_VoltageTransformerTypeController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_Administrator_Department administrator_Department = new Business_Administrator_Department();
        private readonly Business_Category_VoltageTransformerType business_Category_VoltageTransformerType = new Business_Category_VoltageTransformerType();

        [HttpGet]
        [Route("Category_VoltageTransformerTypeManager")]
        public HttpResponseMessage Category_VoltageTransformerTypeManager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    var query = db.Category_VoltageTransformerType.Where(item => item.Status == true).Select(item => new Category_VoltageTransformerTypeModel
                    {
                        Accuracy = item.Accuracy,
                        ConnectionRatio = item.ConnectionRatio,
                        Description = item.Description,
                        NumberOfPhases = item.NumberOfPhases,
                        Status = item.Status,
                        TypeCode = item.TypeCode,
                        TypeName = item.TypeName,
                        Voltage = item.Voltage,
                        VTTypeId = item.VTTypeId,
                        TestingDay = item.TestingDay
                    });

                    if (!string.IsNullOrEmpty(search))
                    {
                        query = (IQueryable<Category_VoltageTransformerTypeModel>)query.Where(item => item.TypeName.Contains(search) || item.TypeCode.Contains(search));
                    }

                    var pagedVoltageTransformerType = (IPagedList<Category_VoltageTransformerTypeModel>)query.OrderBy(p => p.VTTypeId).ToPagedList(pageNumber, pageSize);

                    var response = new
                    {
                        pagedVoltageTransformerType.PageNumber,
                        pagedVoltageTransformerType.PageSize,
                        pagedVoltageTransformerType.TotalItemCount,
                        pagedVoltageTransformerType.PageCount,
                        pagedVoltageTransformerType.HasNextPage,
                        pagedVoltageTransformerType.HasPreviousPage,
                        VoltageTransformerTypes = pagedVoltageTransformerType.ToList()
                    };
                    respone.Status = 1;
                    respone.Message = "Lấy danh sách chủng loại TU thành công.";
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
        public HttpResponseMessage GetVoltageTransformerTypeById(int vTTypeId)
        {
            try
            {
                if (vTTypeId < 0 || vTTypeId == 0)
                {
                    throw new ArgumentException($"vTTypeId {vTTypeId} không hợp lệ.");
                }
                using (var dbContext = new CCISContext())
                {
                    var voltageTransformerType = dbContext.Category_VoltageTransformerType.Where(p => p.VTTypeId == vTTypeId).Select(item => new Category_VoltageTransformerTypeModel
                    {
                        Accuracy = item.Accuracy,
                        ConnectionRatio = item.ConnectionRatio,
                        Description = item.Description,
                        NumberOfPhases = item.NumberOfPhases,
                        Status = item.Status,
                        TypeCode = item.TypeCode,
                        TypeName = item.TypeName,
                        Voltage = item.Voltage,
                        VTTypeId = item.VTTypeId,
                        TestingDay = item.TestingDay
                    });

                    if (voltageTransformerType?.Any() == true)
                    {
                        var response = voltageTransformerType.FirstOrDefault();
                        if (response.Status)
                        {
                            respone.Status = 1;
                            respone.Message = "Lấy thông tin chủng loại TU thành công.";
                            respone.Data = response;
                            return createResponse();
                        }
                        else
                        {
                            throw new ArgumentException($"Chủng loại TU {response.TypeName} đã bị vô hiệu.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"Chủng loại TU có VoltageTransformerTypeId {vTTypeId} không tồn tại.");
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
        [Route("ThemMoi")]
        public HttpResponseMessage AddCategory_VoltageTransformerType(Category_VoltageTransformerTypeModel model)
        {
            try
            {
                //Kiểm tra đã tồn tại mã chủng loại TU
                if (business_Category_VoltageTransformerType.CheckExistTypeCode(model.TypeCode))
                {
                    throw new ArgumentException("Mã chủng loại TU đã tồn tại.");
                }
                else
                {
                    business_Category_VoltageTransformerType.AddCategory_VoltageTransformerType(model);

                    using (var dbContext = new CCISContext())
                    {
                        var chungLoaiTU = dbContext.Category_VoltageTransformerType.Where(p => p.TypeName == model.TypeName && p.TypeCode == model.TypeCode).FirstOrDefault();
                        if (chungLoaiTU != null)
                        {
                            respone.Status = 1;
                            respone.Message = "Thêm mới chủng loại TU thành công.";
                            respone.Data = chungLoaiTU.VTTypeId;
                            return createResponse();
                        }
                        else
                        {
                            respone.Status = 0;
                            respone.Message = "Thêm mới chủng loại TU không thành công.";
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
        [Route("Sua")]
        public HttpResponseMessage EditCategory_VoltageTransformerType(Category_VoltageTransformerTypeModel model)
        {
            try
            {
                using (var dbContext = new CCISContext())
                {
                    var chungLoaiTU = dbContext.Category_VoltageTransformerType.Where(p => p.VTTypeId == model.VTTypeId).FirstOrDefault();
                    if (chungLoaiTU == null)
                    {
                        throw new ArgumentException($"Không tồn tại vTTypeId {model.VTTypeId}");
                    }

                    //Kiểm tra đã tồn tại mã chủng loại TU
                    if (business_Category_VoltageTransformerType.CheckExistTypeCodeForEdit(model.TypeCode, model.VTTypeId))
                    {
                        throw new ArgumentException("Mã chủng loại TU đã tồn tại.");
                    }
                    else
                    {
                        business_Category_VoltageTransformerType.EditCategory_VoltageTransformerType(model);

                        respone.Status = 1;
                        respone.Message = "Chỉnh sửa chủng loại TU thành công.";
                        respone.Data = model.VTTypeId;

                        return createResponse();
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
        [Route("Xoa")]
        public HttpResponseMessage DeleteCategory_VoltageTransformerType(int vTTypeId)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    var target = db.Category_VoltageTransformerType.Where(item => item.VTTypeId == vTTypeId).FirstOrDefault();
                    target.Status = false;
                    db.SaveChanges();
                }
                respone.Status = 1;
                respone.Message = "Xóa chủng loại TU thành công.";
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
}
