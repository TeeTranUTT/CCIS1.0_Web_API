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
    [RoutePrefix("api/DanhMuc/Lo")]
    public class Category_RouteController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_Administrator_Department administrator_Department = new Business_Administrator_Department();
        private readonly Business_Category_Route businessRoute = new Business_Category_Route();
        private readonly CCISContext _dbContext;

        public Category_RouteController()
        {
            _dbContext = new CCISContext();
        }

        [HttpGet]
        [Route("Category_RouteManager")]
        public HttpResponseMessage Category_RouteManager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search, [DefaultValue(0)] int departmentId)
        {
            try
            {
                if (departmentId == 0)
                    departmentId = TokenHelper.GetDepartmentIdFromToken();
                //list đơn vị con của đơn vị được search
                var listDepartments = DepartmentHelper.GetChildDepIds(departmentId);

                var query = _dbContext.Category_Route.Where(item => listDepartments.Contains(item.DepartmentId) && item.Status == true).Select(item => new Category_RouteModel
                {
                    RouteId = item.RouteId,
                    DepartmentId = item.DepartmentId,
                    RouteName = item.RouteName,
                    RouteCode = item.RouteCode,
                    Type = item.Type,
                    PotentialCode = item.PotentialCode,
                    PotentialName = _dbContext.Category_Potential.Where(a => a.PotentialCode.Equals(item.PotentialCode)).Select(a => a.PotentialName).FirstOrDefault(),
                    Status = item.Status
                });

                if (!string.IsNullOrEmpty(search))
                {
                    query = (IQueryable<Category_RouteModel>)query.Where(item => item.RouteName.Contains(search) || item.RouteCode.Contains(search));
                }

                var pagedRoute = (IPagedList<Category_RouteModel>)query.OrderBy(p => p.RouteId).ToPagedList(pageNumber, pageSize);

                var response = new
                {
                    pagedRoute.PageNumber,
                    pagedRoute.PageSize,
                    pagedRoute.TotalItemCount,
                    pagedRoute.PageCount,
                    pagedRoute.HasNextPage,
                    pagedRoute.HasPreviousPage,
                    Routes = pagedRoute.ToList()
                };
                respone.Status = 1;
                respone.Message = "Lấy danh sách lộ thành công.";
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
        public HttpResponseMessage GetRouteById(int routeId)
        {
            try
            {
                if (routeId < 0 || routeId == 0)
                {
                    throw new ArgumentException($"RouteId {routeId} không hợp lệ.");
                }
                var route = _dbContext.Category_Route.Where(p => p.RouteId == routeId).Select(p => new Category_RouteModel
                {
                    RouteId = p.RouteId,
                    DepartmentId = p.DepartmentId,
                    RouteCode = p.RouteCode,
                    RouteName = p.RouteName,
                    Type = p.Type,
                    Status = p.Status,
                    PotentialCode = p.PotentialCode
                });

                if (route?.Any() == true)
                {
                    var response = route.FirstOrDefault();
                    if (response.Status)
                    {
                        respone.Status = 1;
                        respone.Message = "Lấy thông tin lộ thành công.";
                        respone.Data = response;
                        return createResponse();
                    }
                    else
                    {
                        throw new ArgumentException($"Lộ {response.RouteName} đã bị vô hiệu.");
                    }
                }
                else
                {
                    throw new ArgumentException($"Lộ có RouteId {routeId} không tồn tại.");
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
        public HttpResponseMessage AddCategory_Route(Category_RouteModel route)
        {
            try
            {
                #region Get DepartmentId From Token

                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                route.DepartmentId = departmentId;
                #endregion

                businessRoute.AddCategory_Route(route);

                var lo = _dbContext.Category_Route.Where(p => p.RouteName == route.RouteName && p.RouteCode == route.RouteCode).FirstOrDefault();
                if (lo != null)
                {
                    respone.Status = 1;
                    respone.Message = "Thêm mới lộ thành công.";
                    respone.Data = lo.RouteId;
                    return createResponse();
                }
                else
                {
                    respone.Status = 0;
                    respone.Message = "Thêm mới lộ không thành công.";
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
        public HttpResponseMessage EditCategory_Route(Category_RouteModel route)
        {
            try
            {
                var lo = _dbContext.Category_Route.Where(p => p.RouteId == route.RouteId).FirstOrDefault();
                if (lo == null)
                {
                    throw new ArgumentException($"Không tồn tại RouteId {route.RouteId}");
                }

                #region Get DepartmentId From Token

                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                route.DepartmentId = departmentId;
                #endregion

                businessRoute.EditCategory_Route(route);

                respone.Status = 1;
                respone.Message = "Chỉnh sửa lộ thành công.";
                respone.Data = route.RouteId;

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
        public HttpResponseMessage DeleteCategory_Route(int routeId)
        {
            try
            {
                var target = _dbContext.Category_Route.Where(item => item.RouteId == routeId).FirstOrDefault();
                target.Status = false;
                _dbContext.SaveChanges();

                respone.Status = 1;
                respone.Message = "Xóa lộ thành công.";
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
        [Route("KhoiPhuc")]
        public HttpResponseMessage RestoreCategory_Route(int routeId)
        {
            try
            {
                var target = _dbContext.Category_Route.Where(item => item.RouteId == routeId).FirstOrDefault();
                target.Status = true;
                _dbContext.SaveChanges();

                respone.Status = 1;
                respone.Message = "Khôi phục lộ thành công.";
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
