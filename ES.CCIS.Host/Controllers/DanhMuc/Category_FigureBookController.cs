using CCIS_BusinessLogic;
using CCIS_DataAccess;
using ES.CCIS.Host.Helpers;
using ES.CCIS.Host.Models;
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
    [RoutePrefix("api/DanhMuc/SoGCS")]
    public class Category_FigureBookController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_Administrator_Department administrator_Department = new Business_Administrator_Department();
        private readonly Business_Category_FigureBook business_FigureBook = new Business_Category_FigureBook();
        private readonly CCISContext _dbContext;

        public Category_FigureBookController()
        {
            _dbContext = new CCISContext();
        }

        [HttpGet]
        [Route("Category_FigureBookManager")]
        public HttpResponseMessage Category_FigureBookManager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search, [DefaultValue(0)] int departmentId)
        {
            try
            {
                //Thong tin user from token                
                var userInfo = TokenHelper.GetUserInfoFromRequest();
                if (departmentId == 0)
                    departmentId = TokenHelper.GetDepartmentIdFromToken();

                //list đơn vị con của user đăng nhập
                var lstDepartmentIds = DepartmentHelper.GetChildDepIdsByUser(userInfo.UserName);

                IQueryable<Category_FigureBookModel> query;

                var listDepartments = DepartmentHelper.GetChildDepIds(departmentId);

                query = _dbContext.Category_FigureBook.Where(item => listDepartments.Contains(item.DepartmentId) && item.Status == true).Select(item => new Category_FigureBookModel
                {
                    FigureBookId = item.FigureBookId,
                    BookCode = item.BookCode,
                    BookName = item.BookName,
                    SaveDate = item.SaveDate,
                    PeriodNumber = item.PeriodNumber,
                    Status = item.Status,
                    IsRootBook = item.IsRootBook,
                    DepartmentName = _dbContext.Administrator_Department.Where(p => p.DepartmentId == item.DepartmentId).Select(p => p.DepartmentName).FirstOrDefault(),
                    DepartmentId = item.DepartmentId,
                    TeamId = item.TeamId,
                    BookType = item.BookType
                });


                if (!string.IsNullOrEmpty(search))
                {
                    query = (IQueryable<Category_FigureBookModel>)query.Where(item => item.BookName.Contains(search) || item.BookCode.Contains(search));
                }

                var pagedFigureBook = (IPagedList<Category_FigureBookModel>)query.OrderBy(p => p.FigureBookId).ToPagedList(pageNumber, pageSize);

                var response = new
                {
                    pagedFigureBook.PageNumber,
                    pagedFigureBook.PageSize,
                    pagedFigureBook.TotalItemCount,
                    pagedFigureBook.PageCount,
                    pagedFigureBook.HasNextPage,
                    pagedFigureBook.HasPreviousPage,
                    Routes = pagedFigureBook.ToList()
                };
                respone.Status = 1;
                respone.Message = "Lấy danh sách sổ ghi chỉ số thành công.";
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
        public HttpResponseMessage GetFigureBookById(int FigureBookId)
        {
            try
            {
                if (FigureBookId < 0 || FigureBookId == 0)
                {
                    throw new ArgumentException($"FigureBookId {FigureBookId} không hợp lệ.");
                }
                var figureBook = _dbContext.Category_FigureBook.Where(p => p.FigureBookId == FigureBookId).Select(item => new Category_FigureBookModel
                {
                    FigureBookId = item.FigureBookId,
                    BookCode = item.BookCode,
                    BookName = item.BookName,
                    SaveDate = item.SaveDate,
                    PeriodNumber = item.PeriodNumber,
                    Status = item.Status,
                    IsRootBook = item.IsRootBook,
                    DepartmentId = item.DepartmentId,
                    TeamId = item.TeamId,
                    BookType = item.BookType
                });

                if (figureBook?.Any() == true)
                {
                    var response = figureBook.FirstOrDefault();
                    if ((bool)response.Status)
                    {
                        respone.Status = 1;
                        respone.Message = "Lấy thông tin sổ ghi chỉ số thành công.";
                        respone.Data = response;
                        return createResponse();
                    }
                    else
                    {
                        throw new ArgumentException($"Sổ ghi chỉ số {response.BookName} đã bị vô hiệu.");
                    }
                }
                else
                {
                    throw new ArgumentException($"Sổ ghi chỉ số có FigureBookId {FigureBookId} không tồn tại.");
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
        public HttpResponseMessage AddCategory_FigureBook(Category_FigureBookModel category_FigureBook)
        {
            try
            {
                //Thong tin user from token                
                var userInfo = TokenHelper.GetUserInfoFromRequest();
                var userId = TokenHelper.GetUserIdFromToken();

                #region Get DepartmentId From Token

                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                category_FigureBook.DepartmentId = departmentId;
                #endregion

                var checkFigureBookCodeExisted = _dbContext.Category_FigureBook.Any(item => item.BookCode == category_FigureBook.BookCode && item.Status == true);
                if (checkFigureBookCodeExisted)
                {
                    throw new ArgumentException($"Thêm mới sổ ghi chỉ số không thành công. Đã có sổ này trong hệ thống. Xin kiểm tra lại.");
                }

                business_FigureBook.AddCategory_FigureBook(category_FigureBook, userId);

                var figureBook = _dbContext.Category_FigureBook.FirstOrDefault(item => item.BookCode == category_FigureBook.BookCode && item.BookName == category_FigureBook.BookName);
                if (figureBook != null)
                {
                    //Phân sổ cho tài khoản đăng nhập sau khi thêm mới sổ ghi chỉ số thành công
                    var _BookOfUser = new Administrator_BookOfUser();
                    _BookOfUser.UserId = userId;
                    _BookOfUser.FigureBookId = figureBook.FigureBookId;

                    _dbContext.Administrator_BookOfUser.Add(_BookOfUser);
                    _dbContext.SaveChanges();

                    respone.Status = 1;
                    respone.Message = "Thêm mới sổ ghi chỉ số thành công.";
                    respone.Data = figureBook.FigureBookId;
                    return createResponse();
                }
                else
                {
                    respone.Status = 0;
                    respone.Message = "Thêm mới sổ ghi chỉ số không thành công.";
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
        public HttpResponseMessage EditCategory_FigureBook(Category_FigureBookModel figureBook)
        {
            try
            {                
                    var soGCS = _dbContext.Category_FigureBook.Where(p => p.FigureBookId == figureBook.FigureBookId).FirstOrDefault();
                    if (soGCS == null)
                    {
                        throw new ArgumentException($"Không tồn tại FigureBookId {figureBook.FigureBookId}");
                    }

                    #region Get DepartmentId From Token

                    var departmentId = TokenHelper.GetDepartmentIdFromToken();

                    figureBook.DepartmentId = departmentId;
                    #endregion

                    business_FigureBook.EditCategory_FigureBook(figureBook);

                    respone.Status = 1;
                    respone.Message = "Chỉnh sửa sổ ghi chỉ số thành công.";
                    respone.Data = figureBook.FigureBookId;

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
        public HttpResponseMessage DeleteCategory_FigureBook(int figureBookId)
        {
            try
            {
                var target = _dbContext.Category_FigureBook.Where(item => item.FigureBookId == figureBookId).FirstOrDefault();
                target.Status = false;

                //xóa cả ở bảng Index_CalendarOfSaveIndex với trường hợp sổ đó đã lập lịch
                var delete = _dbContext.Index_CalendarOfSaveIndex.Where(item => item.FigureBookId == figureBookId).OrderByDescending(it => it.CreateDate).FirstOrDefault();
                if (delete != null)
                {
                    _dbContext.Index_CalendarOfSaveIndex.Remove(delete);
                }

                _dbContext.SaveChanges();

                respone.Status = 1;
                respone.Message = "Xóa sổ ghi chỉ số thành công.";
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
        [Route("PhanSoGCS")]
        public HttpResponseMessage Allocate_FigureBook(Category_AllocateFigureBookModelInput input)
        {
            try
            {
                var lstBookOfUser = _dbContext.Administrator_BookOfUser.Where(item => item.FigureBookId == input.FigureBookId).ToList();
                if (lstBookOfUser.Count > 0)
                {
                    foreach (var item in lstBookOfUser)
                        _dbContext.Administrator_BookOfUser.Remove(item);
                }

                if (input.ListUser != null && input.ListUser.Count > 0)
                    foreach (var item in input.ListUser)
                    {
                        if (item.IsChecked)
                        {
                            var _BookOfUser = new Administrator_BookOfUser();
                            _BookOfUser.UserId = item.UserId;
                            _BookOfUser.FigureBookId = input.FigureBookId;
                            _dbContext.Administrator_BookOfUser.Add(_BookOfUser);
                        }
                    }
                _dbContext.SaveChanges();

                respone.Status = 1;
                respone.Message = "Phân sổ ghi chỉ số cho người dùng thành công.";
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
