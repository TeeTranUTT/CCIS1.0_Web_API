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
    [RoutePrefix("api/DanhMuc/KhuVuc")]
    public class Category_RegionsController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_Administrator_Department administrator_Department = new Business_Administrator_Department();
        private readonly Business_Category_Regions businessRegions = new Business_Category_Regions();
        private readonly CCISContext _dbContext;

        public Category_RegionsController()
        {
            _dbContext = new CCISContext();
        }

        [HttpGet]
        [Route("Category_RegionManager")]
        public HttpResponseMessage Category_RegionManager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search, [DefaultValue(0)] int departmentId)
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

                var query = _dbContext.Category_Regions.Where(item => listDepartments.Contains(item.DepartmentId) && item.Status == true).Select(item => new Category_RegionsModel
                {
                    DepartmentId = item.DepartmentId,
                    RegionId = item.RegionId,
                    RegionCode = item.RegionCode,
                    RegionName = item.RegionName,
                    Status = item.Status
                });

                if (!string.IsNullOrEmpty(search))
                {
                    query = (IQueryable<Category_RegionsModel>)query.Where(item => item.RegionName.Contains(search) || item.RegionCode.Contains(search));
                }

                var pagedRegion = (IPagedList<Category_RegionsModel>)query.OrderBy(p => p.RegionId).ToPagedList(pageNumber, pageSize);

                var response = new
                {
                    pagedRegion.PageNumber,
                    pagedRegion.PageSize,
                    pagedRegion.TotalItemCount,
                    pagedRegion.PageCount,
                    pagedRegion.HasNextPage,
                    pagedRegion.HasPreviousPage,
                    Regions = pagedRegion.ToList()
                };
                respone.Status = 1;
                respone.Message = "Lấy danh sách khu vực thành công.";
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
        public HttpResponseMessage GetRegionById(int regionId)
        {
            try
            {
                if (regionId < 0 || regionId == 0)
                {
                    throw new ArgumentException($"regionId {regionId} không hợp lệ.");
                }
                var route = _dbContext.Category_Regions.Where(p => p.RegionId == regionId).Select(item => new Category_RegionsModel
                {
                    DepartmentId = item.DepartmentId,
                    RegionId = item.RegionId,
                    RegionCode = item.RegionCode,
                    RegionName = item.RegionName,
                    Status = item.Status
                });

                if (route?.Any() == true)
                {
                    var response = route.FirstOrDefault();
                    if (response.Status)
                    {
                        respone.Status = 1;
                        respone.Message = "Lấy thông tin khu vực thành công.";
                        respone.Data = response;
                        return createResponse();
                    }
                    else
                    {
                        throw new ArgumentException($"Khu vực {response.RegionName} đã bị vô hiệu.");
                    }
                }
                else
                {
                    throw new ArgumentException($"Khu vực có RegionId {regionId} không tồn tại.");
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
        public HttpResponseMessage AddCategory_Regions(Category_RegionsModel model)
        {
            try
            {
                #region Get DepartmentId From Token

                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                model.DepartmentId = departmentId;
                #endregion

                businessRegions.AddCategory_Regions(model);

                var khuVuc = _dbContext.Category_Regions.Where(p => p.RegionName == model.RegionName && p.RegionCode == model.RegionCode).FirstOrDefault();
                if (khuVuc != null)
                {
                    respone.Status = 1;
                    respone.Message = "Thêm mới khu vực thành công.";
                    respone.Data = khuVuc.RegionId;
                    return createResponse();
                }
                else
                {
                    respone.Status = 0;
                    respone.Message = "Thêm mới khu vực không thành công.";
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
        public HttpResponseMessage EditCategory_Regions(Category_RegionsModel model)
        {
            try
            {
                var khuVuc = _dbContext.Category_Regions.Where(p => p.RegionId == model.RegionId).FirstOrDefault();
                if (khuVuc == null)
                {
                    throw new ArgumentException($"Không tồn tại RegionId {model.RegionId}");
                }

                #region Get DepartmentId From Token

                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                model.DepartmentId = departmentId;
                #endregion

                businessRegions.EditCategory_Regions(model);

                respone.Status = 1;
                respone.Message = "Chỉnh sửa khu vực thành công.";
                respone.Data = model.RegionId;

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
        public HttpResponseMessage DeleteCategory_Regions(int regionId)
        {
            try
            {
                var target = _dbContext.Category_Regions.Where(item => item.RegionId == regionId).FirstOrDefault();
                target.Status = false;
                _dbContext.SaveChanges();

                respone.Status = 1;
                respone.Message = "Xóa khu vực thành công.";
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
