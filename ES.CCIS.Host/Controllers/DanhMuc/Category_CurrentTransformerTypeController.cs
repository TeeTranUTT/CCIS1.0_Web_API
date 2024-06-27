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
    [RoutePrefix("api/DanhMuc/ChungLoaiTI")]
    public class Category_CurrentTransformerTypeController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_Administrator_Department administrator_Department = new Business_Administrator_Department();
        private readonly Business_Category_CurrentTransformerType business_Category_CurrentTransformerType = new Business_Category_CurrentTransformerType();

        [HttpGet]
        [Route("Category_CurrentTransformerTypeManager")]
        public HttpResponseMessage Category_CurrentTransformerTypeManager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    var query = db.Category_CurrentTransformerType.Where(item => item.Status == true).Select(item => new Category_CurrentTransformerTypeModel
                    {
                        Accuracy = item.Accuracy,
                        ConnectionRatio = item.ConnectionRatio,
                        Description = item.Description,
                        NumberOfPhases = item.NumberOfPhases,
                        Status = item.Status,
                        TypeCode = item.TypeCode,
                        TypeName = item.TypeName,
                        Voltage = item.Voltage,
                        CTTypeId = item.CTTypeId,
                        TestingDay = item.TestingDay
                    });

                    if (!string.IsNullOrEmpty(search))
                    {
                        query = (IQueryable<Category_CurrentTransformerTypeModel>)query.Where(item => item.TypeName.Contains(search) || item.TypeCode.Contains(search));
                    }

                    var pagedCurrentTransformerType = (IPagedList<Category_CurrentTransformerTypeModel>)query.OrderBy(p => p.CTTypeId).ToPagedList(pageNumber, pageSize);

                    var response = new
                    {
                        pagedCurrentTransformerType.PageNumber,
                        pagedCurrentTransformerType.PageSize,
                        pagedCurrentTransformerType.TotalItemCount,
                        pagedCurrentTransformerType.PageCount,
                        pagedCurrentTransformerType.HasNextPage,
                        pagedCurrentTransformerType.HasPreviousPage,
                        CurrentTransformerTypes = pagedCurrentTransformerType.ToList()
                    };
                    respone.Status = 1;
                    respone.Message = "Lấy danh sách chủng loại TI thành công.";
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
        public HttpResponseMessage GetCurrentTransformerTypeById(int cTTypeId)
        {
            try
            {
                if (cTTypeId < 0 || cTTypeId == 0)
                {
                    throw new ArgumentException($"cTTypeId {cTTypeId} không hợp lệ.");
                }
                using (var dbContext = new CCISContext())
                {
                    var currentTransformerType = dbContext.Category_CurrentTransformerType.Where(p => p.CTTypeId == cTTypeId).Select(item => new Category_CurrentTransformerTypeModel
                    {
                        Accuracy = item.Accuracy,
                        ConnectionRatio = item.ConnectionRatio,
                        Description = item.Description,
                        NumberOfPhases = item.NumberOfPhases,
                        Status = item.Status,
                        TypeCode = item.TypeCode,
                        TypeName = item.TypeName,
                        Voltage = item.Voltage,
                        CTTypeId = item.CTTypeId,
                        TestingDay = item.TestingDay
                    });

                    if (currentTransformerType?.Any() == true)
                    {
                        var response = currentTransformerType.FirstOrDefault();
                        if (response.Status)
                        {
                            respone.Status = 1;
                            respone.Message = "Lấy thông tin chủng loại TI thành công.";
                            respone.Data = response;
                            return createResponse();
                        }
                        else
                        {
                            throw new ArgumentException($"Chủng loại TI {response.TypeName} đã bị vô hiệu.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"Chủng loại TI có CurrentTransformerTypeId {cTTypeId} không tồn tại.");
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
        public HttpResponseMessage AddCategory_CurrentTransformerType(Category_CurrentTransformerTypeModel model)
        {
            try
            {
                //Kiểm tra đã tồn tại mã chủng loại TI
                if (business_Category_CurrentTransformerType.CheckExistTypeCode(model.TypeCode))
                {
                    throw new ArgumentException("Mã chủng loại TI đã tồn tại.");
                }
                else
                {
                    business_Category_CurrentTransformerType.AddCategory_CurrentTransformerType(model);

                    using (var dbContext = new CCISContext())
                    {
                        var chungLoaiTI = dbContext.Category_CurrentTransformerType.Where(p => p.TypeName == model.TypeName && p.TypeCode == model.TypeCode).FirstOrDefault();
                        if (chungLoaiTI != null)
                        {
                            respone.Status = 1;
                            respone.Message = "Thêm mới chủng loại TI thành công.";
                            respone.Data = chungLoaiTI.CTTypeId;
                            return createResponse();
                        }
                        else
                        {
                            respone.Status = 0;
                            respone.Message = "Thêm mới chủng loại TI không thành công.";
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
        public HttpResponseMessage EditCategory_CurrentTransformerType(Category_CurrentTransformerTypeModel model)
        {
            try
            {
                using (var dbContext = new CCISContext())
                {
                    var chungLoaiTI = dbContext.Category_CurrentTransformerType.Where(p => p.CTTypeId == model.CTTypeId).FirstOrDefault();
                    if (chungLoaiTI == null)
                    {
                        throw new ArgumentException($"Không tồn tại CurrentTransformerTypeId {model.CTTypeId}");
                    }

                    //Kiểm tra đã tồn tại mã chủng loại TI
                    if (business_Category_CurrentTransformerType.CheckExistTypeCodeForEdit(model.TypeCode, model.CTTypeId))
                    {
                        throw new ArgumentException("Mã chủng loại TI đã tồn tại.");
                    }
                    else
                    {
                        business_Category_CurrentTransformerType.EditCategory_CurrentTransformerType(model);

                        respone.Status = 1;
                        respone.Message = "Chỉnh sửa chủng loại TI thành công.";
                        respone.Data = model.CTTypeId;

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
        public HttpResponseMessage DeleteCategory_CurrentTransformerType(int cTTypeId)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    var target = db.Category_CurrentTransformerType.Where(item => item.CTTypeId == cTTypeId).FirstOrDefault();
                    target.Status = false;
                    db.SaveChanges();
                }
                respone.Status = 1;
                respone.Message = "Xóa chủng loại TI thành công.";
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
