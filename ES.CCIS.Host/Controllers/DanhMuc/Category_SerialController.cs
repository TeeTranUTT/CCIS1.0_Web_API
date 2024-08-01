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
    [RoutePrefix("api/DanhMuc/MauHoaDonDienTu")]
    public class Category_SerialController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_Administrator_Department administrator_Department = new Business_Administrator_Department();
        private readonly Business_Category_Serial businessSerial = new Business_Category_Serial();
        private readonly CCISContext _dbContext;

        public Category_SerialController()
        {
            _dbContext = new CCISContext();
        }

        [HttpGet]
        [Route("Category_SerialManager")]
        public HttpResponseMessage Category_SerialManager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string BillType, [DefaultValue(0)] int departmentId)
        {
            try
            {
                //Thong tin user from token                
                var userInfo = TokenHelper.GetUserInfoFromRequest();
                if (departmentId == 0)
                    departmentId = TokenHelper.GetDepartmentIdFromToken();
                //list đơn vị con của đơn vị được search
                var listDepartments = DepartmentHelper.GetChildDepIds(departmentId);

                //list đơn vị con của user đăng nhập
                var lstDepCombo = DepartmentHelper.GetChildDepIds(administrator_Department.GetIddv(userInfo.UserName));

                var query = _dbContext.Category_Serial.Where(item => listDepartments.Contains(item.DepartmentId) && item.Status == true).Select(item => new Category_SerialModel
                {
                    SerialId = item.SerialId,
                    DepartmentId = item.DepartmentId,
                    BillType = item.BillType,
                    SpecimenNumber = item.SpecimenNumber,
                    SpecimenCode = item.SpecimenCode,
                    TaxCode = item.TaxCode,
                    MinSerial = item.MinSerial,
                    MaxSerial = item.MaxSerial,
                    CurrenSerial = item.CurrenSerial,
                    ActiveDate = item.ActiveDate,
                    EndDate = item.EndDate,
                    Status = item.Status,
                    //CreateDate =item.
                    CurrenSerialBefore = item.CurrenSerialBefore
                });

                if (!string.IsNullOrEmpty(BillType))
                {
                    query = (IQueryable<Category_SerialModel>)query.Where(item => item.BillType == BillType);
                }

                var pagedStation = (IPagedList<Category_SerialModel>)query.OrderBy(p => p.SerialId).ToPagedList(pageNumber, pageSize);

                var response = new
                {
                    pagedStation.PageNumber,
                    pagedStation.PageSize,
                    pagedStation.TotalItemCount,
                    pagedStation.PageCount,
                    pagedStation.HasNextPage,
                    pagedStation.HasPreviousPage,
                    Stations = pagedStation.ToList()
                };
                respone.Status = 1;
                respone.Message = "Lấy danh sách mẫu hóa đơn điện tử thành công.";
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
        public HttpResponseMessage GetSerialById(int serialId)
        {
            try
            {
                if (serialId < 0 || serialId == 0)
                {
                    throw new ArgumentException($"SerialId {serialId} không hợp lệ.");
                }
                var station = _dbContext.Category_Serial.Where(item => item.SerialId == serialId).Select(item => new Category_SerialModel
                {
                    SerialId = item.SerialId,
                    DepartmentId = item.DepartmentId,
                    BillType = item.BillType,
                    SpecimenNumber = item.SpecimenNumber,
                    SpecimenCode = item.SpecimenCode,
                    TaxCode = item.TaxCode,
                    MinSerial = item.MinSerial,
                    MaxSerial = item.MaxSerial,
                    CurrenSerial = item.CurrenSerial,
                    ActiveDate = item.ActiveDate,
                    EndDate = item.EndDate,
                    Status = item.Status,
                    //CreateDate =item.
                    CurrenSerialBefore = item.CurrenSerialBefore
                });

                if (station?.Any() == true)
                {
                    var response = station.FirstOrDefault();
                    if (response.Status)
                    {
                        respone.Status = 1;
                        respone.Message = "Lấy thông tin mẫu hóa đơn điện tử thành công.";
                        respone.Data = response;
                        return createResponse();
                    }
                    else
                    {
                        throw new ArgumentException($"Mẫu hóa đơn điện tử {response.SpecimenCode} đã bị vô hiệu.");
                    }
                }
                else
                {
                    throw new ArgumentException($"Mẫu hóa đơn điện tử có SerialId {serialId} không tồn tại.");
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
        public HttpResponseMessage AddCategory_Serial(Category_SerialModel model)
        {
            try
            {
                #region Get DepartmentId From Token

                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                model.DepartmentId = departmentId;
                #endregion

                businessSerial.AddCategory_Serial(model);

                var serial = _dbContext.Category_Serial.Where(p => p.SpecimenCode == model.SpecimenCode && p.SpecimenNumber == model.SpecimenNumber).FirstOrDefault();
                if (serial != null)
                {
                    respone.Status = 1;
                    respone.Message = "Thêm mới mẫu hóa đơn điện tử thành công.";
                    respone.Data = serial.SerialId;
                    return createResponse();
                }
                else
                {
                    respone.Status = 0;
                    respone.Message = "Thêm mới mẫu hóa đơn điện tử không thành công.";
                    respone.Data = null;
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
        [Route("Sua")]
        public HttpResponseMessage EditCategory_Serial(Category_SerialModel model)
        {
            try
            {
                var mauHoaDon = _dbContext.Category_Serial.Where(p => p.SerialId == model.SerialId).FirstOrDefault();
                if (mauHoaDon == null)
                {
                    throw new ArgumentException($"Không tồn tại SerialId {model.SerialId}");
                }

                #region Get DepartmentId From Token

                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                model.DepartmentId = departmentId;
                #endregion

                businessSerial.EditCategory_Serial(model);

                respone.Status = 1;
                respone.Message = "Chỉnh sửa mẫu hóa đơn điện tử thành công.";
                respone.Data = mauHoaDon.SerialId;

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
        [Route("Xoa")]
        public HttpResponseMessage DeleteCategory_Serial(int serialId)
        {
            try
            {
                var target = _dbContext.Category_Serial.Where(item => item.SerialId == serialId).FirstOrDefault();
                //kiểm tra nếu chưa dùng thì được xóa
                if (target.CurrenSerial == null || target.CurrenSerial == 0)
                {
                    _dbContext.Category_Serial.Remove(target);
                }
                else
                {
                    target.Status = false;
                }
                _dbContext.SaveChanges();

                respone.Status = 1;
                respone.Message = "Xóa mẫu hóa đơn điện tử thành công.";
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
