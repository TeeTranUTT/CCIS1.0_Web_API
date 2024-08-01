using CCIS_BusinessLogic;
using CCIS_BusinessLogic.DTO.Hilo;
using CCIS_DataAccess;
using ES.CCIS.Host.Models.Hilo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using static CCIS_BusinessLogic.DefaultBusinessValue;

namespace ES.CCIS.Host.Controllers.Hilo
{
    [Authorize]
    [RoutePrefix("api/Hilo")]
    public class HiloController : ApiBaseController
    {
        private readonly Business_Hilo hilo = new Business_Hilo();
        private readonly CCISContext _dbContext;

        public HiloController()
        {
            _dbContext = new CCISContext();
        }
        [HttpGet]
        [Route("ErrorHiloBillManage")]
        public HttpResponseMessage ErrorHiloBillManage([DefaultValue(-1)] int departmentID, [DefaultValue("HILO")] string TypeLog)
        {
            try
            {
                var lstErrorHiloLog = _dbContext.App_Log.Where(i => i.Status == false && i.DepartmentID == departmentID && i.TypeLog == TypeLog).ToList();
                var lstBillErr = new List<ErrorBillHilo>();
                var lstBillErrTaxInvice = new List<ErrorBillHilo>();
                var lstBillAdjustment = new List<ErrorBillHilo>();
                var listBillID = new List<decimal>();
                foreach (var log in lstErrorHiloLog)
                {
                    try
                    {
                        listBillID.Add(log.BillID);
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentException($"Lỗi trong khi lấy dữ liệu Bill Log {ex.Message}");
                    }
                }

                listBillID = listBillID.Distinct().ToList();
                lstBillErr = (from a in _dbContext.Bill_ElectricityBill
                              join b in _dbContext.Bill_ElectronicBill on a.BillId equals b.BillId
                              join c in _dbContext.Category_FigureBook on a.FigureBookId equals c.FigureBookId
                              where listBillID.Contains(a.BillId)
                              select new ErrorBillHilo
                              {
                                  BillId = a.BillId,
                                  CustomerName = a.CustomerName,
                                  DepartmentId = a.DepartmentId,
                                  FigureBook = c.BookCode,
                                  Ky = a.Term,
                                  Thang = a.Month,
                                  Nam = a.Year
                              }).Distinct().ToList();

                lstBillErrTaxInvice = (from a in _dbContext.Bill_TaxInvoice
                                       join b in _dbContext.Bill_ElectronicBill on a.BillId equals b.BillId
                                       where listBillID.Contains(a.BillId)
                                       select new ErrorBillHilo
                                       {
                                           BillId = a.BillId,
                                           CustomerName = a.CustomerName,
                                           DepartmentId = a.DepartmentId,
                                           FigureBook = "Đây là Hóa đơn GT",
                                           Ky = -1, // dùng kỳ để phân biệt là hóa đơn loại nào nếu có tồn tại 2 hóa đơn cùng một billId
                                           Thang = a.Month,
                                           Nam = a.Year
                                       }).Distinct().ToList();

                lstBillAdjustment = (from a in _dbContext.Bill_ElectricityBillAdjustment
                                     join b in _dbContext.Bill_ElectronicBill on a.BillId equals b.BillId
                                     where listBillID.Contains(a.BillId)
                                     select new ErrorBillHilo
                                     {
                                         BillId = a.BillId,
                                         CustomerName = a.CustomerName,
                                         DepartmentId = a.DepartmentId,
                                         FigureBook = "Đây là hóa đơn sửa sai",
                                         Ky = -2, // dùng kỳ để phân biệt là hóa đơn loại nào nếu có tồn tại 2 hóa đơn cùng một billId
                                         Thang = a.Month,
                                         Nam = a.Year
                                     }).Distinct().ToList();

                lstBillErr.AddRange(lstBillErrTaxInvice);
                lstBillErr.AddRange(lstBillAdjustment);
                foreach (var item in lstBillErr)
                {
                    item.ErrorMessage = lstErrorHiloLog.Where(i => i.BillID == item.BillId).Select(i => i.ContentLog).FirstOrDefault();
                }

                respone.Status = 1;
                respone.Message = "Lấy danh sách khách hàng thành công.";
                respone.Data = lstBillErr;
                return createResponse();
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
        [Route("PushHiloBill")]
        public HttpResponseMessage PushHiloBill(PushHiloBillInput input)
        {
            try
            {
                //lấy đúng hóa đơn phát hành lại
                var vErrorHiloLog = _dbContext.App_Log.Where(i => i.DepartmentID == input.DepartmentId && input.BillId.Contains(i.BillID.ToString()) && i.TypeLog == AppLogTypes.HILO && i.Status == false).Select(x => x.BillID).ToList();

                var billElectronic = (from bill in _dbContext.Bill_ElectronicBill
                                      join appLog in _dbContext.App_Log on new { A = bill.BillId, B = bill.BillType } equals new { A = appLog.BillID, B = appLog.BillType }
                                      where appLog.TypeLog == AppLogTypes.HILO && appLog.Status == false && input.BillId.Contains(appLog.BillID.ToString())
                                      && appLog.DepartmentID == input.DepartmentId
                                      select bill
                                     ).ToList();


                var loginInfo = _dbContext.Administrator_Parameter.FirstOrDefault(i => i.DepartmentId == input.DepartmentId && i.ParameterName == Administrator_Parameter_Common.HILO_AUTHENTICATION);
                if (loginInfo == null)
                {
                    throw new ArgumentException("Chưa cấu hình Parameter đăng nhập vào Hilo, vui lòng kiểm tra lại.");
                }
                if (billElectronic.Count() > 0)
                {
                    var taxtcode = _dbContext.Category_Serial.Where(i => i.DepartmentId == input.DepartmentId).Select(i => i.TaxCode).FirstOrDefault();
                    var headers = new HeadersRequest
                    {
                        Username = loginInfo.ParameterValue.Split(';')[0],
                        Password = loginInfo.ParameterValue.Split(';')[1],
                        Taxcode = taxtcode
                    };
                    var response = hilo.CreateInvoiceMultiple(billElectronic, headers);

                    respone.Status = 1;
                    respone.Message = "OK";
                    respone.Data = response;
                    return createResponse();
                }
                else
                {
                    throw new ArgumentException("Không tìm thấy thông tin hóa đơn trong bảng Bill_ElectronicBill.");
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
        [Route("DeleteLog")]
        public HttpResponseMessage DeleteLog(decimal BillId)
        {
            try
            {
                var billLog = _dbContext.App_Log.Where(i => i.BillID == BillId && i.Status == false && i.TypeLog == AppLogTypes.HILO).FirstOrDefault();
                if (billLog != null)
                {
                    _dbContext.App_Log.Remove(billLog);
                    _dbContext.SaveChanges();
                }
                else
                {
                    throw new ArgumentException("Không tồn tại hóa đơn này trong bảng log.");
                }

                respone.Status = 1;
                respone.Message = "Đã xóa thành công.";
                respone.Data = null;
                return createResponse();
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"{ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }
    }
}
