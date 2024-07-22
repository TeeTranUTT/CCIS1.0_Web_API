using CCIS_BusinessLogic;
using CCIS_BusinessLogic.DTO.Sms;
using CCIS_DataAccess;
using CCIS_DataAccess.ViewModels;
using ES.CCIS.Host.Helpers;
using ES.CCIS.Host.Models;
using ES.CCIS.Host.Models.EnumMethods;
using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web.Configuration;
using System.Web.Http;
using static CCIS_BusinessLogic.DefaultBusinessValue;

namespace ES.CCIS.Host.Controllers
{
    [Authorize]
    [RoutePrefix("api/SmsManager")]
    public class SmsManagerController : ApiBaseController
    {
        private readonly Business_Sms_Manager businessSms = new Business_Sms_Manager();
        private readonly Business_Sms _businessSms;
        private readonly CCISContext _ccisContext;
        private readonly Business_Administrator_Parameter paramsmeter = new Business_Administrator_Parameter();
        const int SMS_TYPE_ThongBaoTienDienNuoc = 1; // là hình thức gửi tin nhắn thông báo tiền điện
        const int SMS_TYPE_NhacNo = 2; // là hình thức gửi tin nhắn nhắc nợ tiền điện
        const int TYPE_SEND_By_Customer = 1; // là hình thức gửi gin nhắn theo khách hàng
        const int TYPE_SEND_By_Book = 2; // là hình thức gửi gin nhắn theo sổ;
        private int UserId;

        public SmsManagerController()
        {
            _ccisContext = new CCISContext();
            _businessSms = new Business_Sms(_ccisContext);
            //LstBusinessLogics.Add(_businessSms);
        }

        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private int PageSize_SMS = 25;

        private void setUserId()
        {
            UserId = TokenHelper.GetUserIdFromToken();
            businessSms.UserCreated = UserId;
        }

        [HttpGet]
        [Route("SmsToBook")]
        public HttpResponseMessage SmsToBook([DefaultValue(0)] int departmentId, [DefaultValue(0)] int Term, DateTime? saveDate)
        {
            try
            {
                List<Sms_ShowBook> list1 = new List<Sms_ShowBook>();
                using (var db = new CCISContext())
                {
                    List<Administrator_DepartmentViewModels> lstdepartments = new List<Administrator_DepartmentViewModels>();
                    Business_Administrator_Department businessDepartment = new Business_Administrator_Department();
                    //Lấy phòng ban hiện tại và tất cả phòng ban cấp dưới DỰA VÀO departmentId
                    var listDepartmentUserAll = DepartmentHelper.GetChildDepIds(departmentId);

                    var getSmsTemplate = businessSms.GetSms_TemplateId(departmentId, 1);

                    var email_Time_Sleep = paramsmeter.GetParameterValue(Administrator_Parameter_Common.Email_Time_Sleep, null, departmentId);
                    //<<TruongVM Testing lấy tất cả sổ theo kỳ
                    List<int> lstTerm = new List<int> { };
                    switch (Term)
                    {
                        case 0:
                        case 1:
                            lstTerm = new List<int> { 1, 2, 3 };
                            break;
                        case 2:
                            lstTerm = new List<int> { 2, 3 };
                            break;
                        case 3:
                            lstTerm = new List<int> { 3 };
                            break;
                    }
                    //Lấy danh sách sổ GCS và trạng thái của sổ

                    var lstIndexCalendar = db.Index_CalendarOfSaveIndex.Where(item => item.DepartmentId == departmentId && item.Term == Term && item.Month == saveDate.Value.Month && item.Year == saveDate.Value.Year).Select(
                        item => new
                        {
                            item.FigureBookId
                        }).ToList();

                    var lstBill = (from bill in db.Bill_ElectricityBill
                                   join cus in db.Concus_Customer
                                   on bill.CustomerId equals cus.CustomerId
                                   where bill.DepartmentId == departmentId && bill.Term == Term && bill.Month == saveDate.Value.Month && bill.Year == saveDate.Value.Year
                                   select new
                                   {
                                       bill.FigureBookId,
                                       customer = new
                                       {
                                           PhoneCustomerCare = cus.PhoneCustomerCare,
                                           ZaloCustomerCare = cus.ZaloCustomerCare,
                                           Email = cus.Email,
                                       }
                                   }
                                   ).ToList();

                    var leftOuterJoin = (from calendar in lstIndexCalendar
                                         join bill in lstBill
                                         on calendar.FigureBookId equals bill.FigureBookId
                                         group bill by calendar.FigureBookId into grouped
                                         select new
                                         {
                                             FigureBookId = grouped.Key,
                                             CustomersPhone = grouped.Count(o => o.customer.PhoneCustomerCare != null),
                                             CustomersTotal = grouped.Count(),
                                             CustomersZalo = grouped.Count(o => !string.IsNullOrEmpty(o.customer.ZaloCustomerCare)),
                                             CustomersEmail = grouped.Count(o => !string.IsNullOrEmpty(o.customer.Email)),
                                         }
                                         ).ToList();

                    var listCalender = db.Index_CalendarOfSaveIndex.Where(item => item.DepartmentId == departmentId && item.Term == Term && item.Month == saveDate.Value.Month && item.Year == saveDate.Value.Year).ToList();
                    var lstTrackDebt = db.Liabilities_TrackDebt.Where(o => o.DepartmentId == departmentId && o.Term == Term && o.Month == saveDate.Value.Month && o.Year == saveDate.Value.Year).Select(o2 => new { o2.CustomerId, o2.FigureBookId }).ToList();
                    var lstBook = db.Category_FigureBook.Where(item => item.DepartmentId == departmentId).Select(item => new { item.FigureBookId, item.BookName, item.BookCode }).ToList();
                    var SmsTypeId = db.Sms_Service_Type.FirstOrDefault(item => item.SmsTypeCode == SMS_TYPECODE.TinNhanThongBaoTienDien)?.SmsTypeId;
                    var lstSMSHistory = db.Sms_Track_Customer.Where(item => item.BillId > 0 && item.Status == 1 && item.SmsTypeId == SmsTypeId && item.Term == Term && item.Month == saveDate.Value.Month && item.Year == saveDate.Value.Year).Select(item => new
                    {
                        item.AppSend,
                        item.CustomerId,
                        item.FigureBookId
                    }).ToList();
                    List<Sms_ShowBook> ds = new List<Sms_ShowBook>();
                    foreach (var v in leftOuterJoin)
                    {
                        try
                        {
                            Sms_ShowBook s = new Sms_ShowBook();
                            s.FigureBookId = v.FigureBookId;
                            s.CustomersNoPay = lstTrackDebt.Where(o => o.FigureBookId == v.FigureBookId).Count();
                            s.Status = listCalender.Where(o => o.FigureBookId == v.FigureBookId).Select(o => o.Status).FirstOrDefault();
                            s.IsChecked = (s.Status == 11 ? false : true);
                            s.BookCode = lstBook.Where(o => o.FigureBookId == v.FigureBookId).Select(o2 => o2.BookCode).FirstOrDefault();
                            s.BookName = lstBook.Where(o => o.FigureBookId == v.FigureBookId).Select(o2 => o2.BookName).FirstOrDefault();
                            s.CustomersPhone = v.CustomersPhone;
                            s.CustomersTotal = v.CustomersTotal;
                            s.CustomersSent = lstSMSHistory.Where(o => o.AppSend == APP_SEND.SMS && o.FigureBookId == v.FigureBookId).Select(o2 => o2.CustomerId).Count();
                            s.CustomersZaloSent = lstSMSHistory.Where(o => o.AppSend == APP_SEND.ZALO && o.FigureBookId == v.FigureBookId).Select(o2 => o2.CustomerId).Count();
                            s.CustomersEmailSent = lstSMSHistory.Where(o => o.AppSend == APP_SEND.EMAIL && o.FigureBookId == v.FigureBookId).Select(o2 => o2.CustomerId).Count();
                            s.CustomersZalo = v.CustomersZalo;
                            s.CustomersEmails = v.CustomersEmail;
                            s.CustomersTotalSent = s.CustomersSent + s.CustomersZaloSent + s.CustomersEmailSent;

                            ds.Add(s);
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    list1 = ds; //.ToList().OrderBy(o => o.BookCode);
                    #region                    
                    #endregion

                    var response = new
                    {
                        GetSmsTemplate = getSmsTemplate,
                        Email_Time_Sleep = email_Time_Sleep,
                        ListSms_ShowBook = list1
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

        /// <summary>
        /// thông báo cắt điện/nước theo kế hoạch có một khoảng thời gian
        /// thông báo cắt điện/nước theo kế hoạch có nhiều khoảng thời gian
        /// thông báo mất điện/nước do sự cố
        /// thay đổi điều kiện bán điện/nước
        /// </summary>
        /// <param name="stationId"></param>
        /// <param name="templateId"></param>
        /// <returns></returns>
        /// 
         //Todo: chưa viết api SmsToBook_Alert vì có param HttpPostedFileBase

        //Các hàm thực hiện gửi
        [HttpPost]
        [Route("SmsToBook_Alert_2")]
        public HttpResponseMessage SmsToBook_Alert_2(string[] listData, [DefaultValue(0)] int stationId, [DefaultValue(0)] int templateId, bool isSendAgain, [DefaultValue(0)] int? Email_Time_Sleep)
        {
            try
            {
                string strKQ = "";
                using (var db = new CCISContext())
                {
                    var smsTemplate = db.Sms_Template.FirstOrDefault(o => o.SmsTemplateId == templateId);

                    // cập nhật giá trị thời gian gửi email
                    if (Email_Time_Sleep != 0)
                    {
                        paramsmeter.SetParameterValue(Administrator_Parameter_Common.Email_Time_Sleep, Email_Time_Sleep.ToString(), smsTemplate.DepartmentId);
                    }

                    var tramInfor = db.Category_Satiton.Where(o => o.StationId == stationId).FirstOrDefault();
                    List<Sms_Data_SendModel> listKH = new List<Sms_Data_SendModel>();
                    var today = DateTime.Now.Date;

                    var listDaGui = new List<int>();
                    if (!isSendAgain)
                    {
                        listDaGui = db.Sms_Track_Customer.Where(track =>
                        track.SmsTypeId == smsTemplate.SmsTypeId
                        && track.Status == 1
                        && track.StationId == stationId
                        //&& DbFunctions.TruncateTime(track.CreateDate) == today //Todo cần xem lại không biết tại sao lại lỗi trong khi bảng Sms_Track_Customer đã kế thừa EntityBase
                        )
                        .Select(track => track.CustomerId).ToList();
                    }


                    for (int i = 0; i < listData.Length; i++)
                    {
                        string lstItem = listData[i].ToString();
                        string[] split = lstItem.Split(',');
                        string phoneNumber = split[0];
                        string customerCode = split[1];
                        int figureBookId = Convert.ToInt32(split[2]);
                        int customerId = Convert.ToInt32(split[3]);
                        string ZaloNumber = split[4];
                        if (!listDaGui.Contains(customerId)) // Loại bỏ những khách hàng đã gửi thành công trc đó
                        {
                            Sms_Data_SendModel modelCustomer = new Sms_Data_SendModel();
                            modelCustomer.BillId = 0;
                            //các trường bắt buộc có
                            modelCustomer.makh = customerCode;
                            modelCustomer.CustomerId = customerId;
                            modelCustomer.PhoneNumber = phoneNumber;
                            modelCustomer.ZaloNumber = ZaloNumber;
                            modelCustomer.FigureBookId = figureBookId;
                            modelCustomer.thang = DateTime.Now.Month;
                            modelCustomer.nam = DateTime.Now.Year;
                            modelCustomer.ky = 0; // hdon.Term,  
                            modelCustomer.matram = tramInfor.StationCode;
                            modelCustomer.tentram = tramInfor.StationName;
                            modelCustomer.stationid = tramInfor.StationId;
                            modelCustomer.TypeSend = TYPE_SEND_By_Customer;
                            listKH.Add(modelCustomer);
                        }
                    }
                    if (listKH.Count == 0)
                    {
                        throw new ArgumentException("Danh sách gửi không có KH nào hoặc các KH này đã gửi thành công trước đó gửi lòng kiểm tra lịch sử gửi tin nhắn.");
                    }
                    strKQ = businessSms.GuiThongBaoChung(listKH, templateId, db);

                    if (strKQ != "OK")
                    {
                        throw new ArgumentException($"Gửi tin nhắn có lỗi: {strKQ}");
                    }
                    else
                    {
                        respone.Status = 1;
                        respone.Message = "OK";
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
        [Route("SmsToBook_Post")]
        public HttpResponseMessage SmsToBook_Post(SmsToBook_PostInput input)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    setUserId();
                    // cập nhật giá trị thời gian gửi email
                    if (input.Email_Time_Sleep != 0)
                    {
                        paramsmeter.SetParameterValue(Administrator_Parameter_Common.Email_Time_Sleep, input.Email_Time_Sleep.ToString(), input.DepartmentId);
                    }

                    Sms_Track_FindModel find = new Sms_Track_FindModel();
                    find.departmentid = input.DepartmentId;
                    find.term = input.Term;
                    find.month = input.SaveDate.Value.Month;
                    find.year = input.SaveDate.Value.Year;
                    find.SmsTemplateId = input.TemplateId;
                    find.ListCustomerId = new List<int>();
                    // Lấy danh sách từng sổ 1
                    StringBuilder message = new StringBuilder();
                    for (int i = 0; i < input.ListFigureBookId.Length; i++)
                    {
                        find.FigureBookId = input.ListFigureBookId[i];
                        string strKQ = businessSms.GuiThongBaoTienDienVaNuoc(find, db, input.IsSendAgain);
                        var sogcs = db.Category_FigureBook.FirstOrDefault(item => item.FigureBookId == find.FigureBookId);
                        message.Append($"{sogcs.BookCode} - {sogcs.BookName}: ");
                        message.Append(strKQ);
                        message.Append("\n");
                    }

                    respone.Status = 1;
                    respone.Message = "OK";
                    respone.Data = message.ToString();
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

        //[HttpGet]
        //[Route("SmsToCustomer")]
        //public HttpResponseMessage SmsToCustomer([DefaultValue(1)] int pageNumber, [DefaultValue(0)] int term, [DefaultValue(0)] int departmentId, [DefaultValue(0)] int figureBookId, DateTime? month, string typeOfGet)
        //{
        //    try
        //    {
        //        if (month == null)
        //        {
        //            month = DateTime.Now;
        //        }

        //        if (departmentId == 0)
        //            departmentId = TokenHelper.GetDepartmentIdFromToken();

        //        var userInfo = TokenHelper.GetUserInfoFromRequest();
        //        //Lấy phòng ban hiện tại và tất cả phòng ban cấp dưới DỰA VÀO departmentId
        //        var listDepartmentUserAll = DepartmentHelper.GetChildDepIds(departmentId);
        //        //Lấy phòng ban hiện tại và tất cả phòng ban cấp dưới DỰA VÀO tài khoản đang đăng nhập
        //        var listDepartmentUser = DepartmentHelper.GetChildDepIdsByUser(userInfo.UserName);

        //        using (var db = new CCISContext())
        //        {
        //            //List billId theo kỳ
        //            var listBillId = db.Liabilities_TrackDebt.Where(item => listDepartmentUserAll.Contains(item.DepartmentId)
        //               && (figureBookId == 0 || item.FigureBookId == figureBookId)
        //               && item.Year == month.Value.Year && item.Month == month.Value.Month
        //               && item.Term == term &&
        //                  (
        //                        item.Status == 0 ||
        //                        item.Status == 4 ||
        //                        item.Status == 5
        //                    )
        //               )
        //               .Select(item => item.BillId).ToList();

        //            var lstBill = db.Bill_ElectricityBill.Where(i => listBillId.Contains(i.BillId)).Select(i => new Bill_ElectricityBillModel
        //            {
        //                BillId = i.BillId,
        //                CustomerCode = i.CustomerCode,
        //                CustomerName = i.CustomerName,
        //                BillType = i.BillType,
        //                Total = i.Total,
        //                DepartmentId = i.DepartmentId,
        //                CustomerId = i.CustomerId,
        //                FigureBookId = i.FigureBookId,
        //                ElectricityIndex = i.ElectricityIndex
        //            }).ToList();
        //            lstBill.AddRange(db.Bill_ElectricityBillAdjustment.Where(i => listBillId.Contains(i.BillId)).Select(i => new Bill_ElectricityBillModel
        //            {
        //                BillId = i.BillId,
        //                CustomerCode = i.CustomerCode,
        //                CustomerName = i.CustomerName,
        //                BillType = i.BillType,
        //                Total = i.Total,
        //                DepartmentId = i.DepartmentId,
        //                CustomerId = i.CustomerId,
        //                FigureBookId = i.FigureBookId,
        //                ElectricityIndex = i.ElectricityIndex
        //            }).ToList());

        //            var lstCustomer = lstBill.Select(x => x.CustomerId).Distinct().ToList();

        //            var lst1 = (from ccon in db.Concus_Customer
        //                        join cc in db.Concus_Contract
        //                        on ccon.CustomerId equals cc.CustomerId
        //                        join cs in db.Concus_ServicePoint
        //                        on cc.ContractId equals cs.ContractId
        //                        join ct in db.Category_Satiton
        //                        on cs.StationId equals ct.StationId
        //                        where lstCustomer.Contains(ccon.CustomerId) && cs.Status
        //                        select new
        //                        {
        //                            ccon.CustomerId,
        //                            ccon.PhoneCustomerCare,
        //                            ccon.ZaloCustomerCare,
        //                            cs.Address,
        //                            ct.StationCode
        //                        }).ToList();
        //            var lst = (from lstBillQ in lstBill
        //                       join lstInfoQ in lst1
        //                       on lstBillQ.CustomerId equals lstInfoQ.CustomerId
        //                       group new { lstBillQ, lstInfoQ } by new
        //                       {
        //                           lstBillQ.BillId,
        //                           lstBillQ.CustomerCode,
        //                           lstBillQ.CustomerName,
        //                           lstBillQ.BillType,
        //                           lstBillQ.Total,
        //                           lstBillQ.DepartmentId,
        //                           lstBillQ.CustomerId,
        //                           lstInfoQ.PhoneCustomerCare,
        //                           lstInfoQ.ZaloCustomerCare,
        //                           lstBillQ.FigureBookId,
        //                           lstBillQ.ElectricityIndex
        //                       } into grbill
        //                       select new
        //                       {
        //                           AddressPoint = grbill.FirstOrDefault().lstInfoQ.Address,
        //                           StationCode = grbill.Select(x => x.lstInfoQ.StationCode).ToList(),
        //                           BillId = grbill.Key.BillId,
        //                           CustomerCode = grbill.Key.CustomerCode,
        //                           CustomerName = grbill.Key.CustomerName,
        //                           BillType = grbill.Key.BillType,
        //                           Total = grbill.Key.Total,
        //                           DepartmentId = grbill.Key.DepartmentId,
        //                           CustomerId = grbill.Key.CustomerId,
        //                           PhoneNumber = grbill.Key.PhoneCustomerCare,
        //                           ZaloNumber = grbill.Key.ZaloCustomerCare,
        //                           FigureBookId = grbill.Key.FigureBookId,
        //                           ElectricityIndex = grbill.Key.ElectricityIndex
        //                       }).AsEnumerable().Select(grbill => new
        //                       {

        //                           AddressPoint = grbill.AddressPoint,
        //                           StationCode = string.Join(";", grbill.StationCode.ToArray()),
        //                           BillId = grbill.BillId,
        //                           CustomerCode = grbill.CustomerCode,
        //                           CustomerName = grbill.CustomerName,
        //                           BillType = grbill.BillType,
        //                           Total = grbill.Total,
        //                           DepartmentId = grbill.DepartmentId,
        //                           CustomerId = grbill.CustomerId,
        //                           PhoneNumber = grbill.PhoneNumber,
        //                           ZaloNumber = grbill.ZaloNumber,
        //                           FigureBookId = grbill.FigureBookId,
        //                           ElectricityIndex = grbill.ElectricityIndex
        //                       }).ToList();

        //            List<int> smsTrackQuery = null;
        //            switch (typeOfGet)
        //            {
        //                case "Sended":
        //                    smsTrackQuery = db.Sms_Track_Customer.Where(i => listBillId.Contains(i.BillId) && i.SmsTypeId == SMS_TYPE_ThongBaoTienDienNuoc && i.Status == 1
        //                    && i.Month == month.Value.Month && i.Year == month.Value.Year).Select(i => i.CustomerId).ToList();
        //                    lst = lst.Where(i => smsTrackQuery.Contains(i.CustomerId)).ToList();
        //                    break;
        //                case "Unsend":
        //                    smsTrackQuery = db.Sms_Track_Customer.Where(i => listBillId.Contains(i.BillId) && i.SmsTypeId == SMS_TYPE_ThongBaoTienDienNuoc && i.Status == 1
        //                    && i.Month == month.Value.Month && i.Year == month.Value.Year).Select(i => i.CustomerId).ToList();
        //                    lst = lst.Where(i => !smsTrackQuery.Contains(i.CustomerId)).ToList();
        //                    break;
        //                case "Error":
        //                    var template = db.Sms_Template.Select(i => new { i.SmsTemplateId, i.AppSend }).ToList();

        //                    var smsHistory = (from h in db.Sms_Track_Customer
        //                                      join t in db.Sms_Template
        //                                      on h.SmsTemplateId equals t.SmsTemplateId
        //                                      where listBillId.Contains(h.BillId) && h.SmsTypeId == SMS_TYPE_ThongBaoTienDienNuoc && h.Month == month.Value.Month && h.Year == month.Value.Year && h.Status != 1
        //                                      select new { h.BillId, h.Status, h.MessageService, h.CreateDate, t.AppSend }//Todo: Có bỏ h.CreateDate vì gây ra lỗi
        //                                      ).ToList();

        //                    var groupData = smsHistory.GroupBy(o => o.BillId).Select(o => new { BillId = o.Key, history = o.OrderByDescending(o1 => o1.CreateDate).ToList() }).ToList();


        //                    var data = (from a in lst
        //                                join b in groupData
        //                                on a.BillId equals b.BillId
        //                                select new Bill_ElectricityBillModel
        //                                {
        //                                    AddressPoint = a.AddressPoint,
        //                                    StationCode = a.StationCode,
        //                                    BillId = a.BillId,
        //                                    CustomerCode = a.CustomerCode,
        //                                    CustomerName = a.CustomerName,
        //                                    BillType = a.BillType,
        //                                    Total = a.Total,
        //                                    DepartmentId = a.DepartmentId,
        //                                    CustomerId = a.CustomerId,
        //                                    PhoneNumber = a.PhoneNumber,
        //                                    ZaloNumber = a.ZaloNumber,
        //                                    FigureBookId = a.FigureBookId,
        //                                    ElectricityIndex = a.ElectricityIndex,
        //                                    Sms_History = b.history.Select(d => new Bill_Sms_History
        //                                    {
        //                                        AppSend = d.AppSend,
        //                                        Message = d.MessageService,
        //                                        Created = d.CreateDate
        //                                    }).OrderByDescending(h => h.Created).ToList()
        //                                });

        //                    var paged = (IPagedList<Bill_ElectricityBillModel>)data.OrderBy(p => p.CustomerCode).ToPagedList(pageNumber, pageSize);


        //                    var response = new
        //                    {
        //                        paged.PageNumber,
        //                        paged.PageSize,
        //                        paged.TotalItemCount,
        //                        paged.PageCount,
        //                        paged.HasNextPage,
        //                        paged.HasPreviousPage,
        //                        ElectricityBills = paged.ToList()
        //                    };
        //                    respone.Status = 1;
        //                    respone.Message = "OK";
        //                    respone.Data = response;
        //                    return createResponse();

        //            }
        //            // Chỉ hiển thị những khách hàng chưa gửi được tin nhắn thành công
        //            var PageSize_SMS = lst.Count();
        //            if (PageSize_SMS > 0)
        //            {
        //                var lst2 = lst.Select(a => new Bill_ElectricityBillModel
        //                {
        //                    AddressPoint = a.AddressPoint,
        //                    StationCode = a.StationCode,
        //                    BillId = a.BillId,
        //                    CustomerCode = a.CustomerCode,
        //                    CustomerName = a.CustomerName,
        //                    BillType = a.BillType,
        //                    Total = a.Total,
        //                    DepartmentId = a.DepartmentId,
        //                    CustomerId = a.CustomerId,
        //                    PhoneNumber = a.PhoneNumber,
        //                    ZaloNumber = a.ZaloNumber,
        //                    FigureBookId = a.FigureBookId,
        //                    ElectricityIndex = a.ElectricityIndex
        //                }).OrderBy(item => item.CustomerCode);

        //                var paged = (IPagedList<Bill_ElectricityBillModel>)lst2.OrderBy(p => p.CustomerCode).ToPagedList(pageNumber, pageSize);

        //                var response = new
        //                {
        //                    paged.PageNumber,
        //                    paged.PageSize,
        //                    paged.TotalItemCount,
        //                    paged.PageCount,
        //                    paged.HasNextPage,
        //                    paged.HasPreviousPage,
        //                    ElectricityBills = paged.ToList()
        //                };
        //                respone.Status = 1;
        //                respone.Message = "OK";
        //                respone.Data = response;
        //                return createResponse();
        //            }
        //            else
        //            {
        //                throw new ArgumentException("Không có dữ liệu.");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        respone.Status = 0;
        //        respone.Message = $"Lỗi: {ex.Message.ToString()}";
        //        respone.Data = null;
        //        return createResponse();
        //    }
        //}

        [HttpPost]
        [Route("SmsToCustomer_Post")]
        public HttpResponseMessage SmsToCustomer_Post(SmsToCustomer_PostInput input)
        {
            try
            {
                string strKQ = "";
                using (var db = new CCISContext())
                {
                    setUserId();
                    List<Sms_Data_SendModel> listKH = new List<Sms_Data_SendModel>();
                    for (int i = 0; i < input.ListCustomerId.Length; i++)
                    {
                        string lstItem = input.ListCustomerId[i].ToString();
                        string[] split = lstItem.Split(',');
                        int customerId = Convert.ToInt32(split[0]);
                        int billID = Convert.ToInt32(split[1]);
                        string phoneNumber = split[2];
                        string customerCode = split[3];
                        int figureBookId = Convert.ToInt32(split[4]);
                        decimal ToTal_TD = Convert.ToDecimal(split[5]);
                        int dientthu = Convert.ToInt32(Convert.ToDecimal(split[6]));
                        string ZaloNumber = split[7];

                        Sms_Data_SendModel modelCustomer = new Sms_Data_SendModel();
                        modelCustomer.BillId = billID;
                        Bill_ElectricityBillModel hoadon = db.Bill_ElectricityBill.Where(o => o.DepartmentId == input.DepartmentId && o.BillId == billID).Select(
                            x => new Bill_ElectricityBillModel
                            {
                                BillAddress = x.BillAddress,
                                CustomerName = x.CustomerName,
                                SubTotal = x.SubTotal,
                                VAT = x.VAT,
                                Month = x.Month,
                                Year = x.Year,
                                Term = x.Term,
                                AdjustmentType = EnumMethod.D_TinhChatHoaDon.PhatSinh,
                                BillType = x.BillType
                            }).FirstOrDefault();
                        if (hoadon == null)
                        {
                            hoadon = db.Bill_ElectricityBillAdjustment.Where(o => o.DepartmentId == input.DepartmentId && o.BillId == billID).Select(
                              x => new Bill_ElectricityBillModel
                              {
                                  BillAddress = x.BillAddress,
                                  CustomerName = x.CustomerName,
                                  SubTotal = x.SubTotal,
                                  VAT = x.VAT,
                                  Month = x.Month,
                                  Year = x.Year,
                                  Term = x.Term,
                                  AdjustmentType = EnumMethod.D_TinhChatHoaDon.LapLai,
                                  BillType = x.BillType
                              }).FirstOrDefault();
                        }


                        modelCustomer.dchikh = hoadon.BillAddress;
                        modelCustomer.tenkh = hoadon.CustomerName;
                        modelCustomer.sanluong = dientthu;
                        modelCustomer.SubTotal = hoadon.SubTotal;
                        modelCustomer.VAT = hoadon.VAT;
                        modelCustomer.tientd = ToTal_TD;
                        //các trường bắt buộc có
                        modelCustomer.makh = customerCode;
                        modelCustomer.CustomerId = customerId;
                        modelCustomer.PhoneNumber = phoneNumber;
                        modelCustomer.ZaloNumber = ZaloNumber;
                        modelCustomer.FigureBookId = figureBookId;
                        modelCustomer.thang = hoadon.Month;
                        modelCustomer.nam = hoadon.Year;
                        modelCustomer.ky = hoadon.Term;
                        modelCustomer.BillType = hoadon.BillType;
                        modelCustomer.TypeSend = TYPE_SEND_By_Customer;
                        modelCustomer.DepartmentId = hoadon.DepartmentId;
                        listKH.Add(modelCustomer);
                    }

                    #region chuẩn hóa để bổ sung thêm thông tin cho trường dữ liệu phức tạp
                    var listRemove = new List<decimal>();
                    foreach (var hd in listKH)
                    {
                        var vPoint = db.Bill_ElectricityBillDetail.Where(o => o.DepartmentId == input.DepartmentId && o.Term == hd.ky
                                     && o.Month == hd.thang && o.Year == hd.nam
                                     && o.BillId == hd.BillId
                                  ).Select(x => new Bill_ElectricityBillDetailModel
                                  {
                                      StationId = x.StationId,
                                      PointId = x.PointId,
                                      PointAddress = x.Concus_ServicePoint.Address
                                  }).FirstOrDefault();
                        if (vPoint == null)
                        {
                            vPoint = db.Bill_ElectricityBillAdjustmentDetail.Where(o => o.DepartmentId == input.DepartmentId && o.Term == hd.ky
                                    && o.Month == hd.thang && o.Year == hd.nam
                                    && o.BillId == hd.BillId
                                 ).Select(x => new Bill_ElectricityBillDetailModel
                                 {
                                     StationId = x.StationId,
                                     PointId = x.PointId,
                                     PointAddress = x.Concus_ServicePoint.Address
                                 }).FirstOrDefault();
                        }
                        var kh = db.Concus_Customer.Where(x => x.CustomerId == hd.CustomerId && x.DepartmentId == input.DepartmentId).Select(x => new { x.Email, x.BankAccount }).FirstOrDefault();
                        hd.email = kh?.Email;
                        hd.stknganhang = kh?.BankAccount;
                        var emailRelationship = db.Concus_Customer_Relationship.Where(x => x.CustomerId == hd.CustomerId && x.DepartmentId == input.DepartmentId && !string.IsNullOrEmpty(x.Email) && !x.IsDelete)
                            .Select(x => x.Email).ToList();
                        if (hd.email != null)
                        {
                            emailRelationship.Add(hd.email);
                        }
                        hd.email = $"{string.Join(";", emailRelationship.ToArray())}";

                        var stations = db.Category_Satiton.Where(x => x.StationId == vPoint.StationId).Select(x => new { x.StationName, x.StationCode }).FirstOrDefault();

                        hd.matram = stations.StationCode;
                        hd.tentram = stations.StationName;
                        //cập nhật địa chỉ điểm đo
                        hd.diachiddo = vPoint.PointAddress;
                        //cập nhật chỉ số định kỳ
                        var listChiSo = db.Index_Value.Where(o => o.DepartmentId == input.DepartmentId && o.PointId == vPoint.PointId
                                                && o.Year == hd.nam && o.Month == hd.thang && o.Term == hd.ky
                                                && o.IndexType == EnumMethod.LoaiChiSo.DDK
                                        ).ToList();
                        var cultureInfo = new CultureInfo("vi-VN");
                        if (listChiSo != null)
                        {
                            if (listChiSo.Count == 1)
                            {
                                hd.chisodk = listChiSo.FirstOrDefault().NewValue.ToString();
                            }
                            else
                            {
                                if (listChiSo.Count > 0) // Không có chỉ số loại khỏi danh sách, nguyên nhân có thể điểm đo này đã thanh lý
                                {
                                    foreach (var cs in listChiSo)
                                    {
                                        hd.chisodk = hd.chisodk + cs.TimeOfUse + ":" + string.Format(cultureInfo, "{0:N0}", cs.NewValue) + ";";
                                    }
                                    hd.chisodk = hd.chisodk.Substring(0, hd.chisodk.Length - 1);
                                }
                                else
                                {
                                    listRemove.Add(hd.BillId.Value);
                                }
                            }
                        }
                    }
                    #endregion
                    strKQ = businessSms.GuiThongBaoTienDienNuocTheoDanhSachKhachHang(listKH, input.TemplateId, db);

                    if (strKQ != "OK")
                    {
                        throw new ArgumentException($"Gửi tin nhắn có lỗi: {strKQ}");
                    }
                    else
                    {
                        respone.Status = 1;
                        respone.Message = "OK";
                        respone.Data = null;
                        return createResponse();
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

        [HttpGet]
        [Route("SmsToCustomer_TrackDebt")]
        public HttpResponseMessage SmsToCustomer_TrackDebt([DefaultValue(1)] int pageNumber, [DefaultValue(0)] int departmentId, [DefaultValue(0)] int figureBookId)
        {
            try
            {
                if (departmentId == 0)
                    departmentId = TokenHelper.GetDepartmentIdFromToken();

                var userInfo = TokenHelper.GetUserInfoFromRequest();
                //Lấy phòng ban hiện tại và tất cả phòng ban cấp dưới DỰA VÀO departmentId
                var listDepartmentUserAll = DepartmentHelper.GetChildDepIds(departmentId);
                //Lấy phòng ban hiện tại và tất cả phòng ban cấp dưới DỰA VÀO tài khoản đang đăng nhập
                var listDepartmentUser = DepartmentHelper.GetChildDepIdsByUser(userInfo.UserName);

                using (var db = new CCISContext())
                {
                    var query = (from a in db.Liabilities_TrackDebt
                               join b in db.Concus_Customer
                               on a.CustomerId equals b.CustomerId

                               join cc in db.Concus_Contract
                               on a.CustomerId equals cc.CustomerId
                               join cs in db.Concus_ServicePoint
                               on cc.ContractId equals cs.ContractId
                               join ct in db.Category_Satiton
                               on cs.StationId equals ct.StationId

                               where (listDepartmentUserAll.Contains(a.DepartmentId)
                               && (figureBookId == 0 || a.FigureBookId == figureBookId)
                               && (a.Status == 0 || a.Status == 4 || a.Status == 5)
                               && ((b.PhoneCustomerCare != null && b.PhoneCustomerCare != "") || (b.ZaloCustomerCare != null && b.ZaloCustomerCare != ""))
                               && (a.TaxDebt + a.Debt) > 0)
                               && cs.Status
                               select new Liabilities_TrackDebtModel
                               {
                                   AddressPoint = cs.Address,
                                   StationCode = ct.StationCode,
                                   FigureBookId = a.FigureBookId,
                                   CustomerId = a.CustomerId,
                                   CustomerName = b.Name,
                                   Address = b.Address,
                                   CustomerCode = b.CustomerCode,
                                   Total = a.Debt + a.TaxDebt,
                                   PhoneNumber = b.PhoneCustomerCare,
                                   ZaloNumber = b.ZaloCustomerCare,
                               }).GroupBy(r => new
                               {
                                   r.CustomerCode,
                                   r.CustomerId,
                                   r.CustomerName,
                                   r.PhoneNumber,
                                   r.ZaloNumber,
                                   r.Address,
                                   r.FigureBookId,
                                   r.AddressPoint,
                                   r.StationCode
                               })
                                .Select(cr => new Liabilities_TrackDebtViewModel
                                {
                                    CustomerId = cr.Key.CustomerId,
                                    Address = cr.Key.Address,
                                    CustomerCode = cr.Key.CustomerCode,
                                    Total = cr.Sum(t => t.Total.Value),
                                    PhoneNumber = cr.Key.PhoneNumber,
                                    ZaloNumber = cr.Key.ZaloNumber,
                                    CustomerName = cr.Key.CustomerName,
                                    FigureBookId = cr.Key.FigureBookId.Value,
                                    AddressPoint = cr.Key.AddressPoint,
                                    StationCode = cr.Key.StationCode
                                });

                    var paged = (IPagedList<Liabilities_TrackDebtViewModel>)query.OrderBy(p => p.CustomerCode).ToPagedList(pageNumber, pageSize);

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
        [Route("SmsToCustomer_TrackDebt_Post")]
        public HttpResponseMessage SmsToCustomer_TrackDebt_Post(SmsToCustomer_TrackDebt_PostInput input)
        {
            try
            {
                string strKQ = "";
                using (var db = new CCISContext())
                {
                    setUserId();
                    List<Sms_Data_SendModel> listKH = new List<Sms_Data_SendModel>();
                    for (int i = 0; i < input.ListCustomerId.Length; i++)
                    {
                        string lstItem = input.ListCustomerId[i].ToString();
                        string[] split = lstItem.Split(',');
                        int customerId = Convert.ToInt32(split[0]);
                        string phoneNumber = split[1];
                        string customerCode = split[2];
                        decimal ToTal_TD = Convert.ToDecimal(split[3]);
                        int figureBookId = Convert.ToInt32(split[4]);
                        string zaloNumber = split[5];

                        Sms_Data_SendModel modelCustomer = new Sms_Data_SendModel();
                        modelCustomer.BillId = 0;                        
                        modelCustomer.tientd = ToTal_TD;
                        //các trường bắt buộc có
                        modelCustomer.makh = customerCode;
                        modelCustomer.CustomerId = customerId;
                        modelCustomer.PhoneNumber = phoneNumber;
                        modelCustomer.ZaloNumber = zaloNumber;
                        modelCustomer.FigureBookId = figureBookId;
                        modelCustomer.thang = DateTime.Now.Month;
                        modelCustomer.nam = DateTime.Now.Year;
                        modelCustomer.ky = 0;
                        modelCustomer.TypeSend = TYPE_SEND_By_Customer;
                        listKH.Add(modelCustomer);
                    }

                    #region chuẩn hóa để bổ sung thêm thông tin cho trường dữ liệu phức tạp
                    foreach (var hd in listKH)
                    {
                        var vPoint = db.Concus_ServicePoint.Where(o => o.DepartmentId == input.DepartmentId
                                     && o.Concus_Contract.CustomerId == hd.CustomerId
                                     && o.Status
                                  ).FirstOrDefault();
                        var vCustomerName = db.Concus_Customer.Where(o => o.DepartmentId == input.DepartmentId && o.CustomerId == hd.CustomerId
                                  ).FirstOrDefault()?.Name;
                        var emailRelationship = db.Concus_Customer_Relationship.Where(x => x.CustomerId == hd.CustomerId && x.DepartmentId == input.DepartmentId && !string.IsNullOrEmpty(x.Email) && !x.IsDelete)
                            .Select(x => x.Email).ToList();
                        if (hd.email != null)
                        {
                            emailRelationship.Add(hd.email);
                        }
                        hd.email = $"{string.Join(";", emailRelationship.ToArray())}";
                        var stations = db.Category_Satiton.Where(x => x.StationId == vPoint.StationId).Select(x => new { x.StationName, x.StationCode }).FirstOrDefault();
                        hd.matram = stations.StationCode;
                        hd.tentram = stations.StationName;
                        //cập nhật địa chỉ điểm đo
                        hd.diachiddo = vPoint.Address;
                        hd.DepartmentId = vPoint.DepartmentId;
                        hd.tenkh = vCustomerName;
                        hd.BillType = vPoint.NumberOfPhases == 0 ? "TN" : "TD";
                    }
                    #endregion
                    strKQ = businessSms.GuiThongBaoNhacNoTheoDanhSachKhachHang(listKH, input.TemplateId, db);

                    if (strKQ != "OK")
                    {
                        throw new ArgumentException($"Gửi tin nhắn có lỗi: {strKQ}");                        
                    }
                    else
                    {
                        respone.Status = 1;
                        respone.Message = "OK";
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

        [HttpGet]
        [Route("SmsToBook_TrackDebt")]
        public HttpResponseMessage SmsToBook_TrackDebt([DefaultValue(1)] int pageNumber, [DefaultValue(0)] int departmentId, [DefaultValue(0)] int figureBookId)
        {
            try
            {
                if (departmentId == 0)
                    departmentId = TokenHelper.GetDepartmentIdFromToken();

                var userId = TokenHelper.GetUserIdFromToken();
                //Lấy phòng ban hiện tại và tất cả phòng ban cấp dưới DỰA VÀO departmentId
                var listDepartmentUserAll = DepartmentHelper.GetChildDepIds(departmentId);

                //Lấy danh mục sổ ghi chỉ số combobox
                var listFigureBook = DepartmentHelper.GetFigureBook(userId, listDepartmentUserAll)
                    .ToList();

                var getFigureBookId = listFigureBook.Select(item => item.FigureBookId).ToList();
                using (var db = new CCISContext())
                {
                    // Lấy danh sách sổ với điều kiện khách trong sổ chưa thanh toán
                    var query = (from a in db.Liabilities_TrackDebt
                                                     join b in db.Category_FigureBook
                                                     on a.FigureBookId equals b.FigureBookId
                                                     join c in db.Concus_Customer
                                                     on a.CustomerId equals c.CustomerId

                                                     where (listDepartmentUserAll.Contains(a.DepartmentId)
                                                     && getFigureBookId.Contains(a.FigureBookId.Value)
                                                     && (figureBookId == 0 || a.FigureBookId == figureBookId)
                                                     &&
                                                        (a.Status == 0 || a.Status == 4 || a.Status == 5)
                                                     && (a.TaxDebt + a.Debt) > 0)
                                                     select new Liabilities_TrackDebtViewModel
                                                     {
                                                         FigureBookId = a.FigureBookId.Value,
                                                         BookName = b.BookName,
                                                         BookCode = b.BookCode,
                                                         CustomerId = a.CustomerId,
                                                         PhoneNumber = c.PhoneCustomerCare,
                                                         ZaloNumber = c.ZaloCustomerCare,                                                         
                                                     }).GroupBy(item => new { item.FigureBookId, item.BookName, item.BookCode })
                                                     .Select(ite => new Sms_ShowBook
                                                     {
                                                         FigureBookId = ite.Key.FigureBookId,
                                                         BookCode = ite.Key.BookCode,
                                                         BookName = ite.Key.BookName,
                                                         CustomersTotal = ite.Select(r => r.CustomerId).Distinct().Count(),
                                                         CustomersPhone = ite.Where(r => r.PhoneNumber.Length > 0).Distinct().Count(),

                                                     });
                    var paged = (IPagedList<Sms_ShowBook>)query.ToPagedList(pageNumber, pageSize);

                    var response = new
                    {
                        paged.PageNumber,
                        paged.PageSize,
                        paged.TotalItemCount,
                        paged.PageCount,
                        paged.HasNextPage,
                        paged.HasPreviousPage,
                        Sms_ShowBooks = paged.ToList()
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
        [Route("SmsToBook_TrackDebt")]
        public HttpResponseMessage SmsToBook_TrackDebt(SmsToBook_TrackDebtInput input)
        {
            try
            {
                var getSmsTemplate = businessSms.GetSms_TemplateId(input.DepartmentId, input.TemplateId);

                using (var db = new CCISContext())
                {
                    setUserId();
                    // cập nhật giá trị thời gian gửi email
                    if (input.Email_Time_Sleep != 0)
                    {
                        paramsmeter.SetParameterValue(Administrator_Parameter_Common.Email_Time_Sleep, input.Email_Time_Sleep.ToString(), input.DepartmentId);
                    }

                    Sms_Track_FindModel find = new Sms_Track_FindModel();
                    find.departmentid = input.DepartmentId;
                    find.SmsTemplateId = input.TemplateId;
                    find.month = DateTime.Now.Month;
                    find.year = DateTime.Now.Year;
                    find.ListCustomerId = new List<int>();
                    // Lấy danh sách từng sổ 1
                    Logger.Info($"SmsToBook_TrackDebt: Danh sách sổ gửi tin nhắc nợ: {string.Join(",", input.ListFigureBookId)}");
                    StringBuilder message = new StringBuilder();
                    for (int i = 0; i < input.ListFigureBookId.Count; i++)
                    {
                        find.FigureBookId = input.ListFigureBookId[i];
                        var sogcs = db.Category_FigureBook.FirstOrDefault(item => item.FigureBookId == find.FigureBookId);
                        string strKQ = businessSms.GuiThongBaoNhacNo(find, db, input.IsSendAgain);
                        message.Append($"{sogcs.BookCode} - {sogcs.BookName}: ");
                        message.Append(strKQ);
                        message.Append("\n");                        
                    }

                    respone.Status = 1;
                    respone.Message = "OK";
                    respone.Data = message.ToString();
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
        [Route("SmsToCustomer_BillTaxInvoice")]
        public HttpResponseMessage SmsToCustomer_BillTaxInvoice([DefaultValue(1)] int pageNumber, DateTime? month, [DefaultValue(0)] int departmentId, [DefaultValue(0)] int figureBookId, [DefaultValue(0)] int smsTemplateId)
        {
            try
            {
                if (departmentId == 0)
                    departmentId = TokenHelper.GetDepartmentIdFromToken();

                var listDepartmentUserAll = DepartmentHelper.GetChildDepIds(departmentId);                

                if (month == null)
                {
                    month = DateTime.Now;
                }

                using (var db = new CCISContext())
                {
                    List<Sms_Track_Taxinvoice> listData;
                    var listTaxInvoice = (from taxinvoice in db.Bill_TaxInvoice
                                          join detail in db.Bill_TaxInvoiceDetail on taxinvoice.TaxInvoiceId equals detail.TaxInvoiceId
                                          join customer in db.Concus_Customer on taxinvoice.CustomerId equals customer.CustomerId
                                          join trackdept in db.Liabilities_TrackDebt_TaxInvoice on taxinvoice.TaxInvoiceId equals trackdept.TaxInvoiceId
                                          join book in db.Category_FigureBook on detail.FigureBookId equals book.FigureBookId
                                          where (figureBookId == 0 || detail.FigureBookId == figureBookId) && detail.ServiceTypeId == TRANG_THAI_ServiceTypeId.PHI_QLVH_EMB // Chức năng này mới chỉ phục vụ cho EMB lúc nào có các đơn vị khác với nhiều loại hóa đơn hơn thì sẽ phải xử lý lại
                                          && trackdept.Status == 0 && (month == null || (taxinvoice.Month == month.Value.Month && taxinvoice.Year == month.Value.Year))
                                          select new Sms_Track_Taxinvoice
                                          {
                                              TotalAmount = taxinvoice.Total,
                                              CustomerCode = taxinvoice.CustomerCode,
                                              CustomerName = taxinvoice.CustomerName,
                                              TaxInvoiceId = taxinvoice.TaxInvoiceId,
                                              Address = taxinvoice.TaxInvoiceAddress,
                                              CustomerId = taxinvoice.CustomerId,
                                              PhoneNumber = customer.PhoneCustomerCare,
                                              SoGcs = book.BookName,
                                              Month = taxinvoice.Month,
                                              Year = taxinvoice.Year
                                          }).ToList();
                    List<int> dsDaGui = db.Sms_Track_Customer.Where(track => track.SmsTemplateId == smsTemplateId && track.Status == 1
                          && track.Month == month.Value.Month && track.Year == month.Value.Year)
                          .Select(track => track.CustomerId).ToList();
                    listTaxInvoice.ForEach(item =>
                    {
                        var isSent = dsDaGui.Any(customerId => customerId == item.CustomerId);
                        if (isSent)
                            item.Message = "Đã gửi";
                    });

                    var paged = (IPagedList<Sms_Track_Taxinvoice>)listTaxInvoice.ToPagedList(pageNumber, pageSize);

                    var response = new
                    {
                        paged.PageNumber,
                        paged.PageSize,
                        paged.TotalItemCount,
                        paged.PageCount,
                        paged.HasNextPage,
                        paged.HasPreviousPage,
                        TaxInvoices = paged.ToList()
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
        [Route("SmsToCustomer_BillTaxInvoice")]
        public HttpResponseMessage SmsToCustomer_BillTaxInvoice(List<decimal> TaxiInvoiceId, int templateId)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    string message = "";
                    if (TaxiInvoiceId == null || TaxiInvoiceId.Count == 0)
                    {
                        message = "Danh sách gửi trống vui lòng tích chọn để gửi";
                    }
                    else if (templateId <= 0)
                    {
                        message = "Vui lòng chọn mẫu tin nhắn cần gửi";
                    }
                    else
                    {
                        setUserId();
                        var result = businessSms.GuiThongBaoTienHoaDonGT(TaxiInvoiceId, templateId, db);
                        message = result;
                    }

                    respone.Status = 1;
                    respone.Message = "OK";
                    respone.Data = message;
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

        //[HttpGet]
        //[Route("")]
        //public HttpResponseMessage SmsViewStatus([DefaultValue(1)] int page,
        //                                    [DefaultValue("")] string txtTimKiem, [DefaultValue(-1)] int smsTemplateId,
        //                                    [DefaultValue("0")] string statusService, [DefaultValue("All")] string LoaiTin,
        //                                    DateTime? dateFrom, DateTime? dateTo, [DefaultValue(false)] bool isExportExcel)
        //{
        //    try
        //    {

        //    }
        //    catch (Exception ex)
        //    {
        //        respone.Status = 0;
        //        respone.Message = $"Lỗi: {ex.Message.ToString()}";
        //        respone.Data = null;
        //        return createResponse();
        //    }
        }
        #region Class
        public class SmsToBook_PostInput
        {
            public int[] ListFigureBookId { get; set; }
            public int DepartmentId { get; set; }
            public int Term { get; set; }
            public DateTime? SaveDate { get; set; }
            public int TemplateId { get; set; }
            public bool IsSendAgain { get; set; }
            public int? Email_Time_Sleep { get; set; }
        }

        public class SmsToCustomer_PostInput
        {
            public string[] ListCustomerId { get; set; }
            public int DepartmentId { get; set; }
            public int Term { get; set; }
            public DateTime? SaveDate { get; set; }
            public int TemplateId { get; set; }
        }

        public class SmsToCustomer_TrackDebt_PostInput
        {
            public string[] ListCustomerId { get; set; }
            public int DepartmentId { get; set; }
            public int TemplateId { get; set; }
        }

        public class SmsToBook_TrackDebtInput
        {
            public List<int> ListFigureBookId { get; set; }
            public int DepartmentId { get; set; }
            public int TemplateId { get; set; }
            public bool IsSendAgain { get; set; }
            public int? Email_Time_Sleep { get; set; }
        }
        #endregion    
}
