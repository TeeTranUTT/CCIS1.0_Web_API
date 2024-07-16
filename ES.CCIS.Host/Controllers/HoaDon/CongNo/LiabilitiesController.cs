using CCIS_BusinessLogic;
using CCIS_DataAccess;
using ES.CCIS.Host.Helpers;
using ES.CCIS.Host.Models;
using ES.CCIS.Host.Models.EnumMethods;
using ES.CCIS.Host.Models.HoaDon.CongNo;
using Hangfire;
using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Web.Configuration;
using System.Web.Http;
using static CCIS_BusinessLogic.DefaultBusinessValue;

namespace ES.CCIS.Host.Controllers.HoaDon.CongNo
{
    [Authorize]
    [RoutePrefix("api/Liabilities")]
    public class LiabilitiesController : ApiBaseController
    {
        private int PageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_Index_CalendarOfSaveIndex Index_CalendarOfSaveIndex = new Business_Index_CalendarOfSaveIndex();
        private readonly Business_Bill_ElectronicBill BusiElectronicBill = new Business_Bill_ElectronicBill();
        private readonly Business_Administrator_Parameter vParameters = new Business_Administrator_Parameter();
        private readonly Business_Liabilities_TrackDebt_TaxInvoice trackDebt = new Business_Liabilities_TrackDebt_TaxInvoice();
        private readonly Business_Liabilities_TrackDebt_Log TrackDebtLog = new Business_Liabilities_TrackDebt_Log();
        private readonly Business_Liabilities_TrackDebt TrackDebt = new Business_Liabilities_TrackDebt();
        private readonly Business_Liabilities_ExcessMoney_Log ExcessMoneyLog = new Business_Liabilities_ExcessMoney_Log();
        private readonly Business_Liabilities_DebitBalance_TaxInvoice TaxInvoice = new Business_Liabilities_DebitBalance_TaxInvoice();
        private readonly Business_Liabilities_ExcessMoney_TaxInvoice_Log ExcessMoney_TaxInvoiceLog = new Business_Liabilities_ExcessMoney_TaxInvoice_Log();

        //Xác nhận số liệu hóa đơn
        [HttpGet]
        [Route("InvoiceValidation")]
        public HttpResponseMessage InvoiceValidation(DateTime? saveDate, [DefaultValue(1)] int term)
        {
            try
            {
                List<Index_CalendarOfSaveIndexModel> CalendarOfSave = new List<Index_CalendarOfSaveIndexModel>();
                var userInfo = TokenHelper.GetUserInfoFromRequest();
                var lstDepartmentIds = DepartmentHelper.GetChildDepIdsByUser(userInfo.UserName);

                if (saveDate == null)
                    saveDate = DateTime.Now;

                using (var db = new CCISContext())
                {
                    var SaveIndex =
                        db.Index_CalendarOfSaveIndex.Where(item =>
                                item.Term.Equals(term) && item.Month.Equals(saveDate.Value.Month) &&
                                item.Year.Equals(saveDate.Value.Year)
                                && (item.Status == (int)(DefaultBusinessValue.StatusCalendarOfSaveIndex.Bill) || item.Status == (int)(DefaultBusinessValue.StatusCalendarOfSaveIndex.ConfirmData)) && lstDepartmentIds.Contains(item.DepartmentId))
                            .ToList();

                    for (var i = 0; i < SaveIndex.Count; i++)
                    {
                        int FigureBookId = Convert.ToInt32(SaveIndex[i].FigureBookId);
                        List<Index_CalendarOfSaveIndexModel> list =
                            db.Index_CalendarOfSaveIndex.Where(item => item.FigureBookId.Equals(FigureBookId) && item.Term.Equals(term) && item.Month.Equals(saveDate.Value.Month) && item.Year.Equals(saveDate.Value.Year) && lstDepartmentIds.Contains(item.DepartmentId))
                                .Select(item => new Index_CalendarOfSaveIndexModel
                                {
                                    FigureBookId = item.FigureBookId,
                                    BookCode = item.Category_FigureBook.BookCode,
                                    BookName = item.Category_FigureBook.BookName,
                                    Term = term,
                                    Month = saveDate.Value.Month,
                                    Year = saveDate.Value.Year,
                                    DepartmentId = item.DepartmentId,
                                    Status = item.Status
                                }).ToList();
                        CalendarOfSave.AddRange(list);
                    }
                    if (CalendarOfSave.Count > 0)
                    {
                        for (int i = 0; i < CalendarOfSave.Count; i++)
                        {
                            int FigureBookId = Convert.ToInt32(CalendarOfSave[i].FigureBookId);
                            int ky = Convert.ToInt32(CalendarOfSave[i].Term);
                            int thang = Convert.ToInt32(CalendarOfSave[i].Month);
                            int nam = Convert.ToInt32(CalendarOfSave[i].Year);
                            var listBills
                                = db.Bill_ElectricityBill.Where(item =>
                                           FigureBookId == item.FigureBookId &&
                                           item.Term == ky && item.Month == thang &&
                                           item.Year == nam && lstDepartmentIds.Contains(item.DepartmentId)).ToList();
                            if (listBills.Count > 0)
                            {
                                var cout = listBills.Count;
                                var dientt = listBills.Where(item2 => item2.BillType.Equals(EnumMethod.LoaiHoaDon.TienDien)).Select(a => a.ElectricityIndex).DefaultIfEmpty(0).Sum();
                                var tongtien = listBills.Where(item2 => item2.BillType.Equals(EnumMethod.LoaiHoaDon.TienDien)).Select(a => a.Total).DefaultIfEmpty(0).Sum();
                                CalendarOfSave[i].CountBill = cout;
                                CalendarOfSave[i].SumElectricityIndex = dientt;
                                CalendarOfSave[i].Total = (decimal)tongtien;
                            }
                        }
                    }
                }

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = CalendarOfSave;
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
        [Route("SaveInvoiceValidation")]
        public HttpResponseMessage SaveInvoiceValidation(int editing, int Term, int Month, int Year, int DepartmentId)
        {
            // lay danh sach chi tiết hóa đơn dua vao so ghi chi so, ky, thang, nam
            using (var db = new CCISContext())
            {
                using (var dbContextTransaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        Index_CalendarOfSaveIndexModel model = new Index_CalendarOfSaveIndexModel();
                        model.FigureBookId = editing;
                        model.Term = Term;
                        model.Month = Month;
                        model.Year = Year;
                        model.DepartmentId = DepartmentId;
                        string Eror = "";
                        Index_CalendarOfSaveIndexModel calendarOfSave = model;

                        // lay ra Total từng (BT, CD, TD) để check xem có < 0 không, nếu có đưa ra cảnh báo và không cho xác nhận
                        var checkTotal = db.Bill_ElectricityBillDetail.Where(
                            item =>
                                item.FigureBookId.Equals(calendarOfSave.FigureBookId) &&
                                item.Term.Equals(calendarOfSave.Term)
                                && item.Month.Equals(calendarOfSave.Month) && item.Year.Equals(calendarOfSave.Year) &&
                                item.DepartmentId.Equals(calendarOfSave.DepartmentId))
                            .Select(item => new Bill_ElectricityBillDetailModel
                            {
                                Total = item.Total,
                                CustomerCode = item.CustomerCode
                            }).ToList();

                        for (int j = 0; j < checkTotal.Count; j++)
                        {
                            decimal total = Convert.ToDecimal(checkTotal[j].Total);
                            if (total < 0)
                            {
                                Eror = Eror + "/" + checkTotal[j].CustomerCode.Trim();
                            }
                        }
                        if (!string.IsNullOrEmpty(Eror))
                        {
                            throw new ArgumentException($"Lỗi: {Eror}");
                        }

                        Index_CalendarOfSaveIndex SaveIndex = new Index_CalendarOfSaveIndex();
                        SaveIndex.Term = Term;
                        SaveIndex.Month = Month;
                        SaveIndex.Year = Year;
                        SaveIndex.FigureBookId = editing;
                        SaveIndex.DepartmentId = DepartmentId;
                        SaveIndex.Status = (int)(DefaultBusinessValue.StatusCalendarOfSaveIndex.ConfirmData);
                        Index_CalendarOfSaveIndex.UpdataStatus(SaveIndex, db);
                        dbContextTransaction.Commit();

                        respone.Status = 1;
                        respone.Message = "Xác nhận hóa đơn thành công.";
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

        // hủy bó xác nhận hóa đơn trong sổ ghi chỉ số
        [HttpGet]
        [Route("CancelInvoiceValidation")]
        public HttpResponseMessage CancelInvoiceValidation(int editing, int Term, int Month, int Year, int DepartmentId)
        {
            using (var db = new CCISContext())
            {
                using (var dbContextTransaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        Index_CalendarOfSaveIndexModel model = new Index_CalendarOfSaveIndexModel();
                        model.FigureBookId = editing;
                        model.Term = Term;
                        model.Month = Month;
                        model.Year = Year;
                        model.DepartmentId = DepartmentId;
                        Index_CalendarOfSaveIndexModel calendarOfSave = model;

                        // up date trang thai lịch ghi chi số về 7
                        Index_CalendarOfSaveIndex SaveIndex = new Index_CalendarOfSaveIndex();
                        SaveIndex.Term = calendarOfSave.Term;
                        SaveIndex.Month = calendarOfSave.Month;
                        SaveIndex.Year = calendarOfSave.Year;
                        SaveIndex.FigureBookId = calendarOfSave.FigureBookId;
                        SaveIndex.DepartmentId = calendarOfSave.DepartmentId;
                        SaveIndex.Status = (int)(DefaultBusinessValue.StatusCalendarOfSaveIndex.Bill);

                        // check xem trạng thái = 9 mới đc thực hiện bước hủy
                        var status =
                            db.Index_CalendarOfSaveIndex.Where(
                                item =>
                                    item.Term.Equals(calendarOfSave.Term) && item.Month.Equals(calendarOfSave.Month) &&
                                    item.Year.Equals(calendarOfSave.Year) &&
                                    item.FigureBookId.Equals(calendarOfSave.FigureBookId))
                                .Select(item => item.Status)
                                .FirstOrDefault();
                        // lấy ra chỉ số seri trong bảng Category_Serial ung voi hoa don TD va VC về giá trị trước
                        if (status == 9)
                        {
                            Index_CalendarOfSaveIndex.UpdataStatus(SaveIndex, db);
                            dbContextTransaction.Commit();

                            respone.Status = 1;
                            respone.Message = "Hủy xác nhận hóa đơn thành công.";
                            respone.Data = null;
                            return createResponse();
                        }
                        else
                        {
                            throw new ArgumentException("Hủy xác nhận hóa đơn không thành công.");
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

        //xóa nợ tiền điện
        //Todo:chưa viết api LiabilitiesManager kiểu xóa nợ bằng file

        [HttpPost]
        [Route("LiabilitiesManager_ByCustomerId")]
        public HttpResponseMessage LiabilitiesManager_ByCustomerId(LiabilitiesManager_ByCustomerIdInput input)
        {
            try
            {
                DateTime ngayCham;
                DateTime.TryParseExact(input.NgayCham, "dd-MM-yyyy",
                   System.Globalization.CultureInfo.InvariantCulture,
                   System.Globalization.DateTimeStyles.None, out ngayCham);

                List<Liabilities_TrackDebtModel> allList = new List<Liabilities_TrackDebtModel>();
                Concus_Customer name_add = new Concus_Customer();
                decimal DebtAll, DebtTaxDebt, AllExcessMoney;
                int Month_Job, Year_Job;

                using (var db = new CCISContext())
                {
                    var listLiabilities =
                        db.Liabilities_TrackDebt.Where(item => item.BillId.Equals(input.BillId))
                            .Select(item => new Liabilities_TrackDebtModel
                            {
                                LiabilitiesId = item.LiabilitiesId,
                                BillId = item.BillId,
                                BillType = item.BillType,
                                DepartmentId = item.DepartmentId,
                                CustomerId = item.CustomerId,
                                CustomerCode = item.CustomerCode,
                                PointId = item.PointId,
                                Name = item.Name,
                                Address = item.Address,
                                InvoiceAddress = item.InvoiceAddress,
                                Term = item.Term,
                                Month = item.Month,
                                Year = item.Year,
                                FundsGenerated = item.FundsGenerated,
                                TaxesIncurred = item.TaxesIncurred,
                                Debt = item.Debt,
                                TaxDebt = item.TaxDebt,
                                PaymentMethodsCode = item.PaymentMethodsCode,
                                FigureBookId = item.FigureBookId,
                                StatusDebt = item.StatusDebt,
                                StatusCorrection = item.StatusCorrection,
                                CountOfDelivery = item.CountOfDelivery,
                                ReleaseDateBill = item.ReleaseDateBill,
                                CreateDate = item.CreateDate,
                                CreateUser = item.CreateUser,
                                EditDate = item.EditDate,
                                Status = item.Status,
                                PointCode = item.Concus_ServicePoint.PointCode,
                                AddressPoint = item.Concus_ServicePoint.Address,
                                TermMonthYear = "Kỳ " + item.Term + "/Tháng " + item.Month + "-" + item.Year,
                                BillingStatus = 1,

                            }).OrderBy(item => item.CustomerId).ThenBy(item => item.PointId).ToList();
                    // sau khi kết thúc vòng lặp, nếu list danh sách nợ còn thì phải cho hết vào danh sách tổng
                    allList.AddRange(listLiabilities);
                    // thực hiện vòng  lặp để lấy ra số tiền thừa của khách hàng
                    for (var m = 0; m < allList.Count; m++)
                    {
                        int departmentId = Convert.ToInt32(allList[m].DepartmentId);
                        int customerId = Convert.ToInt32(allList[m].CustomerId);
                        decimal excessMoney = ValueExcessMoney(customerId, departmentId, db);
                        allList[m].ExcessMoney = excessMoney;
                    }
                    name_add = db.Concus_Customer.Where(item => item.CustomerId.Equals(input.CustomerId)).FirstOrDefault();
                    DebtAll = allList.Sum(x => x.Debt);
                    DebtTaxDebt = allList.Sum(x => x.TaxDebt);
                    AllExcessMoney = allList.Select(x => x.ExcessMoney).FirstOrDefault();
                    int department_Id = TokenHelper.GetDepartmentIdFromToken();
                    var JobLatch = db.Liabilities_JobLatch
                        .Where(item => item.DepartmentId.Equals(department_Id)).ToList().FirstOrDefault();
                    if (JobLatch != null)
                    {
                        Month_Job = JobLatch.Month;
                        Year_Job = JobLatch.Year;
                    }
                    else
                    {
                        Month_Job = DateTime.Now.Month;
                        Year_Job = DateTime.Now.Year;
                    }
                }

                var response = new
                {
                    MonthJob = Month_Job,
                    YearJob = Year_Job,
                    BillId = input.BillId,
                    CustomerId = input.CustomerId,
                    CustomerCode = input.CustomerCode,
                    Name = name_add.Name,
                    sumall = DebtAll + DebtTaxDebt,
                    ExcessMoney = AllExcessMoney,
                    PayMoney = (DebtAll + DebtTaxDebt) > AllExcessMoney ? ((DebtAll + DebtTaxDebt) - AllExcessMoney) : 0,
                    allList
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
        [Route("SaveLiabilities_TrackDebt")]
        public HttpResponseMessage SaveLiabilities_TrackDebt(SaveLiabilities_TrackDebtInput input)
        {
            try
            {
                var payment = PaymentTrackDebt(input.CustomerId, input.NameMoney, input.Bill, input.PaymentDate, HThucTToan.CCIS);
                bool status = (bool)payment["MessageStatus"];
                // Tự động gửi email hóa đơn đến KH
                if (status)
                {
                    var businessDepartment = new Business_Administrator_Department();
                    int department_Id = TokenHelper.GetDepartmentIdFromToken();
                    var checkSendEmailInvoiceAuto = vParameters.GetParameterValue(Administrator_Parameter_Common.GUIHDON_TUDONG_SAUDUYET, "");
                    if (!string.IsNullOrEmpty(checkSendEmailInvoiceAuto))
                    {
                        Business_Sms_Manager business = new Business_Sms_Manager();
                        BackgroundJob.Enqueue(() => business.GuiThongBaoHoaDonTuDongSauKhiDuyetXoaNo(new List<decimal> { input.Bill }, department_Id));

                    }
                }
                else
                {
                    throw new ArgumentException($"{(string)payment["Message"]}");
                }

                respone.Status = 1;
                respone.Message = "OK";
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
        [Route("SaveLiabilities_TrackDebtAjaxCall")]
        public HttpResponseMessage SaveLiabilities_TrackDebtAjaxCall(List<PaymentTrackDeptModel> req)
        {
            try
            {
                int successCount = 0, errorCount = 0;
                string errorMessage = "";
                List<decimal> lstBillIdSucces = new List<decimal>();
                req.ForEach(item =>
                {
                    try
                    {
                        var PaymentMethodsCode = item.PaymentMethodsCode == "CK" ? HThucTToan.CHUYEN_KHOAN : HThucTToan.CCIS;
                        var payment = PaymentTrackDebt(item.CustomerId, item.namemoney, item.Bill, item.paymentDate, PaymentMethodsCode);
                        bool status = (bool)payment["MessageStatus"];
                        string message = message = (string)payment["Message"];
                        if (status)
                        {
                            lstBillIdSucces.Add(item.Bill);
                            successCount++;
                        }
                        else
                        {
                            errorCount++;
                            errorMessage = string.Concat(errorMessage, "\n", message);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        errorMessage = $"Mã KH lỗi: {item.CustomerId} {string.Concat(errorMessage, "\n", ex.Message)}";
                    }
                });
                // Tự động gửi email hóa đơn đến KH
                if (lstBillIdSucces.Count > 0)
                {
                    var businessDepartment = new Business_Administrator_Department();
                    int department_Id = businessDepartment.GetIddv(User.Identity.Name);
                    Business_Administrator_Parameter vParameters = new Business_Administrator_Parameter();
                    var checkSendEmailInvoiceAuto = vParameters.GetParameterValue(Administrator_Parameter_Common.GUIHDON_TUDONG_SAUDUYET, "", department_Id);
                    if (!string.IsNullOrEmpty(checkSendEmailInvoiceAuto))
                    {
                        Business_Sms_Manager business = new Business_Sms_Manager();
                        BackgroundJob.Enqueue(() => business.GuiThongBaoHoaDonTuDongSauKhiDuyetXoaNo(lstBillIdSucces, department_Id));
                    }
                }

                var response = new
                {
                    errorMessage = errorMessage,
                    successCount = successCount,
                    errorCount = errorCount
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


        #region Tiền non tải
        //xóa nợ non tải
        [HttpGet]
        [Route("LiabilitiesManager_TaxInvoice")]
        public HttpResponseMessage LiabilitiesManager_TaxInvoice(string Name = "", string CustomerCode = "")
        {
            try
            {
                int TongKH = 0;
                decimal TongTien = 0;
                int Month_Job, Year_Job;

                var department_Id = TokenHelper.GetDepartmentIdFromToken();
                List<Liabilities_TrackDebt_TaxInvoiceModel> allList = new List<Liabilities_TrackDebt_TaxInvoiceModel>();

                if (Name == "" && CustomerCode == "")
                {
                    AllLiabilities_TrackDebt_TaxInvoice(TongKH, TongTien, allList, department_Id);
                }
                else
                {
                    CustomerLiabilities_TrackDebt_TaxInvoice(TongKH, TongTien, allList, Name, CustomerCode);
                }
                using (var db = new CCISContext())
                {
                    var JobLatch = db.Liabilities_JobLatch
                        .Where(item => item.DepartmentId.Equals(department_Id)).ToList().FirstOrDefault();
                    if (JobLatch != null)
                    {
                        Month_Job = JobLatch.Month;
                        Year_Job = JobLatch.Year;
                    }
                    else
                    {
                        Month_Job = DateTime.Now.Month;
                        Year_Job = DateTime.Now.Year;
                    }
                }

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = allList;
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
        [Route("SaveLiabilities_TrackDebt_TaxInvoice")]
        public HttpResponseMessage SaveLiabilities_TrackDebt_TaxInvoice(SaveLiabilities_TrackDebt_TaxInvoiceInput input)
        {
            using (var db = new CCISContext())
            {
                using (var dbContextTransaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        string parames = "";
                        string saveDate = "";
                        decimal money = 0;
                        decimal paymentMoney = 0;
                        if (input.NameMoney != "")
                        {
                            input.NameMoney = input.NameMoney.Replace(" ", "");
                            money = Convert.ToDecimal(input.NameMoney);
                            paymentMoney = Convert.ToDecimal(input.NameMoney);
                        }

                        Liabilities_ExcessMoney ExcessMoney = new Liabilities_ExcessMoney();
                        int createUser = TokenHelper.GetUserIdFromToken();
                        // thực hiện chấm xóa nợ
                        // lấy ra tiền thừa khách hàng nếu có
                        decimal excessMoney = GetLiabilities_ExcessMoney_TaxInvoice(input.CustomerId);
                        money = money + excessMoney;
                        // lấy dang sách nông nợ ứng với CustomerId;
                        var listLiabilitiesTrackDebt =
                        db.Liabilities_TrackDebt_TaxInvoice.Where(
                            item => item.CustomerId.Equals(input.CustomerId) && item.TaxInvoiceId.Equals(input.TaxInvoiceId) && (item.Debt != 0 || item.TaxDebt != 0))
                            .Select(item => new Liabilities_TrackDebt_TaxInvoiceModel
                            {
                                Liabilities_TaxInvoiceId = item.Liabilities_TaxInvoiceId,
                                TaxInvoiceId = item.TaxInvoiceId,
                                BillType = item.BillType,
                                DepartmentId = item.DepartmentId,
                                CustomerId = item.CustomerId,
                                CustomerCode = item.CustomerCode,
                                Name = item.Name,
                                Address = item.Address,
                                InvoiceAddress = item.InvoiceAddress,
                                Month = item.Month,
                                Year = item.Year,
                                FundsGenerated = item.FundsGenerated,
                                TaxesIncurred = item.TaxesIncurred,
                                Debt = item.Debt,
                                TaxDebt = item.TaxDebt,
                                PaymentMethodsCode = item.PaymentMethodsCode,
                                FigureBookId = item.FigureBookId,
                                StatusDebt = item.StatusDebt,
                                StatusCorrection = item.StatusCorrection,
                                CountOfDelivery = item.CountOfDelivery,
                                ReleaseDateBill = item.ReleaseDateBill,
                                CreateDate = item.CreateDate,
                                CreateUser = item.CreateUser,
                                EditDate = item.EditDate,
                                Status = item.Status,
                            })
                            .OrderBy(item => item.CustomerId)
                            .ThenBy(item => item.Year)
                            .ThenBy(item => item.Month)
                            .ThenBy(item => item.BillType)
                            .ToList();

                        #region Cập nhật tiền nợ phát sinh và nợ tồn
                        for (var i = 0; i < listLiabilitiesTrackDebt.Count; i++)
                        {
                            // bước 1 : -  cập nhật tiền từng row trước,
                            decimal debt = Convert.ToDecimal(listLiabilitiesTrackDebt[i].Debt);
                            decimal taxDebt = Convert.ToDecimal(listLiabilitiesTrackDebt[i].TaxDebt);
                            // nếu tổng tiền nhỏ hơn thuế
                            if (money < taxDebt && money > 0)
                            {
                                // thực hiện updata tiền thuế trước, đưa tiền thuế về 0;
                                listLiabilitiesTrackDebt[i].TaxDebt = (taxDebt - money);
                                money = money - taxDebt;
                            }
                            // nếu tổng tiền lớn hơn thuế
                            if (money >= taxDebt && money > 0)
                            {
                                // thực hiện updata tiền thuế trước, đưa tiền thuế về 0;
                                listLiabilitiesTrackDebt[i].TaxDebt = 0;
                                money = money - taxDebt;
                                //  nếu tổng tiền nhỏ hơn Debt
                                if (money < debt && money > 0)
                                {
                                    // thực hiện updata tiền thuế trước, đưa tiền thuế về 0;
                                    listLiabilitiesTrackDebt[i].Debt = (debt - money);
                                    money = money - debt;
                                }
                                // nếu tổng tiền lớn hơn Debt.
                                if (money >= debt && money > 0)
                                {
                                    // thực hiện updata tiền thuế trước, đưa tiền thuế về 0;
                                    listLiabilitiesTrackDebt[i].Debt = 0;
                                    money = money - debt;
                                }
                            }
                            listLiabilitiesTrackDebt[i].Status = 1;
                            listLiabilitiesTrackDebt[i].EditDate = DateTime.Now;
                            Insert_LiabilitiesTrackDebt_TaxInvoiceLog(listLiabilitiesTrackDebt[i].Liabilities_TaxInvoiceId, db, createUser, input.PaymentDate, paymentMoney);
                            trackDebt.Updata_Liabilities_TrackDebt_TaxInvoice(listLiabilitiesTrackDebt[i], db);
                        }
                        #endregion

                        #region cập nhật tiền thừa
                        // nếu tiền đang còn thừa, sẽ cập nhật vào bảng tiền thừa
                        money = money > 0 ? money : 0;
                        // kiểm tra xem có chưa, nếu chưa có thì insert
                        int checkLiabilitiesExcessMoney =
                            db.Liabilities_ExcessMoney_TaxInvoice.Where(item => item.CustomerId.Equals(input.CustomerId)).Count();
                        if (checkLiabilitiesExcessMoney > 0)
                        {
                            // thực hiện lưu vào log trước Liabilities_ExcessMoney_Log
                            Insert_LiabilitiesExcessMoney_TaxInvoiceLog(input.CustomerId, db, createUser);
                            //  da có tien thua trong bang du lieu => thuc hien updata
                            ExcessMoney.CustomerId = input.CustomerId;
                            ExcessMoney.ExcessMoney = money;
                            ExcessMoney.EditDate = DateTime.Now;
                            ExcessMoney.EditUser = createUser;
                            trackDebt.Edit_Liabilities_ExcessMoney_TaxInvoice(ExcessMoney, db);
                        }
                        else
                        {
                            var customerCode =
                                (db.Concus_Customer.Where(item => item.CustomerId.Equals(input.CustomerId))
                                    .Select(item => item.CustomerCode).FirstOrDefault());
                            // chua co tien thua trong bang du lieu => thuc hien insert
                            ExcessMoney.CustomerId = input.CustomerId;
                            ExcessMoney.ExcessMoney = money;
                            ExcessMoney.CreateDate = DateTime.Now;
                            ExcessMoney.EditDate = DateTime.Now;
                            ExcessMoney.CreateUser = createUser;
                            ExcessMoney.DepartmentId =
                                (db.Concus_Customer.Where(item => item.CustomerId.Equals(input.CustomerId))
                                    .Select(item => item.DepartmentId)
                                    .FirstOrDefault());
                            if (customerCode != null) ExcessMoney.CustomerCode = customerCode.ToString();
                            trackDebt.Insert_Liabilities_ExcessMoney_TaxInvoice(ExcessMoney, db);
                        }
                        #endregion
                        #region Kiểm tra xem khách hàng còn nợ tiền hay không, nếu có thì hiển thị khách hàng lên
                        // check xem nguoi dung nay con cong no khong, neu con thi hien thi nguoi dung nay len, khong thi thoi
                        var checkshown =
                            db.Liabilities_TrackDebt_TaxInvoice.Where(
                                item => item.CustomerId.Equals(input.CustomerId) && (item.Debt != 0 || item.TaxDebt != 0)).FirstOrDefault();
                        var nameKh = (db.Concus_Customer.Where(item => item.CustomerId.Equals(input.CustomerId))
                                .Select(item => item.CustomerCode).FirstOrDefault());
                        if (checkshown != null)
                        {
                            parames = nameKh;
                            saveDate = checkshown.Month + "-" + checkshown.Year;
                        }

                        // Tự động gửi email hóa đơn đến KH
                        Business_Administrator_Parameter vParameters = new Business_Administrator_Parameter();
                        var taxinvoice = db.Bill_TaxInvoice.Where(x => x.TaxInvoiceId == input.TaxInvoiceId).Select(x => new { x.BillId, x.DepartmentId }).FirstOrDefault();
                        var checkSendEmailInvoiceAuto = vParameters.GetParameterValue(Administrator_Parameter_Common.GUIHDON_TUDONG_SAUDUYET, "", iDepartmentId: taxinvoice.DepartmentId);
                        if (!string.IsNullOrEmpty(checkSendEmailInvoiceAuto))
                        {
                            Business_Sms_Manager business = new Business_Sms_Manager();
                            BackgroundJob.Enqueue(() => business.GuiThongBaoHoaDonTuDongSauKhiDuyetXoaNo(new List<decimal> { taxinvoice.BillId }, taxinvoice.DepartmentId));
                        }
                        #endregion
                        dbContextTransaction.Commit();

                        respone.Status = 1;
                        respone.Message = $"Cập nhật công nợ thành công khách hàng {nameKh}.";
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

        [HttpPost]
        [Route("LiabilitiesManager_TaxInvoice")]
        public HttpResponseMessage LiabilitiesManager_TaxInvoice(LiabilitiesManager_TaxInvoiceInput input)
        {
            try
            {
                List<Liabilities_TrackDebt_TaxInvoiceModel> allList = new List<Liabilities_TrackDebt_TaxInvoiceModel>();
                Concus_Customer name_add = new Concus_Customer();
                Bill_TaxInvoice Bill_TaxInvoice = new Bill_TaxInvoice();
                decimal DebtAll, DebtTaxDebt, AllExcessMoney;
                int Month_Job, Year_Job;

                using (var db = new CCISContext())
                {
                    // khai báo  danh sách chứa cả nợ tồn + pát sinh trong tháng.

                    // lấy ra danh sách row có trạng thái = 0
                    var listLiabilities =
                        db.Liabilities_TrackDebt_TaxInvoice.Where(item => item.CustomerId.Equals(input.CustomerId) && item.TaxInvoiceId.Equals(input.TaxInvoiceId) && (item.Status != 2 && item.Status != 3))
                            .Select(item => new Liabilities_TrackDebt_TaxInvoiceModel
                            {
                                Liabilities_TaxInvoiceId = item.Liabilities_TaxInvoiceId,
                                TaxInvoiceId = item.TaxInvoiceId,
                                BillType = item.BillType,
                                DepartmentId = item.DepartmentId,
                                CustomerId = item.CustomerId,
                                CustomerCode = item.CustomerCode,
                                Name = item.Name,
                                Address = item.Address,
                                InvoiceAddress = item.InvoiceAddress,
                                Month = item.Month,
                                Year = item.Year,
                                FundsGenerated = item.FundsGenerated,
                                TaxesIncurred = item.TaxesIncurred,
                                Debt = item.Debt,
                                TaxDebt = item.TaxDebt,
                                PaymentMethodsCode = item.PaymentMethodsCode,
                                FigureBookId = item.FigureBookId,
                                StatusDebt = item.StatusDebt,
                                StatusCorrection = item.StatusCorrection,
                                CountOfDelivery = item.CountOfDelivery,
                                ReleaseDateBill = item.ReleaseDateBill,
                                CreateDate = item.CreateDate,
                                CreateUser = item.CreateUser,
                                EditDate = item.EditDate,
                                Status = item.Status,
                                TermMonthYear = "Tháng " + item.Month + "-" + item.Year,
                                TaxInvoiceStatus = 1,
                            }).OrderBy(item => item.CustomerId).ThenBy(item => item.CustomerId).ToList();

                    // lay ra danh sách khách hàng còn nợ (check : Debt != 0  hoặc TaxDebt != 0)
                    var listTrackDebt =
                        db.Liabilities_TrackDebt_TaxInvoice.Where(item => item.CustomerId.Equals(input.CustomerId) && (item.Debt != 0 || item.TaxDebt != 0) && (item.Status != 2 && item.Status != 3))
                            .Select(item => new Liabilities_TrackDebt_TaxInvoiceModel
                            {
                                Liabilities_TaxInvoiceId = item.Liabilities_TaxInvoiceId,
                                TaxInvoiceId = item.TaxInvoiceId,
                                BillType = item.BillType,
                                DepartmentId = item.DepartmentId,
                                CustomerId = item.CustomerId,
                                CustomerCode = item.CustomerCode,
                                Name = item.Name,
                                Address = item.Address,
                                InvoiceAddress = item.InvoiceAddress,
                                Month = item.Month,
                                Year = item.Year,
                                FundsGenerated = item.FundsGenerated,
                                TaxesIncurred = item.TaxesIncurred,
                                Debt = item.Debt,
                                TaxDebt = item.TaxDebt,
                                PaymentMethodsCode = item.PaymentMethodsCode,
                                FigureBookId = item.FigureBookId,
                                StatusDebt = item.StatusDebt,
                                StatusCorrection = item.StatusCorrection,
                                CountOfDelivery = item.CountOfDelivery,
                                ReleaseDateBill = item.ReleaseDateBill,
                                CreateDate = item.CreateDate,
                                CreateUser = item.CreateUser,
                                EditDate = item.EditDate,
                                Status = item.Status,
                                TermMonthYear = "Tháng " + item.Month + "-" + item.Year,
                                TaxInvoiceStatus = 0,
                            }).ToList();

                    // thực hiện vòng lặp, ghép khách hàng nợ tồn với phát sinh lại cạnh nhau                     
                    // sau khi kết thúc vòng lặp, nếu list danh sách nợ còn thì phải cho hết vào danh sách tổng
                    allList.AddRange(listLiabilities);
                    // thực hiện vòng  lặp để lấy ra số tiền thừa của khách hàng
                    for (var m = 0; m < allList.Count; m++)
                    {
                        int departmentId = Convert.ToInt32(allList[m].DepartmentId);
                        int customerId = Convert.ToInt32(allList[m].CustomerId);
                        decimal excessMoney = ValueExcessMoneyTaxInvoice(customerId, departmentId, db);
                        allList[m].ExcessMoney = excessMoney;
                        if ((allList[m].Debt != 0 || allList[m].TaxDebt != 0) && allList[m].Status == 1)
                        {
                            allList[m].TaxInvoiceStatus = 0;
                        }
                    }

                    name_add = db.Concus_Customer.Where(item => item.CustomerId.Equals(input.CustomerId)).FirstOrDefault();
                    DebtAll = allList.Sum(x => x.Debt);
                    DebtTaxDebt = allList.Sum(x => x.TaxDebt);
                    AllExcessMoney = allList.Select(x => x.ExcessMoney).FirstOrDefault();


                    int department_Id = TokenHelper.GetDepartmentIdFromToken();
                    var JobLatch = db.Liabilities_JobLatch
                        .Where(item => item.DepartmentId.Equals(department_Id)).ToList().FirstOrDefault();
                    if (JobLatch != null)
                    {
                        Month_Job = JobLatch.Month;
                        Year_Job = JobLatch.Year;
                    }
                    else
                    {
                        Month_Job = DateTime.Now.Month;
                        Year_Job = DateTime.Now.Year;
                    }
                }

                var response = new
                {
                    MonthJob = Month_Job,
                    YearJob = Year_Job,
                    TaxInvoiceId = input.TaxInvoiceId,
                    CustomerId = input.CustomerId,
                    Name = name_add.Name,
                    sumall = DebtAll + DebtTaxDebt,
                    ExcessMoney = AllExcessMoney,
                    allList
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
        #endregion

        #region Hủy bỏ xóa nợ
        [HttpGet]
        [Route("DebtCancellation")]
        public HttpResponseMessage DebtCancellation([DefaultValue("")] string Name, DateTime? saveDate, [DefaultValue(0)] int Term, [DefaultValue(0)] int FigureBookId)
        {
            try
            {
                var response = new DebtCancellationModel();
                using (var db = new CCISContext())
                {
                    var departmentId = TokenHelper.GetDepartmentIdFromToken();
                    var listDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);

                    if (saveDate != null)
                    {
                        int month = saveDate.Value.Month;
                        int year = saveDate.Value.Year;
                        var listTrackDebt = (from no in db.Liabilities_TrackDebt
                                             join lg in db.Liabilities_TrackDebt_Log
                                             on no.BillId equals lg.BillId
                                             join tg in db.Liabilities_JobLatch
                                             on no.DepartmentId equals tg.DepartmentId
                                             where no.DepartmentId == lg.DepartmentId && no.CustomerId == lg.CustomerId
                                                   && no.Month == month && no.Year == year
                                                   && (Term == 0 || no.Term == Term)
                                                   && lg.BillId == lg.BillId
                                                   && (FigureBookId == 0 || no.FigureBookId == FigureBookId)
                                                   && (Name == "" || no.Name.Contains(Name) || no.CustomerCode.Contains(Name))
                                                   && listDepartmentId.Contains(no.DepartmentId)
                                             select new Liabilities_TrackDebtModel
                                             {
                                                 LiabilitiesId = no.LiabilitiesId,
                                                 BillId = no.BillId,
                                                 BillType = no.BillType,
                                                 DepartmentId = no.DepartmentId,
                                                 CustomerId = no.CustomerId,
                                                 CustomerCode = no.CustomerCode,
                                                 PointId = no.PointId,
                                                 Name = no.Name,
                                                 Address = no.Address,
                                                 InvoiceAddress = no.InvoiceAddress,
                                                 Term = no.Term,
                                                 Month = no.Month,
                                                 Year = no.Year,
                                                 FundsGenerated = no.FundsGenerated,
                                                 TaxesIncurred = no.TaxesIncurred,
                                                 Debt = no.Debt,
                                                 TaxDebt = no.TaxDebt,
                                                 PaymentMethodsCode = no.PaymentMethodsCode,
                                                 FigureBookId = no.FigureBookId,
                                                 StatusDebt = no.StatusDebt,
                                                 StatusCorrection = no.StatusCorrection,
                                                 CountOfDelivery = no.CountOfDelivery,
                                                 ReleaseDateBill = no.ReleaseDateBill,
                                                 CreateDate = no.CreateDate,
                                                 CreateUser = no.CreateUser,
                                                 EditDate = no.EditDate,
                                                 Status = no.Status,
                                                 PointCode = no.Concus_ServicePoint.PointCode,
                                                 AddressPoint = no.Concus_ServicePoint.Address,
                                                 TermMonthYear = "Kỳ " + no.Term + "/Tháng " + no.Month + "-" + no.Year,
                                                 BillingStatus = 0,
                                                 AdjustmentType = db.Bill_ElectricityBillAdjustment.Where(item => item.BillId.Equals(no.BillId)).Select(item => item.AdjustmentType).FirstOrDefault(),
                                             }).OrderBy(item => item.CustomerId).ToList(); // Bỏ Distinct() vì nếu có sẽ ko sắp xếp theo CustomerId

                        for (var i = 0; i < listTrackDebt.Count; i++)
                        {
                            if (listTrackDebt[i].AdjustmentType == "HB")
                            {
                                listTrackDebt.RemoveAt(i);
                            }
                        }
                        response.liabilities_TrackDebts = listTrackDebt;
                        response.TongTien = (int)listTrackDebt.Sum(item => item.FundsGenerated + item.TaxesIncurred - item.Debt - item.TaxDebt);
                        response.TongKH = listTrackDebt.Count();
                    }
                }

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
        [Route("GetLiabilities_TrackDebt_Log")]
        public HttpResponseMessage GetLiabilities_TrackDebt_Log(int BillId, int CustomerId)
        {
            try
            {
                List<Liabilities_TrackDebt_LogModel> allList = new List<Liabilities_TrackDebt_LogModel>();
                string thoigian;
                Concus_Customer name_add = new Concus_Customer();

                using (var db = new CCISContext())
                {
                    int departmentId = TokenHelper.GetDepartmentIdFromToken();
                    var listLiabilities =
                        db.Liabilities_TrackDebt_Log.Where(item => item.BillId.Equals(BillId))
                            .Select(item => new Liabilities_TrackDebt_LogModel
                            {
                                Id_LiabilitiesIdLog = item.Id_LiabilitiesIdLog,
                                LiabilitiesId = item.LiabilitiesId,
                                BillId = item.BillId,
                                BillType = item.BillType,
                                DepartmentId = item.DepartmentId,
                                CustomerId = item.CustomerId,
                                CustomerCode = item.CustomerCode,
                                PointId = item.PointId,
                                DateDot = item.DateDot,
                                CreateDate = item.CreateDate,
                                FundsGenerated = item.FundsGenerated,
                                TaxesIncurred = item.TaxesIncurred,
                                TongTienNo = (db.Liabilities_TrackDebt.Where(a => a.BillId.Equals(BillId)).Select(a => a.FundsGenerated).FirstOrDefault()),
                                TongThueNo = (db.Liabilities_TrackDebt.Where(a => a.BillId.Equals(BillId)).Select(a => a.TaxesIncurred).FirstOrDefault()),
                                PointCode = (db.Concus_ServicePoint.Where(a => a.PointId.Equals(item.PointId)).Select(a => a.PointCode).FirstOrDefault()),
                                Day = item.DateDot.Day,
                                Month = item.DateDot.Month,
                                Year = item.DateDot.Year,
                                Payment = item.Payment,
                                Name = (db.UserProfile.Where(a => a.UserId.Equals(item.CreateUser)).Select(a => a.UserName)).FirstOrDefault()
                            }).OrderByDescending(item => item.Id_LiabilitiesIdLog).ThenBy(item => item.DateDot).ToList();
                    // sau khi kết thúc vòng lặp, nếu list danh sách nợ còn thì phải cho hết vào danh sách tổng
                    allList.AddRange(listLiabilities);
                    // thực hiện vòng  lặp để lấy ra số tiền thừa của khách hàng
                    name_add = db.Concus_Customer.Where(item => item.CustomerId.Equals(CustomerId)).FirstOrDefault();
                    var Liabilities_JobLatch = db.Liabilities_JobLatch
                        .Where(item => item.DepartmentId.Equals(departmentId)).ToList().FirstOrDefault();
                    thoigian = Liabilities_JobLatch.Month + "/" + Liabilities_JobLatch.Year;
                    var figureBookId = db.Bill_ElectricityBill.Where(item => item.BillId == BillId).FirstOrDefault().FigureBookId;

                    var response = new
                    {
                        BillId = BillId,
                        CustomerId = CustomerId,
                        Name = name_add.Name,
                        thoigian = thoigian,
                        Liabilities_TrackDebts = allList
                    };

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
        [Route("DebtCancellation_TrackDebt_Log")]
        public HttpResponseMessage DebtCancellation_TrackDebt_Log(int Id_LiabilitiesIdLog)
        {
            try
            {
                bool trangthai = false;
                var errorMess = "";
                using (var db = new CCISContext())
                {
                    int departmentId = TokenHelper.GetDepartmentIdFromToken();

                    var listLiabilities = db.Liabilities_TrackDebt_Log
                        .FirstOrDefault(item => item.Id_LiabilitiesIdLog.Equals(Id_LiabilitiesIdLog));
                    var Liabilities_JobLatch = db.Liabilities_JobLatch
                        .Where(item => item.DepartmentId.Equals(departmentId) &&
                                       item.Month.Equals(listLiabilities.Month) &&
                                       item.Year.Equals(listLiabilities.Year)).ToList().Count();
                    if (Liabilities_JobLatch != 0)  //trong tháng hoạch toán -> cho xóa
                    {
                        // Lấy dòng hóa đơn cần hủy
                        var liabilitiesTrackDebt = db.Liabilities_TrackDebt.FirstOrDefault(item => item.DepartmentId == listLiabilities.DepartmentId                            
                            && item.LiabilitiesId == listLiabilities.LiabilitiesId && item.BillId.Equals(listLiabilities.BillId));

                        if (listLiabilities != null && liabilitiesTrackDebt != null)
                        {
                            using (var dbContextTransaction = db.Database.BeginTransaction())
                            {
                                try
                                {
                                    //Trả lại 2 cột tiền nợ , thuế nợ trước đây
                                    liabilitiesTrackDebt.Debt += listLiabilities.DeptPay;
                                    liabilitiesTrackDebt.TaxDebt += listLiabilities.TaxPay;
                                    liabilitiesTrackDebt.Status = 0;                                    

                                    //Kiểm tra xem có liên quan tiền thừa hay không?
                                    var lstExcessMoneyLog = db.Liabilities_ExcessMoney_Log.FirstOrDefault(item => item.BillId.Equals(listLiabilities.BillId)
                                        && item.CustomerCode.Equals(listLiabilities.CustomerCode)
                                        && item.PayMonth.Equals(listLiabilities.Month)
                                        && item.PayYear.Equals(listLiabilities.Year)
                                        && item.PayDay.Equals(listLiabilities.DateDot.Day)
                                        && item.Id_LiabilitiesIdLog.Equals(listLiabilities.Id_LiabilitiesIdLog));
                                    if (lstExcessMoneyLog != null)  //nếu có liên quan tiền thừa?
                                    {
                                        // lấy tiền thừa hiện tại
                                        var vExcessMoney = db.Liabilities_ExcessMoney
                                            .FirstOrDefault(item => item.DepartmentId == listLiabilities.DepartmentId && item.CustomerId.Equals(listLiabilities.CustomerId));
                                        //trả lại tiền thừa về trạng thái trước đấy
                                        vExcessMoney.ExcessMoney = vExcessMoney.ExcessMoney - lstExcessMoneyLog.ExcessMoney;
                                        db.SaveChanges();
                                        //xóa log tiền thừa
                                        db.Liabilities_ExcessMoney_Log.Remove(lstExcessMoneyLog);
                                        db.SaveChanges();
                                    }
                                    
                                    //xóa dòng log đã thêm gần đây nhất đi
                                    db.Liabilities_TrackDebt_Log.Remove(listLiabilities);
                                    db.SaveChanges();
                                    trangthai = true;
                                    dbContextTransaction.Commit();
                                }
                                catch (Exception ex)
                                {
                                    dbContextTransaction.Rollback();
                                    trangthai = false;
                                    errorMess = ex.ToString();

                                }
                            }
                        }
                        else
                        {
                            trangthai = false;
                            errorMess = "Không tìm thấy khách hàng này (Liabilities_TrackDebt) Vui lòng liên hệ hỗ trợ";
                        }
                    }
                    else
                    {
                        errorMess = "Các khách hàng này đã chuyển tháng không thể thao tác hủy";
                    }

                    var response = new
                    {
                        Trangthai = trangthai,
                        Message = errorMess
                    };

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
        #endregion

        #region Hủy bỏ xóa nợ tiền non tải
        [HttpGet]
        [Route("TaxInvoiceCancellation")]
        public HttpResponseMessage TaxInvoiceCancellation([DefaultValue("")] string Name, DateTime? saveDate, [DefaultValue(0)] int FigureBookId)
        {
            try
            {
                List<Liabilities_TrackDebt_TaxInvoiceModel> allList = new List<Liabilities_TrackDebt_TaxInvoiceModel>();
                int TongKH = 0;
                decimal TongTien = 0;

                if (saveDate == null)
                    saveDate = DateTime.Now;                
                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var lstDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);

                using (var db = new CCISContext())
                {
                    AllTaxInvoiceCancellation(TongKH, TongTien, allList, db, Name.Trim(), saveDate, FigureBookId);
                    allList = allList.OrderBy(p => p.CustomerId).ToList();
                }
                
                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = allList;
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
        [Route("GetLiabilities_TrackDebt_TaxInvoice_Log")]
        public HttpResponseMessage GetLiabilities_TrackDebt_TaxInvoice_Log(int TaxInvoiceId, int CustomerId)
        {
            try
            {
                List<Liabilities_TrackDebt_TaxInvoice_LogModel> allList = new List<Liabilities_TrackDebt_TaxInvoice_LogModel>();
                string thoigian;
                Concus_Customer name_add = new Concus_Customer();

                using (var db = new CCISContext())
                {
                    int departmentId = TokenHelper.GetDepartmentIdFromToken();
                    var listLiabilities =
                        db.Liabilities_TrackDebt_TaxInvoice_Log.Where(item => item.TaxInvoiceId.Equals(TaxInvoiceId))
                            .Select(item => new Liabilities_TrackDebt_TaxInvoice_LogModel
                            {
                                Id_LiabilitiesTaxInvoiceLog = item.Id_LiabilitiesTaxInvoiceLog,
                                Liabilities_TaxInvoiceId = item.Liabilities_TaxInvoiceId,
                                TaxInvoiceId = item.TaxInvoiceId,
                                DepartmentId = item.DepartmentId,
                                CustomerId = item.CustomerId,
                                CustomerCode = item.CustomerCode,
                                DateDot = item.DateDot,
                                CreateDate = item.CreateDate,
                                FundsGenerated = item.FundsGenerated,
                                TaxesIncurred = item.TaxesIncurred,
                                TongTienNo = (db.Liabilities_TrackDebt_TaxInvoice.Where(a => a.TaxInvoiceId.Equals(TaxInvoiceId)).Select(a => a.FundsGenerated).FirstOrDefault()),
                                TongThueNo = (db.Liabilities_TrackDebt_TaxInvoice.Where(a => a.TaxInvoiceId.Equals(TaxInvoiceId)).Select(a => a.TaxesIncurred).FirstOrDefault()),
                                Day = item.DateDot.Day,
                                Month = item.DateDot.Month,
                                Year = item.DateDot.Year,
                                Payment = item.Payment,
                                Name = (db.UserProfile.Where(a => a.UserId.Equals(item.CreateUser)).Select(a => a.UserName)).FirstOrDefault()
                            }).OrderByDescending(item => item.Id_LiabilitiesTaxInvoiceLog).ToList();
                    // sau khi kết thúc vòng lặp, nếu list danh sách nợ còn thì phải cho hết vào danh sách tổng
                    allList.AddRange(listLiabilities);
                    // thực hiện vòng  lặp để lấy ra số tiền thừa của khách hàng
                    name_add = db.Concus_Customer.Where(item => item.CustomerId.Equals(CustomerId)).FirstOrDefault();
                    var Liabilities_JobLatch = db.Liabilities_JobLatch
                        .Where(item => item.DepartmentId.Equals(departmentId)).ToList().FirstOrDefault();
                    thoigian = Liabilities_JobLatch.Month + "/" + Liabilities_JobLatch.Year;
                }

                var response = new
                {
                    TaxInvoiceId = TaxInvoiceId,
                    CustomerId = CustomerId,
                    Name = name_add.Name,
                    thoigian = thoigian,
                    allList
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
        [Route("DebtCancellation_TaxInvoice_Log")]
        public HttpResponseMessage DebtCancellation_TaxInvoice_Log(int Id_LiabilitiesTaxInvoiceLog)
        {
            try
            {
                bool trangthai = false;

                using (var db = new CCISContext())
                {
                    int departmentId = TokenHelper.GetDepartmentIdFromToken();

                    var listLiabilities =
                        db.Liabilities_TrackDebt_TaxInvoice_Log.Where(item => item.Id_LiabilitiesTaxInvoiceLog.Equals(Id_LiabilitiesTaxInvoiceLog))
                            .FirstOrDefault();
                    var Liabilities_JobLatch = db.Liabilities_JobLatch
                        .Where(item => item.DepartmentId.Equals(departmentId) &&
                                       item.Month.Equals(listLiabilities.Month) &&
                                       item.Year.Equals(listLiabilities.Year)).ToList().Count();
                    if (Liabilities_JobLatch != 0)
                    {
                        // thực hiện vòng  lặp để lấy ra số tiền thừa của khách hàng
                        var liabilitiesTrackDebt = db.Liabilities_TrackDebt_TaxInvoice
                            .Where(item => item.TaxInvoiceId.Equals(listLiabilities.TaxInvoiceId)).FirstOrDefault();
                        // lấy tiền thừa trong bảng tiền thừa ứng với khách hàng
                        var liabilitiesExcessMoney = db.Liabilities_ExcessMoney_TaxInvoice
                            .Where(item => item.CustomerId.Equals(listLiabilities.CustomerId)).FirstOrDefault();
                        // lấy ra dòng mới nhất trong bảng Liabilities_ExcessMoney_Log ứng với khách hàng
                        var ExcessMoneyLog_Id = db.Liabilities_ExcessMoney_TaxInvoice_Log.OrderByDescending(item => item.ExcessMoneyLogTaxInvoice_Id)
                            .Where(item => item.CustomerCode.Equals(listLiabilities.CustomerCode))
                            .Select(item => item.ExcessMoneyLogTaxInvoice_Id).FirstOrDefault();

                        if (listLiabilities != null && liabilitiesTrackDebt != null)
                        {
                            decimal sotienNhapvao = listLiabilities.Payment;
                            decimal tienthuaHientai = liabilitiesExcessMoney != null ? liabilitiesExcessMoney.ExcessMoney : 0;
                            decimal tongtienconnoHientai = (liabilitiesTrackDebt.TaxDebt + liabilitiesTrackDebt.Debt);
                            decimal tongtienconnoQuakhu = listLiabilities.FundsGenerated + listLiabilities.TaxesIncurred;
                            decimal tienthuaLichsu = tienthuaHientai -
                                                     (sotienNhapvao - (tongtienconnoQuakhu - tongtienconnoHientai));
                            Liabilities_ExcessMoney_TaxInvoice money = new Liabilities_ExcessMoney_TaxInvoice();
                            money.CustomerId = listLiabilities.CustomerId;
                            money.ExcessMoney = tienthuaLichsu;
                            Liabilities_TrackDebt_TaxInvoice TrackDebt = new Liabilities_TrackDebt_TaxInvoice();
                            TrackDebt.TaxInvoiceId = listLiabilities.TaxInvoiceId;
                            TrackDebt.Debt = listLiabilities.FundsGenerated;
                            TrackDebt.TaxDebt = listLiabilities.TaxesIncurred;
                            Liabilities_TrackDebt_TaxInvoice_Log TrackDebt_Log = new Liabilities_TrackDebt_TaxInvoice_Log();
                            TrackDebt_Log.Id_LiabilitiesTaxInvoiceLog = listLiabilities.Id_LiabilitiesTaxInvoiceLog;

                            Liabilities_ExcessMoney_TaxInvoice_Log ExcessMoney_Log = new Liabilities_ExcessMoney_TaxInvoice_Log();
                            ExcessMoney_Log.ExcessMoneyLogTaxInvoice_Id = ExcessMoneyLog_Id;
                            using (var dbContextTransaction = db.Database.BeginTransaction())
                            {
                                try
                                {
                                    //Bước 1 : trả lại tiền thừa về trạng thái trước đấy
                                    if (liabilitiesExcessMoney != null)
                                    {
                                        TaxInvoice.Rollback_ExcessMoneyTaxInvoice(money, db);
                                    }
                                    //bước 2 : trả lại 2 cột tiền nợ , thuế nợ trước đây
                                    TaxInvoice.Rollback_TaxInvoice(TrackDebt, db);
                                    // bước 3 : xóa dòng log đã thêm gần đây nhất đi
                                    TaxInvoice.DeleteLiabilities_TrackDebt_TaxInvoice_Log(TrackDebt_Log, db);
                                    // Bước 4 : xóa dòng tiền thừa trong bảng log đi
                                    ExcessMoney_TaxInvoiceLog.DeleteLiabilities_ExcessMoney_TaxInvoice_Log(ExcessMoney_Log, db);
                                    trangthai = true;
                                    dbContextTransaction.Commit();
                                }
                                catch (Exception ex)
                                {
                                    dbContextTransaction.Rollback();
                                }
                            }
                        }
                        else
                        {
                            trangthai = false;
                        }
                    }

                }

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = trangthai;
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

        #region Commons
        protected decimal ValueExcessMoney(int CustomerId, int DepartmentId, CCISContext db)
        {
            var value =
                db.Liabilities_ExcessMoney.Where(
                    item => item.CustomerId.Equals(CustomerId) && item.DepartmentId.Equals(DepartmentId))
                    .Select(item => item.ExcessMoney)
                    .FirstOrDefault();
            return value;
        }
        // hàm truyền vào người dùng id, lấy ra tiền thừa khách hàng
        protected decimal GetLiabilities_ExcessMoney(int customerId)
        {
            using (var db = new CCISContext())
            {
                var excessMoney =
                    db.Liabilities_ExcessMoney.Where(item => item.CustomerId.Equals(customerId))
                        .Select(item => item.ExcessMoney)
                        .FirstOrDefault();
                return excessMoney;
            }
        }

        protected void Update_LiabilitiesExcessMoney(Liabilities_TrackDebtModel TrackDept, int iUser, DateTime PayDate, CCISContext db, decimal excessMoneyUsed, int logTrack)
        {
            if (excessMoneyUsed == 0)
                return;
            var ExcessMoney = db.Liabilities_ExcessMoney.Where(item => item.CustomerId.Equals(TrackDept.CustomerId)).FirstOrDefault();
            if (ExcessMoney != null) //đã có dòng
            {
                ExcessMoney.ExcessMoney = ExcessMoney.ExcessMoney + excessMoneyUsed;
                db.SaveChanges();
            }
            else
            {
                // chua co tien thua trong bang du lieu => thuc hien insert
                ExcessMoney = new Liabilities_ExcessMoney();
                ExcessMoney.DepartmentId = TrackDept.DepartmentId;
                ExcessMoney.CustomerId = TrackDept.CustomerId;
                ExcessMoney.CreateDate = DateTime.Now;
                ExcessMoney.CreateUser = iUser;
                ExcessMoney.EditDate = DateTime.Now;
                ExcessMoney.EditUser = iUser;
                ExcessMoney.ExcessMoney = excessMoneyUsed;
                ExcessMoney.CustomerCode = TrackDept.CustomerCode;
                db.Liabilities_ExcessMoney.Add(ExcessMoney);
                db.SaveChanges();
            }

            //lưu log sử dụng tiền thừa
            var target = new Liabilities_ExcessMoney_Log();
            target.ExcessMoney_Id = ExcessMoney.ExcessMoney_Id;
            target.DepartmentId = ExcessMoney.DepartmentId;
            target.CustomerId = ExcessMoney.CustomerId;
            target.CustomerCode = ExcessMoney.CustomerCode;
            target.CreateDate = DateTime.Now;
            target.EditDate = DateTime.Now;
            target.EditUser = iUser;
            target.ExcessMoney = excessMoneyUsed;
            target.PayDay = PayDate.Day;
            target.PayMonth = PayDate.Month;
            target.PayYear = PayDate.Year;
            target.BillId = TrackDept.BillId;
            target.OldExcessMoney = ExcessMoney.ExcessMoney - excessMoneyUsed;
            target.Id_LiabilitiesIdLog = logTrack;
            db.Liabilities_ExcessMoney_Log.Add(target);
            db.SaveChanges();
        }

        private Dictionary<string, object> PaymentTrackDebt(int CustomerId, string namemoney, int Bill, DateTime paymentDate, string PaymentMethodsCode)
        {

            Dictionary<string, object> result = new Dictionary<string, object>();
            string parames = "";
            decimal money = 0;
            decimal paymentMoney = 0;
            string saveDate = "";
            decimal payForBill_Dept = 0;
            decimal payForBill_Tax = 0;
            decimal excessMoneyUsed = 0;

            if (!string.IsNullOrEmpty(namemoney))
            {
                namemoney = namemoney.Replace(" ", "");
                money = Convert.ToDecimal(namemoney);
                paymentMoney = Convert.ToDecimal(namemoney);
            }

            Liabilities_ExcessMoney ExcessMoney = new Liabilities_ExcessMoney();
            int createUser = TokenHelper.GetUserIdFromToken();
            // thực hiện chấm xóa nợ
            // lấy ra tiền thừa khách hàng nếu có
            decimal excessMoney = GetLiabilities_ExcessMoney(CustomerId);
            money = money + excessMoney;
            // lấy dang sách công nợ ứng với BillId;
            using (var db = new CCISContext())
            {
                using (var dbContextTransaction = db.Database.BeginTransaction())
                {
                    string customerCode = "";
                    try
                    {
                        var AdjustmentType = db.Bill_ElectricityBillAdjustment
                            .Where(item => item.BillId.Equals(Bill)).Select(item => item.AdjustmentType)
                            .FirstOrDefault();
                        if (AdjustmentType == "TH")
                        {
                            paymentMoney = paymentMoney * -1;
                            money = paymentMoney; //không động vào tiền thừa nếu là hóa đơn TH
                        }
                        #region Lấy bản ghi cần thanh toán trong bảng TrackDebt
                        var liabilitiesTrackDebt =
                        db.Liabilities_TrackDebt.Where(
                            item => item.BillId.Equals(Bill))
                            .Select(item => new Liabilities_TrackDebtModel
                            {
                                LiabilitiesId = item.LiabilitiesId,
                                BillId = item.BillId,
                                BillType = item.BillType,
                                DepartmentId = item.DepartmentId,
                                CustomerId = item.CustomerId,
                                CustomerCode = item.CustomerCode,
                                PointId = item.PointId,
                                Name = item.Name,
                                Address = item.Address,
                                InvoiceAddress = item.InvoiceAddress,
                                Term = item.Term,
                                Month = item.Month,
                                Year = item.Year,
                                FundsGenerated = item.FundsGenerated,
                                TaxesIncurred = item.TaxesIncurred,
                                Debt = item.Debt,
                                TaxDebt = item.TaxDebt,
                                PaymentMethodsCode = PaymentMethodsCode,
                                FigureBookId = item.FigureBookId,
                                StatusDebt = item.StatusDebt,
                                StatusCorrection = item.StatusCorrection,
                                CountOfDelivery = item.CountOfDelivery,
                                ReleaseDateBill = item.ReleaseDateBill,
                                CreateDate = item.CreateDate,
                                CreateUser = item.CreateUser,
                                EditDate = item.EditDate,
                                Status = item.Status
                            }).FirstOrDefault();
                        customerCode = liabilitiesTrackDebt.CustomerCode;
                        #endregion

                        //lấy ra ngày cuối kỳ 
                        var endDate = db.Index_CalendarOfSaveIndex.Where(
                                item => item.FigureBookId == liabilitiesTrackDebt.FigureBookId &&
                                        item.Term == liabilitiesTrackDebt.Term &&
                                        item.Month == liabilitiesTrackDebt.Month &&
                                        item.Year == liabilitiesTrackDebt.Year)
                            .Select(item => item.EndDate).FirstOrDefault();

                        // chỉ cho phép xóa nợ nếu ngày xóa nợ lớn hơn hoặc bằng với ngày cuối kỳ
                        if (paymentDate >= endDate)
                        {
                            #region logic
                            saveDate = liabilitiesTrackDebt.Month + "-" + liabilitiesTrackDebt.Year; // trường này dùng để redirect url
                            // bước 1 : -  cập nhật tiền từng row trước,
                            decimal debt = liabilitiesTrackDebt.Debt;
                            decimal taxDebt = liabilitiesTrackDebt.TaxDebt;
                            int logTrack;
                            if (money > 0) //trường hợp phổ biến, hóa đơn tiền > 0
                            {
                                if (paymentMoney >= debt + taxDebt)  //nếu tiền nộp lần này nhiều hơn hoặc bằng tiền hóa đơn chấm thì chỉ sử dụng tiền nộp thôi
                                {
                                    liabilitiesTrackDebt.Debt = 0;
                                    liabilitiesTrackDebt.TaxDebt = 0;
                                    liabilitiesTrackDebt.Status = 1;
                                    //Cập nhật bảng nợ (xóa nợ)
                                    logTrack = TrackDebt.Updata_Liabilities_TrackDebt(liabilitiesTrackDebt, db, createUser, paymentDate);
                                    if (paymentMoney > debt + taxDebt)  //nếu tiền nộp lớn hơn tiền nợ thì cập nhật thêm vào tiền thừa
                                    {
                                        Update_LiabilitiesExcessMoney(liabilitiesTrackDebt, createUser, paymentDate, db, paymentMoney - debt - taxDebt, logTrack);
                                    }
                                }
                                else  //trường hợp tiền nộp ít hơn tiền phải nộp thì sử dụng tiền nộp và tiền thừa nếu có
                                {
                                    if (paymentMoney > 0) //tiền nộp ưu tiên sử dụng trước
                                    {
                                        // trường hợp tiền nộp vào nhỏ hơn hoặc bằng tiền thuế thì ưu tiên xử lý tiền thuế trước
                                        // trường hợp tiền nộp lớn hơn tiền thuế thì xử lý cả tiền thuế và tiền nợ
                                        // nếu tiền nộp mà ít hơn tiền nợ thì sẽ trừ đi và lưu tổng tiền nợ cuối cùng vào bảng TrackDebt
                                        if (paymentMoney <= taxDebt)
                                        {
                                            liabilitiesTrackDebt.TaxDebt = (taxDebt - paymentMoney);
                                        }
                                        else
                                        {
                                            liabilitiesTrackDebt.TaxDebt = 0;
                                            liabilitiesTrackDebt.Debt = debt - (paymentMoney - taxDebt);
                                        }
                                        //Cập nhật bảng nợ (xóa nợ)
                                        logTrack = TrackDebt.Updata_Liabilities_TrackDebt(liabilitiesTrackDebt, db, createUser, paymentDate);
                                    }
                                    // sau khi xử lý xong tiền nộp nếu vẫn còn dư nợ thì xử lý tiếp đến tiền thừa nếu có để trừ đi nốt số tiền nợ
                                    if (excessMoney > 0) //tiền thừa sử dụng sau (nếu có)
                                    {
                                        debt = liabilitiesTrackDebt.Debt;
                                        taxDebt = liabilitiesTrackDebt.TaxDebt;
                                        if (excessMoney <= taxDebt) // nếu tiền thừa nhỏ hơn tiền thuế thì ưu tiên xử lý tiền thuế
                                        {
                                            liabilitiesTrackDebt.TaxDebt = (taxDebt - excessMoney);
                                            excessMoneyUsed = excessMoney;
                                        }
                                        else
                                        {
                                            liabilitiesTrackDebt.TaxDebt = 0;
                                            if ((excessMoney - taxDebt) >= debt) //nếu tiền thừa sau khi trừ vào tiền thuế còn nhiều hơn tiền nợ - thì trừ hết nợ
                                            {
                                                liabilitiesTrackDebt.Debt = 0;
                                                liabilitiesTrackDebt.Status = 1;
                                                excessMoneyUsed = taxDebt + debt;
                                            }
                                            else
                                            {
                                                liabilitiesTrackDebt.Debt = debt - (excessMoney - taxDebt);
                                                excessMoneyUsed = excessMoney;
                                            }
                                        }
                                        //Cập nhật bảng nợ (xóa nợ)
                                        logTrack = TrackDebt.Updata_Liabilities_TrackDebt(liabilitiesTrackDebt, db, createUser, paymentDate);
                                        Update_LiabilitiesExcessMoney(liabilitiesTrackDebt, createUser, paymentDate, db, (excessMoneyUsed * -1), logTrack);
                                    }
                                }
                            }
                            else  //tiền thoái hoàn, trả lại khách hàng
                            {
                                // nếu tổng tiền nhỏ hơn thuế
                                if (money > taxDebt && money < 0)
                                {
                                    // thực hiện updata tiền thuế trước, đưa tiền thuế về 0;
                                    liabilitiesTrackDebt.TaxDebt = (taxDebt - money);
                                    payForBill_Dept = 0;
                                    payForBill_Tax = money;
                                    money = 0;  //đã hết tiền
                                }
                                // nếu tổng tiền lớn hơn thuế
                                if (money <= taxDebt && money < 0)
                                {
                                    // thực hiện updata tiền thuế trước, đưa tiền thuế về 0;
                                    liabilitiesTrackDebt.TaxDebt = 0;
                                    payForBill_Tax = taxDebt;
                                    money = money - taxDebt;
                                    //  nếu tổng tiền nhỏ hơn Debt
                                    if (money > debt && money < 0)
                                    {
                                        // thực hiện updata tiền thuế trước, đưa tiền thuế về 0;
                                        liabilitiesTrackDebt.Debt = (debt - money);
                                        payForBill_Dept = money;
                                        money = 0;
                                    }
                                    // nếu tổng tiền lớn hơn Debt.
                                    if (money <= debt && money > 0)
                                    {
                                        // thực hiện updata tiền thuế trước, đưa tiền thuế về 0;
                                        liabilitiesTrackDebt.Debt = 0;
                                        liabilitiesTrackDebt.Status = 1;
                                        money = 0;
                                        payForBill_Dept = debt;
                                    }
                                }
                            }

                            #region Kiểm tra xem khách hàng còn nợ tiền hay không, nếu có thì hiển thị khách hàng lên
                            // check xem nguoi dung nay con cong no khong, neu con thi hien thi nguoi dung nay len, khong thi thoi
                            var checkshown = db.Liabilities_TrackDebt.Where(
                                     item => item.CustomerId.Equals(CustomerId) && (item.Debt != 0 || item.TaxDebt != 0)).FirstOrDefault();
                            if (checkshown != null)
                            {
                                parames = checkshown.CustomerCode;
                            }
                            //var 
                            #endregion
                            result.Add("MessageStatus", true);
                            result.Add("Message", $"Cập nhật công nợ thành công khách hàng {liabilitiesTrackDebt.CustomerCode}");
                            // Gọi hàm gửi tin nhắn
                            //SmsManagerController smsManager = new SmsManagerController();
                            //smsManager.SmsTrackDebt_Alert(new List<int> { Bill }, createUser);
                            dbContextTransaction.Commit();
                            #endregion
                        }
                        else
                        {
                            result.Add("MessageStatus", false);
                            result.Add("Message", $"Cập nhật công nợ không thành công mã {customerCode}: ngày chấm ({paymentDate.ToString("dd/MM/yyyy")}) không được nhỏ hơn ngày cuối kỳ hóa đơn là ngày {endDate.ToString("dd/MM/yyyy")}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("PaymentTrackDebt", ex);
                        result.Add("MessageStatus", false);
                        result.Add("Message", $"Lỗi khi xóa nợ mã {customerCode}: {ex.Message}");
                        dbContextTransaction.Rollback();
                    }
                    result.Add("saveDate", saveDate);
                    result.Add("parames", parames);
                    return result;
                }
            }
        }

        #region Non tải
        // lấy toàn bộ danh sánh công nợ non tải
        protected void AllLiabilities_TrackDebt_TaxInvoice(int TongKH, decimal TongTien, List<Liabilities_TrackDebt_TaxInvoiceModel> allList, int departmentId)
        {
            using (var db = new CCISContext())
            {
                // khai báo  danh sách chứa cả nợ tồn + pát sinh trong tháng.

                // lấy ra danh sách row có trạng thái = 0
                var listLiabilitiesTaxInvoice =
                    db.Liabilities_TrackDebt_TaxInvoice.Where(item => (item.Debt != 0 || item.TaxDebt != 0) && item.Status == 0 && item.DepartmentId == departmentId)
                        .Select(item => new Liabilities_TrackDebt_TaxInvoiceModel
                        {
                            Liabilities_TaxInvoiceId = item.Liabilities_TaxInvoiceId,
                            TaxInvoiceId = item.TaxInvoiceId,
                            BillType = item.BillType,
                            DepartmentId = item.DepartmentId,
                            CustomerId = item.CustomerId,
                            CustomerCode = item.CustomerCode,
                            Name = item.Name,
                            Address = item.Address,
                            InvoiceAddress = item.InvoiceAddress,
                            Month = item.Month,
                            Year = item.Year,
                            FundsGenerated = item.FundsGenerated,
                            TaxesIncurred = item.TaxesIncurred,
                            Debt = item.Debt,
                            TaxDebt = item.TaxDebt,
                            PaymentMethodsCode = item.PaymentMethodsCode,
                            FigureBookId = item.FigureBookId,
                            StatusDebt = item.StatusDebt,
                            StatusCorrection = item.StatusCorrection,
                            CountOfDelivery = item.CountOfDelivery,
                            ReleaseDateBill = item.ReleaseDateBill,
                            CreateDate = item.CreateDate,
                            CreateUser = item.CreateUser,
                            EditDate = item.EditDate,
                            Status = item.Status,
                            TermMonthYear = "Tháng " + item.Month + "-" + item.Year,
                            TaxInvoiceStatus = 1,
                        }).OrderBy(item => item.CustomerId).ThenBy(item => item.CustomerId).ToList();

                // lay ra danh sách khách hàng còn nợ (check : Debt != 0  hoặc TaxDebt != 0)
                var listTrackDebtTaxInvoice =
                    db.Liabilities_TrackDebt_TaxInvoice.Where(item => item.Status == 1 && (item.Debt != 0 || item.TaxDebt != 0) && item.DepartmentId == departmentId)
                        .Select(item => new Liabilities_TrackDebt_TaxInvoiceModel
                        {
                            Liabilities_TaxInvoiceId = item.Liabilities_TaxInvoiceId,
                            TaxInvoiceId = item.TaxInvoiceId,
                            BillType = item.BillType,
                            DepartmentId = item.DepartmentId,
                            CustomerId = item.CustomerId,
                            CustomerCode = item.CustomerCode,
                            Name = item.Name,
                            Address = item.Address,
                            InvoiceAddress = item.InvoiceAddress,
                            Month = item.Month,
                            Year = item.Year,
                            FundsGenerated = item.FundsGenerated,
                            TaxesIncurred = item.TaxesIncurred,
                            Debt = item.Debt,
                            TaxDebt = item.TaxDebt,
                            PaymentMethodsCode = item.PaymentMethodsCode,
                            FigureBookId = item.FigureBookId,
                            StatusDebt = item.StatusDebt,
                            StatusCorrection = item.StatusCorrection,
                            CountOfDelivery = item.CountOfDelivery,
                            ReleaseDateBill = item.ReleaseDateBill,
                            CreateDate = item.CreateDate,
                            CreateUser = item.CreateUser,
                            EditDate = item.EditDate,
                            Status = item.Status,
                            TermMonthYear = "Tháng " + item.Month + "-" + item.Year,
                            TaxInvoiceStatus = 0,
                        }).OrderBy(item => item.CustomerId).ThenBy(item => item.CustomerId).ToList();

                // thực hiện vòng lặp, ghép khách hàng nợ tồn với phát sinh lại cạnh nhau 


                allList.AddRange(listLiabilitiesTaxInvoice);
                // thực hiện vòng  lặp để lấy ra số tiền thừa của khách hàng
                for (var m = 0; m < allList.Count; m++)
                {
                    TongKH = TongKH + 1;
                    TongTien = TongTien + Convert.ToDecimal(allList[m].Debt) + Convert.ToDecimal(allList[m].TaxDebt);
                    int _departmentId = Convert.ToInt32(allList[m].DepartmentId);
                    int customerId = Convert.ToInt32(allList[m].CustomerId);
                    decimal excessMoney = ValueExcessMoneyTaxInvoice(customerId, _departmentId, db);
                    allList[m].ExcessMoney = excessMoney;
                    if ((allList[m].Debt != 0 || allList[m].TaxDebt != 0) && allList[m].Status == 1)
                    {
                        allList[m].TaxInvoiceStatus = 0;
                    }

                }
                //@ViewBag.tongTien = TongTien;
                //@ViewBag.TongKH = TongKH;

            }
        }

        protected decimal ValueExcessMoneyTaxInvoice(int CustomerId, int DepartmentId, CCISContext db)
        {
            var value =
                db.Liabilities_ExcessMoney_TaxInvoice.Where(
                    item => item.CustomerId.Equals(CustomerId) && item.DepartmentId.Equals(DepartmentId))
                    .Select(item => item.ExcessMoney)
                    .FirstOrDefault();
            return value;
        }

        // lấy người dùng còn nợ non tải
        protected void CustomerLiabilities_TrackDebt_TaxInvoice(int TongKH, decimal TongTien, List<Liabilities_TrackDebt_TaxInvoiceModel> allList, string Name, string CustomerCode)
        {
            using (var db = new CCISContext())
            {
                // khai báo  danh sách chứa cả nợ tồn + pát sinh trong tháng.

                // lấy ra danh sách row có trạng thái = 0
                var listLiabilities =
                    db.Liabilities_TrackDebt_TaxInvoice.Where(item => ((item.Name.Contains(Name.Trim()) || item.CustomerCode.Contains(CustomerCode.Trim())) && item.Debt > 0) && (item.Status != 2 && item.Status != 3))
                        .Select(item => new Liabilities_TrackDebt_TaxInvoiceModel
                        {
                            Liabilities_TaxInvoiceId = item.Liabilities_TaxInvoiceId,
                            TaxInvoiceId = item.TaxInvoiceId,
                            BillType = item.BillType,
                            DepartmentId = item.DepartmentId,
                            CustomerId = item.CustomerId,
                            CustomerCode = item.CustomerCode,
                            Name = item.Name,
                            Address = item.Address,
                            InvoiceAddress = item.InvoiceAddress,
                            Month = item.Month,
                            Year = item.Year,
                            FundsGenerated = item.FundsGenerated,
                            TaxesIncurred = item.TaxesIncurred,
                            Debt = item.Debt,
                            TaxDebt = item.TaxDebt,
                            PaymentMethodsCode = item.PaymentMethodsCode,
                            FigureBookId = item.FigureBookId,
                            StatusDebt = item.StatusDebt,
                            StatusCorrection = item.StatusCorrection,
                            CountOfDelivery = item.CountOfDelivery,
                            ReleaseDateBill = item.ReleaseDateBill,
                            CreateDate = item.CreateDate,
                            CreateUser = item.CreateUser,
                            EditDate = item.EditDate,
                            Status = item.Status,
                            TermMonthYear = "Tháng " + item.Month + "-" + item.Year,
                            TaxInvoiceStatus = 1,
                        }).OrderBy(item => item.CustomerId).ThenBy(item => item.CustomerId).ToList();

                // lay ra danh sách khách hàng còn nợ (check : Debt != 0  hoặc TaxDebt != 0)
                var listTrackDebt =
                    db.Liabilities_TrackDebt_TaxInvoice.Where(item => (item.Status != 0 && item.Status != 2 && item.Status != 3) && (item.Debt != 0 || item.TaxDebt != 0) && (item.Name.Contains(Name.Trim()) || item.CustomerCode.Contains(CustomerCode.Trim())))
                        .Select(item => new Liabilities_TrackDebt_TaxInvoiceModel
                        {
                            Liabilities_TaxInvoiceId = item.Liabilities_TaxInvoiceId,
                            TaxInvoiceId = item.TaxInvoiceId,
                            BillType = item.BillType,
                            DepartmentId = item.DepartmentId,
                            CustomerId = item.CustomerId,
                            CustomerCode = item.CustomerCode,
                            Name = item.Name,
                            Address = item.Address,
                            InvoiceAddress = item.InvoiceAddress,
                            Month = item.Month,
                            Year = item.Year,
                            FundsGenerated = item.FundsGenerated,
                            TaxesIncurred = item.TaxesIncurred,
                            Debt = item.Debt,
                            TaxDebt = item.TaxDebt,
                            PaymentMethodsCode = item.PaymentMethodsCode,
                            FigureBookId = item.FigureBookId,
                            StatusDebt = item.StatusDebt,
                            StatusCorrection = item.StatusCorrection,
                            CountOfDelivery = item.CountOfDelivery,
                            ReleaseDateBill = item.ReleaseDateBill,
                            CreateDate = item.CreateDate,
                            CreateUser = item.CreateUser,
                            EditDate = item.EditDate,
                            Status = item.Status,
                            TermMonthYear = "Tháng " + item.Month + "-" + item.Year,
                            TaxInvoiceStatus = 0,
                        }).OrderBy(item => item.CustomerId).ThenBy(item => item.CustomerId).ToList();

                // thực hiện vòng lặp, ghép khách hàng nợ tồn với phát sinh lại cạnh nhau 


                // sau khi kết thúc vòng lặp, nếu list danh sách nợ còn thì phải cho hết vào danh sách tổng
                allList.AddRange(listLiabilities);
                // thực hiện vòng  lặp để lấy ra số tiền thừa của khách hàng
                for (var m = 0; m < allList.Count; m++)
                {
                    TongKH = TongKH + 1;
                    TongTien = TongTien + Convert.ToDecimal(allList[m].Debt) + Convert.ToDecimal(allList[m].TaxDebt);
                    int departmentId = Convert.ToInt32(allList[m].DepartmentId);
                    int customerId = Convert.ToInt32(allList[m].CustomerId);
                    decimal excessMoney = ValueExcessMoneyTaxInvoice(customerId, departmentId, db);
                    allList[m].ExcessMoney = excessMoney;
                    if ((allList[m].Debt != 0 || allList[m].TaxDebt != 0) && allList[m].Status == 1)
                    {
                        allList[m].TaxInvoiceStatus = 0;
                    }

                }
                //@ViewBag.tongTien = TongTien;
                //@ViewBag.TongKH = TongKH;

            }
        }

        // hàm truyền vào người dùng id, lấy ra tiền thừa khách hàng
        protected decimal GetLiabilities_ExcessMoney_TaxInvoice(int customerId)
        {
            using (var db = new CCISContext())
            {
                var excessMoney =
                    db.Liabilities_ExcessMoney_TaxInvoice.Where(item => item.CustomerId.Equals(customerId))
                        .Select(item => item.ExcessMoney)
                        .FirstOrDefault();
                return excessMoney;
            }
        }

        // insert Log bảng theo dõi công nợ Liabilities_TrackDebt_Log
        protected void Insert_LiabilitiesTrackDebt_TaxInvoiceLog(int Liabilities_TaxInvoiceId, CCISContext db, int CreateUser, DateTime paymentDate, decimal payment)
        {
            Business_Liabilities_TrackDebt_TaxInvoice_Log trackDebtLog = new Business_Liabilities_TrackDebt_TaxInvoice_Log();
            var TrackDebt =
                db.Liabilities_TrackDebt_TaxInvoice.Where(item => item.Liabilities_TaxInvoiceId.Equals(Liabilities_TaxInvoiceId)).FirstOrDefault();
            Liabilities_TrackDebt_TaxInvoice_LogModel model = new Liabilities_TrackDebt_TaxInvoice_LogModel();
            model.Liabilities_TaxInvoiceId = TrackDebt.Liabilities_TaxInvoiceId;
            model.TaxInvoiceId = TrackDebt.TaxInvoiceId;
            model.DepartmentId = TrackDebt.DepartmentId;
            model.CustomerId = TrackDebt.CustomerId;
            model.FundsGenerated = TrackDebt.Debt;
            model.TaxesIncurred = TrackDebt.TaxDebt;
            model.CreateDate = DateTime.Now;
            model.CreateUser = CreateUser;
            model.DateDot = paymentDate;
            model.CustomerCode = TrackDebt.CustomerCode;
            model.Month = paymentDate.Month;
            model.Year = paymentDate.Year;
            model.Payment = payment;
            trackDebtLog.Insert_Liabilities_TrackDebt_TaxInvoice_Log(model, db);
        }

        protected void Insert_LiabilitiesExcessMoney_TaxInvoiceLog(int CustomerId, CCISContext db, int CreateUser)
        {            
            var ExcessMoney =
                db.Liabilities_ExcessMoney_TaxInvoice.Where(item => item.CustomerId.Equals(CustomerId)).FirstOrDefault();
            Liabilities_ExcessMoney_Log model = new Liabilities_ExcessMoney_Log();
            model.ExcessMoney_Id = ExcessMoney.ExcessMoneyTaxInvoice_Id;
            model.DepartmentId = ExcessMoney.DepartmentId;
            model.CustomerId = ExcessMoney.CustomerId;
            model.CustomerCode = ExcessMoney.CustomerCode;
            model.CreateDate = DateTime.Now;
            model.CreateUser = ExcessMoney.CreateUser;
            model.EditDate = ExcessMoney.EditDate;
            model.EditUser = CreateUser;
            model.ExcessMoney = ExcessMoney.ExcessMoney;
            ExcessMoney_TaxInvoiceLog.Insert_Liabilities_ExcessMoneyTaxInvoice_Log(model, db);
        }
        #endregion

        #region Hủy bỏ xóa nợ tiền non tải
        protected void AllTaxInvoiceCancellation(int TongKH, decimal TongTien, List<Liabilities_TrackDebt_TaxInvoiceModel> allList, CCISContext db, string name, DateTime? saveDate, int FigureBookId)
        {
            // khai báo  danh sách chứa cả nợ tồn + pát sinh trong tháng.
            int month = saveDate.Value.Month;
            int year = saveDate.Value.Year;

            int idGcs = Convert.ToInt32(FigureBookId);
            // lay ra danh sách khách hàng het nợ (check : Debt == 0  và TaxDebt == 0 và status = 1)
            var listTaxInvoice =
                db.Liabilities_TrackDebt_TaxInvoice.Where(
                        item => item.Status == 1 && (item.FundsGenerated != item.Debt || item.TaxesIncurred != item.TaxDebt) && item.Month == month &&
                                (idGcs == 0
                                        ? (name == ""
                                            ? item.Year == year
                                            : item.Year == year && (item.Name.Contains(name) || item.CustomerCode.Contains(name)))
                                        : ((name == ""
                                            ? item.Year == year && item.FigureBookId == idGcs
                                            : item.Year == year && item.FigureBookId == idGcs &&
                                              (item.Name.Contains(name) || item.CustomerCode.Contains(name))))))

                    .Select(item => new Liabilities_TrackDebt_TaxInvoiceModel
                    {
                        Liabilities_TaxInvoiceId = item.Liabilities_TaxInvoiceId,
                        TaxInvoiceId = item.TaxInvoiceId,
                        BillType = item.BillType,
                        DepartmentId = item.DepartmentId,
                        CustomerId = item.CustomerId,
                        CustomerCode = item.CustomerCode,
                        Name = item.Name,
                        Address = item.Address,
                        InvoiceAddress = item.InvoiceAddress,
                        Month = item.Month,
                        Year = item.Year,
                        FundsGenerated = item.FundsGenerated,
                        TaxesIncurred = item.TaxesIncurred,
                        Debt = item.Debt,
                        TaxDebt = item.TaxDebt,
                        PaymentMethodsCode = item.PaymentMethodsCode,
                        FigureBookId = item.FigureBookId,
                        StatusDebt = item.StatusDebt,
                        StatusCorrection = item.StatusCorrection,
                        CountOfDelivery = item.CountOfDelivery,
                        ReleaseDateBill = item.ReleaseDateBill,
                        CreateDate = item.CreateDate,
                        CreateUser = item.CreateUser,
                        EditDate = item.EditDate,
                        Status = item.Status,
                        TermMonthYear = "/Tháng " + item.Month + "-" + item.Year,
                    }).OrderBy(item => item.CustomerId).ToList();


            listTaxInvoice = listTaxInvoice.OrderBy(item => item.CustomerId).ToList();
            allList.AddRange(listTaxInvoice);
            // sau khi kết thúc vòng lặp, nếu list danh sách nợ còn thì phải cho hết vào danh sách tổng
            for (var m = 0; m < allList.Count; m++)
            {
                TongKH = TongKH + 1;
                TongTien = TongTien + Convert.ToDecimal(allList[m].FundsGenerated) + Convert.ToDecimal(allList[m].TaxesIncurred);
            }
            // xóa những hóa đơn là hủy bỏ đi, không cho hiển thị trên công nợ
            //@ViewBag.tongTien = TongTien;
            //@ViewBag.TongKH = TongKH;
        }
        #endregion
        #endregion
    }
}
