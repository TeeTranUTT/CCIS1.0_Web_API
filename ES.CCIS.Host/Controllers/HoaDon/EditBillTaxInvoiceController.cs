using CCIS_BusinessLogic;
using CCIS_BusinessLogic.CustomBusiness.BillEdit_TaxInvoice;
using CCIS_DataAccess;
using ES.CCIS.Host.Helpers;
using ES.CCIS.Host.Models;
using ES.CCIS.Host.Models.EnumMethods;
using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Web.Configuration;
using System.Web.Http;

namespace ES.CCIS.Host.Controllers.HoaDon
{
    [Authorize]
    [RoutePrefix("api/Bill/EditBillTaxInvoice")]
    public class EditBillTaxInvoiceController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_Administrator_Department businessDepartment = new Business_Administrator_Department();
        private readonly BillEdit_TaxInvoiceBUS billEdit_TaxInvoiceBUS = new BillEdit_TaxInvoiceBUS();

        #region Hóa đơn sửa sai
        [HttpGet]
        [Route("SearchBillEdit")]
        public HttpResponseMessage SearchBillEdit(string customerCode, DateTime? Date, [DefaultValue(1)] int pageNumber, string seriNumber, string Type)
        {
            try
            {
                var userInfo = TokenHelper.GetUserInfoFromRequest();
                var lstDepartments = DepartmentHelper.GetChildDepIdsByUser(userInfo.UserName);

                if (Date == null)
                    Date = DateTime.Now;

                var query = billEdit_TaxInvoiceBUS.getLstBill(Date.Value, customerCode, seriNumber, lstDepartments, Type);

                var paged = (IPagedList<BillConvertViewModel>)query.ToPagedList(pageNumber, pageSize);

                var response = new
                {
                    paged.PageNumber,
                    paged.PageSize,
                    paged.TotalItemCount,
                    paged.PageCount,
                    paged.HasNextPage,
                    paged.HasPreviousPage,
                    BillConvert = paged.ToList()
                };
                respone.Status = 1;
                respone.Message = "Lấy danh sách hóa đơn sửa sai thành công.";
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
        [Route("BillTaxInvoice")]
        public HttpResponseMessage BillTaxInvoice(decimal taxInvoiceId)
        {
            try
            {
                var bill = billEdit_TaxInvoiceBUS.BillTaxInvoice(taxInvoiceId);

                respone.Status = 1;
                respone.Message = "Lấy thông tin hóa đơn sửa sai thành công.";
                respone.Data = bill;
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
        [Route("BillEditContent")]
        public HttpResponseMessage BillEditContent(EditBillContentModel model)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    var userId = TokenHelper.GetUserIdFromToken();

                    if ((model.SubTotal + model.VAT) != model.Total)
                    {
                        throw new ArgumentException("Tiền trên hóa đơn không đúng! Thêm mới hóa đơn sửa sai không thành công.");
                    }
                    else
                    {
                        var create = billEdit_TaxInvoiceBUS.CreateBill(model, userId);
                        if (create)
                        {
                            respone.Status = 1;
                            respone.Message = "Thêm mới hóa đơn sửa sai thành công.";
                            respone.Data = null;
                            return createResponse();
                        }
                        else
                        {
                            throw new ArgumentException("Thêm mới hóa đơn sửa sai không thành công.");
                        }
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
        #endregion

        #region Hóa đơn lập lại
        //Tìm kiếm hóa đơn lập lại
        [HttpGet]
        [Route("SearchBillDelected")]
        public HttpResponseMessage SearchBillDelected(string customerCode, DateTime? Date, [DefaultValue(1)] int pageNumber)
        {
            try
            {
                var userInfo = TokenHelper.GetUserInfoFromRequest();
                var lstDepartments = DepartmentHelper.GetChildDepIdsByUser(userInfo.UserName);

                var lstBill = billEdit_TaxInvoiceBUS.getListBillAdjustment(customerCode, Date, lstDepartments);

                var paged = (IPagedList<BillAdjustmentModel>)lstBill.ToPagedList(pageNumber, pageSize);

                var response = new
                {
                    paged.PageNumber,
                    paged.PageSize,
                    paged.TotalItemCount,
                    paged.PageCount,
                    paged.HasNextPage,
                    paged.HasPreviousPage,
                    BillAdjustment = paged.ToList()
                };
                respone.Status = 1;
                respone.Message = "Lấy danh sách hóa đơn lập lại thành công.";
                respone.Data = response;
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
        [Route("")]

        #endregion
    }
}
