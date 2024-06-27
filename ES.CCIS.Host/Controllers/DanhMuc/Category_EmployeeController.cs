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
    [RoutePrefix("api/DanhMuc/NhanVien")]
    public class Category_EmployeeController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_Administrator_Department administrator_Department = new Business_Administrator_Department();
        private readonly Business_Category_Employee business_Category_Employee = new Business_Category_Employee();

        [HttpGet]
        [Route("Category_EmployeeManager")]
        public HttpResponseMessage Category_EmployeeManager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search, [DefaultValue(0)] int departmentId)
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

                using (var db = new CCISContext())
                {
                    var query = db.Category_Employee.Where(item => listDepartments.Contains(item.DepartmentId) && item.Status == true).Select(item => new Category_EmployeeModel
                    {
                        DepartmentId = item.DepartmentId,
                        EmployeeCode = item.EmployeeCode,
                        EmployeeId = item.EmployeeId,
                        FullName = item.FullName,
                        Status = item.Status,
                        Type = item.Type
                    });

                    if (!string.IsNullOrEmpty(search))
                    {
                        query = (IQueryable<Category_EmployeeModel>)query.Where(item => item.FullName.Contains(search) || item.EmployeeCode.Contains(search));
                    }

                    var pagedEmployee = (IPagedList<Category_EmployeeModel>)query.OrderBy(p => p.EmployeeId).ToPagedList(pageNumber, pageSize);

                    var response = new
                    {
                        pagedEmployee.PageNumber,
                        pagedEmployee.PageSize,
                        pagedEmployee.TotalItemCount,
                        pagedEmployee.PageCount,
                        pagedEmployee.HasNextPage,
                        pagedEmployee.HasPreviousPage,
                        Employees = pagedEmployee.ToList()
                    };
                    respone.Status = 1;
                    respone.Message = "Lấy danh sách nhân viên thành công.";
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
        public HttpResponseMessage GetEmployeeById(int employeeId)
        {
            try
            {
                if (employeeId < 0 || employeeId == 0)
                {
                    throw new ArgumentException($"EmployeeId {employeeId} không hợp lệ.");
                }
                using (var dbContext = new CCISContext())
                {
                    var employee = dbContext.Category_Employee.Where(p => p.EmployeeId == employeeId).Select(item => new Category_EmployeeModel
                    {
                        DepartmentId = item.DepartmentId,
                        EmployeeCode = item.EmployeeCode,
                        EmployeeId = item.EmployeeId,
                        FullName = item.FullName,
                        Status = item.Status,
                        Type = item.Type
                    });

                    if (employee?.Any() == true)
                    {
                        var response = employee.FirstOrDefault();
                        if (response.Status)
                        {
                            respone.Status = 1;
                            respone.Message = "Lấy thông tin nhân viên thành công.";
                            respone.Data = response;
                            return createResponse();
                        }
                        else
                        {
                            throw new ArgumentException($"Nhân viên {response.FullName} đã bị vô hiệu.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"Nhân viên có EmployeeId {employeeId} không tồn tại.");
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
        public HttpResponseMessage AddCategory_Employee(Category_EmployeeModel model)
        {
            try
            {
                #region Get DepartmentId From Token

                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                model.DepartmentId = departmentId;
                #endregion
                var ktra = business_Category_Employee.GetCategory_Employee(model.EmployeeCode);
                if (ktra != null)
                {
                    throw new ArgumentException("Mã nhân viên đã tồn tại.");
                }
                else
                {
                    business_Category_Employee.AddCategory_Employee(model);

                    using (var dbContext = new CCISContext())
                    {
                        var nhanVien = dbContext.Category_Employee.Where(p => p.FullName == model.FullName && p.EmployeeCode == model.EmployeeCode).FirstOrDefault();
                        if (nhanVien != null)
                        {
                            respone.Status = 1;
                            respone.Message = "Thêm mới nhân viên thành công.";
                            respone.Data = nhanVien.EmployeeId;
                            return createResponse();
                        }
                        else
                        {
                            respone.Status = 0;
                            respone.Message = "Thêm mới nhân viên không thành công.";
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
        public HttpResponseMessage EditCategory_Employee(Category_EmployeeModel model)
        {
            try
            {
                using (var dbContext = new CCISContext())
                {
                    var nhanVien = dbContext.Category_Employee.Where(p => p.EmployeeId == model.EmployeeId).FirstOrDefault();
                    if (nhanVien == null)
                    {
                        throw new ArgumentException($"Không tồn tại EmployeeId {model.EmployeeId}");
                    }

                    business_Category_Employee.EditCategory_Employee(model);

                    respone.Status = 1;
                    respone.Message = "Chỉnh sửa nhân viên thành công.";
                    respone.Data = model.EmployeeId;

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
        public HttpResponseMessage DeleteCategory_Employee(int employeeId)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    var target = db.Category_Employee.Where(item => item.EmployeeId == employeeId).FirstOrDefault();
                    target.Status = false;
                    db.SaveChanges();
                }
                respone.Status = 1;
                respone.Message = "Xóa nhân viên thành công.";
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
