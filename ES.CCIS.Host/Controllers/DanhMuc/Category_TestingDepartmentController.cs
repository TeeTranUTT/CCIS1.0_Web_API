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
    [RoutePrefix("api/DanhMuc/DonViKiemDinh")]
    public class Category_TestingDepartmentController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_Administrator_Department administrator_Department = new Business_Administrator_Department();
        private readonly Business_Category_TestingDepartment business_Category_TestingDepartment = new Business_Category_TestingDepartment();
        private readonly CCISContext _dbContext;

        public Category_TestingDepartmentController()
        {
            _dbContext = new CCISContext();
        }

        [HttpGet]
        [Route("Category_TestingDepartmentManager")]
        public HttpResponseMessage Category_TestingDepartmentManager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search)
        {
            try
            {
                var query = _dbContext.Category_TestingDepartment.Select(item => new Category_TestingDepartmentModel
                {
                    Note = item.Note,
                    TestingDepartmentCode = item.TestingDepartmentCode,
                    TestingDepartmentId = item.TestingDepartmentId,
                    TestingDepartmentName = item.TestingDepartmentName
                });

                if (!string.IsNullOrEmpty(search))
                {
                    query = (IQueryable<Category_TestingDepartmentModel>)query.Where(item => item.TestingDepartmentName.Contains(search) || item.TestingDepartmentCode.Contains(search));
                }

                var pagedTestingDepartmentpeId = (IPagedList<Category_TestingDepartmentModel>)query.OrderBy(p => p.TestingDepartmentId).ToPagedList(pageNumber, pageSize);

                var response = new
                {
                    pagedTestingDepartmentpeId.PageNumber,
                    pagedTestingDepartmentpeId.PageSize,
                    pagedTestingDepartmentpeId.TotalItemCount,
                    pagedTestingDepartmentpeId.PageCount,
                    pagedTestingDepartmentpeId.HasNextPage,
                    pagedTestingDepartmentpeId.HasPreviousPage,
                    TestingDepartmentpeIds = pagedTestingDepartmentpeId.ToList()
                };
                respone.Status = 1;
                respone.Message = "Lấy danh sách đơn vị kiểm định thành công.";
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
        public HttpResponseMessage GetTestingDepartmentById(int testingDepartmentId)
        {
            try
            {
                if (testingDepartmentId < 0 || testingDepartmentId == 0)
                {
                    throw new ArgumentException($"TestingDepartmentId {testingDepartmentId} không hợp lệ.");
                }
                var testingDepartment = _dbContext.Category_TestingDepartment.Where(p => p.TestingDepartmentId == testingDepartmentId).Select(item => new Category_TestingDepartmentModel
                {
                    Note = item.Note,
                    TestingDepartmentCode = item.TestingDepartmentCode,
                    TestingDepartmentId = item.TestingDepartmentId,
                    TestingDepartmentName = item.TestingDepartmentName
                });

                if (testingDepartment?.Any() == true)
                {
                    var response = testingDepartment.FirstOrDefault();

                    respone.Status = 1;
                    respone.Message = "Lấy thông tin đơn vị kiểm định thành công.";
                    respone.Data = response;
                    return createResponse();

                }
                else
                {
                    throw new ArgumentException($"Đơn vị kiểm định có TestingDepartmentId {testingDepartmentId} không tồn tại.");
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
        public HttpResponseMessage AddCategory_TestingDepartment(Category_TestingDepartmentModel model)
        {
            try
            {
                business_Category_TestingDepartment.AddCategory_TestingDepartment(model);

                var donViKiemDinh = _dbContext.Category_TestingDepartment.Where(p => p.TestingDepartmentName == model.TestingDepartmentName && p.TestingDepartmentCode == model.TestingDepartmentCode).FirstOrDefault();
                if (donViKiemDinh != null)
                {
                    respone.Status = 1;
                    respone.Message = "Thêm mới đơn vị kiểm định thành công.";
                    respone.Data = donViKiemDinh.TestingDepartmentId;
                    return createResponse();
                }
                else
                {
                    respone.Status = 0;
                    respone.Message = "Thêm mới đơn vị kiểm định không thành công.";
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
        public HttpResponseMessage EditCategory_TestingDepartment(Category_TestingDepartmentModel model)
        {
            try
            {
                var donViKiemDinh = _dbContext.Category_TestingDepartment.Where(p => p.TestingDepartmentId == model.TestingDepartmentId).FirstOrDefault();
                if (donViKiemDinh == null)
                {
                    throw new ArgumentException($"Không tồn tại TestingDepartmentId {model.TestingDepartmentId}");
                }


                business_Category_TestingDepartment.EditCategory_TestingDepartment(model);

                respone.Status = 1;
                respone.Message = "Chỉnh sửa đơn vị kiểm định thành công.";
                respone.Data = model.TestingDepartmentId;

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
        public HttpResponseMessage DeleteCategory_TestingDepartment(int testingDepartmentId)
        {
            try
            {
                var target = _dbContext.Category_TestingDepartment.Where(item => item.TestingDepartmentId == testingDepartmentId).FirstOrDefault();
                _dbContext.Category_TestingDepartment.Remove(target);
                _dbContext.SaveChanges();

                respone.Status = 1;
                respone.Message = "Xóa đơn vị kiểm định thành công.";
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
