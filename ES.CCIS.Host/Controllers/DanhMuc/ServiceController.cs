using CCIS_BusinessLogic;
using CCIS_DataAccess;
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

namespace ES.CCIS.Host.Controllers.DanhMuc
{
    [Authorize]
    [RoutePrefix("api/DanhMuc/DichVu")]
    public class ServiceController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_Category_Service businessService = new Business_Category_Service();
        private readonly CCISContext _dbContext;

        public ServiceController()
        {
            _dbContext = new CCISContext();
        }

        [HttpGet]
        [Route("Category_ServiceManager")]
        public HttpResponseMessage Category_ServiceManager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search, [DefaultValue(0)] int departmentId)
        {
            try
            {
                if (departmentId == 0)
                    departmentId = TokenHelper.GetDepartmentIdFromToken();
                //list đơn vị con của đơn vị được search
                var listDepartments = DepartmentHelper.GetChildDepIds(departmentId);
                var query = _dbContext.Category_Service.Where(item => listDepartments.Contains(item.DepartmentId) && item.IsDelete == false).Select(item => new Category_ServiceModel
                {
                    ServiceId = item.ServiceId,
                    DepartmentId = item.DepartmentId,
                    ServiceName = item.ServiceName,
                    ServiceCode = item.ServiceCode,
                    IsDelete = item.IsDelete,
                    Unit = item.Unit,
                    Quantity = item.Quantity,
                    Price = item.Price,
                    Total = item.Total
                });

                if (!string.IsNullOrEmpty(search))
                {
                    query = (IQueryable<Category_ServiceModel>)query.Where(item => item.ServiceName.Contains(search));
                }

                var paged = (IPagedList<Category_ServiceModel>)query.OrderBy(p => p.ServiceId).ToPagedList(pageNumber, pageSize);

                var response = new
                {
                    paged.PageNumber,
                    paged.PageSize,
                    paged.TotalItemCount,
                    paged.PageCount,
                    paged.HasNextPage,
                    paged.HasPreviousPage,
                    Services = paged.ToList()
                };
                respone.Status = 1;
                respone.Message = "Lấy danh sách dịch vụ thành công.";
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
        public HttpResponseMessage GetCategory_ServiceById(int serviceId)
        {
            try
            {
                var categoryService =
                    _dbContext.Category_Service.Where(item => item.ServiceId.Equals(serviceId))
                        .Select(item => new Category_ServiceModel
                        {
                            ServiceId = item.ServiceId,
                            DepartmentId = item.DepartmentId,
                            IsDelete = item.IsDelete,
                            ServiceName = item.ServiceName,
                            ServiceCode = item.ServiceCode,
                            Unit = item.Unit,
                            Quantity = item.Quantity,
                            Price = item.Price,
                            Total = item.Total,
                            DepartmentName = (_dbContext.Administrator_Department.Where(a => a.DepartmentId == item.DepartmentId).Select(a => a.DepartmentName).FirstOrDefault())
                        });

                if (categoryService?.Any() == true)
                {
                    var response = categoryService.FirstOrDefault();
                    if (response.IsDelete == EnumMethod.TrangThai.Active)
                    {
                        respone.Status = 1;
                        respone.Message = "Lấy thông tin dịch vụ thành công.";
                        respone.Data = response;
                        return createResponse();
                    }
                    else
                    {
                        throw new ArgumentException($"Dịch vụ {response.ServiceName} đã bị vô hiệu.");
                    }
                }
                else
                {
                    throw new ArgumentException($"Dịch vụ có ServiceId {serviceId} không tồn tại.");
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
        public HttpResponseMessage AddCategory_Service(Category_ServiceModel service)
        {
            try
            {
                #region Get DepartmentId From Token

                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                service.DepartmentId = departmentId;
                #endregion

                businessService.AddCategory_Service(service);

                var dichVu = _dbContext.Category_Service.Where(p => p.ServiceName == service.ServiceName && p.ServiceCode == service.ServiceCode).FirstOrDefault();
                if (dichVu != null)
                {
                    respone.Status = 1;
                    respone.Message = "Thêm mới dịch vụ thành công.";
                    respone.Data = dichVu.ServiceId;
                    return createResponse();
                }
                else
                {
                    respone.Status = 0;
                    respone.Message = "Thêm mới dịch vụ không thành công.";
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
        public HttpResponseMessage EditCategory_Service(Category_ServiceModel service)
        {
            try
            {
                var dichVu = _dbContext.Category_Service.Where(p => p.ServiceId == service.ServiceId).FirstOrDefault();
                if (dichVu == null)
                {
                    throw new ArgumentException($"Không tồn tại ServiceId {service.ServiceId}");
                }

                #region Get DepartmentId From Token

                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                service.DepartmentId = departmentId;
                #endregion

                businessService.EditCategory_Service(service);

                respone.Status = 1;
                respone.Message = "Chỉnh sửa dịch vụ thành công.";
                respone.Data = service.ServiceId;

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
        public HttpResponseMessage DeleteCategory_Service(int serviceId)
        {
            try
            {
                var target = _dbContext.Category_Service.Where(item => item.ServiceId == serviceId).FirstOrDefault();
                target.IsDelete = true;
                _dbContext.SaveChanges();

                respone.Status = 1;
                respone.Message = "Xóa dịch vụ thành công.";
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
