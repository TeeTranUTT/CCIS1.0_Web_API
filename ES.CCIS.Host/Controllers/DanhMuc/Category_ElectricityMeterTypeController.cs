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
    [RoutePrefix("api/DanhMuc/ChungLoaiCongTo")]
    public class Category_ElectricityMeterTypeController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_Administrator_Department administrator_Department = new Business_Administrator_Department();
        private readonly Business_Category_ElectricityMeterType bussiness_Category_ElectricityMeterType = new Business_Category_ElectricityMeterType();
        private readonly CCISContext _dbContext;

        public Category_ElectricityMeterTypeController()
        {
            _dbContext = new CCISContext();
        }

        [HttpGet]
        [Route("Category_ElectricityMeterTypeManager")]
        public HttpResponseMessage Category_ElectricityMeterTypeManager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search)
        {
            try
            {
                var query = _dbContext.Category_ElectricityMeterType.Where(item => item.Status == true).Select(item => new Category_ElectricityMeterTypeModel
                {
                    Accuracy = item.Accuracy,
                    AccuracyReactivePower = item.AccuracyReactivePower,
                    Coefficient = item.Coefficient,
                    Current = item.Current,
                    Description = item.Description,
                    ElectricityMeterTypeId = item.ElectricityMeterTypeId,
                    K_Constant = item.K_Constant,
                    NumberOfPhases = item.NumberOfPhases,
                    NumberOfWire = item.NumberOfWire,
                    Status = item.Status,
                    Type = item.Type,
                    TypeCode = item.TypeCode,
                    TypeName = item.TypeName,
                    Voltage = item.Voltage
                });

                if (!string.IsNullOrEmpty(search))
                {
                    query = (IQueryable<Category_ElectricityMeterTypeModel>)query.Where(item => item.TypeName.Contains(search) || item.TypeCode.Contains(search));
                }

                var pagedElectricityMeterType = (IPagedList<Category_ElectricityMeterTypeModel>)query.OrderBy(p => p.ElectricityMeterTypeId).ToPagedList(pageNumber, pageSize);

                var response = new
                {
                    pagedElectricityMeterType.PageNumber,
                    pagedElectricityMeterType.PageSize,
                    pagedElectricityMeterType.TotalItemCount,
                    pagedElectricityMeterType.PageCount,
                    pagedElectricityMeterType.HasNextPage,
                    pagedElectricityMeterType.HasPreviousPage,
                    ElectricityMeterTypes = pagedElectricityMeterType.ToList()
                };
                respone.Status = 1;
                respone.Message = "Lấy danh sách chủng loại công tơ thành công.";
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

        [HttpGet]
        [Route("")]
        public HttpResponseMessage GetElectricityMeterTypeById(int electricityMeterTypeId)
        {
            try
            {
                if (electricityMeterTypeId < 0 || electricityMeterTypeId == 0)
                {
                    throw new ArgumentException($"ElectricityMeterTypeId {electricityMeterTypeId} không hợp lệ.");
                }
                var electricityMeterType = _dbContext.Category_ElectricityMeterType.Where(p => p.ElectricityMeterTypeId == electricityMeterTypeId).Select(item => new Category_ElectricityMeterTypeModel
                {
                    Accuracy = item.Accuracy,
                    AccuracyReactivePower = item.AccuracyReactivePower,
                    Coefficient = item.Coefficient,
                    Current = item.Current,
                    Description = item.Description,
                    ElectricityMeterTypeId = item.ElectricityMeterTypeId,
                    K_Constant = item.K_Constant,
                    NumberOfPhases = item.NumberOfPhases,
                    NumberOfWire = item.NumberOfWire,
                    Status = item.Status,
                    Type = item.Type,
                    TypeCode = item.TypeCode,
                    TypeName = item.TypeName,
                    Voltage = item.Voltage
                });

                if (electricityMeterType?.Any() == true)
                {
                    var response = electricityMeterType.FirstOrDefault();
                    if (response.Status)
                    {
                        respone.Status = 1;
                        respone.Message = "Lấy thông tin chủng loại công tơ thành công.";
                        respone.Data = response;
                        return createResponse();
                    }
                    else
                    {
                        throw new ArgumentException($"Chủng loại công tơ {response.TypeName} đã bị vô hiệu.");
                    }
                }
                else
                {
                    throw new ArgumentException($"Chủng loại công tơ có ElectricityMeterTypeId {electricityMeterTypeId} không tồn tại.");
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
        public HttpResponseMessage AddCategory_ElectricityMeterType(Category_ElectricityMeterTypeModel model)
        {
            try
            {
                //Kiểm tra đã tồn tại mã chủng loại
                if (bussiness_Category_ElectricityMeterType.CheckExistTypeCode(model.TypeCode))
                {
                    throw new ArgumentException("Mã chủng loại công tơ đã tồn tại.");
                }
                else
                {
                    bussiness_Category_ElectricityMeterType.AddCategory_ElectricityMeterType(model);

                    var chungLoaiCongTo = _dbContext.Category_ElectricityMeterType.Where(p => p.TypeName == model.TypeName && p.TypeCode == model.TypeCode).FirstOrDefault();
                    if (chungLoaiCongTo != null)
                    {
                        respone.Status = 1;
                        respone.Message = "Thêm mới chủng loại công tơ thành công.";
                        respone.Data = chungLoaiCongTo.ElectricityMeterTypeId;
                        return createResponse();
                    }
                    else
                    {
                        respone.Status = 0;
                        respone.Message = "Thêm mới chủng loại công tơ không thành công.";
                        respone.Data = null;
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
        [Route("Sua")]
        public HttpResponseMessage EditCategory_ElectricityMeterType(Category_ElectricityMeterTypeModel model)
        {
            try
            {
                var chungLoaiCongTo = _dbContext.Category_ElectricityMeterType.Where(p => p.ElectricityMeterTypeId == model.ElectricityMeterTypeId).FirstOrDefault();
                if (chungLoaiCongTo == null)
                {
                    throw new ArgumentException($"Không tồn tại ElectricityMeterTypeId {model.ElectricityMeterTypeId}");
                }

                //Kiểm tra đã tồn tại mã chủng loại
                if (bussiness_Category_ElectricityMeterType.CheckExistTypeCodeForEdit(model.TypeCode, model.ElectricityMeterTypeId))
                {
                    throw new ArgumentException("Mã chủng loại đã tồn tại.");
                }
                else
                {
                    bussiness_Category_ElectricityMeterType.EditCategory_ElectricityMeterType(model);

                    respone.Status = 1;
                    respone.Message = "Chỉnh sửa chủng loại công tơ thành công.";
                    respone.Data = model.ElectricityMeterTypeId;

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
        [Route("Xoa")]
        public HttpResponseMessage DeleteCategory_ElectricityMeterType(int electricityMeterTypeId)
        {
            try
            {
                var target = _dbContext.Category_ElectricityMeterType.Where(item => item.ElectricityMeterTypeId == electricityMeterTypeId).FirstOrDefault();
                target.Status = false;
                _dbContext.SaveChanges();

                respone.Status = 1;
                respone.Message = "Xóa chủng loại công tơ thành công.";
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
