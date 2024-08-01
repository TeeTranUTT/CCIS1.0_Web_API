using CCIS_BusinessLogic;
using CCIS_DataAccess;
using CCIS_BusinessLogic.CustomBusiness.BillEdit_TaxInvoice;
using ES.CCIS.Host.Helpers;
using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Web.Configuration;
using System.Web.Http;
using System.IO;
using System.Xml;
using System.Text;

namespace ES.CCIS.Host.Controllers.HoaDon
{
    [Authorize]
    [RoutePrefix("api/ConvertBill")]
    public class ConvertBillController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly CCISContext _dbContext;

        public ConvertBillController()
        {
            _dbContext = new CCISContext();
        }

        [HttpGet]
        [Route("Search_Bill")]
        public HttpResponseMessage Search_Bill(string customerCode, DateTime? date, [DefaultValue(1)] int pageNumber)
        {
            try
            {
                var userInfo = TokenHelper.GetUserInfoFromRequest();
                var listDepartment = DepartmentHelper.GetChildDepIdsByUser(userInfo.UserName);

                var query = (from b_eTro in _dbContext.Bill_ElectronicBill
                             join b_tInv in _dbContext.Bill_TaxInvoice on b_eTro.BillId equals b_tInv.BillId into table1
                             from tbl1 in table1.DefaultIfEmpty()
                             join b_eLec in _dbContext.Bill_ElectricityBill on b_eTro.BillId equals b_eLec.BillId into table2
                             from tbl2 in table2.DefaultIfEmpty()
                             join b_adjust in _dbContext.Bill_ElectricityBillAdjustment on b_eTro.BillId equals b_adjust.BillAdjustmentId into table3
                             from tbl3 in table3.DefaultIfEmpty().Where(i => i.AdjustmentType == "LL").DefaultIfEmpty()
                             where listDepartment.Contains(b_eTro.DepartmentId)
                             && !(b_eTro.SignValue_XML == null)
                             select new BillConvertViewModel
                             {
                                 CustomerCode = !string.IsNullOrEmpty(tbl1.CustomerCode) ? tbl1.CustomerCode : tbl2.CustomerCode,
                                 CustomerName = !string.IsNullOrEmpty(tbl1.CustomerName) ? tbl1.CustomerName : tbl2.CustomerName,
                                 Address = !string.IsNullOrEmpty(tbl1.TaxInvoiceAddress) ? tbl1.TaxInvoiceAddress : tbl2.BillAddress,
                                 DeparmentId = !string.IsNullOrEmpty(tbl1.DepartmentId.ToString()) ? tbl1.DepartmentId : tbl2.DepartmentId,
                                 Bill_Type = b_eTro.BillType,
                                 BillId = tbl3 != null ? (int)tbl3.BillId : (int)b_eTro.BillId,
                                 SerialNumber = tbl3 != null ? tbl3.SerialNumber : b_eTro.SerialNumber,
                                 SerialCode = b_eTro.SerialCode,
                                 Subtotal = tbl3 != null ? tbl3.SubTotal : !string.IsNullOrEmpty(tbl1.SubTotal.ToString()) ? tbl1.SubTotal : tbl2.SubTotal,
                                 VAT = tbl3 != null ? tbl3.VAT : !string.IsNullOrEmpty(tbl1.VAT.ToString()) ? tbl1.VAT : tbl2.VAT,
                                 Total = tbl3 != null ? tbl3.Total : !string.IsNullOrEmpty(tbl1.Total.ToString()) ? tbl1.Total : tbl2.Total,
                                 Month = b_eTro.Month,
                                 Year = b_eTro.Year,
                                 isTransformBill =
                                 tbl3 != null ? _dbContext.Bill_ElectronicBill.Any(b => b.BillId == tbl3.BillId && b.ConvertUser == null && b.ConvertDate == null)
                                 : (b_eTro.ConvertUser == null && b_eTro.ConvertDate == null) ? true : false,
                                 Printed = b_eTro.Printed
                             }).Distinct();

                if (!string.IsNullOrEmpty(customerCode))
                {
                    query = query.Where(item => item.CustomerCode == customerCode);
                }

                if (date != null)
                {
                    query = query.Where(item => item.Month == date.Value.Month && item.Year == date.Value.Year);
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
                    Bills = paged.ToList()
                };
                respone.Status = 1;
                respone.Message = "Lấy danh sách hóa đơn thành công.";
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
        [Route("BillConvert")]
        public HttpResponseMessage BillConvert(EditBillContentModel model)
        {
            try
            {
                // check phát nữa cho sure
                string billType = _dbContext.Bill_ElectronicBill.Where(r => r.BillId == model.BillId).Select(r => r.BillType).FirstOrDefault();

                if (!string.IsNullOrEmpty(billType))
                {
                    var bill = _dbContext.Bill_TaxInvoice.Where(r => r.TaxInvoiceId == model.BillId).FirstOrDefault();

                    var data = new
                    {
                        TaxInvoiceId = model.BillId,
                        CustomerId = bill.CustomerId,
                        CustomerCode = bill.CustomerCode,
                        Address = bill.TaxInvoiceAddress,
                        SignValueDate = bill.Create_SignValue_XML,
                        Tatol = bill.Total,
                        Name = bill.CustomerName,
                        SerialCode = bill.SerialCode,
                        SerialNumber = bill.SerialNumber,
                    };

                    respone.Status = 1;
                    respone.Message = "OK";
                    respone.Data = data;
                    return createResponse();
                }
                else
                {
                    throw new ArgumentException($"Hóa đơn điện có BillId {model.BillId} không tồn tại.");
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
        //ToDo: Chưa xử lý được api Print_Bill 
        //[HttpGet]
        //[Route("Print_Bill")]
        //public HttpResponseMessage Print_Bill(int BIllId, string BillType)
        //{
        //    try
        //    {
        //        using (var _dbContext = new CCISContext())
        //        {
        //            // Lấy file XML
        //            string getFileXML = _dbContext.Bill_ElectronicBill
        //                .Where(item => item.BillId == BIllId)
        //                .Select(item => item.SignValue_XML)
        //                .FirstOrDefault();
        //            // Lấy file XSL
        //            string getFileXSL = _dbContext.Category_ElectronicBillForm
        //                .Where(item => item.BillType == BillType)
        //                .Select(item => item.XML_BillConvert)
        //                .FirstOrDefault();

        //            string error = "";
        //            byte[] pdfBuffer = null;
        //            var tempPath = Server.MapPath("~/UploadFoldel");
        //            if (!string.IsNullOrEmpty(getFileXML) && !string.IsNullOrEmpty(getFileXSL))
        //            {
        //                Business_ConvertBill _bussConvertBill = new Business_ConvertBill();
        //                //Convert XSL to byte
        //                byte[] byteXSL = Encoding.UTF8.GetBytes(getFileXSL);
        //                //Convert XML to byte
        //                byte[] byteXML = Encoding.UTF8.GetBytes(getFileXML);

        //                // format XML
        //                //byte[] buffer = (byte[])ds.Tables[0].Rows[0]["HOA_DON_XML"];
        //                MemoryStream m = new MemoryStream();
        //                m.Write(byteXML, 0, byteXML.Length);
        //                m.Position = 0;
        //                XmlDocument hdn = new XmlDocument();
        //                hdn.Load(m);
        //                byteXML = m.ToArray();
        //                m.Close();
        //                string billContentHtml = _bussConvertBill.TransXSLToHTML00(byteXSL, byteXML, "", ref error);

        //                if (!string.IsNullOrEmpty(billContentHtml) && error == "")
        //                {

        //                    pdfBuffer = _bussConvertBill.ConvertHTML_PDF(billContentHtml, BIllId, tempPath);
        //                }
        //            }
        //            MemoryStream pdfStream = new MemoryStream();
        //            pdfStream.Write(pdfBuffer, 0, pdfBuffer.Length);
        //            pdfStream.Position = 0;
        //            return File(pdfStream, "application/pdf");                    

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
        [Route("UpdatePrinter")]
        public HttpResponseMessage UpdatePrinter(int billId)
        {
            try
            {
                // cập nhật trang thái in
                var billElectronic = _dbContext.Bill_ElectronicBill.FirstOrDefault(r => r.BillId == billId);
                if (billElectronic != null)
                {
                    billElectronic.Printed = true;
                    _dbContext.SaveChanges();
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
        [Route("SaveBillConvert")]
        public HttpResponseMessage Save_Bill_Convert(decimal BillId, string ConvertName, string customerCode, DateTime Date, DateTime DateConvert, [DefaultValue(1)] int page, string Note, string ConvertPosition)
        {
            try
            {
                using (var _dbContextTransaction = _dbContext.Database.BeginTransaction())
                {
                    try
                    {
                        var bill = _dbContext.Bill_ElectronicBill.FirstOrDefault(r => r.BillId == BillId);
                        bill.ConvertUser = ConvertName;
                        bill.ConvertDate = DateConvert;
                        _dbContext.SaveChanges();
                        _dbContextTransaction.Commit();

                        respone.Status = 1;
                        respone.Message = "Chuyển đổi hóa đơn thành công.";
                        respone.Data = null;
                        return createResponse();
                    }
                    catch
                    {
                        _dbContextTransaction.Rollback();
                        throw new ArgumentException("Chuyển đổi hóa đơn không thành công.");
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
    }
}
