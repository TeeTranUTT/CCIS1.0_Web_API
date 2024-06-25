using CCIS_BusinessLogic;
using CCIS_DataAccess;
using ES.CCIS.Host.Helpers;
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

                using (var db = new CCISContext())
                {
                    IEnumerable<Category_FigureBookModel> query;
                    if (departmentId == 0)
                    {
                        query = new List<Category_FigureBookModel>();
                    }
                    else
                    {
                        var listDepartments = DepartmentHelper.GetChildDepIds(departmentId);
                        query = from item in db.Category_FigureBook
                                join a in db.Administrator_Department on item.DepartmentId equals a.DepartmentId
                                where listDepartments.Contains(item.DepartmentId) && item.Status == true
                                select new Category_FigureBookModel
                                {
                                    FigureBookId = item.FigureBookId,
                                    BookCode = item.BookCode,
                                    BookName = item.BookName,
                                    SaveDate = item.SaveDate,
                                    PeriodNumber = item.PeriodNumber,
                                    Status = item.Status,
                                    IsRootBook = item.IsRootBook,
                                    DepartmentName = a.DepartmentName,
                                    DepartmentId = item.DepartmentId,
                                    TeamId = item.TeamId,
                                    BookType = item.BookType
                                };
                    }                    

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
                using (var dbContext = new CCISContext())
                {
                    var figureBook = dbContext.Category_FigureBook.Where(p => p.FigureBookId == FigureBookId).Select(item => new Category_FigureBookModel
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

                using (var db = new CCISContext())
                {
                    var checkFigureBookCodeExisted = db.Category_FigureBook.Any(item => item.BookCode == category_FigureBook.BookCode && item.Status == true);
                    if (checkFigureBookCodeExisted)
                    {                        
                        throw new ArgumentException($"Thêm mới sổ ghi chỉ số không thành công. Đã có sổ này trong hệ thống. Xin kiểm tra lại.");                        
                    }
                   
                    business_FigureBook.AddCategory_FigureBook(category_FigureBook, userId);

                    var figureBook = db.Category_FigureBook.FirstOrDefault(item => item.BookCode == category_FigureBook.BookCode && item.BookName == category_FigureBook.BookName);
                    if (figureBook != null)
                    {
                        //Phân sổ cho tài khoản đăng nhập sau khi thêm mới sổ ghi chỉ số thành công
                        var _BookOfUser = new Administrator_BookOfUser();
                        _BookOfUser.UserId = userId;
                        _BookOfUser.FigureBookId = figureBook.FigureBookId;

                        db.Administrator_BookOfUser.Add(_BookOfUser);
                        db.SaveChanges();

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
                using (var dbContext = new CCISContext())
                {
                    var soGCS = dbContext.Category_FigureBook.Where(p => p.FigureBookId == figureBook.FigureBookId).FirstOrDefault();
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
                using (var db = new CCISContext())
                {
                    var target = db.Category_FigureBook.Where(item => item.FigureBookId == figureBookId).FirstOrDefault();
                    target.Status = false;

                    //xóa cả ở bảng Index_CalendarOfSaveIndex với trường hợp sổ đó đã lập lịch
                    var delete = db.Index_CalendarOfSaveIndex.Where(item => item.FigureBookId == figureBookId).OrderByDescending(it => it.CreateDate).FirstOrDefault();
                    if (delete != null)
                    {
                        db.Index_CalendarOfSaveIndex.Remove(delete);
                    }

                    db.SaveChanges();
                }
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
        public HttpResponseMessage Allocate_FigureBook(List<Category_AllocateFigureBookModel> model, int figureBookId)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    var lstBookOfUser = db.Administrator_BookOfUser.Where(item => item.FigureBookId == figureBookId).ToList();
                    if (lstBookOfUser.Count > 0)
                    {
                        foreach (var item in lstBookOfUser)
                            db.Administrator_BookOfUser.Remove(item);
                    }

                    if (model != null && model.Count > 0)
                        foreach (var item in model)
                        {
                            if (item.IsChecked)
                            {
                                var _BookOfUser = new Administrator_BookOfUser();
                                _BookOfUser.UserId = item.UserId;
                                _BookOfUser.FigureBookId = figureBookId;
                                db.Administrator_BookOfUser.Add(_BookOfUser);
                            }
                        }
                    db.SaveChanges();

                    respone.Status = 1;
                    respone.Message = "Phân sổ ghi chỉ số cho người dùng thành công.";
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
    }
}
