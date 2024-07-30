using CCIS_BusinessLogic;
using CCIS_DataAccess;
using CCIS_DataAccess.ViewModels;
using ES.CCIS.Host.Helpers;
using ES.CCIS.Host.Models;
using ES.CCIS.Host.Models.EnumMethods;
using ES_CCIS.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Web.Configuration;
using System.Web.Http;

namespace ES.CCIS.Host.Controllers.GiaoThu
{
    [Authorize]
    [RoutePrefix("api/Track_Debt_Delivery")]
    public class Track_Debt_DeliveryController : ApiBaseController
    {
        private readonly CCISContext _dbContext;
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_Liabilities_TrackDebt_TaxInvoice businessInvoiceDelivery = new Business_Liabilities_TrackDebt_TaxInvoice();

        public Track_Debt_DeliveryController()
        {
            _dbContext = new CCISContext();
        }

        [HttpGet]
        [Route("InvoiceDelivery")]
        public HttpResponseMessage InvoiceDelivery(DateTime? filterDate, [DefaultValue("")] string typeStatus, [DefaultValue(0)] int teamId, [DefaultValue(1)] int pageNumber, [DefaultValue(0)] int figureBookId)
        {
            try
            {
                List<Liabilities_FigureBookDeliveryModel> allList = new List<Liabilities_FigureBookDeliveryModel>();
                if (!filterDate.HasValue)
                    filterDate = DateTime.Now;
                int monthsearch = filterDate.Value.Month;
                int yearserach = filterDate.Value.Year;

                int departmentId = TokenHelper.GetDepartmentIdFromToken();

                var selTeam = _dbContext.Category_Team.Where(c => c.Status == true && (departmentId == 0 || c.DepartmentId == departmentId));

                var selFigureBook = _dbContext.Category_FigureBook.Where(item => selTeam.Select(bdo => bdo.TeamId).Contains(item.TeamId.Value) && (departmentId == 0 || item.DepartmentId == departmentId) && (teamId == 0 || item.TeamId == teamId));

                var listFigureBook = selFigureBook.ToList();

                int typeStatusSearch = 0;//0 la chua giao thu; 1 la da giao thu
                if (typeStatus.Length > 0)
                    typeStatusSearch = Int16.Parse(typeStatus);

                var lstDelivery = _dbContext.Liabilities_FigureBookDelivery.Where(i => i.Month == monthsearch && i.Year == yearserach
                                    && (figureBookId == 0 || i.FigureBookId == figureBookId)
                                    && (departmentId == 0 || i.DepartmentId == departmentId)).ToList();

                var lstFigureBookIdDeli = lstDelivery.Select(x => x.FigureBookId).ToList();

                var lstdebt = _dbContext.Liabilities_TrackDebt.Where(i => i.Month == monthsearch && i.Year == yearserach
                                && (figureBookId == 0 || i.FigureBookId == figureBookId)
                                && (departmentId == 0 || i.DepartmentId.Equals(departmentId))
                                && i.Status != 2 && i.Status != 3 && i.Status != 1).ToList();
                switch (typeStatusSearch)
                {
                    case 0:
                        lstdebt = lstdebt.Where(x => x.Status == 0 && !lstFigureBookIdDeli.Contains(x.FigureBookId)).ToList();
                        break;
                    case 1:
                        lstdebt = lstdebt.Where(x => x.Status == 6).ToList();
                        break;
                    case 4:
                        lstdebt = lstdebt.Where(x => x.Status == 4).ToList();
                        break;
                    case 5:
                        lstdebt = lstdebt.Where(x => x.Status == 5).ToList();
                        break;
                    default:
                        break;
                }

                var lstFigureBookDebt = lstdebt.GroupBy(c => new { c.FigureBookId }).Select(d => new
                {
                    FigureBookId = d.Key.FigureBookId,
                    Status = d.Min(k => k.Status),
                    Total = d.Sum(k => k.FundsGenerated + k.TaxesIncurred),
                    SubTotal = d.Sum(k => k.FundsGenerated),
                    VAT = d.Sum(k => k.TaxesIncurred),
                    BillAmount = d.Count()
                }).Where(i => i.Status == typeStatusSearch);
                //  var aa = lstFigureBookDebt.ToList();
                var allList1 = (from i in listFigureBook
                                join p in lstFigureBookDebt
                                on i.FigureBookId equals p.FigureBookId
                                select new Liabilities_FigureBookDeliveryModel
                                {
                                    DeliveryInvoiceId = 0,
                                    DepartmentId = i.DepartmentId,
                                    Month = monthsearch,
                                    Year = yearserach,
                                    SubTotal = p.SubTotal,
                                    VAT = p.VAT,
                                    Total = p.Total,
                                    FigureBookId = p.FigureBookId ?? default(int),
                                    Status = p.Status,

                                    BookName = i.BookType + " - " + i.BookName,
                                    BookCode = i.BookCode,
                                    BillAmount = p.BillAmount,
                                    IsSelected = true,
                                }).ToList();

                if (lstDelivery != null && lstDelivery.Count() > 0)
                {
                    allList = (from i in allList1
                               join u in lstDelivery
                               on i.FigureBookId equals u.FigureBookId into ps
                               from p in ps.DefaultIfEmpty()
                               select new Liabilities_FigureBookDeliveryModel
                               {
                                   DeliveryInvoiceId = p == null ? i.DeliveryInvoiceId : p.DeliveryInvoiceId,
                                   DepartmentId = i.DepartmentId,
                                   Month = p == null ? i.Month : p.Month,
                                   Year = p == null ? i.Year : p.Year,
                                   SubTotal = i.SubTotal,
                                   VAT = i.VAT,
                                   Total = i.Total,
                                   FigureBookId = i.FigureBookId,

                                   Status = i.Status,
                                   BookName = i.BookName,
                                   BookCode = i.BookCode,
                                   BillAmount = i.BillAmount,
                                   IsSelected = i.IsSelected,
                               }).ToList();
                }
                else
                {

                    allList = (from i in allList1
                               select new Liabilities_FigureBookDeliveryModel
                               {
                                   DeliveryInvoiceId = i.DeliveryInvoiceId,
                                   DepartmentId = i.DepartmentId,
                                   Month = i.Month,
                                   Year = i.Year,
                                   SubTotal = i.SubTotal,
                                   VAT = i.VAT,
                                   Total = i.Total,
                                   FigureBookId = i.FigureBookId,

                                   Status = i.Status,
                                   BookName = i.BookName,
                                   BookCode = i.BookCode,
                                   BillAmount = i.BillAmount,
                                   IsSelected = i.IsSelected,
                               }).ToList();
                    if (typeStatusSearch == 1)
                    {
                        allList = allList.Where(r => r.DeliveryInvoiceId != 0).ToList();
                    }
                }

                var allListAlready = allList.ToList();

                var paged = (IPagedList<Liabilities_FigureBookDeliveryModel>)allListAlready.OrderBy(p => p.BookCode).ToPagedList(pageNumber, pageSize);

                var response = new
                {
                    paged.PageNumber,
                    paged.PageSize,
                    paged.TotalItemCount,
                    paged.PageCount,
                    paged.HasNextPage,
                    paged.HasPreviousPage,
                    FigureBookDeliverys = paged.ToList()
                };
                respone.Status = 1;
                respone.Message = "OK";
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

        [HttpPost]
        [Route("InvoiceDelivery")]
        public HttpResponseMessage InvoiceDelivery(InvoiceDeliveryInput input)
        {
            try
            {
                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var userId = TokenHelper.GetUserIdFromToken();

                if (input.FigureBookDeliveryModels?.Any() == true)
                {
                    using (var dbContextTransaction = _dbContext.Database.BeginTransaction())
                    {
                        try
                        {
                            int status = -1;

                            var modelInsertTemp = input.FigureBookDeliveryModels.Where(c => c.IsSelected == true).ToList();

                            var modelInsert = (from i in modelInsertTemp
                                               select new Liabilities_FigureBookDelivery
                                               {
                                                   DepartmentId = i.DepartmentId,
                                                   Month = i.Month,
                                                   Year = i.Year,
                                                   SubTotal = i.SubTotal,
                                                   VAT = i.VAT,
                                                   Total = i.Total,
                                                   FigureBookId = i.FigureBookId,

                                                   BillAmount = i.BillAmount ?? default(int),
                                                   CreateDate = DateTime.Now,
                                                   CreateUser = userId
                                               }).ToList();

                            int month = modelInsert.Select(c => c.Month).FirstOrDefault();
                            int year = modelInsert.Select(c => c.Year).FirstOrDefault();
                            status = businessInvoiceDelivery.InvoiceDeliveryToEmployee(modelInsert, departmentId, userId, month, year, _dbContext, input.EmployeeId);
                            if (status == 0)
                            {
                                dbContextTransaction.Commit();

                                respone.Status = 1;
                                respone.Message = "Giao thu thành công.";
                                respone.Data = null;
                                return createResponse();
                            }
                            else
                            {
                                dbContextTransaction.Rollback();

                                throw new ArgumentException("Giao thu không thành công.");
                            }                           
                                                                                                                                                                
                        }
                        catch (Exception ex)
                        {
                            dbContextTransaction.Rollback();

                            throw new ArgumentException($"Cập nhật không thành công. Lỗi: {ex.Message}");
                        }
                    }
                }
                else
                {
                    throw new ArgumentException("Chưa chọn sổ nào trên lưới.");
                }
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"{ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }

        [HttpPost]
        [Route("CancelInvoiceDelivery")]
        public HttpResponseMessage CancelInvoiceDelivery(CancelInvoiceDeliveryInput input)
        {
            try
            {
                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var userId = TokenHelper.GetUserIdFromToken();

                using (var dbContextTransaction = _dbContext.Database.BeginTransaction())
                {
                    try
                    {
                        int Month = 0;
                        int Year = 0;
                        Year = input.FilterDateSearch.Value.Year;
                        Month = input.FilterDateSearch.Value.Month;
                        int i = businessInvoiceDelivery.DeleteInvoiceDelivery(input.LstDeliveryId, departmentId, Month, Year, userId, _dbContext);

                        if (i == 0)
                        {
                            dbContextTransaction.Commit();

                            respone.Status = 1;
                            respone.Message = "Hủy giao thu thành công.";
                            respone.Data = null;
                            return createResponse();                            
                        }
                        else
                        {
                            dbContextTransaction.Rollback();

                            throw new ArgumentException("Hủy giao thu không thành công.");
                        }
                    }
                    catch (Exception)
                    {
                        dbContextTransaction.Rollback();

                        throw new ArgumentException("Hủy giao thu không thành công.");
                    }
                }
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"{ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }

        #region Hủy giao thu theo khách hàng
        [HttpGet]
        [Route("CustomersCancelInvoice")]
        public HttpResponseMessage CustomersCancelInvoice([DefaultValue("")] string Name, DateTime? saveDate, [DefaultValue(0)] int Term, [DefaultValue(0)] int FigureBookId, [DefaultValue(1)] int pageNumber)
        {
            try
            {
                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var listDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);

                if (saveDate != null)
                {                    
                    int month = saveDate.Value.Month;
                    int year = saveDate.Value.Year;
                    var query = (from no in _dbContext.Liabilities_TrackDebt
                                         where no.Month == month && no.Year == year                                               
                                               && listDepartmentId.Contains(no.DepartmentId)
                                               && (no.Status == 4 || no.Status == 5)
                                         select new Liabilities_TrackDebtModel
                                         {
                                             LiabilitiesId = no.LiabilitiesId,
                                             BillId = no.BillId,
                                             BillType = no.BillType,
                                             CustomerId = no.CustomerId,
                                             CustomerCode = no.CustomerCode,
                                             Name = no.Name,
                                             FundsGenerated = no.FundsGenerated,
                                             TaxesIncurred = no.TaxesIncurred,
                                             Debt = no.Debt,
                                             TaxDebt = no.TaxDebt,
                                             EditDate = no.EditDate,
                                             Status = no.Status,
                                             AddressPoint = no.Concus_ServicePoint.Address,
                                             TermMonthYear = "Kỳ " + no.Term + "/Tháng " + no.Month + "-" + no.Year,

                                         });
                    if (Term != 0)
                    {
                        query = query.Where(item => item.Term == Term);
                    }

                    if (FigureBookId != 0)
                    {
                        query = query.Where(item => item.FigureBookId == FigureBookId);
                    }

                    if (!string.IsNullOrEmpty(Name))
                    {
                        query = query.Where(item => item.Name.Contains(Name) || item.CustomerCode.Contains(Name));
                    }

                    var paged = query.ToPagedList(pageNumber, pageSize);

                    var response = new
                    {
                        paged.PageNumber,
                        paged.PageSize,
                        paged.TotalItemCount,
                        paged.PageCount,
                        paged.HasNextPage,
                        paged.HasPreviousPage,
                        TrackDebts = paged.ToList()
                    };
                    respone.Status = 1;
                    respone.Message = "Lấy danh sách giao thu thành công.";
                    respone.Data = response;
                    return createResponse();

                }
                else
                {
                    throw new ArgumentException("Dữ liệu đầu vào không được để trống.");
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
        [Route("ConfirmInvoiceDelivery")]
        public HttpResponseMessage ConfirmInvoiceDelivery(List<decimal> listBillId)
        {
            try
            {
                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var userId = TokenHelper.GetUserIdFromToken();

                using (var dbContextTransaction = _dbContext.Database.BeginTransaction())
                {
                    try
                    {                        
                        int i = businessInvoiceDelivery.DeleteInvoiceDelivery_Customers(listBillId, _dbContext);

                        if (i == 0)
                        {
                            dbContextTransaction.Commit();

                            respone.Status = 1;
                            respone.Message = "Hủy giao thu thành công.";
                            respone.Data = null;
                            return createResponse();
                        }
                        else
                        {
                            dbContextTransaction.Rollback();

                            throw new ArgumentException("Hủy giao thu không thành công.");
                        }
                    }
                    catch (Exception ex)
                    {
                        dbContextTransaction.Rollback();

                        throw new ArgumentException("Hủy giao thu không thành công.");
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
        [Route("ConfirmInvoiceDeliveryExcel")]
        public HttpResponseMessage ConfirmInvoiceDeliveryExcel(List<ExcelCancelInvoiceModel> listCustomer)
        {
            try
            {
                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var userId = TokenHelper.GetUserIdFromToken();

                using (var dbContextTransaction = _dbContext.Database.BeginTransaction())
                {

                    try
                    {
                        //var convertBill = Convert.ToDecimal(billId);
                        string result = "";
                        int totalSucess = 0;                        
                        List<ExcelCancelInvoiceModel> model = businessInvoiceDelivery.DeleteInvoiceDelivery_Customers_Excel(listCustomer, departmentId, _dbContext, userId, ref result, ref totalSucess);
                        if (result == "OK")
                        {
                            dbContextTransaction.Commit();

                            respone.Status = 1;
                            respone.Message = $"Hủy giao thu thành công {totalSucess}/{model.Count(i => i.ISCHECKED)} khách hàng đã chọn.";
                            respone.Data = model;
                            return createResponse();                            
                        }
                        else
                        {
                            dbContextTransaction.Rollback();

                            throw new ArgumentException($"Hủy giao thu không thành công. Lỗi: {result}");                           
                        }                        
                    }
                    catch (Exception ex)
                    {
                        dbContextTransaction.Rollback();

                        throw new ArgumentException($"Hủy giao thu không thành công. Lỗi: {ex.Message}");                        
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
        #endregion

        #region Class
        public class InvoiceDeliveryInput
        {
            public List<Liabilities_FigureBookDeliveryModel> FigureBookDeliveryModels { get; set; }
            public string FilterDateSearch { get; set; }
            public string TypeStatusSearch { get; set; }
            public int TeamIdSearch { get; set; }
            public int EmployeeId { get; set; }
        }

        public class CancelInvoiceDeliveryInput
        {
            public List<int> LstDeliveryId { get; set; }
            public DateTime? FilterDateSearch { get; set; }
            //public string TypeStatusSearch { get; set; }
            //public int? TeamIdSearch { get; set; }

        }
        #endregion
    }
}
