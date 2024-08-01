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
    [RoutePrefix("api/DanhMuc/Kho")]
    public class Category_StockController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_Administrator_Department administrator_Department = new Business_Administrator_Department();
        private readonly Business_Category_Stock business_CategoryStock = new Business_Category_Stock();
        private readonly CCISContext _dbContext;

        public Category_StockController()
        {
            _dbContext = new CCISContext();
        }

        [HttpGet]
        [Route("Category_StockManager")]
        public HttpResponseMessage Category_StockManager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search, [DefaultValue(0)] int departmentId)
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

                var query = _dbContext.Category_Stock.Where(item => listDepartments.Contains(item.DepartmentId)).Select(item => new Category_StockModel
                {
                    DepartmentId = item.DepartmentId,
                    Description = item.Description,
                    StockCode = item.StockCode,
                    StockId = item.StockId
                });

                if (!string.IsNullOrEmpty(search))
                {
                    query = (IQueryable<Category_StockModel>)query.Where(item => item.Description.Contains(search) || item.StockCode.Contains(search));
                }

                var pagedStock = (IPagedList<Category_StockModel>)query.OrderBy(p => p.StockId).ToPagedList(pageNumber, pageSize);

                var response = new
                {
                    pagedStock.PageNumber,
                    pagedStock.PageSize,
                    pagedStock.TotalItemCount,
                    pagedStock.PageCount,
                    pagedStock.HasNextPage,
                    pagedStock.HasPreviousPage,
                    Stocks = pagedStock.ToList()
                };
                respone.Status = 1;
                respone.Message = "Lấy danh sách kho thành công.";
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
        public HttpResponseMessage GetStockById(int StockId)
        {
            try
            {
                if (StockId < 0 || StockId == 0)
                {
                    throw new ArgumentException($"StockId {StockId} không hợp lệ.");
                }

                var Stock = _dbContext.Category_Stock.Where(p => p.StockId == StockId).Select(p => new Category_StockModel
                {
                    StockId = p.StockId,
                    DepartmentId = p.DepartmentId,
                    StockCode = p.StockCode,
                    Description = p.Description
                });

                if (Stock?.Any() == true)
                {
                    var response = Stock.FirstOrDefault();

                    respone.Status = 1;
                    respone.Message = "Lấy thông tin kho thành công.";
                    respone.Data = response;
                    return createResponse();
                }
                else
                {
                    throw new ArgumentException($"Kho có StockId {StockId} không tồn tại.");
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
        public HttpResponseMessage AddCategory_Stock(Category_StockModel model)
        {
            try
            {
                #region Get DepartmentId From Token

                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                model.DepartmentId = departmentId;
                #endregion

                business_CategoryStock.AddCategory_Stock(model);

                var kho = _dbContext.Category_Stock.Where(p => p.StockCode == model.StockCode).FirstOrDefault();
                if (kho != null)
                {
                    respone.Status = 1;
                    respone.Message = "Thêm mới kho thành công.";
                    respone.Data = kho.StockId;
                    return createResponse();
                }
                else
                {
                    respone.Status = 0;
                    respone.Message = "Thêm mới kho không thành công.";
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
        public HttpResponseMessage EditCategory_Stock(Category_StockModel model)
        {
            try
            {
                var kho = _dbContext.Category_Stock.Where(p => p.StockId == model.StockId).FirstOrDefault();
                if (kho == null)
                {
                    throw new ArgumentException($"Không tồn tại StockId {model.StockId}");
                }

                #region Get DepartmentId From Token

                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                model.DepartmentId = departmentId;
                #endregion

                business_CategoryStock.EditCategory_Stock(model);

                respone.Status = 1;
                respone.Message = "Chỉnh sửa kho thành công.";
                respone.Data = model.StockId;

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
        public HttpResponseMessage DeleteCategory_Stock(int stockId)
        {
            try
            {
                var target = _dbContext.Category_Stock.Where(item => item.StockId == stockId).FirstOrDefault();

                _dbContext.Category_Stock.Remove(target);
                _dbContext.SaveChanges();

                respone.Status = 1;
                respone.Message = "Xóa kho thành công.";
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
