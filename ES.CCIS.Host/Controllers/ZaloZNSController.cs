using CCIS_BusinessLogic;
using CCIS_BusinessLogic.DTO.Sms;
using CCIS_DataAccess;
using ES.CCIS.Host.Helpers;
using ES.CCIS.Host.Models.EnumMethods;
using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Web.Configuration;
using System.Web.Http;

namespace ES.CCIS.Host.Controllers
{
    [Authorize]
    [RoutePrefix("api/ZaloZNS")]
    public class ZaloZNSController : ApiBaseController
    {
        private readonly CCISContext _dbContext;
        private readonly Business_ZaloZNS znsBusiness = new Business_ZaloZNS();
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        const int SMS_TYPE_ID_1 = 1; // là hình thức gửi tin nhắn thông báo tiền điện
        const int SMS_TYPE_ID_2 = 2; // là hình thức gửi tin nhắn nhắc nợ tiền điện
        const int TYPE_SEND_CUSTOMER = 1; // là hình thức gửi gin nhắn theo khách hàng
        const int TYPE_SEND_BOOK = 2; // là hình thức gửi gin nhắn theo sổ;
        public ZaloZNSController()
        {
            _dbContext = new CCISContext();
        }

        [HttpGet]
        [Route("ZaloZNSSendToCustomer")]
        public HttpResponseMessage ZaloZNSSendToCustomer([DefaultValue(1)] int pageNumber, [DefaultValue(0)] int term, [DefaultValue(0)] int departmentId, [DefaultValue(0)] int figureBookId, DateTime? month, string typeOfGet, string SearchText)
        {
            try
            {
                if (month == null)
                {
                    month = DateTime.Now;
                }

                if (departmentId == 0)
                    departmentId = TokenHelper.GetDepartmentIdFromToken();

                //Lấy phòng ban hiện tại và tất cả phòng ban cấp dưới DỰA VÀO departmentId
                var listDepartmentUserAll = DepartmentHelper.GetChildDepIds(departmentId);

                //List billId theo kỳ
                var listBillId = _dbContext.Liabilities_TrackDebt.Where(item => listDepartmentUserAll.Contains(item.DepartmentId)
                   && (figureBookId == 0 || item.FigureBookId == figureBookId)
                   && item.Year == month.Value.Year && item.Month == month.Value.Month
                   && item.Term == term &&
                      (
                            item.Status == 0 ||
                            item.Status == 4 ||
                            item.Status == 5
                        )
                   )
                   .Select(item => item.BillId).ToList();

                var lst = (from bill in (_dbContext.Bill_ElectricityBill.Where(i => listBillId.Contains(i.BillId) && (string.IsNullOrEmpty(SearchText) || SearchText.Contains(i.CustomerCode) || SearchText.Contains(i.Concus_Customer.PhoneNumber))).Select(i => new
                {
                    i.BillId,
                    i.CustomerCode,
                    i.CustomerName,
                    i.BillType,
                    i.Total,
                    i.DepartmentId,
                    i.CustomerId,
                    i.Concus_Customer.PhoneCustomerCare,
                    i.Concus_Customer.ZaloCustomerCare,
                    i.FigureBookId,
                    i.ElectricityIndex
                }))
                           join cc in _dbContext.Concus_Contract
                           on bill.CustomerId equals cc.CustomerId
                           join cs in _dbContext.Concus_ServicePoint
                           on cc.ContractId equals cs.ContractId
                           join ct in _dbContext.Category_Satiton
                           on cs.StationId equals ct.StationId
                           where cs.Status
                           select new
                           {
                               AddressPoint = cs.Address,
                               StationCode = ct.StationCode,
                               BillId = bill.BillId,
                               CustomerCode = bill.CustomerCode,
                               CustomerName = bill.CustomerName,
                               BillType = bill.BillType,
                               Total = bill.Total,
                               DepartmentId = bill.DepartmentId,
                               CustomerId = bill.CustomerId,
                               PhoneNumber = bill.PhoneCustomerCare,
                               ZaloNumber = bill.ZaloCustomerCare,
                               FigureBookId = bill.FigureBookId,
                               ElectricityIndex = bill.ElectricityIndex
                           }).ToList();

                IQueryable<int> smsTrackQuery = null;
                IEnumerable<Bill_ElectricityBillModel> query = null;

                switch (typeOfGet)
                {
                    case "Sended":
                        smsTrackQuery = _dbContext.Sms_Track_Customer.Where(i => listBillId.Contains(i.BillId) && i.SmsTypeId == SMS_TYPE_ID_1 && i.Status == 1
                        && i.Month == month.Value.Month && i.Year == month.Value.Year).Select(i => i.CustomerId);
                        lst = lst.Where(i => smsTrackQuery.Contains(i.CustomerId)).ToList();
                        break;
                    case "Unsend":
                        smsTrackQuery = _dbContext.Sms_Track_Customer.Where(i => listBillId.Contains(i.BillId) && i.SmsTypeId == SMS_TYPE_ID_1 && i.Status == 1
                        && i.Month == month.Value.Month && i.Year == month.Value.Year).Select(i => i.CustomerId);
                        lst = lst.Where(i => !smsTrackQuery.Contains(i.CustomerId)).ToList();
                        break;
                    case "Error":
                        var template = _dbContext.Sms_Template.Select(i => new { i.SmsTemplateId, i.AppSend }).ToList();

                        var smsHistory = (from h in _dbContext.Sms_Track_Customer
                                          join t in _dbContext.Sms_Template
                                          on h.SmsTemplateId equals t.SmsTemplateId
                                          where listBillId.Contains(h.BillId) && h.SmsTypeId == SMS_TYPE_ID_1 && h.Month == month.Value.Month && h.Year == month.Value.Year && h.Status != 1
                                          select new { h.BillId, h.Status, h.MessageService, h.CreateDate, t.AppSend }
                                          ).ToList();

                        var groupData = smsHistory.GroupBy(o => o.BillId).Select(o => new { BillId = o.Key, history = o.OrderByDescending(o1 => o1.CreateDate).ToList() }).ToList();


                        query = (from a in lst
                                 join b in groupData
                                 on a.BillId equals b.BillId
                                 where string.IsNullOrEmpty(a.ZaloNumber) && !string.IsNullOrEmpty(a.PhoneNumber)
                                 select new Bill_ElectricityBillModel
                                 {
                                     AddressPoint = a.AddressPoint,
                                     StationCode = a.StationCode,
                                     BillId = a.BillId,
                                     CustomerCode = a.CustomerCode,
                                     CustomerName = a.CustomerName,
                                     BillType = a.BillType,
                                     Total = a.Total,
                                     DepartmentId = a.DepartmentId,
                                     CustomerId = a.CustomerId,
                                     PhoneNumber = a.PhoneNumber,
                                     ZaloNumber = a.ZaloNumber,
                                     FigureBookId = a.FigureBookId,
                                     ElectricityIndex = a.ElectricityIndex,
                                     Sms_History = b.history.Select(d => new Bill_Sms_History
                                     {
                                         AppSend = d.AppSend,
                                         Message = d.MessageService,
                                         Created = d.CreateDate
                                     }).OrderByDescending(h => h.Created).ToList()
                                 });
                        break;
                }

                if (lst?.Any() == true)
                {
                    query = lst.Where(a => string.IsNullOrEmpty(a.ZaloNumber) && !string.IsNullOrEmpty(a.PhoneNumber)).Select(a => new Bill_ElectricityBillModel
                    {
                        AddressPoint = a.AddressPoint,
                        StationCode = a.StationCode,
                        BillId = a.BillId,
                        CustomerCode = a.CustomerCode,
                        CustomerName = a.CustomerName,
                        BillType = a.BillType,
                        Total = a.Total,
                        DepartmentId = a.DepartmentId,
                        CustomerId = a.CustomerId,
                        PhoneNumber = a.PhoneNumber,
                        ZaloNumber = a.ZaloNumber,
                        FigureBookId = a.FigureBookId,
                        ElectricityIndex = a.ElectricityIndex
                    });
                }

                var paged = query.OrderBy(p => p.CustomerCode).ToPagedList(pageNumber, pageSize);

                var response = new
                {
                    paged.PageNumber,
                    paged.PageSize,
                    paged.TotalItemCount,
                    paged.PageCount,
                    paged.HasNextPage,
                    paged.HasPreviousPage,
                    BillElectricitys = paged.ToList()
                };
                respone.Status = 1;
                respone.Message = "Lấy danh sách thành công.";
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
        [Route("ZaloZNSToCustomer_Send")]
        public HttpResponseMessage ZaloZNSToCustomer_Send(ZaloZNSToCustomer_SendInput input)
        {
            try
            {
                string strKQ = "";
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
                    var hoadon = _dbContext.Bill_ElectricityBill.Where(o => o.DepartmentId == input.DepartmentId && o.BillId == billID).FirstOrDefault();
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
                    modelCustomer.DepartmentId = hoadon.DepartmentId;
                    modelCustomer.TypeSend = TYPE_SEND_CUSTOMER;
                    listKH.Add(modelCustomer);
                }

                #region chuẩn hóa để bổ sung thêm thông tin cho trường dữ liệu phức tạp
                var listRemove = new List<decimal>();
                foreach (var hd in listKH)
                {
                    var vPoint = _dbContext.Bill_ElectricityBillDetail.Where(o => o.DepartmentId == input.DepartmentId && o.Term == hd.ky
                                 && o.Month == hd.thang && o.Year == hd.nam
                                 && o.BillId == hd.BillId
                              ).FirstOrDefault();
                    //cập nhật địa chỉ điểm đo
                    //hd.diachiddo = vPoint.Concus_ServicePoint.Address;
                    hd.email = _dbContext.Concus_Customer.Where(c => hd.CustomerId == c.CustomerId).Select(c => c.Email)
                       .FirstOrDefault();
                    hd.diachiddo = _dbContext.Concus_ServicePoint.Where(c => c.Concus_Contract.CustomerId == hd.CustomerId && c.Status).FirstOrDefault().Address;
                    //cập nhật chỉ số định kỳ
                    var listChiSo = _dbContext.Index_Value.Where(o => o.DepartmentId == input.DepartmentId && o.PointId == vPoint.PointId
                                            && o.Year == hd.nam && o.Month == hd.thang && o.Term == hd.ky
                                            && o.IndexType == EnumMethod.LoaiChiSo.DDK
                                    ).ToList();
                    if (listChiSo != null)
                    {
                        if (listChiSo.Count == 1)
                        {
                            hd.chisodk = listChiSo.FirstOrDefault().NewValue.ToString("N0");
                            hd.chisodauky = listChiSo.FirstOrDefault().OldValue.ToString("N0");
                        }
                        else
                        {
                            if (listChiSo.Count > 0) // Không có chỉ số loại khỏi danh sách, nguyên nhân có thể điểm đo này đã thanh lý
                            {
                                foreach (var cs in listChiSo)
                                {
                                    hd.chisodk = hd.chisodk + cs.TimeOfUse + ":" + cs.NewValue.ToString() + ";";
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

                var lstError = new List<string>();
                foreach (var item in listKH)
                {
                    strKQ = znsBusiness.SendZNSMessage(item, input.TemplateId);
                    if (strKQ != "OK")
                    {
                        lstError.Add($"Mã: {item.makh} => {strKQ}");
                    }
                }
                if (lstError.Count > 0)
                {
                    throw new ArgumentException($"{string.Join(",", lstError.ToArray())}");
                }
                else
                {
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

        //Todo: Chưa viết api ZNSToBook_Alert

        [HttpPost]
        [Route("ZNSToBook_Alert_Send")]
        public HttpResponseMessage ZNSToBook_Alert_Send(ZNSToBook_Alert_SendInput input)
        {
            try
            {
                string strKQ = "";

                var tramInfor = _dbContext.Category_Satiton.Where(o => o.StationId == input.StationId).FirstOrDefault();
                List<Sms_Data_SendModel> listKH = new List<Sms_Data_SendModel>();
                var today = DateTime.Now.Date;

                var listDaGui = new List<int>();
                var _templateId = Convert.ToInt32(input.TemplateId);
                if (!input.IsSendAgain)
                {
                    listDaGui = _dbContext.Sms_Track_Customer.Where(track =>
                    //track.SmsTypeId == smsTemplate.SmsTypeId
                    track.SmsTemplateId == _templateId
                    && track.Status == 1
                    && track.StationId == input.StationId
                    && DbFunctions.TruncateTime(track.CreateDate) == today
                    )
                    .Select(track => track.CustomerId).ToList();
                }


                for (int i = 0; i < input.ListData.Length; i++)
                {
                    string lstItem = input.ListData[i].ToString();
                    string[] split = lstItem.Split(',');
                    string phoneNumber = split[0];
                    string customerCode = split[1];
                    int figureBookId = Convert.ToInt32(split[2]);
                    int customerId = Convert.ToInt32(split[3]);
                    string ZaloNumber = split[4];
                    var customer = _dbContext.Concus_Customer.Where(x => x.CustomerId == customerId).FirstOrDefault();
                    if (!listDaGui.Contains(customerId)) // Loại bỏ những khách hàng đã gửi thành công trc đó
                    {
                        Sms_Data_SendModel modelCustomer = new Sms_Data_SendModel();
                        modelCustomer.BillId = 0;
                        //các trường bắt buộc có
                        modelCustomer.tenkh = customer != null ? customer.Name : "";
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
                        modelCustomer.TypeSend = TYPE_SEND_CUSTOMER;
                        modelCustomer.DepartmentId = tramInfor.DepartmentId;

                        listKH.Add(modelCustomer);
                    }
                }
                if (listKH.Count == 0)
                {
                    throw new ArgumentException($"Danh sách gửi không có KH nào hoặc các KH này đã gửi thành công trước đó gửi lòng kiểm tra lịch sử gửi tin nhắn.");
                }

                var lstError = new List<string>();
                foreach (var item in listKH)
                {
                    strKQ = znsBusiness.SendZNSMessage(item, input.TemplateId);
                    if (strKQ != "OK")
                    {
                        lstError.Add($"Mã: {item.makh} => {strKQ}");
                    }
                }
                if (lstError.Count > 0)
                {
                    throw new ArgumentException($"{string.Join(",", lstError.ToArray())}");
                }
                else
                {
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

        [HttpGet]
        [Route("ZNSNhacNoKH")]
        public HttpResponseMessage ZNSNhacNoKH([DefaultValue(1)] int pageNumber, [DefaultValue(0)] int departmentId, [DefaultValue(0)] int figureBookId)
        {
            try
            {
                bool initFirt = departmentId == 0 ? true : false;
                if (departmentId == 0)
                    departmentId = TokenHelper.GetDepartmentIdFromToken();

                //Lấy phòng ban hiện tại và tất cả phòng ban cấp dưới DỰA VÀO departmentId
                var listDepartmentUserAll = DepartmentHelper.GetChildDepIds(departmentId);

                var query = (from a in _dbContext.Liabilities_TrackDebt
                             join b in _dbContext.Concus_Customer
                             on a.CustomerId equals b.CustomerId

                             join cc in _dbContext.Concus_Contract
                             on a.CustomerId equals cc.CustomerId
                             join cs in _dbContext.Concus_ServicePoint
                             on cc.ContractId equals cs.ContractId
                             join ct in _dbContext.Category_Satiton
                             on cs.StationId equals ct.StationId

                             where (listDepartmentUserAll.Contains(a.DepartmentId)
                             && (figureBookId == 0 || a.FigureBookId == figureBookId)
                             && (a.Status == 0 || a.Status == 4 || a.Status == 5)
                             && string.IsNullOrEmpty(b.ZaloCustomerCare) && !string.IsNullOrEmpty(b.PhoneCustomerCare)
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

                var paged = (IPagedList<Liabilities_TrackDebtViewModel>)query.ToPagedList(pageNumber, pageSize);

                var response = new
                {
                    paged.PageNumber,
                    paged.PageSize,
                    paged.TotalItemCount,
                    paged.PageCount,
                    paged.HasNextPage,
                    paged.HasPreviousPage,
                    TrackDebtViews = paged.ToList()
                };
                respone.Status = 1;
                respone.Message = "Lấy danh sách thành công.";
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
        [Route("ZNSNhacNoKH_Send")]
        public HttpResponseMessage ZNSNhacNoKH_Send(ZNSNhacNoKH_SendInput input)
        {
            try
            {
                string strKQ = "";
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
                    modelCustomer.TypeSend = TYPE_SEND_CUSTOMER;
                    listKH.Add(modelCustomer);
                }

                #region chuẩn hóa để bổ sung thêm thông tin cho trường dữ liệu phức tạp
                foreach (var hd in listKH)
                {
                    var vPoint = _dbContext.Concus_ServicePoint.Where(o => o.Concus_Contract.CustomerId == hd.CustomerId && o.Status
                              ).FirstOrDefault();
                    //cập nhật địa chỉ điểm đo
                    hd.diachiddo = vPoint.Address;
                    hd.DepartmentId = vPoint.DepartmentId;
                }
                #endregion
                if (listKH.Count == 0)
                {
                    throw new ArgumentException("Danh sách gửi không có KH nào hoặc các KH này đã gửi thành công trước đó gửi lòng kiểm tra lịch sử gửi tin nhắn.");
                }

                var lstError = new List<string>();
                foreach (var item in listKH)
                {
                    strKQ = znsBusiness.SendZNSMessage(item, input.TemplateId);
                    if (strKQ != "OK")
                    {
                        lstError.Add($"Mã: {item.makh} => {strKQ}");
                    }
                }
                if (lstError.Count > 0)
                {
                    throw new ArgumentException($"{string.Join(",", lstError.ToArray())}");
                }
                else
                {
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

        #region Class
        public class ZaloZNSToCustomer_SendInput
        {
            public string[] ListCustomerId { get; set; }
            public int DepartmentId { get; set; }
            public int Term { get; set; }
            public DateTime? SaveDate { get; set; }
            public string TemplateId { get; set; }
        }

        public class ZNSToBook_Alert_SendInput
        {
            public string[] ListData { get; set; }
            public int StationId { get; set; }
            public string TemplateId { get; set; }
            public bool IsSendAgain { get; set; }
        }

        public class ZNSNhacNoKH_SendInput
        {
            public string[] ListCustomerId { get; set; }
            public int DepartmentId { get; set; }
            public string TemplateId { get; set; }
        }
        #endregion
    }
}
