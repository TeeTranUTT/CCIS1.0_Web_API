using CCIS_BusinessLogic;
using CCIS_DataAccess;
using ES.CCIS.Host.Helpers;
using ES.CCIS.Host.Models;
using ES.CCIS.Host.Models.EnumMethods;
using ES.CCIS.Host.Models.HoaDon;
using ES_CCIS.Models.BillDTO;
using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Web.Configuration;
using System.Web.Http;
using static CCIS_BusinessLogic.DefaultBusinessValue;

namespace ES.CCIS.Host.Controllers.HoaDon
{
    [Authorize]
    [RoutePrefix("api/Bill")]
    public class BillController : ApiBaseController
    {
        private readonly Business_Administrator_Department administrator_Department = new Business_Administrator_Department();
        private readonly Business_Index_CalendarOfSaveIndex SaveAddIndex = new Business_Index_CalendarOfSaveIndex();
        private readonly Business_Bill_ElectricityBill_New businessBillElectricityBill = new Business_Bill_ElectricityBill_New();
        private readonly Business_Bill_TaxInvoice businessBillTaxInvoice = new Business_Bill_TaxInvoice();
        private readonly CCISContext _dbContext;

        public BillController()
        {
            _dbContext = new CCISContext();
        }

        //Tính hóa đơn
        [HttpPost]
        [Route("ElectricityBillCalculator")]
        public HttpResponseMessage ElectricityBillCalculator(ElectricityBillCalculatorInput model)
        {
            try
            {
                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var listDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);

                //Gán = ngày tháng hiện tại nếu chưa chọn tháng tính hóa đơn
                DateTime dFilterDate = DateTime.Now;
                int iMonthSearch = dFilterDate.Month;
                int iYearSearch = dFilterDate.Year;

                if (model.FilterDate != null)
                {
                    iMonthSearch = Convert.ToInt32(model.FilterDate.Substring(0, 2));
                    iYearSearch = Convert.ToInt32(model.FilterDate.Substring(3, 4));
                }
                else
                {
                    model.FilterDate = dFilterDate.ToString("MM-yyyy");
                }

                //Lấy danh mục sổ ghi chỉ số theo điều kiện lọc lập lịch
                var listCalendarOfSaveIndex = _dbContext.Index_CalendarOfSaveIndex.Where(
                        item => listDepartmentId.Contains(item.DepartmentId)
                                && (item.Status == 5 || item.Status == 7)
                                && item.Term == model.Term
                                && item.Year == iYearSearch
                                && item.Month == iMonthSearch
                                && ((model.FigureBookId == 0) ? item.FigureBookId != 0 : item.FigureBookId.Equals(model.FigureBookId))
                                && ((model.SaveDate == "0") ? item.SaveDate != "0" : item.SaveDate.Equals(model.SaveDate)))
                    .Select(item => new Bill_Index_CalendarOfSaveIndexModel
                    {
                        FigureBookId = item.FigureBookId,
                        BookCode = item.Category_FigureBook.BookCode,
                        BookName = item.Category_FigureBook.BookName,
                        Term = item.Term,
                        Month = item.Month,
                        Year = item.Year,
                        SaveDate = item.SaveDate,
                        IsBillCalculator = false,
                        Status = item.Status
                    }).ToList();

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = listCalendarOfSaveIndex;
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
        [Route("CancelBillCalculator")]
        public HttpResponseMessage CancelBillCalculator(CancelBillCalculatorInput model)
        {
            try
            {
                if (model != null)
                {
                    string strKQ = SaveAddIndex.CalBillElectricityByBook(model.FigureBookId, model.Term, model.Month, model.Year);
                    if (strKQ == "OK")
                    {
                        respone.Status = 1;
                        respone.Message = "Hủy tính hóa đơn thành công.";
                        respone.Data = null;
                        return createResponse();
                    }
                    else
                    {
                        throw new ArgumentException($"{strKQ}");
                    }
                }
                else
                {
                    throw new ArgumentException("Hủy tính hóa đơn không thành công.");
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

        //Todo: Chưa viết api tính hóa đơn pElectricityBillCalculator

        [HttpGet]
        [Route("BillPrintingManager")]
        public HttpResponseMessage BillPrintingManager(DateTime? filterDate, [DefaultValue(1)] int term, [DefaultValue(0)] int figureBookId)
        {
            try
            {
                //Gán = ngày tháng hiện tại nếu chưa chọn tháng in hóa đơn
                if (filterDate == null)
                    filterDate = DateTime.Now;

                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var listDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);

                //Lấy danh mục sổ ghi chỉ số theo điều kiện lọc
                var model = _dbContext.Index_CalendarOfSaveIndex
                    .Where(item => item.DepartmentId.Equals(departmentId) && item.Term.Equals(term) &&
                                   item.Year.Equals(filterDate.Value.Year) &&
                                   item.Month.Equals(filterDate.Value.Month) && ((figureBookId == 0)
                                       ? item.FigureBookId != 0
                                       : item.FigureBookId.Equals(figureBookId)))
                    .Select(item => new Index_CalendarOfSaveIndexModel
                    {
                        FigureBookId = item.FigureBookId,
                        BookCode = item.Category_FigureBook.BookCode,
                        BookName = item.Category_FigureBook.BookName,
                        Term = item.Term,
                        Month = item.Month,
                        Year = item.Year
                    }).ToList();

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

        #region Tính hóa đơn GTGT
        [HttpGet]
        [Route("TaxInvoiceCalculator")]
        public HttpResponseMessage TaxInvoiceCalculator(DateTime? filterDate, [DefaultValue(0)] int figureBookId)
        {
            try
            {
                //Gán = ngày tháng hiện tại nếu chưa chọn tháng tính hóa đơn
                if (filterDate == null)
                    filterDate = DateTime.Now;

                int departmentId = TokenHelper.GetDepartmentIdFromToken();
                var listDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);

                var userId = TokenHelper.GetUserIdFromToken();

                //Lấy danh mục sổ ghi chỉ số combobox
                var listFigureBook = DepartmentHelper.GetFigureBook(userId, listDepartmentId);
                var listFigureBookId = listFigureBook.Select(item => item.FigureBookId).ToList();

                List<Index_CalendarOfSaveIndexModel> listCalendarOfSaveIndex = new List<Index_CalendarOfSaveIndexModel>();
                //Lấy danh mục sổ ghi chỉ số theo trạng thái
                if (figureBookId == 0)
                {
                     listCalendarOfSaveIndex = _dbContext.Bill_TaxInvoiceStatus.Where(
                            item => item.Month == filterDate.Value.Month && item.Year == filterDate.Value.Year
                                    && listFigureBookId.Contains(item.FigureBookId))
                        .Select(item => new Index_CalendarOfSaveIndexModel
                        {
                            DepartmentId = item.Category_FigureBook.DepartmentId,
                            FigureBookId = item.FigureBookId,
                            Month = item.Month,
                            Year = item.Year,
                            BookCode = item.Category_FigureBook.BookCode,
                            BookName = item.Category_FigureBook.BookName,
                            IsBillCalculator = false,
                            Status = item.Status
                        }).Distinct().ToList();                    
                }
                else
                {
                     listCalendarOfSaveIndex = _dbContext.Bill_TaxInvoiceStatus.Where(
                            item => item.Month == filterDate.Value.Month && item.Year == filterDate.Value.Year
                                    && item.FigureBookId == figureBookId)
                        .Select(item => new Index_CalendarOfSaveIndexModel
                        {
                            DepartmentId = item.Category_FigureBook.DepartmentId,
                            FigureBookId = item.FigureBookId,
                            Month = item.Month,
                            Year = item.Year,
                            BookCode = item.Category_FigureBook.BookCode,
                            BookName = item.Category_FigureBook.BookName,
                            Status = item.Status
                        }).Distinct().ToList();                    
                }

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = listCalendarOfSaveIndex;
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
        [Route("TaxInvoiceCalculator")]
        public HttpResponseMessage TaxInvoiceCalculator(TaxInvoiceCalculatorInput input)
        {
            using (var dbContextTransaction = _dbContext.Database.BeginTransaction())
            {
                try
                {
                    int departmentId = TokenHelper.GetDepartmentIdFromToken();
                    int userId = TokenHelper.GetUserIdFromToken();
                    var listDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);

                    var result = businessBillTaxInvoice.BillTaxInvoiceCalculator(input.FigureBookId, departmentId, userId, input.Month, input.Year, _dbContext);

                    if (result != "Ok")
                    {
                        dbContextTransaction.Rollback();
                        throw new ArgumentException($"{result}");
                    }
                    dbContextTransaction.Commit();

                    respone.Status = 1;
                    respone.Message = "Tính hóa đơn thành công.";
                    respone.Data = null;
                    return createResponse();
                }
                catch (Exception ex)
                {
                    var message = "Tính hóa đơn không thành công.";
                    dbContextTransaction.Rollback();
                    respone.Status = 0;
                    respone.Message = $"{(!string.IsNullOrEmpty(ex.Message.ToString()) ? ex.Message.ToString() : message)}";
                    respone.Data = null;
                    return createResponse();
                }
            }
        }

        [HttpPost]
        [Route("CancelTaxInvoiceCalculator")]
        public HttpResponseMessage CancelTaxInvoiceCalculator(TaxInvoiceCalculatorInput input)
        {
            try
            {
                int departmentId = TokenHelper.GetDepartmentIdFromToken();
                var listDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);

                var result = businessBillTaxInvoice.CancelBillTaxInvoiceCalculator(input.FigureBookId, departmentId, input.Month, input.Year);

                if (result == "ok")
                {
                    respone.Status = 1;
                    respone.Message = "Hủy tính hóa đơn thành công.";
                    respone.Data = null;
                    return createResponse();                    
                }
                else
                {
                    throw new ArgumentException($"{result}");
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

        //Xác nhận chờ hóa đơn điện tử
        [HttpPost]
        [Route("ConfirmTaxInvoice")]
        public HttpResponseMessage ConfirmTaxInvoice(TaxInvoiceCalculatorInput input)
        {
            try
            {
                int departmentId = TokenHelper.GetDepartmentIdFromToken();
                var listDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);

                // thực hiện chuẩn hóa dữ liệu sang xml
                var target =
                    _dbContext.Bill_TaxInvoiceStatus.Where(
                        item =>
                            item.FigureBookId == input.FigureBookId && item.Month == input.Month && item.Year == input.Year);
                if (target != null && target.Any())
                {
                    target.FirstOrDefault().Status = (int)(StatusCalendarOfSaveIndex.ConfirmData);
                    _dbContext.SaveChanges();
                }

                respone.Status = 1;
                respone.Message = "Xác nhận số liệu thành công.";
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

        //Hủy xác nhận hóa đơn non tải
        [HttpPost]
        [Route("ConfirmCancelTaxInvoice")]
        public HttpResponseMessage ConfirmCancelTaxInvoice(TaxInvoiceCalculatorInput input)
        {
            using (var dbContextTransaction = _dbContext.Database.BeginTransaction())
            {
                try
                {
                    int departmentId = TokenHelper.GetDepartmentIdFromToken();
                    int userId = TokenHelper.GetUserIdFromToken();
                    var listDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);

                    var billTaxInvoiceDetail =
                            _dbContext.Bill_TaxInvoiceDetail.Where(
                                item =>
                                    item.FigureBookId.Equals(input.FigureBookId) & item.Month.Equals(input.Month) &
                                    item.Year.Equals(input.Year) && item.DepartmentId.Equals(departmentId)).ToList();
                    if (billTaxInvoiceDetail.Count != 0)
                    {
                        var target =
                            _dbContext.Bill_TaxInvoiceStatus.Where(
                                item =>
                                    item.FigureBookId == input.FigureBookId && item.Month == input.Month && item.Year == input.Year &&
                                    item.Status == (int)(StatusCalendarOfSaveIndex.ConfirmData));
                        if (target != null && target.Any())
                        {
                            target.FirstOrDefault().Status = (int)(StatusCalendarOfSaveIndex.Gcs);
                            _dbContext.SaveChanges();
                        }
                        dbContextTransaction.Commit();

                        respone.Status = 1;
                        respone.Message = "Hủy xác nhận thành công.";
                        respone.Data = null;
                        return createResponse();
                    }
                    else
                    {
                        throw new ArgumentException("Không tìm thấy hóa đơn.");
                    }
                }
                catch (Exception ex)
                {
                    dbContextTransaction.Rollback();
                    respone.Status = 0;
                    respone.Message = $"{ex.Message.ToString()}";
                    respone.Data = null;
                    return createResponse();
                }
            }
        }
        #endregion

        #region Lập hóa đơn trực tiếp

        #endregion
    }
}
