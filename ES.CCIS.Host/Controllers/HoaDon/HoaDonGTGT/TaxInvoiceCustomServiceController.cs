using CCIS_BusinessLogic;
using CCIS_DataAccess;
using ES.CCIS.Host.Helpers;
using ES.CCIS.Host.Models;
using ES.CCIS.Host.Models.EnumMethods;
using ES.CCIS.Host.Models.HoaDon.HoaDonGTGT;
using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Web.Configuration;
using System.Web.Http;
using static ES.CCIS.Host.Models.EnumMethods.EnumMethod;

namespace ES.CCIS.Host.Controllers.HoaDon.HoaDonGTGT
{
    [Authorize]
    [RoutePrefix("api/TaxInvoiceCustomService")]
    public class TaxInvoiceCustomServiceController : ApiBaseController
    {
        private readonly Business_Bill_TaxInvoice businessBillTaxInvoice = new Business_Bill_TaxInvoice();
        #region Tính hóa đơn GTGT
        [HttpGet]
        [Route("TaxInvoiceCalculator_EMB")]
        public HttpResponseMessage TaxInvoiceCalculator_EMB(DateTime? filterDate, [DefaultValue(0)] int figureBookId)
        {
            try
            {
                //Gán = ngày tháng hiện tại nếu chưa chọn tháng tính hóa đơn
                if (filterDate == null)
                    filterDate = DateTime.Now;

                //Lấy id đơn vị theo người đăng nhập
                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var userId = TokenHelper.GetUserIdFromToken();
                var listDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);

                using (var db = new CCISContext())
                {
                    //Lấy danh mục sổ ghi chỉ số combobox
                    var listFigureBook = DepartmentHelper.GetFigureBook(userId, listDepartmentId);

                    var listFigureBookId = listFigureBook.Select(item => item.FigureBookId).ToList();

                    var response = new List<Index_CalendarOfSaveIndexModel>();
                    //Lấy danh mục sổ ghi chỉ số theo trạng thái
                    if (figureBookId == 0)
                    {
                        response = db.Bill_TaxInvoiceStatus.Where(
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
                        response = db.Bill_TaxInvoiceStatus.Where(
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

        [HttpPost]
        [Route("TaxInvoiceCalculator_EMB")]
        public HttpResponseMessage TaxInvoiceCalculator_EMB(TaxInvoiceCalculator_EMBInput input)
        {
            using (var db = new CCISContext())
            {
                using (var dbContextTransaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        //Lấy id đơn vị theo người đăng nhập
                        int departmentId = TokenHelper.GetDepartmentIdFromToken();
                        int userId = TokenHelper.GetUserIdFromToken();
                        var listDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);

                        var result = businessBillTaxInvoice.TinhPhiDichVu_EMB(input.FigureBookId, departmentId, userId, input.Month, input.Year, db);
                        if (result != "Ok")
                        {
                            dbContextTransaction.Rollback();
                            throw new ArgumentException("Tính hóa đơn không thành công.");

                        }
                        dbContextTransaction.Commit();

                        respone.Status = 1;
                        respone.Message = "Tính hóa đơn thành công.";
                        respone.Data = null;
                        return createResponse();
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
        }

        //Todo: api TaxInvoiceCalculator_ThanhHoa thấy giống với api TaxInvoiceCalculator_EMB ở bên trên

        [HttpPost]
        [Route("CancelTaxInvoiceCalculator")]
        public HttpResponseMessage CancelTaxInvoiceCalculator(CancelTaxInvoiceCalculatorInput input)
        {
            try
            {
                //Lấy id đơn vị theo người đăng nhập
                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var listDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);

                var result = businessBillTaxInvoice.HuyTinhPhiDichVu(input.FigureBookId, departmentId, input.Month, input.Year, input.ServiceTypeIds);
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
                respone.Message = $"{ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }

        // xác nhận chờ ký hóa đơn điện tử
        [HttpPost]
        [Route("ConfirmTaxInvoice")]
        public HttpResponseMessage ConfirmTaxInvoice(ConfirmTaxInvoiceInput input)
        {
            try
            {
                //Lấy id đơn vị theo người đăng nhập
                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var listDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);

                using (var db = new CCISContext())
                {
                    var target =
                        db.Bill_TaxInvoiceStatus.Where(
                            item =>
                                item.FigureBookId == input.FigureBookId && item.Month == input.Month && item.Year == input.Year);
                    if (target != null && target.Any())
                    {
                        target.FirstOrDefault().Status = (int)(StatusCalendarOfSaveIndex.ConfirmData);
                        db.SaveChanges();
                    }

                    respone.Status = 1;
                    respone.Message = "OK";
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

        //Hủy xác nhận hóa đơn non tải
        [HttpPost]
        [Route("ConfirmCancelTaxInvoice")]
        public HttpResponseMessage ConfirmCancelTaxInvoice(ConfirmCancelTaxInvoiceInput input)
        {
            using (var db = new CCISContext())
            {
                using (var dbContextTransaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        //Lấy id đơn vị theo người đăng nhập
                        var departmentId = TokenHelper.GetDepartmentIdFromToken();
                        var userId = TokenHelper.GetUserIdFromToken();
                        var listDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);

                        var billTaxInvoiceDetail =
                            db.Bill_TaxInvoiceDetail.Where(
                                item =>
                                    item.FigureBookId.Equals(input.FigureBookId) & item.Month.Equals(input.Month) &
                                    item.Year.Equals(input.Year) && item.DepartmentId.Equals(departmentId)).ToList();
                        if (billTaxInvoiceDetail?.Any() == true)
                        {
                            var target =
                                db.Bill_TaxInvoiceStatus.Where(
                                    item =>
                                        item.FigureBookId == input.FigureBookId && item.Month == input.Month && item.Year == input.Year &&
                                        item.Status == (int)(StatusCalendarOfSaveIndex.ConfirmData));
                            if (target != null && target.Any())
                            {
                                target.FirstOrDefault().Status = (int)(StatusCalendarOfSaveIndex.Gcs);
                                db.SaveChanges();
                            }
                            dbContextTransaction.Commit();

                            respone.Status = 1;
                            respone.Message = "Hủy xác nhận thành công.";
                            respone.Data = null;
                            return createResponse();
                        }
                        else
                        {
                            throw new ArgumentException("Hủy xác nhận số liệu không thành công.");
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
        }
        #endregion

    }
}
