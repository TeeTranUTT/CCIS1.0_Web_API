using CCIS_BusinessLogic;
using CCIS_BusinessLogic.CustomBusiness.Models;
using CCIS_BusinessLogic.CustomBusiness.TaxInVoice_Bill;
using CCIS_DataAccess;
using ES.CCIS.Host.Helpers;
using ES.CCIS.Host.Models;
using ES.CCIS.Host.Models.EnumMethods;
using ES.CCIS.Host.Models.HoaDon;
using ES_CCIS.Models.BillDTO;
using Microsoft.Ajax.Utilities;
using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Web.Configuration;
using System.Web.Http;
using static CCIS_BusinessLogic.DefaultBusinessValue;
using static ES.CCIS.Host.Models.EnumMethods.EnumMethod;
using StatusCalendarOfSaveIndex = CCIS_BusinessLogic.DefaultBusinessValue.StatusCalendarOfSaveIndex;

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
        private readonly Busniess_Bill_ElectricityBillAdjustmentDetail electricityBillAdjustmentDetail = new Busniess_Bill_ElectricityBillAdjustmentDetail();
        private readonly BusinessBill_IndexValueAdjustment businessBill_IndexValueAdjustment = new BusinessBill_IndexValueAdjustment();
        private readonly Business_Bill_ElectricityBillAdjustment electricityBillAdjustmen = new Business_Bill_ElectricityBillAdjustment();
        private readonly Business_Liabilities_TrackDebt bsTrackDebt = new Business_Liabilities_TrackDebt();
        private readonly Liabilities_TrackDebtModel trackdebt = new Liabilities_TrackDebtModel();
        private readonly Business_Bill_AdjustmentReport business_Bill_AdjustmentReport = new Business_Bill_AdjustmentReport();
        private readonly Business_Bill_AdjustmentDetailLamThinh billAdjustment = new Business_Bill_AdjustmentDetailLamThinh();
        private readonly Bussiness_TaxInVoice_Bill business_Bill_TaxInvoice = new Bussiness_TaxInVoice_Bill();
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
        [HttpPost]
        [Route("DeleteBill_ElectricityBillAdjustment")]
        public HttpResponseMessage DeleteBill_ElectricityBillAdjustment(int billId)
        {
            using (var dbContextTransaction = _dbContext.Database.BeginTransaction())
            {
                try
                {
                    // với trường hợp đây là hóa đơn hủy bỏ 
                    //- mới lập đến hóa đơn hủy bỏ trạng thái = 2, khi xóa đi thì update trang thai ve 0  (chi xoa khi = 2)
                    // - trương hop da làm den huy bo (trang thai = 3) khi xóa đi update trang thai = 2 (chi xoa khi = 3)
                    var ds = _dbContext.Bill_ElectricityBillAdjustment
                        .Where(item => item.BillId.Equals(billId)).FirstOrDefault();
                    decimal billAdjustmentId = Convert.ToDecimal(ds.BillAdjustmentId);
                    decimal BillId = Convert.ToDecimal(ds.BillId);
                    string adjustmentType = ds.AdjustmentType;
                    int status = _dbContext.Liabilities_TrackDebt.Where(item => item.BillId.Equals(billAdjustmentId))
                        .Select(item => item.Status).FirstOrDefault();
                    int statusHdHienTai = _dbContext.Liabilities_TrackDebt.Where(item => item.BillId.Equals(BillId))
                        .Select(item => item.Status).FirstOrDefault();
                    int count = _dbContext.Liabilities_TrackDebt.Where(item => item.BillId.Equals(BillId)).Count();

                    if (statusHdHienTai == 0)
                    {
                        if (status == 2 && adjustmentType == EnumMethod.D_TinhChatHoaDon.HuyBo)
                        {
                            // được xóa và  update trang thai id sua sai ve 0
                            trackdebt.BillId = billAdjustmentId;
                            trackdebt.Status = 0;
                            bsTrackDebt.Updata_Status_Liabilities_TrackDebt(trackdebt, _dbContext);
                        }
                        if (status == 3 && adjustmentType == EnumMethod.D_TinhChatHoaDon.LapLai)
                        {
                            // được xóa, nhưng update trang thai id sua sai ve 2
                            trackdebt.BillId = billAdjustmentId;
                            trackdebt.Status = 2;
                            bsTrackDebt.Updata_Status_Liabilities_TrackDebt(trackdebt, _dbContext);
                        }

                        // đã ký hóa đơn lâp lại, không được xóa hóa đơn hủy bỏ trước đấy
                        if ((count == 0 && adjustmentType != EnumMethod.D_TinhChatHoaDon.HuyBo) || (status != 3 && adjustmentType == EnumMethod.D_TinhChatHoaDon.HuyBo))
                        {
                            Bill_ElectricityBillAdjustment model = new Bill_ElectricityBillAdjustment();
                            model.BillId = billId;

                            // xóa hóa đơn sửa sai
                            electricityBillAdjustmen.DeleteElectricityBillAdjustment(model, _dbContext);
                            // xóa chi tiết hóa đơn sửa sai
                            electricityBillAdjustmentDetail.DeleteElectricityBillAdjustmentDetail(model, _dbContext);
                            // xóa chỉ số sửa sai
                            businessBill_IndexValueAdjustment.DeleteIndexValueAdjustment(model, _dbContext);

                            // xóa biên bản
                            business_Bill_AdjustmentReport.DeleteBill_AdjustmentReport(model, _dbContext);

                            dbContextTransaction.Commit();
                            respone.Status = 1;
                            respone.Message = "Xóa hóa đơn sửa sai thành công.";
                            respone.Data = null;
                            return createResponse();
                        }
                        else
                        {
                            throw new ArgumentException("Hóa đơn đã được lập lại, không thể xóa dòng dữ liệu hủy bỏ này.");
                        }

                    }
                    else
                    {
                        throw new ArgumentException("Xóa hóa đơn sửa sai không thành công.");
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

        // lấy thông tin dựa vào id hóa đơn
        [HttpGet]
        [Route("GetBill_ByBillId")]
        public HttpResponseMessage GetBill_ByBillId(int billId)
        {
            try
            {
                var trans = _dbContext.Liabilities_Trans.Where(x => x.BillId == billId && x.BillType != BillType.GT).FirstOrDefault();
                var mobile = _dbContext.Mobile_Debt_Informations.Where(x => x.BillId == billId && x.Status == 0).FirstOrDefault();

                if (trans != null || mobile != null)
                {
                    throw new ArgumentException($"Không tìm thấy hóa đơn có billId {billId}");
                }

                List<Concus_ServicePointModel> listmode = new List<Concus_ServicePointModel>();

                var Bill_ElectricityBill =
                        _dbContext.Bill_ElectricityBill.Where(item => item.BillId.Equals(billId))
                            .Select(item => new Bill_ElectricityBillModel()
                            {
                                CustomerId = item.CustomerId,
                                CustomerCode = item.CustomerCode,
                                CustomerCode_Pay = item.CustomerCode_Pay,
                                Term = item.Term,
                                Month = item.Month,
                                Year = item.Year,
                                BillType = item.BillType,
                                CustomerName = item.CustomerName,
                                BillAddress = item.BillAddress,
                                HouseholdNumber = item.HouseholdNumber,
                                Total = item.Total,
                                SubTotal = item.SubTotal,
                                VAT = item.VAT,
                                TaxRatio = item.TaxRatio,
                                StartDate = item.StartDate,
                                EndDate = item.EndDate
                            }).FirstOrDefault();
                if (Bill_ElectricityBill == null)
                {
                    Bill_ElectricityBill = _dbContext.Bill_ElectricityBillAdjustment.Where(item => item.BillId.Equals(billId))
                        .Select(item => new Bill_ElectricityBillModel()
                        {
                            CustomerId = item.CustomerId,
                            CustomerCode = item.CustomerCode,
                            CustomerCode_Pay = item.CustomerCode_Pay,
                            Term = item.Term,
                            Month = item.Month,
                            Year = item.Year,
                            BillType = item.BillType,
                            CustomerName = item.CustomerName,
                            BillAddress = item.BillAddress,
                            HouseholdNumber = item.HouseholdNumber,
                            Total = item.Total,
                            SubTotal = item.SubTotal,
                            VAT = item.VAT,
                            TaxRatio = item.TaxRatio,
                            StartDate = item.StartDate,
                            EndDate = item.EndDate
                        }).FirstOrDefault();
                }
                var Concus_Customer = _dbContext.Concus_Customer
                    .Where(item => item.CustomerId.Equals(Bill_ElectricityBill.CustomerId)).Select(
                        item => new Concus_CustomerModel
                        {
                            PhoneNumber = item.PhoneNumber,
                            BankAccount = item.BankAccount,
                            BankName = item.BankName,
                            TaxCode = item.TaxCode,
                            Ratio = item.Ratio,

                        }).FirstOrDefault();

                var ListPointId =
                    _dbContext.Bill_ElectricityBillDetail.Where(item => item.BillId.Equals(billId))
                        .Select(item => new Bill_ElectricityBillDetailModel
                        {
                            PointId = item.PointId,
                        }).ToList();
                if (ListPointId == null || ListPointId.Count == 0)
                {
                    ListPointId =
                       _dbContext.Bill_ElectricityBillAdjustmentDetail.Where(item => item.BillId.Equals(billId))
                           .Select(item => new Bill_ElectricityBillDetailModel
                           {
                               PointId = item.PointId,
                           }).ToList();
                }
                ListPointId = ListPointId.DistinctBy(item => item.PointId).ToList();
                if (ListPointId != null)
                {
                    for (int i = 0; i < ListPointId.Count; i++)
                    {
                        int PointId = Convert.ToInt32(ListPointId[i].PointId);
                        var Concus_ServicePoint =
                            _dbContext.Concus_ServicePoint.Where(item => item.PointId.Equals(PointId))
                                .Select(item => new Concus_ServicePointModel
                                {
                                    PointId = item.PointId,
                                    PointCode = item.PointCode
                                }).ToList();
                        listmode.AddRange(Concus_ServicePoint);
                    }
                }
                // kiểm tra xem hóa đơn đã thanh toán tiền chưa, nếu chưa thanh toán thì nó được sửa sai, còn nếu đã nộp tiền thì là hóa đơn tuy thu, thoái hoàn
                var checkBiilId =
                    _dbContext.Liabilities_TrackDebt.Where(item => item.BillId.Equals(billId))
                        .Select(item => item.Status)
                        .FirstOrDefault();
                //kiểm tra đơn vị có tính chung hóa đơn VC vào TD không?
                Business_Administrator_Parameter vParameters = new Business_Administrator_Parameter();
                bool groupVC = Convert.ToBoolean(vParameters.GetParameterValue("groupVC", "false"));

                var response = new
                {
                    check = true,
                    CustomerCode = Bill_ElectricityBill.CustomerCode,
                    CustomerCode_Pay = Bill_ElectricityBill.CustomerCode_Pay,
                    Term = Bill_ElectricityBill.Term,
                    Month = Bill_ElectricityBill.Month,
                    Year = Bill_ElectricityBill.Year,
                    BillType = Bill_ElectricityBill.BillType,
                    groupVC = groupVC,
                    CustomerName = Bill_ElectricityBill.CustomerName,
                    BillAddress = Bill_ElectricityBill.BillAddress,
                    HouseholdNumber = Bill_ElectricityBill.HouseholdNumber,
                    Total = Bill_ElectricityBill.Total,
                    SubTotal = Bill_ElectricityBill.SubTotal,
                    VAT = Bill_ElectricityBill.VAT,
                    TaxRatio = Concus_Customer.Ratio,
                    BillId = billId,

                    PhoneNumber = Concus_Customer.PhoneNumber,
                    BankAccount = Concus_Customer.BankAccount,
                    BankName = Concus_Customer.BankName,
                    TaxCode = Concus_Customer.TaxCode,
                    Ratio = Concus_Customer.Ratio,
                    listpoint = listmode,
                    checkBiilId = checkBiilId,
                    StartDate = Bill_ElectricityBill.StartDate.Value.Day + "-" + Bill_ElectricityBill.StartDate.Value.Month + "-" + Bill_ElectricityBill.StartDate.Value.Year,
                    EndDate = Bill_ElectricityBill.EndDate.Value.Day + "-" + Bill_ElectricityBill.EndDate.Value.Month + "-" + Bill_ElectricityBill.EndDate.Value.Year
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

        [HttpGet]
        [Route("GetBill_ByCusCode")]
        public HttpResponseMessage GetBill_ByCusCode(string CusCode)
        {
            try
            {
                List<Concus_ServicePointModel> listmode = new List<Concus_ServicePointModel>();
                var Bill_ElectricityBill =
                    _dbContext.Bill_ElectricityBill.Where(item => item.CustomerCode.Equals(CusCode))
                        .Select(item => new Bill_ElectricityBillModel()
                        {
                            CustomerId = item.CustomerId,
                            CustomerCode = item.CustomerCode,
                            CustomerCode_Pay = item.CustomerCode_Pay,
                            Term = item.Term,
                            Month = item.Month,
                            Year = item.Year,
                            BillType = item.BillType,
                            CustomerName = item.CustomerName,
                            BillAddress = item.BillAddress,
                            HouseholdNumber = item.HouseholdNumber,
                            Total = item.Total,
                            SubTotal = item.SubTotal,
                            VAT = item.VAT,
                            TaxRatio = item.TaxRatio,
                            StartDate = item.StartDate,
                            EndDate = item.EndDate,
                            BillId = item.BillId
                        }).FirstOrDefault();

                var Concus_Customer = _dbContext.Concus_Customer
                    .Where(item => item.CustomerId.Equals(Bill_ElectricityBill.CustomerId)).Select(
                        item => new Concus_CustomerModel
                        {
                            PhoneNumber = item.PhoneNumber,
                            BankAccount = item.BankAccount,
                            BankName = item.BankName,
                            TaxCode = item.TaxCode,
                            Ratio = item.Ratio,

                        }).FirstOrDefault();
                var ListPointId =
                    _dbContext.Bill_ElectricityBillDetail.Where(item => item.BillId.Equals(Bill_ElectricityBill.BillId))
                        .Select(item => new Bill_ElectricityBillDetailModel
                        {
                            PointId = item.PointId,
                        }).ToList();
                ListPointId = ListPointId.DistinctBy(item => item.PointId).ToList();
                if (ListPointId != null)
                {
                    for (int i = 0; i < ListPointId.Count; i++)
                    {
                        int PointId = Convert.ToInt32(ListPointId[i].PointId);
                        var Concus_ServicePoint =
                            _dbContext.Concus_ServicePoint.Where(item => item.PointId.Equals(PointId))
                                .Select(item => new Concus_ServicePointModel
                                {
                                    PointId = item.PointId,
                                    PointCode = item.PointCode
                                }).ToList();
                        listmode.AddRange(Concus_ServicePoint);
                    }
                }
                // kiểm tra xem hóa đơn đã thanh toán tiền chưa, nếu chưa thanh toán thì nó được sửa sai, còn nếu đã nộp tiền thì là hóa đơn tuy thu, thoái hoàn
                var checkBiilId =
                    _dbContext.Liabilities_TrackDebt.Where(item => item.BillId.Equals(Bill_ElectricityBill.BillId))
                        .Select(item => item.Status)
                        .FirstOrDefault();
                Business_Administrator_Parameter vParameters = new Business_Administrator_Parameter();
                bool groupVC = Convert.ToBoolean(vParameters.GetParameterValue("groupVC", "false"));

                var response = new
                {
                    CustomerCode = Bill_ElectricityBill.CustomerCode,
                    CustomerCode_Pay = Bill_ElectricityBill.CustomerCode_Pay,
                    Term = Bill_ElectricityBill.Term,
                    Month = Bill_ElectricityBill.Month,
                    Year = Bill_ElectricityBill.Year,
                    BillType = Bill_ElectricityBill.BillType,
                    groupVC = groupVC,
                    CustomerName = Bill_ElectricityBill.CustomerName,
                    BillAddress = Bill_ElectricityBill.BillAddress,
                    HouseholdNumber = Bill_ElectricityBill.HouseholdNumber,
                    Total = Bill_ElectricityBill.Total,
                    SubTotal = Bill_ElectricityBill.SubTotal,
                    VAT = Bill_ElectricityBill.VAT,
                    TaxRatio = Bill_ElectricityBill.TaxRatio,
                    BillId = Bill_ElectricityBill.BillId,

                    PhoneNumber = Concus_Customer.PhoneNumber,
                    BankAccount = Concus_Customer.BankAccount,
                    BankName = Concus_Customer.BankName,
                    TaxCode = Concus_Customer.TaxCode,
                    Ratio = Concus_Customer.Ratio,
                    listpoint = listmode,
                    checkBiilId = checkBiilId,
                    StartDate = Bill_ElectricityBill.StartDate.Value.Day + "-" + Bill_ElectricityBill.StartDate.Value.Month + "-" + Bill_ElectricityBill.StartDate.Value.Year,
                    EndDate = Bill_ElectricityBill.EndDate.Value.Day + "-" + Bill_ElectricityBill.EndDate.Value.Month + "-" + Bill_ElectricityBill.EndDate.Value.Year,
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

        // kiểm tra thông tin id hóa đơn
        [HttpGet]
        [Route("CheckBillId")]
        public HttpResponseMessage CheckBillId(int billId)
        {
            try
            {
                var term = 0;

                var trangthai =
                        _dbContext.Liabilities_TrackDebt.Where(item => item.BillId.Equals(billId))
                            .Select(item => item.Status)
                            .FirstOrDefault();
                // KIỂM TRA XEM ID HOA ĐƠN NÀY CÓ ĐƯỢC SỬA SAI HAY KHÔNG (CHỈ KIỂM TRA VỚI HÓA ĐƠN HỦY BỎ)
                var adjustmentType = _dbContext.Bill_ElectricityBillAdjustment
                    .Where(item => item.BillAdjustmentId.Equals(billId)).Select(item => item.AdjustmentType)
                    .FirstOrDefault();

                // lấy thông tin biên bản nếu là hóa đơn lập lại
                var report = _dbContext.Bill_AdjustmentReport.Where(item => item.BillAdjustmentId.Equals(billId)).ToList()
                    .FirstOrDefault();
                var ReportNumber = "";
                var CreateDate = DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year;
                if (report != null)
                {
                    ReportNumber = report.ReportNumber;
                    CreateDate = report.CreateDate.Day + "-" + report.CreateDate.Month + "-" +
                                 report.CreateDate.Year;
                    term = report.Term;
                }

                //Kiểm tra hóa đơn có được chấm nợ qua ngân hàng hoắc máy tính bảng chưa
                var checkBill = 0;
                var trans = _dbContext.Liabilities_Trans.Where(x => x.BillId == billId && x.BillType != BillType.GT).FirstOrDefault();
                if (trangthai != 1 || trangthai != 2)
                {
                    var mobile = _dbContext.Mobile_Debt_Informations.Where(x => x.BillId == billId && x.Status == 0).FirstOrDefault();
                    checkBill = mobile != null ? 2 : 0;
                }

                if (trans != null)
                {
                    checkBill = 1;
                }

                var response = new
                {
                    Term = term,
                    ReportNumber = ReportNumber,
                    CreateDate = CreateDate,
                    trangthai = trangthai,
                    AdjustmentType = adjustmentType,
                    checkBill = checkBill,
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

        [HttpGet]
        [Route("GetAllElectricityMeter")]
        public HttpResponseMessage GetAllElectricityMeter(int point_Id)
        {
            try
            {
                List<EquipmentMT_ElectricityMeterModel> listmode = new List<EquipmentMT_ElectricityMeterModel>();

                var listElectricityMeterId =
                    _dbContext.EquipmentMT_OperationDetail.Where(item => item.PointId.Equals(point_Id))
                        .Select(item => new EquipmentMT_OperationDetailModel
                        {
                            ElectricityMeterId = item.ElectricityMeterId,
                        }).ToList();
                listElectricityMeterId = listElectricityMeterId.DistinctBy(item => item.ElectricityMeterId)
                    .ToList();
                if (listElectricityMeterId != null)
                {
                    for (int i = 0; i < listElectricityMeterId.Count; i++)
                    {
                        int electricityMeterId = Convert.ToInt32(listElectricityMeterId[i].ElectricityMeterId);
                        var equipmentMT_ElectricityMeter =
                            _dbContext.EquipmentMT_ElectricityMeter
                                .Where(item => item.ElectricityMeterId.Equals(electricityMeterId))
                                .Select(item => new EquipmentMT_ElectricityMeterModel
                                {
                                    ElectricityMeterId = item.ElectricityMeterId,
                                    ElectricityMeterCode = item.ElectricityMeterCode
                                }).ToList();
                        listmode.AddRange(equipmentMT_ElectricityMeter);
                    }
                }

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = listmode;
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
        [Route("GetCategory_Price")]
        public HttpResponseMessage GetCategory_Price(int potentialCode, DateTime startDate, DateTime endDate)
        {
            try
            {
                List<Category_PriceModel> listmode = new List<Category_PriceModel>();
                string potentialCodeId = Convert.ToString(potentialCode);
                var listThamChieu =
                    _dbContext.Category_PotentialReference.Where(item => item.PotentialCode == potentialCodeId &&
                                                    item.ActiveDate <= DateTime.Now &&
                                                    item.ExpiryDate.Value > DateTime.Now)
                        .Select(o => o.OccupationsGroupCode + o.PotentialSpace).ToList();
                var listCategory_Price =
                    _dbContext.Category_Price.Where(item => listThamChieu.Contains(item.OccupationsGroupCode + item.PotentialSpace)
                                                    &&
                                                        (
                                                            (item.ActiveDate <= startDate && item.EndDate >= startDate)
                                                            ||
                                                            (item.ActiveDate > startDate && item.ActiveDate <= endDate)
                                                            ||
                                                            (item.ActiveDate <= endDate && item.EndDate >= endDate)
                                                         )
                                                    && item.Price > 0)
                        .Select(item => new Category_PriceModel
                        {
                            Description = item.OccupationsGroupCode + "-" + item.PriceGroupCode + "-" + item.Time +
                                          "-" + item.PotentialSpace + "- [" + item.Description + "]",
                            PriceId = item.PriceId,
                            Price = item.Price,
                            OccupationsGroupCode = item.OccupationsGroupCode
                        }).ToList();
                listmode.AddRange(listCategory_Price);

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = listmode;
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
        [Route("GetStation")]
        public HttpResponseMessage GetStation(int? matchvalue)
        {
            try
            {
                var cusId = _dbContext.Bill_ElectricityBill.Where(item => item.BillId == matchvalue).Select(item => item.CustomerId).DefaultIfEmpty(0).FirstOrDefault();
                if (cusId == 0)
                {
                    cusId = _dbContext.Bill_ElectricityBillAdjustment.Where(item => item.BillId == matchvalue).Select(item => item.CustomerId).DefaultIfEmpty(0).FirstOrDefault();
                }
                var selectedItem = _dbContext.Concus_ServicePoint
                    .Where(item => item.Concus_Contract.CustomerId == cusId)
                    .Select(item => new Concus_ServicePointModel
                    {
                        StationId = item.StationId,
                        StationCode = item.Category_Satiton.StationCode,
                        TeamId = item.TeamId,
                        TeamCode = item.Category_Team.TeamCode,
                        FigureBookId = item.FigureBookId,
                        BookName = item.Category_FigureBook.BookName
                    }).FirstOrDefault();

                var lstAllStation = _dbContext.Category_Satiton.Select(item => new Concus_ServicePointModel
                {
                    StationId = item.StationId,
                    StationCode = item.StationCode,
                }).ToList();

                var lstAllTeam = _dbContext.Category_Team.Select(item => new Concus_ServicePointModel
                {
                    TeamId = item.TeamId,
                    TeamCode = item.TeamCode,
                }).ToList();

                var lstAllFigureBook = _dbContext.Category_FigureBook.Where(item => item.Status == true).Select(item => new Concus_ServicePointModel
                {
                    FigureBookId = item.FigureBookId,
                    BookName = item.BookName,
                }).ToList();

                if (lstAllStation?.Any() == true)
                {
                    foreach (var item in lstAllStation)
                    {
                        if (item.StationId == selectedItem.StationId)
                        {
                            lstAllStation.Remove(item);
                            lstAllStation.Insert(0, selectedItem);
                        }
                    }
                }

                if (lstAllTeam?.Any() == true)
                {
                    foreach (var item in lstAllTeam)
                    {
                        if (item.TeamId == selectedItem.TeamId)
                        {
                            lstAllTeam.Remove(item);
                            lstAllTeam.Insert(0, selectedItem);
                        }
                    }
                }

                if (lstAllFigureBook?.Any() == true)
                {
                    foreach (var item in lstAllFigureBook)
                    {
                        if (item.FigureBookId == selectedItem.FigureBookId)
                        {
                            lstAllFigureBook.Remove(item);
                            lstAllFigureBook.Insert(0, selectedItem);
                        }
                    }
                }

                var response = new
                {
                    ListlStation = lstAllStation,
                    ListTeam = lstAllTeam,
                    ListFigureBook = lstAllFigureBook,
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

        // lấy thông tin nhom nganh nge,  gia
        [HttpGet]
        [Route("Get_OccupationsGroupCode_Price")]
        public HttpResponseMessage Get_OccupationsGroupCode_Price(int priceId)
        {
            try
            {
                List<Category_PriceModel> listmode = new List<Category_PriceModel>();
                int price_Id = Convert.ToInt32(priceId);
                var listCategory_Price =
                    _dbContext.Category_Price.Where(item => item.PriceId == price_Id)
                        .Select(item => new Category_PriceModel
                        {
                            OccupationsGroupCode = item.OccupationsGroupCode,
                            Price = item.Price
                        }).ToList();
                listmode.AddRange(listCategory_Price);

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = listmode;
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
        [Route("GetAllValue")]
        public HttpResponseMessage GetAllValue(GetAllValueInput input)
        {
            try
            {
                if (input.MyArrayBillDetail?.Any() == false || input.MyArrayCustomer?.Any() == false)
                {
                    throw new ArgumentException("Lỗi dữ liệu đầu vào: không có thông tin khách hàng, chi tiết hóa đơn.");
                }
                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                var userId = TokenHelper.GetUserIdFromToken();

                var Liabilities_JobLatch = _dbContext.Liabilities_JobLatch.Where(x => x.DepartmentId == departmentId).FirstOrDefault();
                if (Liabilities_JobLatch == null)
                {
                    throw new ArgumentException("Lỗi không xác định được tháng năm công nợ (Liabilities_JobLatch).");                    
                }

                decimal billAdjustmentId = Convert.ToDecimal(input.MyArrayCustomer[0].BillAdjustmentId);
                var billElectricityBill = _dbContext.Bill_ElectricityBill.Where(item => item.BillId.Equals(billAdjustmentId)).Select(item => new Bill_ElectricityBillModel()
                {
                    CustomerId = item.CustomerId,
                    CustomerCode = item.CustomerCode,
                    CustomerCode_Pay = item.CustomerCode_Pay,
                    BankName = item.BankName,
                    BankAccount = item.BankAccount,
                    Address_Pay = item.Address_Pay,
                    FormOfPayment = item.FormOfPayment,
                    FigureBookId = item.FigureBookId,
                    ContractId = item.ContractId,
                    Term = item.Term,
                    Month = item.Month,
                    Year = item.Year,
                    BillType = item.BillType,
                    CustomerName = item.CustomerName,
                    BillAddress = item.BillAddress,
                    HouseholdNumber = item.HouseholdNumber,
                    Total = item.Total,
                    SubTotal = item.SubTotal,
                    VAT = item.VAT,
                    TaxRatio = item.TaxRatio,
                    StartDate = item.StartDate,
                    EndDate = item.EndDate,
                }).FirstOrDefault();

                if (billElectricityBill == null)
                {
                    var customerCode = input.MyArrayCustomer[0].CustomerCode;
                    var customerId = _dbContext.Concus_Customer.FirstOrDefault(x => x.CustomerCode == customerCode && x.Status == 1)?.CustomerId;
                    billElectricityBill = _dbContext.Bill_ElectricityBillAdjustment.Where(item => item.BillId.Equals(billAdjustmentId)).Select(item => new Bill_ElectricityBillModel()
                    {
                        CustomerId = item.CustomerId,
                        CustomerCode = item.CustomerCode,
                        CustomerCode_Pay = item.CustomerCode_Pay,
                        BankName = item.BankName,
                        BankAccount = item.BankAccount,
                        Address_Pay = item.Address_Pay,
                        FormOfPayment = item.FormOfPayment,
                        FigureBookId = item.FigureBookId,
                        ContractId = _dbContext.Concus_Contract.Where(x => x.CustomerId == customerId).Select(x => x.ContractId).FirstOrDefault(),
                        Term = item.Term,
                        Month = item.Month,
                        Year = item.Year,
                        BillType = item.BillType,
                        CustomerName = item.CustomerName,
                        BillAddress = item.BillAddress,
                        HouseholdNumber = item.HouseholdNumber,
                        Total = item.Total,
                        SubTotal = item.SubTotal,
                        VAT = item.VAT,
                        TaxRatio = item.TaxRatio,
                        StartDate = item.StartDate,
                        EndDate = item.EndDate,
                    }).FirstOrDefault();
                }

                if (billElectricityBill == null)
                {
                    throw new ArgumentException($"Lỗi không xác định được hóa đơn sai (ID: {billAdjustmentId.ToString()})");                    
                }
                // lấy ID mới, không cần cho vào transaction
                // Chỗ này cực kỳ quan trọng để tránh bị trùng id giữa 2 bảng Bill_ElectricityBill và Bill_ElectricityBillAdjustment
                // nếu trùng lên báo cáo sẽ sai số liệu vì lấy sai ở cả 2 bảng cùng billid và bảng lưu hóa đơn Electronic cũng sẽ sai khi lấy sai số liệu của bảng
                int billId = Convert.ToInt32(_dbContext.Database
                    .SqlQuery<decimal>("Select IDENT_CURRENT ('dbo.Bill_ElectricityBill')", new object[0])
                    .FirstOrDefault());
                billId = billId + 1;
                _dbContext.Database.ExecuteSqlCommand("DBCC CHECKIDENT ('Bill_ElectricityBill', RESEED," + billId + ")");
                _dbContext.SaveChanges();

                using (var dbContextTransaction = _dbContext.Database.BeginTransaction())
                {
                    var error = "";
                    try
                    {
                        #region tạo dữ liệu bảng hóa đơn
                        Bill_ElectricityBillAdjustmentModel electricityBillAdjustment = new Bill_ElectricityBillAdjustmentModel();
                        //1. các thông tin chung
                        error = "01";
                        electricityBillAdjustment.BillAdjustmentId = billAdjustmentId;
                        electricityBillAdjustment.DepartmentId = departmentId;
                        electricityBillAdjustment.BillId = Convert.ToDecimal(billId);
                        electricityBillAdjustment.CustomerId = billElectricityBill.CustomerId;
                        electricityBillAdjustment.CustomerCode = billElectricityBill.CustomerCode;
                        electricityBillAdjustment.CustomerId_Pay = billElectricityBill.CustomerId_Pay;
                        electricityBillAdjustment.BankName = billElectricityBill.BankName;
                        electricityBillAdjustment.BankAccount = billElectricityBill.BankAccount;
                        electricityBillAdjustment.Address_Pay = billElectricityBill.Address_Pay;
                        error = "02";
                        electricityBillAdjustment.HouseholdNumber = input.MyArrayCustomer[0].HouseholdNumber;
                        electricityBillAdjustment.CustomerName = input.MyArrayCustomer[0].CustomerName;
                        electricityBillAdjustment.CustomerCode_Pay = input.MyArrayCustomer[0].CustomerCode_Pay;
                        electricityBillAdjustment.BillAddress = input.MyArrayCustomer[0].BillAddress;
                        electricityBillAdjustment.TaxCode = input.MyArrayCustomer[0].TaxCode;
                        electricityBillAdjustment.CustomerName_Pay = billElectricityBill.CustomerName_Pay;
                        electricityBillAdjustment.BankName = billElectricityBill.BankName;
                        electricityBillAdjustment.BankAccount = billElectricityBill.BankAccount;
                        electricityBillAdjustment.FormOfPayment = billElectricityBill.FormOfPayment;
                        electricityBillAdjustment.StartDate = input.MyArrayCustomer[0].StartDate;
                        electricityBillAdjustment.EndDate = input.MyArrayCustomer[0].EndDate;
                        electricityBillAdjustment.TaxRatio = input.MyArrayCustomer[0].TaxRatio;
                        electricityBillAdjustment.PhoneNumber = input.MyArrayCustomer[0].PhoneNumber;
                        electricityBillAdjustment.CreateUser = userId;
                        electricityBillAdjustment.CreateDate = DateTime.Now;
                        electricityBillAdjustment.Term = input.TermAdjustment;
                        error = "03";
                        electricityBillAdjustment.Month = Liabilities_JobLatch.Month;
                        electricityBillAdjustment.Year = Liabilities_JobLatch.Year;
                        error = "04";
                        electricityBillAdjustment.Term_Incurred = input.MyArrayCustomer[0].Term_Incurred;
                        electricityBillAdjustment.Month_Incurred = input.MyArrayCustomer[0].Month_Incurred;
                        electricityBillAdjustment.Year_Incurred = input.MyArrayCustomer[0].Year_Incurred;
                        electricityBillAdjustment.ElectricityMeterId = input.MyArrayBillDetail[0].ElectricityMeterId;
                        electricityBillAdjustment.FigureBookId = billElectricityBill.FigureBookId.Value;
                        electricityBillAdjustment.BillType = input.MyArrayCustomer[0].BillType;

                        //2. Tạo thông tin về loại hóa đơn, tiền: phụ thuộc các giá trị đầu vào
                        error = "05";
                        int pointID = input.MyArrayBillDetail[0].PointId;
                        var Total_TD = input.MyArrayBillDetail.AsEnumerable().Sum(item => item.Total);
                        var Total_SV = input.BillAdjustmentServicesModel == null ? 0 : input.BillAdjustmentServicesModel[0].Total;
                        var taxRatio = input.MyArrayCustomer[0].TaxRatio;
                        //tính tiền dịch vụ và thuế
                        error = "06";
                        decimal Total_TD_Vat = 0;
                        decimal Total_SV_Vat = 0;
                        if (input.BillAdjustmentServicesModel != null && input.BillAdjustmentServicesModel.Count > 0)
                        {
                            if (input.BillAdjustmentServicesModel[0].Note3 == "VAT_RIENG")
                            {
                                //tính riêng
                                Total_SV_Vat = Math.Round(Total_SV * input.BillAdjustmentServicesModel[0].TaxRatio / 100, 0);
                                input.BillAdjustmentServicesModel[0].VAT = Total_SV_Vat;
                                Total_TD_Vat = Math.Round((Total_TD * taxRatio) / 100, 0);
                            }
                            else
                            {
                                //tính chung
                                input.BillAdjustmentServicesModel[0].VAT = 0;
                                Total_TD_Vat = Math.Round(((Total_TD + Total_SV) * taxRatio) / 100, 0);
                            }
                            input.BillAdjustmentServicesModel[0].Note3 = "";
                        }
                        else
                        {
                            Total_TD_Vat = Math.Round(((Total_TD + Total_SV) * taxRatio) / 100, 0);
                        }
                        error = "07";
                        electricityBillAdjustment.SubTotal = Total_TD;
                        electricityBillAdjustment.VAT = Total_TD_Vat;
                        electricityBillAdjustment.Total = Total_TD + Total_TD_Vat + Total_SV + Total_SV_Vat;
                        //loại điều chỉnh
                        if (input.AdjustmentType.Trim() == EnumMethod.D_TinhChatHoaDon.ThoaiHoan || input.AdjustmentType.Trim() == EnumMethod.D_TinhChatHoaDon.TruyThu)
                        {
                            electricityBillAdjustment.AdjustmentType = (electricityBillAdjustment.Total < 0 ? EnumMethod.D_TinhChatHoaDon.ThoaiHoan : EnumMethod.D_TinhChatHoaDon.TruyThu);
                        }
                        else
                        {
                            electricityBillAdjustment.AdjustmentType = input.AdjustmentType.Trim();
                        }
                        error = "08";
                        //Thông tin điện năng và cosfi:
                        var vChiTietVC = input.MyArrayBillDetail.Where(itemvc => itemvc.TimeOfUse == "VC").FirstOrDefault();
                        if (electricityBillAdjustment.BillType == "TD" && vChiTietVC != null)
                        {
                            //hóa đơn gộp TD+VC
                            var ElectricityIndex = input.MyArrayBillDetail.Sum(item => item.RealElectricityIndex ?? 0);
                            electricityBillAdjustment.ElectricityIndex = ElectricityIndex - (vChiTietVC.RealElectricityIndex ?? 0);
                            electricityBillAdjustment.CosFi = vChiTietVC.CosFi;
                            electricityBillAdjustment.KCosFi = vChiTietVC.KCosFi;
                        }
                        else
                        {
                            var ElectricityIndex = input.MyArrayBillDetail.Sum(item => item.RealElectricityIndex ?? 0);
                            electricityBillAdjustment.ElectricityIndex = ElectricityIndex;
                            electricityBillAdjustment.CosFi = 0;
                            electricityBillAdjustment.KCosFi = 0;
                        }
                        error = "09";
                        electricityBillAdjustmen.AddBill_ElectricityBillAdjustment(electricityBillAdjustment, _dbContext);
                        _dbContext.SaveChanges();
                        #endregion

                        #region Lập biên bản
                        error = "10";
                        Bill_AdjustmentReportModel listmode = new Bill_AdjustmentReportModel();
                        listmode.ReportNumber = input.ReportNumber;
                        listmode.CreateDate = DateTime.Now;
                        listmode.CreateUser = userId;
                        listmode.ReasonId = input.ReasonId;
                        listmode.BillId = billId;
                        listmode.Term = input.TermAdjustment;
                        listmode.Month = Liabilities_JobLatch.Month;
                        listmode.Year = Liabilities_JobLatch.Year;
                        listmode.BillAdjustmentId = billAdjustmentId;
                        listmode.TermAdjustment = billElectricityBill.Term;
                        listmode.MonthAdjustment = billElectricityBill.Month;
                        listmode.YearAdjustment = billElectricityBill.Year;
                        business_Bill_AdjustmentReport.Save_Bill_AdjustmentReport(listmode, _dbContext);
                        error = "11";
                        #endregion

                        #region dữ liệu chi tiết hóa đơn, chỉ số
                        for (int i = 0; i < input.MyArrayBillDetail.Count; i++)
                        {
                            error = "12";
                            //tạo chỉ số                           
                            decimal indexAdjustmentId = businessBill_IndexValueAdjustment.CheckDup_IndexValueAdjustment_BillAdjustmentDetail(input.MyArrayBillDetail[i], billId, _dbContext);
                            if (indexAdjustmentId == 0)
                            {
                                //chưa có chỉ số thì tạo mới
                                Bill_IndexValueAdjustmentModel indexValueAdjustment = new Bill_IndexValueAdjustmentModel();
                                indexValueAdjustment.BillId = billId;
                                indexValueAdjustment.DepartmentId = departmentId;
                                indexValueAdjustment.Coefficient = input.MyArrayBillDetail[i].Coefficient;
                                indexValueAdjustment.ElectricityMeterId = input.MyArrayBillDetail[i].ElectricityMeterId;
                                indexValueAdjustment.Term = input.TermAdjustment;
                                indexValueAdjustment.Month = Liabilities_JobLatch.Month;
                                indexValueAdjustment.Year = Liabilities_JobLatch.Year;
                                indexValueAdjustment.TimeOfUse = input.MyArrayBillDetail[i].TimeOfUse;
                                indexValueAdjustment.OldValue = input.MyArrayBillDetail[i].OldValue;
                                indexValueAdjustment.NewValue = input.MyArrayBillDetail[i].NewValue;
                                indexValueAdjustment.PointId = input.MyArrayBillDetail[i].PointId;
                                indexValueAdjustment.CreateDate = DateTime.Now;
                                indexValueAdjustment.CreateUser = userId;
                                indexValueAdjustment.StartDate = input.MyArrayCustomer[0].StartDate;
                                indexValueAdjustment.EndDate = input.MyArrayCustomer[0].EndDate;
                                indexValueAdjustment.AdjustPower = input.MyArrayBillDetail[i].AdjustPower ?? 0;
                                indexValueAdjustment.MinusIndex = input.MyArrayBillDetail[i].MinusIndex ?? 0;
                                indexAdjustmentId = businessBill_IndexValueAdjustment.AddBill_IndexValueAdjustment(indexValueAdjustment, _dbContext);
                            }
                            error = "13";

                            #region tạo chi tiết hóa đơn
                            Bill_ElectricityBillAdjustmentDetailModel billAdjustmentDetail = new Bill_ElectricityBillAdjustmentDetailModel();
                            billAdjustmentDetail.BillId = billId;
                            billAdjustmentDetail.DepartmentId = departmentId;
                            billAdjustmentDetail.CustomerId = billElectricityBill.CustomerId;
                            billAdjustmentDetail.CustomerCode = input.MyArrayBillDetail[i].CustomerCode;
                            billAdjustmentDetail.Rated = input.MyArrayBillDetail[i].Rated;
                            billAdjustmentDetail.RatedType = "%";
                            billAdjustmentDetail.PointId = input.MyArrayBillDetail[i].PointId;
                            billAdjustmentDetail.FigureBookId = billElectricityBill.FigureBookId.Value;
                            billAdjustmentDetail.Term = input.TermAdjustment;
                            billAdjustmentDetail.Month = Liabilities_JobLatch.Month;
                            billAdjustmentDetail.Year = Liabilities_JobLatch.Year;
                            error = "14";
                            billAdjustmentDetail.ElectricityIndex = input.MyArrayBillDetail[i].ElectricityIndexHC;
                            billAdjustmentDetail.Total = input.MyArrayBillDetail[i].Total;
                            if (input.MyArrayBillDetail[i].TimeOfUse == "VC")
                            {
                                billAdjustmentDetail.TimeOfSale = "";
                                billAdjustmentDetail.TimeOfUse = "VC";
                                billAdjustmentDetail.CosFi = input.MyArrayBillDetail[i].CosFi;
                                billAdjustmentDetail.KCosFi = input.MyArrayBillDetail[i].KCosFi;
                                billAdjustmentDetail.Price = 0;
                                billAdjustmentDetail.OccupationsGroupCode = "";
                            }
                            else
                            {
                                int vPriceId = input.MyArrayBillDetail[i].PriceId == null ? 0 : input.MyArrayBillDetail[i].PriceId.Value;
                                var vPriceCG = _dbContext.Category_Price.Where(item => item.PriceId == vPriceId).FirstOrDefault();
                                billAdjustmentDetail.TimeOfSale = vPriceCG.Time;
                                billAdjustmentDetail.TimeOfUse = input.MyArrayBillDetail[i].TimeOfUse;
                                billAdjustmentDetail.Price = input.MyArrayBillDetail[i].Price;
                                billAdjustmentDetail.GroupCode = vPriceCG.Step ? "A" + vPriceCG.PriceGroupCode : vPriceCG.PriceGroupCode;
                                billAdjustmentDetail.CosFi = 0;
                                billAdjustmentDetail.KCosFi = 0;
                                billAdjustmentDetail.OccupationsGroupCode = input.MyArrayBillDetail[i].OccupationsGroupCode;
                            }
                            billAdjustmentDetail.ElectricityIndexHC = input.MyArrayBillDetail[i].ElectricityIndexHC;
                            billAdjustmentDetail.TotalHC = input.MyArrayBillDetail[i].TotalHC ?? input.MyArrayBillDetail[i].Total;                            
                            billAdjustmentDetail.RealElectricityIndex = input.MyArrayBillDetail[i].RealElectricityIndex;
                            billAdjustmentDetail.CreateDate = DateTime.Now;
                            billAdjustmentDetail.CreateUser = userId;
                            billAdjustmentDetail.ElectricityMeterId = input.MyArrayBillDetail[i].ElectricityMeterId;
                            billAdjustmentDetail.IndexAdjustmentId = indexAdjustmentId;
                            error = "15";
                            var dd = _dbContext.Concus_ServicePoint.Where(o => o.PointId == billAdjustmentDetail.PointId).FirstOrDefault();
                            var pr = _dbContext.Category_Price.Where(o => o.OccupationsGroupCode == billAdjustmentDetail.OccupationsGroupCode
                                                                    && o.Price == billAdjustmentDetail.Price).FirstOrDefault();
                            if (pr != null)
                            {
                                billAdjustmentDetail.PriceId = pr.PriceId;
                            }
                            error = "16";
                            billAdjustmentDetail.StationId = input.MyArrayBillDetail[i].StationId != 0 ? input.MyArrayBillDetail[i].StationId : dd.StationId;
                            billAdjustmentDetail.RouteId = input.MyArrayBillDetail[i].RouteId != 0 ? input.MyArrayBillDetail[i].RouteId : dd.RouteId == null ? 0 : dd.RouteId;
                            billAdjustmentDetail.RegionId = input.MyArrayBillDetail[i].RegionId != 0 ? input.MyArrayBillDetail[i].RegionId : dd.RegionId;
                            billAdjustmentDetail.TeamId = input.MyArrayBillDetail[i].TeamId != 0 ? input.MyArrayBillDetail[i].TeamId : dd.TeamId;
                            billAdjustmentDetail.ServicePointType = input.MyArrayBillDetail[i].ServicePointType != 0 ? input.MyArrayBillDetail[i].ServicePointType : dd.ServicePointType;
                            billAdjustmentDetail.NumberOfPhases = input.MyArrayBillDetail[i].NumberOfPhases != 0 ? input.MyArrayBillDetail[i].NumberOfPhases : dd.NumberOfPhases;
                            billAdjustmentDetail.HouseholdNumber = input.MyArrayBillDetail[i].HouseholdNumber != 0 ? input.MyArrayBillDetail[i].HouseholdNumber : dd.HouseholdNumber;
                            billAdjustmentDetail.PotentialCode = input.MyArrayBillDetail[i].PotentialCode != null ? input.MyArrayBillDetail[i].PotentialCode : dd.PotentialCode;

                            electricityBillAdjustmentDetail.AddBill_ElectricityBillAdjustment(billAdjustmentDetail, _dbContext);
                            error = "17";
                            #endregion
                        }
                        #endregion

                        #region insert chi phi khac
                        error = "18";
                        if (input.BillAdjustmentServicesModel != null && input.BillAdjustmentServicesModel.Count > 0)
                        {

                            if (billElectricityBill.ContractId == null)
                            {
                                throw new Exception($"Không lấy được Id hợp đồng của khách hàng này với Id hóa đơn là {billElectricityBill.BillId}");
                            }
                            var ServiceTypeId = 0;
                            var billServiceOld = _dbContext.Bill_ElectricityBillServices.Where(item => item.BillId.Equals(billAdjustmentId)).FirstOrDefault();
                            var billServiceAdjustment = _dbContext.Bill_ElectricityBillAdjustmentServices.Where(item => item.BillId.Equals(billAdjustmentId)).FirstOrDefault();
                            if (billServiceOld != null)
                            {
                                ServiceTypeId = billServiceOld.ServiceTypeId ?? 0;
                            }
                            if (billServiceAdjustment != null)
                            {
                                ServiceTypeId = billServiceAdjustment.ServiceTypeId ?? 0;
                            }

                            input.BillAdjustmentServicesModel[0].DepartmentId = departmentId;
                            input.BillAdjustmentServicesModel[0].BillId = billId;
                            input.BillAdjustmentServicesModel[0].FigureBookId = billElectricityBill.FigureBookId.Value;
                            input.BillAdjustmentServicesModel[0].Term = input.TermAdjustment;
                            input.BillAdjustmentServicesModel[0].Month = Liabilities_JobLatch.Month;
                            input.BillAdjustmentServicesModel[0].Year = Liabilities_JobLatch.Year;
                            input.BillAdjustmentServicesModel[0].CustomerId = billElectricityBill.CustomerId;
                            input.BillAdjustmentServicesModel[0].CustomerCode = billElectricityBill.CustomerCode;
                            input.BillAdjustmentServicesModel[0].ContractId = billElectricityBill.ContractId.Value;
                            input.BillAdjustmentServicesModel[0].CreateDate = input.CreateDate;
                            input.BillAdjustmentServicesModel[0].CreateUser = userId;
                            input.BillAdjustmentServicesModel[0].ServiceTypeId = ServiceTypeId;
                            billAdjustment.AddBill_ElectricityBillAdjustment(input.BillAdjustmentServicesModel[0], _dbContext);
                        }
                        #endregion

                        #region kết thúc, cập nhật trạng thái nợ
                        Liabilities_TrackDebt listmode_TrackDebt = new Liabilities_TrackDebt();
                        listmode_TrackDebt.BillId = billAdjustmentId;
                        switch (input.AdjustmentType.Trim())
                        {
                            case EnumMethod.D_TinhChatHoaDon.HuyBo:
                                listmode_TrackDebt.Status = (int)(StatusTrackDebt.Cancel);
                                break;
                            case EnumMethod.D_TinhChatHoaDon.LapLai:
                                listmode_TrackDebt.Status = (int)(StatusTrackDebt.Restore);
                                break;
                            case EnumMethod.D_TinhChatHoaDon.TruyThu:
                                listmode_TrackDebt.Status = (int)(StatusTrackDebt.Paid);
                                break;
                            case EnumMethod.D_TinhChatHoaDon.ThoaiHoan:
                                listmode_TrackDebt.Status = (int)(StatusTrackDebt.Paid);
                                break;
                        }
                        bsTrackDebt.Updata_Status_Liabilities_TrackDebt(listmode_TrackDebt, _dbContext);
                        #endregion
                        
                        dbContextTransaction.Commit();
                        respone.Status = 1;
                        respone.Message = "Lập hóa đơn thành công.";
                        respone.Data = null;
                        return createResponse();                        
                    }
                    catch (Exception ex)
                    {
                        dbContextTransaction.Rollback();
                        throw new ArgumentException($"Lỗi khi lập hóa đơn: {ex.Message}");                        
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

        // chuyển dữ liệu là hóa đơn hủy bỏ về để hủy bỏ
        [HttpPost]
        [Route("GetAllValue_HB")]
        public HttpResponseMessage GetAllValue_HB(GetAllValue_HBInput input)
        {
            try
            {
                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var userId = TokenHelper.GetUserIdFromToken();

                bool isLapLaiCuaHoaDonLapLai = false;

                // lấy ra id hóa đơn trong bảng hóa đơn
                int billId = Convert.ToInt32(_dbContext.Database
                    .SqlQuery<decimal>("Select IDENT_CURRENT ('dbo.Bill_ElectricityBill')", new object[0])
                    .FirstOrDefault());
                billId = billId + 1;
                _dbContext.Database.ExecuteSqlCommand("DBCC CHECKIDENT ('Bill_ElectricityBill', RESEED," + billId + ")");
                _dbContext.SaveChanges();

                using (var dbContextTransaction = _dbContext.Database.BeginTransaction())
                {
                    try
                    {
                        var Liabilities_JobLatch = _dbContext.Liabilities_JobLatch.Where(x => x.DepartmentId == departmentId).FirstOrDefault();

                        // bước 1 : thực hiện lưu chỉ số điều chỉnh
                        // insert vào bảng hóa đơn
                        decimal billAdjustmentId = Convert.ToDecimal(input.BillAdjustmentId);

                        #region Bang hoa don

                        Bill_ElectricityBillAdjustmentModel electricityBillAdjustment =
                            new Bill_ElectricityBillAdjustmentModel();
                        electricityBillAdjustment.BillAdjustmentId = billAdjustmentId;
                        electricityBillAdjustment.DepartmentId = departmentId;
                        // lấy thông tin hóa đơn theo id hóa đơn sửa sai
                        var billElectricityBill =
                            _dbContext.Bill_ElectricityBill.Where(
                                    item => item.BillId == billAdjustmentId).Select(item => new Bill_ElectricityBillModel()
                                    {
                                        CustomerId = item.CustomerId,
                                        CustomerCode = item.CustomerCode,
                                        CustomerCode_Pay = item.CustomerCode_Pay,
                                        BankName = item.BankName,
                                        BankAccount = item.BankAccount,
                                        Address_Pay = item.Address_Pay,
                                        FormOfPayment = item.FormOfPayment,
                                        FigureBookId = item.FigureBookId,
                                        ContractId = item.ContractId,
                                        Term = item.Term,
                                        Month = item.Month,
                                        Year = item.Year,
                                        BillType = item.BillType,
                                        CustomerName = item.CustomerName,
                                        BillAddress = item.BillAddress,
                                        HouseholdNumber = item.HouseholdNumber,
                                        Total = item.Total,
                                        SubTotal = item.SubTotal,
                                        VAT = item.VAT,
                                        TaxRatio = item.TaxRatio,
                                        StartDate = item.StartDate,
                                        EndDate = item.EndDate,
                                        ElectricityIndex = item.ElectricityIndex
                                    }).FirstOrDefault();
                        if (billElectricityBill == null)
                        {
                            billElectricityBill =
                            _dbContext.Bill_ElectricityBillAdjustment.Where(
                                    item => item.BillId == billAdjustmentId).Select(item => new Bill_ElectricityBillModel()
                                    {
                                        CustomerId = item.CustomerId,
                                        CustomerCode = item.CustomerCode,
                                        CustomerCode_Pay = item.CustomerCode_Pay,
                                        BankName = item.BankName,
                                        BankAccount = item.BankAccount,
                                        Address_Pay = item.Address_Pay,
                                        FormOfPayment = item.FormOfPayment,
                                        FigureBookId = item.FigureBookId,
                                        ContractId = _dbContext.Concus_Contract.Where(x => x.CustomerId == item.CustomerId).Select(x => x.ContractId).FirstOrDefault(),
                                        Term = item.Term,
                                        Month = item.Month,
                                        Year = item.Year,
                                        BillType = item.BillType,
                                        CustomerName = item.CustomerName,
                                        BillAddress = item.BillAddress,
                                        HouseholdNumber = item.HouseholdNumber,
                                        Total = item.Total,
                                        SubTotal = item.SubTotal,
                                        VAT = item.VAT,
                                        TaxRatio = item.TaxRatio,
                                        StartDate = item.StartDate,
                                        EndDate = item.EndDate,
                                        ElectricityIndex = item.ElectricityIndex
                                    }).FirstOrDefault();
                            isLapLaiCuaHoaDonLapLai = true;
                        }


                        electricityBillAdjustment.BillId = Convert.ToDecimal(billId);
                        if (billElectricityBill != null)
                        {
                            electricityBillAdjustment.CustomerId = billElectricityBill.CustomerId;
                            electricityBillAdjustment.CustomerCode = billElectricityBill.CustomerCode;
                            electricityBillAdjustment.CustomerId_Pay = billElectricityBill.CustomerId_Pay;
                            electricityBillAdjustment.BillType = billElectricityBill.BillType;
                            electricityBillAdjustment.BankName = billElectricityBill.BankName;
                            electricityBillAdjustment.BankAccount = billElectricityBill.BankAccount;
                            electricityBillAdjustment.Address_Pay = billElectricityBill.Address_Pay;
                            electricityBillAdjustment.HouseholdNumber = billElectricityBill.HouseholdNumber;
                            electricityBillAdjustment.CustomerName = billElectricityBill.CustomerName;
                            electricityBillAdjustment.CustomerCode_Pay = billElectricityBill.CustomerCode_Pay;
                            electricityBillAdjustment.BillAddress = billElectricityBill.BillAddress;
                            electricityBillAdjustment.TaxCode = billElectricityBill.TaxCode;
                            electricityBillAdjustment.CustomerName_Pay = billElectricityBill.CustomerName_Pay;
                            electricityBillAdjustment.BankName = billElectricityBill.BankName;
                            electricityBillAdjustment.BankAccount = billElectricityBill.BankAccount;
                            electricityBillAdjustment.AdjustmentType = input.AdjustmentType.Trim();
                            electricityBillAdjustment.FormOfPayment = "NH";
                            electricityBillAdjustment.SubTotal = -billElectricityBill.SubTotal;
                            electricityBillAdjustment.Total = -billElectricityBill.Total ?? 0;
                            electricityBillAdjustment.VAT = -billElectricityBill.VAT;
                            electricityBillAdjustment.CreateUser = userId;
                            electricityBillAdjustment.CreateDate = DateTime.Now;
                            electricityBillAdjustment.TaxRatio = billElectricityBill.TaxRatio;
                            electricityBillAdjustment.Term = input.TermAdjustment;
                            if (Liabilities_JobLatch != null)
                            {
                                electricityBillAdjustment.Month = Liabilities_JobLatch.Month;
                                electricityBillAdjustment.Year = Liabilities_JobLatch.Year;
                            }
                            else
                            {
                                electricityBillAdjustment.Month = DateTime.Now.Month;
                                electricityBillAdjustment.Year = DateTime.Now.Year;
                            }                            

                            electricityBillAdjustment.Term_Incurred = billElectricityBill.Term;
                            electricityBillAdjustment.Month_Incurred = billElectricityBill.Month;
                            electricityBillAdjustment.Year_Incurred = billElectricityBill.Year;
                            electricityBillAdjustment.StartDate = billElectricityBill.StartDate;
                            electricityBillAdjustment.EndDate = billElectricityBill.EndDate;
                            electricityBillAdjustment.FormOfPayment = billElectricityBill.FormOfPayment;
                            electricityBillAdjustment.PhoneNumber = input.PhoneNumber;
                            electricityBillAdjustment.ElectricityIndex = -billElectricityBill.ElectricityIndex;
                            electricityBillAdjustment.FigureBookId = billElectricityBill.FigureBookId.Value;
                        }
                        electricityBillAdjustmen.AddBill_ElectricityBillAdjustment(electricityBillAdjustment, _dbContext);

                        _dbContext.SaveChanges();

                        #endregion

                        #region Lập biên bản

                        var Bill_ElectricityBill =
                            _dbContext.Bill_ElectricityBill.Where(item => item.BillId.Equals(billAdjustmentId)).Select(item => new Bill_ElectricityBillModel()
                            {
                                Term = item.Term,
                                Month = item.Month,
                                Year = item.Year
                            }).FirstOrDefault();
                        if (Bill_ElectricityBill == null)
                        {
                            Bill_ElectricityBill =
                            _dbContext.Bill_ElectricityBillAdjustment.Where(item => item.BillId.Equals(billAdjustmentId)).Select(item => new Bill_ElectricityBillModel()
                            {
                                Term = item.Term,
                                Month = item.Month,
                                Year = item.Year
                            }).FirstOrDefault();
                        }
                        Bill_AdjustmentReportModel listmode = new Bill_AdjustmentReportModel();
                        listmode.ReportNumber = input.ReportNumber;
                        listmode.CreateDate = DateTime.Now;
                        listmode.CreateUser = userId;
                        listmode.ReasonId = input.ReasonId;
                        listmode.BillId = billId;
                        listmode.Term = input.TermAdjustment;
                        if (Liabilities_JobLatch != null)
                        {
                            listmode.Month = Liabilities_JobLatch.Month;
                            listmode.Year = Liabilities_JobLatch.Year;
                        }
                        else
                        {
                            listmode.Month = DateTime.Now.Month;
                            listmode.Year = DateTime.Now.Year;
                        }
                        listmode.BillAdjustmentId = input.BillAdjustmentId;
                        listmode.TermAdjustment = Bill_ElectricityBill.Term;
                        listmode.MonthAdjustment = Bill_ElectricityBill.Month;
                        listmode.YearAdjustment = Bill_ElectricityBill.Year;
                        // insert vào bảng biên bản
                        business_Bill_AdjustmentReport.Save_Bill_AdjustmentReport(listmode, _dbContext);

                        #endregion

                        // lấy ra danh sách chi tiết hóa đơn
                        var myArrayBillDetail = _dbContext.Bill_ElectricityBillDetail
                            .Where(item => item.BillId.Equals(billAdjustmentId)).Select(x => new Bill_ElectricityBillDetailModel
                            {
                                ElectricityMeterId = x.ElectricityMeterId,
                                TimeOfUse = x.TimeOfUse,
                                TimeOfSale = x.TimeOfSale,
                                PointId = x.PointId,
                                CustomerCode = x.CustomerCode,
                                CustomerId = x.CustomerId,
                                FigureBookId = x.FigureBookId,
                                Term = x.Term,
                                ElectricityIndex = x.ElectricityIndex,
                                Total = x.Total,
                                Price = x.Price,
                                CosFi = x.CosFi,
                                KCosFi = x.KCosFi,
                                ElectricityIndexHC = x.ElectricityIndexHC,
                                TotalHC = x.TotalHC,
                                MinusIndex = x.MinusIndex,
                                StationId = x.StationId,
                                TeamId = x.TeamId,
                                RouteId = x.RouteId,
                                ServicePointType = x.ServicePointType,
                                NumberOfPhases = x.NumberOfPhases,
                                HouseholdNumber = x.HouseholdNumber,
                                PotentialCode = x.PotentialCode,
                                OccupationsGroupCode = x.OccupationsGroupCode,
                                IndexId = x.IndexId,
                                RealElectricityIndex = x.RealElectricityIndex,
                                RegionId = x.RegionId
                            }).ToList();
                        if (myArrayBillDetail.Count == 0)
                        {
                            myArrayBillDetail = _dbContext.Bill_ElectricityBillAdjustmentDetail
                           .Where(item => item.BillId.Equals(billAdjustmentId)).Select(x => new Bill_ElectricityBillDetailModel
                           {
                               ElectricityMeterId = x.ElectricityMeterId,
                               TimeOfUse = x.TimeOfUse,
                               TimeOfSale = x.TimeOfSale,
                               PointId = x.PointId,
                               CustomerCode = x.CustomerCode,
                               CustomerId = x.CustomerId,
                               FigureBookId = x.FigureBookId,
                               Term = x.Term,
                               ElectricityIndex = x.ElectricityIndex,
                               Total = x.Total,
                               Price = x.Price,
                               CosFi = x.CosFi,
                               KCosFi = x.KCosFi,
                               ElectricityIndexHC = x.ElectricityIndexHC,
                               TotalHC = x.TotalHC,
                               MinusIndex = x.MinusIndex,
                               StationId = x.StationId,
                               TeamId = x.TeamId,
                               RouteId = x.RouteId,
                               ServicePointType = x.ServicePointType,
                               NumberOfPhases = x.NumberOfPhases,
                               HouseholdNumber = x.HouseholdNumber,
                               PotentialCode = x.PotentialCode,
                               OccupationsGroupCode = x.OccupationsGroupCode,
                               IndexId = x.IndexAdjustmentId,
                               RealElectricityIndex = x.RealElectricityIndex,
                               RegionId = x.RegionId
                           }).ToList();
                        }

                        #region update bảng chỉ số
                        var lstIndexValue = new Dictionary<string, decimal>();
                        if (myArrayBillDetail.Count() > 0)
                        {
                            var lstDetailDistinct = myArrayBillDetail.GroupBy(x => new { x.IndexId, x.ElectricityMeterId, x.TimeOfUse, x.PointId }).Select(x => new { x.Key.IndexId, x.Key.ElectricityMeterId, x.Key.TimeOfUse, x.Key.PointId }).ToList();
                            lstDetailDistinct.ForEach(indexData =>
                            {
                                decimal IndexId = Convert.ToDecimal(indexData.IndexId);
                                #region danh sach chi tiet chi so
                                Bill_IndexValueAdjustmentModel indexValueAdjustment =
                                    new Bill_IndexValueAdjustmentModel();
                                indexValueAdjustment.BillId = billId;
                                indexValueAdjustment.DepartmentId = departmentId;
                                Index_ValueModel vIndex;
                                if (isLapLaiCuaHoaDonLapLai)
                                {
                                    vIndex = _dbContext.Bill_IndexValueAdjustment.Where(item => item.IndexId.Equals(IndexId)).Select(x => new Index_ValueModel
                                    {
                                        Coefficient = x.Coefficient,
                                        OldValue = x.OldValue,
                                        NewValue = x.NewValue,
                                        StartDate = x.StartDate,
                                        EndDate = x.EndDate,
                                        AdjustPower = x.AdjustPower,
                                        CustomerId = x.CustomerId,
                                        MinusIndex = x.MinusIndex
                                    }).FirstOrDefault();
                                }
                                else
                                {
                                    vIndex = _dbContext.Index_Value.Where(item => item.IndexId.Equals(IndexId)).Select(x => new Index_ValueModel
                                    {
                                        Coefficient = x.Coefficient,
                                        OldValue = x.OldValue,
                                        NewValue = x.NewValue,
                                        StartDate = x.StartDate,
                                        EndDate = x.EndDate,
                                        AdjustPower = x.AdjustPower,
                                        CustomerId = x.CustomerId,
                                        MinusIndex = x.MinusIndex
                                    }).FirstOrDefault();
                                }

                                indexValueAdjustment.Coefficient = vIndex == null ? 1 : vIndex.Coefficient.Value;
                                indexValueAdjustment.ElectricityMeterId = indexData.ElectricityMeterId;
                                indexValueAdjustment.Term = input.TermAdjustment;
                                indexValueAdjustment.Month = Liabilities_JobLatch.Month;
                                indexValueAdjustment.Year = Liabilities_JobLatch.Year;
                                indexValueAdjustment.TimeOfUse = indexData.TimeOfUse;
                                indexValueAdjustment.OldValue = vIndex == null ? 0 : vIndex.OldValue.Value;
                                indexValueAdjustment.NewValue = vIndex == null ? 0 : vIndex.NewValue.Value;
                                indexValueAdjustment.PointId = indexData.PointId;
                                indexValueAdjustment.CreateDate = DateTime.Now;
                                indexValueAdjustment.CreateUser = userId;
                                indexValueAdjustment.StartDate = vIndex == null ? electricityBillAdjustment.StartDate : vIndex.StartDate;
                                indexValueAdjustment.EndDate = vIndex == null ? electricityBillAdjustment.EndDate : vIndex.EndDate;
                                indexValueAdjustment.AdjustPower = vIndex == null ? 0 : vIndex.AdjustPower;
                                indexValueAdjustment.CustomerId = vIndex == null ? electricityBillAdjustment.CustomerId : vIndex.CustomerId;
                                indexValueAdjustment.MinusIndex = vIndex == null ? 0 : vIndex.MinusIndex;
                                decimal indexAdjustmentId = businessBill_IndexValueAdjustment.AddBill_IndexValueAdjustment(indexValueAdjustment, _dbContext);
                                lstIndexValue.Add($"{indexData.IndexId}{indexData.TimeOfUse}", indexAdjustmentId);
                                // update vao bang chi so
                                #endregion
                            });
                        }
                        #endregion

                        for (int i = 0; i < myArrayBillDetail.Count; i++)
                        {
                            #region danh sach chi tiet hoa don

                            Bill_ElectricityBillAdjustmentDetailModel billAdjustmentDetail =
                                new Bill_ElectricityBillAdjustmentDetailModel();
                            billAdjustmentDetail.BillId = billId;
                            billAdjustmentDetail.DepartmentId = departmentId;
                            billAdjustmentDetail.CustomerId = myArrayBillDetail[i].CustomerId;
                            billAdjustmentDetail.CustomerCode = myArrayBillDetail[i].CustomerCode;
                            billAdjustmentDetail.PointId = myArrayBillDetail[i].PointId;
                            billAdjustmentDetail.FigureBookId = myArrayBillDetail[i].FigureBookId;
                            billAdjustmentDetail.Term = myArrayBillDetail[i].Term;

                            //Sửa tháng làm việc theo tháng trong hóa đơn, ko lấy theo tháng đã cài đặt nữa
                            if (Liabilities_JobLatch != null)
                            {
                                billAdjustmentDetail.Month = Liabilities_JobLatch.Month;
                                billAdjustmentDetail.Year = Liabilities_JobLatch.Year;
                            }
                            else
                            {
                                billAdjustmentDetail.Month = DateTime.Now.Month;
                                billAdjustmentDetail.Year = DateTime.Now.Year;
                            }

                            billAdjustmentDetail.ElectricityIndex = -myArrayBillDetail[i].ElectricityIndex;
                            billAdjustmentDetail.Price = myArrayBillDetail[i].Price;
                            billAdjustmentDetail.Total = -myArrayBillDetail[i].Total;
                            billAdjustmentDetail.TimeOfUse = myArrayBillDetail[i].TimeOfUse;
                            billAdjustmentDetail.TimeOfSale = myArrayBillDetail[i].TimeOfSale;

                            billAdjustmentDetail.CosFi = myArrayBillDetail[i].CosFi;
                            billAdjustmentDetail.KCosFi = myArrayBillDetail[i].KCosFi;
                            billAdjustmentDetail.ElectricityIndexHC = myArrayBillDetail[i].ElectricityIndexHC;
                            billAdjustmentDetail.TotalHC = myArrayBillDetail[i].TotalHC;
                            billAdjustmentDetail.MinusIndex = myArrayBillDetail[i].MinusIndex;
                            billAdjustmentDetail.RealElectricityIndex = -myArrayBillDetail[i].RealElectricityIndex;
                            billAdjustmentDetail.CreateDate = DateTime.Now;
                            billAdjustmentDetail.CreateUser = userId;
                            billAdjustmentDetail.OccupationsGroupCode = myArrayBillDetail[i].OccupationsGroupCode;
                            billAdjustmentDetail.ElectricityMeterId = myArrayBillDetail[i].ElectricityMeterId;
                            billAdjustmentDetail.IndexAdjustmentId = lstIndexValue[$"{myArrayBillDetail[i].IndexId}{myArrayBillDetail[i].TimeOfUse}"];

                            billAdjustmentDetail.PriceId = myArrayBillDetail[i].PriceId;
                            billAdjustmentDetail.StationId = myArrayBillDetail[i].StationId;
                            billAdjustmentDetail.TeamId = myArrayBillDetail[i].TeamId;
                            billAdjustmentDetail.RouteId = myArrayBillDetail[i].RouteId;
                            billAdjustmentDetail.RegionId = myArrayBillDetail[i].RegionId;
                            billAdjustmentDetail.ServicePointType = myArrayBillDetail[i].ServicePointType;
                            billAdjustmentDetail.NumberOfPhases = myArrayBillDetail[i].NumberOfPhases;
                            billAdjustmentDetail.HouseholdNumber = myArrayBillDetail[i].HouseholdNumber;
                            billAdjustmentDetail.PotentialCode = myArrayBillDetail[i].PotentialCode;

                            electricityBillAdjustmentDetail.AddBill_ElectricityBillAdjustment(billAdjustmentDetail, _dbContext);

                            #endregion
                        }
                        // up date bảng cong nợ trạng thái, 
                        Liabilities_TrackDebt listmode_TrackDebt = new Liabilities_TrackDebt();
                        listmode_TrackDebt.BillId = billAdjustmentId;
                        switch (input.AdjustmentType.Trim())
                        {
                            case EnumMethod.D_TinhChatHoaDon.HuyBo:
                                listmode_TrackDebt.Status = (int)(StatusTrackDebt.Cancel);
                                break;
                        }
                        bsTrackDebt.Updata_Status_Liabilities_TrackDebt(listmode_TrackDebt, _dbContext);
                        dbContextTransaction.Commit();

                        respone.Status = 1;
                        respone.Message = "Hủy bỏ hóa đơn thành công.";
                        respone.Data = input.BillAdjustmentId;
                        return createResponse();                        
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Hủy bỏ hóa đơn GetAllValue_HB", ex);
                        dbContextTransaction.Rollback();
                        throw new ArgumentException("Hủy bỏ hóa đơn không thành công.");
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

        // đây là hàm dùng cho hóa đơn hủy bỏ, chỉ cần cập nhật dữ liệu vào biên bản + trạng thái trong công nợ ưungs với hóa đơn bị hủy bỏ
        [HttpPost]
        [Route("ElectricityBill_HB")]
        public HttpResponseMessage ElectricityBill_HB(ElectricityBill_HBInput input)
        {
            try
            {
                var userId = TokenHelper.GetUserIdFromToken();
                using (var dbContextTransaction = _dbContext.Database.BeginTransaction())
                {
                    try
                    {
                        var Liabilities_JobLatch = _dbContext.Liabilities_JobLatch.ToList().FirstOrDefault();
                        var Bill_ElectricityBill =
                            _dbContext.Bill_ElectricityBill.Where(item => item.BillId.Equals(input.BillAdjustmentId))
                                .FirstOrDefault();
                        Bill_AdjustmentReportModel listmode = new Bill_AdjustmentReportModel();
                        listmode.ReportNumber = input.ReportNumber;
                        listmode.CreateDate = DateTime.Now;
                        listmode.CreateUser = userId;
                        listmode.ReasonId = input.ReasonId;
                        listmode.BillId = input.BillAdjustmentId;
                        listmode.Term = input.TermAdjustment;
                        if (Liabilities_JobLatch != null)
                        {
                            listmode.Month = Liabilities_JobLatch.Month;
                            listmode.Year = Liabilities_JobLatch.Year;
                        }
                        else
                        {
                            listmode.Month = DateTime.Now.Month;
                            listmode.Year = DateTime.Now.Year;
                        }
                        listmode.BillAdjustmentId = input.BillAdjustmentId;
                        listmode.TermAdjustment = Bill_ElectricityBill.Term;
                        listmode.MonthAdjustment = Bill_ElectricityBill.Month;
                        listmode.YearAdjustment = Bill_ElectricityBill.Year;
                        // insert vaof bangr bieen ban
                        business_Bill_AdjustmentReport.Save_Bill_AdjustmentReport(listmode, _dbContext);
                        // update status row hoas don trong bang con no len 2

                        Liabilities_TrackDebt listmode_TrackDebt = new Liabilities_TrackDebt();
                        listmode_TrackDebt.BillId = input.BillAdjustmentId;
                        bsTrackDebt.Updata_Status_Liabilities_TrackDebt(listmode_TrackDebt, _dbContext);
                        dbContextTransaction.Commit();

                        respone.Status = 1;
                        respone.Message = "OK";
                        respone.Data = null;
                        return createResponse();
                    }
                    catch
                    {
                        dbContextTransaction.Rollback();
                        throw new ArgumentException("Lỗi cập nhật trạng thái hóa đơn hủy bỏ.");
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

        //Todo: chưa viết api EditQuantityService, EditElectricityEstimate vì có model SelectListItem trong mvc

        [HttpGet]
        [Route("UpdateTaxInVoiceBill")]
        public HttpResponseMessage UpdateTaxInVoiceBill(decimal taxInvoiceId)
        {
            try
            {
                var billTaxInvoice = business_Bill_TaxInvoice.GetBillTaxInVoice(taxInvoiceId);

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = billTaxInvoice;
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
        [Route("CreateTaxInVoiceBill")]
        public HttpResponseMessage CreateTaxInVoiceBill(string customerCode)
        {
            try
            {
                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var listDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);

                if (!string.IsNullOrEmpty(customerCode))
                {
                    var concusCustomer = (from customer in _dbContext.Concus_Customer.Where(x => x.CustomerCode == customerCode && x.Status == 1 && x.DepartmentId == departmentId)
                                          join contract in _dbContext.Concus_Contract
                                          on customer.CustomerId equals contract.CustomerId
                                          join service in _dbContext.Concus_ServicePoint
                                          on contract.ContractId equals service.ContractId
                                          select new { customer, contract, service }).FirstOrDefault();

                    if (concusCustomer == null)
                    {
                        throw new ArgumentException($"Không tồn tại khách hàng mã {customerCode}");
                    }

                    var contractDetail = (from detail in _dbContext.Concus_ContractDetail
                                          join contract in _dbContext.Concus_Contract
                                          on detail.ContractId equals contract.ContractId
                                          where contract.CustomerId == concusCustomer.customer.CustomerId
                                          select detail).ToList();

                    List<CreateBillTaxInvoiceItem> billTax = new List<CreateBillTaxInvoiceItem>();
                    if (contractDetail?.Any() == true)
                    {
                        contractDetail.ForEach(item =>
                        {
                            var bill = new CreateBillTaxInvoiceItem
                            {
                                ContractId = concusCustomer.contract.ContractId,
                                CustomerId = concusCustomer.customer.CustomerId,
                                ServicePointId = concusCustomer.service.PointId,
                                TaxInvoiceId = 0,
                                TaxCode = concusCustomer.customer.TaxCode,
                                FigureBook = concusCustomer.service.FigureBookId,
                                CustomName = concusCustomer.customer.Name,
                                ServiceName = item.Description,
                                Unit = "",
                                Quantity = 0,
                                Price = item.Price.ToString("N0"),
                                Money = "0",
                                Total = "0",
                                TaxRatio = concusCustomer.customer.Ratio,
                                SubTotal = "0",
                                VAT = "0",
                                ServiceTypeId = item.ServiceTypeId,
                                ContractDetailId = item.ContractDetailId
                            };
                            billTax.Add(bill);
                        });
                    }
                    else
                    {
                        var bill = new CreateBillTaxInvoiceItem
                        {
                            ContractId = concusCustomer.contract.ContractId,
                            CustomerId = concusCustomer.customer.CustomerId,
                            ServicePointId = concusCustomer.service.PointId,
                            TaxInvoiceId = 0,
                            TaxCode = concusCustomer.customer.TaxCode,
                            FigureBook = concusCustomer.service.FigureBookId,
                            CustomName = concusCustomer.customer.Name,
                            ServiceName = "",
                            Unit = "",
                            Quantity = 0,
                            Price = "0",
                            Money = "0",
                            Total = "0",
                            TaxRatio = concusCustomer.customer.Ratio,
                            SubTotal = "0",
                            VAT = "0",
                            ServiceTypeId = 0,
                            ContractDetailId = null
                        };
                        billTax.Add(bill);
                    }

                    respone.Status = 1;
                    respone.Message = "OK";
                    respone.Data = billTax;
                    return createResponse();
                }
                else
                {
                    throw new ArgumentException($"Không tồn tại khách hàng mã {customerCode}.");
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
        [Route("CreateTaxInVoiceBill")]
        public HttpResponseMessage CreateTaxInVoiceBill(CreateTaxInVoiceBillModel BillTaxInvoiceItem)
        {
            try
            {
                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                bool kq = business_Bill_TaxInvoice.CreateTaxInvoiceBill(BillTaxInvoiceItem.listCreateBillTaxInvoiceItem, BillTaxInvoiceItem.VAT, BillTaxInvoiceItem.Total, User.Identity.Name, departmentId);
                if (kq)
                {
                    respone.Status = 1;
                    respone.Message = "Thêm hóa đơn thành công.";
                    respone.Data = null;
                    return createResponse();                    
                }
                else
                {
                    throw new ArgumentException("Hóa đơn không thêm được.");                   
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
        [Route("UpdateTaxInVoiceBill")]
        public HttpResponseMessage UpdateTaxInVoiceBill(UpdateTaxInVoiceBillModel BillTaxInvoiceItem)
        {
            try
            {
                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                bool kq = business_Bill_TaxInvoice.UpdateTaxInvoiceBill(BillTaxInvoiceItem.listUpdateBillTaxInvoiceItem, BillTaxInvoiceItem.VAT, BillTaxInvoiceItem.Total, User.Identity.Name, departmentId);
                if (kq)
                {
                    respone.Status = 1;
                    respone.Message = "Sửa hóa đơn thành công.";
                    respone.Data = null;
                    return createResponse();
                }
                else
                {
                    throw new ArgumentException("Hóa đơn không sửa được.");
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
        [Route("DeleteTaxInvoiceBill")]
        public HttpResponseMessage DeleteTaxInvoiceBill(UpdateBillItem model)
        {
            try
            {
                bool result = business_Bill_TaxInvoice.DeleteTaxInVoiceBill(model);

                if (result)
                {
                    business_Bill_TaxInvoice.DeleteTaxInVoiceBill(model.TaxInvoiceId);
                    respone.Status = 1;
                    respone.Message = "Xóa hóa đơn thành công.";
                    respone.Data = null;
                    return createResponse();
                }
                else
                {
                    throw new ArgumentException("Không thể xóa hóa đơn.");
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
        [Route("RemoveTaxInvoiceBill")]
        public HttpResponseMessage RemoveTaxInvoiceBill(UpdateBillItem model)
        {
            try
            {
                bool result = business_Bill_TaxInvoice.DeleteTaxInVoiceBillDetail(model);
                if (result)
                {
                    var listDetail = business_Bill_TaxInvoice.ListTaxInVoiceBillDetail(model);
                    if (listDetail.Count() > 1)
                    {
                        business_Bill_TaxInvoice.DeleteTaxInVoiceBillDetail(model.TaxInvoiceDetailId);                        
                        respone.Status = 1;
                        respone.Message = "Xóa hóa đơn thành công.";
                        respone.Data = null;
                        return createResponse();
                    }
                    else
                    {                        
                        business_Bill_TaxInvoice.DeleteTaxInVoiceBillDetail(model.TaxInvoiceDetailId);
                        respone.Status = 1;
                        respone.Message = "Xóa chi tiết hóa đơn thành công.";
                        respone.Data = null;
                        return createResponse();
                    }
                }
                else
                {
                    throw new ArgumentException("Không thể xóa.");                    
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

        #region Xem chi tiết GTGT xác nhận số liệu
        [HttpGet]
        [Route("DetailGTGT")]
        public HttpResponseMessage DetailGTGT(int FigureBookId, int month, int year)
        {
            try
            {
                CultureInfo cul = CultureInfo.GetCultureInfo("vi-VN");
                var model3 = (from A in _dbContext.Bill_TaxInvoiceDetail
                              join B in _dbContext.Concus_Customer
                              on A.CustomerId equals B.CustomerId
                              where A.FigureBookId == FigureBookId && A.Month == month && A.Year == year
                              select new
                              {
                                  TaxInvoiceDetailId = A.TaxInvoiceDetailId,
                                  CustomerId = A.CustomerId,
                                  CustomerCode = A.CustomerCode,
                                  Name = B.Name,
                                  FigureBookId = A.FigureBookId,
                                  ServiceName = A.ServiceName,
                                  Total = A.Total,
                                  Month = A.Month,
                                  TaxInvoiceId = A.TaxInvoiceId,
                                  Amount = A.Amount,
                                  Price = A.Price,
                                  TypeOfUnit = A.TypeOfUnit,
                                  A.ServiceTypeId
                              }).OrderBy(item => item.TaxInvoiceId).ToList();

                var model2 = (from C in model3
                              join D in _dbContext.Bill_TaxInvoice
                              on C.TaxInvoiceId equals D.TaxInvoiceId
                              select new
                              {
                                  TaxInvoiceDetailId = C.TaxInvoiceDetailId,
                                  CustomerId = C.CustomerId,
                                  CustomerCode = C.CustomerCode,
                                  Name = C.Name,
                                  FigureBookId = C.FigureBookId,
                                  ServiceName = C.ServiceName,
                                  Month = C.Month,
                                  TaxInvoiceId = C.TaxInvoiceId,
                                  ContractId = D.ContractId,
                                  SubTotal = D.SubTotal,
                                  Vat = D.VAT,
                                  Total = D.Total,
                                  Amount = C.Amount,
                                  Price = C.Price,
                                  TypeOfUnit = C.TypeOfUnit,
                                  C.ServiceTypeId
                              }).ToList();
                var model = (from E in model2
                             join F in _dbContext.Concus_Contract
                             on E.ContractId equals F.ContractId
                             select new
                             {
                                 TaxInvoiceDetailId = E.TaxInvoiceDetailId,
                                 CustomerId = E.CustomerId,
                                 CustomerCode = E.CustomerCode,
                                 Name = E.Name,
                                 FigureBookId = E.FigureBookId,
                                 ServiceName = E.ServiceName,
                                 Month = E.Month,
                                 TaxInvoiceId = E.TaxInvoiceId,
                                 ContractId = E.ContractId,
                                 ContractCode = F.ContractCode,
                                 SubTotal = E.SubTotal.ToString("N0", cul),
                                 Vat = E.Vat.ToString("N0", cul),
                                 Total = E.Total.ToString("N0", cul),
                                 Amount = E.Amount?.ToString("N0", cul) ?? "",
                                 Price = E.Price?.ToString("N0", cul) ?? "",
                                 TypeOfUnit = E.TypeOfUnit ?? "",
                                 E.ServiceTypeId
                             }).OrderBy(item => item.TaxInvoiceId).ToList();

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

        //Todo: Chưa viết api EditQLVHBill và EditCDCBill

        [HttpGet]
        [Route("GetData_Service")]
        public HttpResponseMessage GetData_Service([DefaultValue(0)] int ServiceId)
        {
            try
            {
                Category_ServiceModelDTO data = new Category_ServiceModelDTO();

                if (ServiceId == 0)
                    ServiceId = _dbContext.Category_Service.Select(item => item.ServiceId).FirstOrDefault();

                data = _dbContext.Category_Service.Where(item => item.ServiceId.Equals(ServiceId) && item.IsDelete == false).Select(item => new Category_ServiceModelDTO
                {
                    ServiceId = item.ServiceId,
                    ServiceName = item.ServiceName,
                    Unit = item.Unit,
                    Quantity = item.Quantity,
                    Price = item.Price,
                    Total = item.Total
                }).OrderBy(item => item.ServiceName).FirstOrDefault();

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = data;
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
        #endregion
    }
}
