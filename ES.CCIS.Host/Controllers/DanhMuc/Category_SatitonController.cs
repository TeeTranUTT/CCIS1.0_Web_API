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
    [RoutePrefix("api/DanhMuc/Tram")]
    public class Category_SatitonController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_Administrator_Department administrator_Department = new Business_Administrator_Department();
        private readonly Business_Category_Satiton businessstation = new Business_Category_Satiton();
        private readonly CCISContext _dbContext;

        public Category_SatitonController()
        {
            _dbContext = new CCISContext();
        }

        #region Trạm
        [HttpGet]
        [Route("Category_SatitonManager")]
        public HttpResponseMessage Category_SatitonManager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search, [DefaultValue(0)] int departmentId)
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

                var query = _dbContext.Category_Satiton.Where(item => listDepartments.Contains(item.DepartmentId) && item.Status == true).Select(item => new Category_SatitonModel
                {
                    StationId = item.StationId,
                    DepartmentId = item.DepartmentId,
                    StationName = item.StationName,
                    StationCode = item.StationCode,
                    Type = item.Type,
                    Power = item.Power,
                    Status = item.Status
                });

                if (!string.IsNullOrEmpty(search))
                {
                    query = (IQueryable<Category_SatitonModel>)query.Where(item => item.StationName.Contains(search) || item.StationCode.Contains(search));
                }

                var pagedStation = (IPagedList<Category_SatitonModel>)query.OrderBy(p => p.StationId).ToPagedList(pageNumber, pageSize);

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
                respone.Message = "Lấy danh sách trạm thành công.";
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
        public HttpResponseMessage GetStationById(int stationId)
        {
            try
            {
                if (stationId < 0 || stationId == 0)
                {
                    throw new ArgumentException($"stationId {stationId} không hợp lệ.");
                }
                var station = _dbContext.Category_Satiton.Where(item => item.StationId == stationId).Select(item => new Category_SatitonModel
                {
                    StationId = item.StationId,
                    DepartmentId = item.DepartmentId,
                    StationName = item.StationName,
                    StationCode = item.StationCode,
                    Type = item.Type,
                    Power = item.Power,
                    Status = item.Status
                });

                if (station?.Any() == true)
                {
                    var response = station.FirstOrDefault();
                    if (response.Status)
                    {
                        respone.Status = 1;
                        respone.Message = "Lấy thông tin trạm thành công.";
                        respone.Data = response;
                        return createResponse();
                    }
                    else
                    {
                        throw new ArgumentException($"Trạm {response.StationName} đã bị vô hiệu.");
                    }
                }
                else
                {
                    throw new ArgumentException($"Trạm có stationId {stationId} không tồn tại.");
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
        public HttpResponseMessage AddCategory_Satiton(Category_SatitonModel station)
        {
            try
            {
                #region Get DepartmentId From Token

                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                station.DepartmentId = departmentId;
                #endregion

                businessstation.AddCategory_Satiton(station);

                var tram = _dbContext.Category_Satiton.Where(p => p.StationName == station.StationName && p.StationCode == station.StationCode).FirstOrDefault();
                if (tram != null)
                {
                    respone.Status = 1;
                    respone.Message = "Thêm mới trạm thành công.";
                    respone.Data = tram.StationId;
                    return createResponse();
                }
                else
                {
                    respone.Status = 0;
                    respone.Message = "Thêm mới trạm không thành công.";
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
        public HttpResponseMessage EditCategory_Satiton(Category_SatitonModel station)
        {
            try
            {
                var tram = _dbContext.Category_Satiton.Where(p => p.StationId == station.StationId).FirstOrDefault();
                if (tram == null)
                {
                    throw new ArgumentException($"Không tồn tại SatitonId {station.StationId}");
                }

                #region Get DepartmentId From Token

                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                station.DepartmentId = departmentId;
                #endregion

                businessstation.EditCategory_Satiton(station);

                respone.Status = 1;
                respone.Message = "Chỉnh sửa trạm thành công.";
                respone.Data = station.StationId;

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
        public HttpResponseMessage DeleteCategory_Satiton(int stationId)
        {
            try
            {
                //kiểm tra điều kiện xóa: đảm bảo không có điểm đo nào trong trạm này
                var kiemtra = _dbContext.Concus_ServicePoint.Where(item => item.StationId == stationId)
                    .Select(item2 => new Concus_ServicePointModel
                    {
                        PointCode = item2.PointCode
                    }).ToList();
                if (kiemtra.Count > 0)
                {
                    throw new ArgumentException($"Đã có điểm đo " + kiemtra[0].PointCode + " trong trạm, không xóa được.");
                }
                var kiemtra2 = _dbContext.Concus_ServicePoint_Log.Where(item => item.StationId == stationId)
                    .Select(item2 => new Concus_ServicePoint_LogModel
                    {
                        PointCode = item2.PointCode
                    }).ToList();
                if (kiemtra2.Count > 0)
                {
                    throw new ArgumentException($"Đã có điểm đo " + kiemtra2[0].PointCode + " trong trạm, không xóa được.");
                }

                var target = _dbContext.Category_Satiton.Where(item => item.StationId == stationId).FirstOrDefault();
                target.Status = false;
                _dbContext.SaveChanges();

                respone.Status = 1;
                respone.Message = "Xóa trạm thành công.";
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
    }
}
