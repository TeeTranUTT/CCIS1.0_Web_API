using CCIS_DataAccess;
using ES.CCIS.Host.Helpers;
using ES.CCIS.Host.Models.HoaDon.HoaDonNuoc;
using System;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
namespace ES.CCIS.Host.Controllers.HoaDon.HoaDonNuoc
{
    [Authorize]
    [RoutePrefix("api/WaterBill")]
    public class WaterBillController : ApiBaseController
    {
        private readonly CCISContext _dbContext;

        public WaterBillController()
        {
            _dbContext = new CCISContext();
        }

        [HttpGet]
        [Route("GetWaterBill")]
        public HttpResponseMessage GetWaterBill([DefaultValue(0)] int year, [DefaultValue(0)] int month)
        {
            try
            {
                var model = new WaterBillAsyncDTO();
                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                #region Get Resources (Tránh trường hợp lên View lỗi null)
                //ResMonthString
                model.ResMonthString = "";
                if (month == 0 && year == 0)
                {
                    month = DateTime.Now.Month;
                    year = DateTime.Now.Year;
                }
                model.ResMonthString = string.Format("{0}-{1}", month, year);
                #endregion

                model.Month = month;
                model.Year = year;

                //Lấy danh sách đã đồng bộ về CCIS
                var lstAsync = _dbContext.Log_Async_WaterBill
                               .Where(x => x.Month == month && x.Year == year)
                               .Select(x => new WaterMonthBookModel
                               {
                                   BookCode = x.BookCode,
                                   BookName = x.BookName,
                                   Term = x.Term,
                                   Month = x.Month,
                                   Year = x.Year,
                                   Status = x.Status,
                                   Total = x.Total,
                                   Number = x.Number,
                                   Id = x.Id,
                                   CreatedDate = x.CreatedDate,
                                   FigureBookId = x.FigureBookId
                               }).ToList();

                //Lấy những sổ chưa đồng bộ về CCIS
                var lstLogFiCode = lstAsync.Select(x => x.BookCode).ToList();
                var lstCFigure = _dbContext.Category_FigureBook.Where(x => x.DepartmentId == departmentId && !lstLogFiCode.Contains(x.BookCode) && x.BookType == "TN").Select(x => new WaterMonthBookModel
                {
                    BookCode = x.BookCode,
                    BookName = x.BookName,
                    Term = 1,
                    Month = month,
                    Year = year,
                    Status = 0
                }).ToList();


                lstAsync.AddRange(lstCFigure);
                model.lstFigurebook = lstAsync;

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = model;
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
        [Route("AsyncWaterBill")]
        public HttpResponseMessage AsyncWaterBill([DefaultValue(0)] int month, [DefaultValue(0)] int year, [DefaultValue("")] string bookCode)
        {
            using (var dbContextTransaction = _dbContext.Database.BeginTransaction())
            {
                try
                {
                    var departmentId = TokenHelper.GetDepartmentIdFromToken();

                    if (month < 0 || month > 12)
                    {
                        throw new ArgumentException($"Tháng {month} không hợp lệ.");
                    }

                    var sql = @"EXEC [dbo].[GetWaterBill] @Thang = " + month + ",@Nam = " + year + ",@MaSo = N'" + bookCode + "',@MaDVi = " + departmentId;
                    _dbContext.Database.ExecuteSqlCommand(sql);

                    var bookId = _dbContext.Category_FigureBook.Where(x => x.BookCode.Equals(bookCode)).FirstOrDefault().FigureBookId;
                    var lstBilDetail = _dbContext.Bill_ElectricityBillDetail.Where(x => x.FigureBookId == bookId && x.Month == month && x.Year == year && x.DepartmentId == departmentId).ToList();
                    foreach (var item in lstBilDetail)
                    {
                        item.IndexId = _dbContext.Index_Value.Where(x => x.PointId == item.PointId).Take(1).FirstOrDefault().IndexId;
                    }

                    _dbContext.SaveChanges();
                    dbContextTransaction.Commit();

                    respone.Status = 1;
                    respone.Message = "Đồng bộ thành công.";
                    respone.Data = null;
                    return createResponse();
                }
                catch (Exception ex)
                {
                    dbContextTransaction.Rollback();
                    respone.Status = 0;
                    respone.Message = $"Lỗi: {ex.Message.ToString()}";
                    respone.Data = null;
                    return createResponse();
                }
            }
        }

        [HttpGet]
        [Route("AsyncMaster")]
        public HttpResponseMessage AsyncMaster([DefaultValue(0)] int month, [DefaultValue(0)] int year)
        {
            try
            {
                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                var sql = @"EXEC [dbo].[GetFigureBook] @MaDVi = " + departmentId;
                _dbContext.Database.ExecuteSqlCommand(sql);

                respone.Status = 1;
                respone.Message = "Đồng bộ thành công.";
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
