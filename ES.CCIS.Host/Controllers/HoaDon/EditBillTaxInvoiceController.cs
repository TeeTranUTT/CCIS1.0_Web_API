using CCIS_BusinessLogic;
using CCIS_BusinessLogic.CustomBusiness.BillEdit_TaxInvoice;
using CCIS_BusinessLogic.CustomBusiness.Models;
using CCIS_BusinessLogic.CustomBusiness.TaxInVoice_Bill;
using CCIS_DataAccess;
using ES.CCIS.Host.Helpers;
using ES.CCIS.Host.Models;
using ES.CCIS.Host.Models.EnumMethods;
using ES.CCIS.Host.Models.HoaDon;
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
        private readonly Bussiness_TaxInVoice_Bill business_Index_Value = new Bussiness_TaxInVoice_Bill();

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

        //Hủy hóa đơn lập lại, cập nhật lại trạng thái hóa đơn được lập lại về hủy bỏ
        [HttpPost]
        [Route("DeleteTaxInvoice")]
        public HttpResponseMessage DeleteTaxInvoice(decimal TaxInvoiceId, string TypeOfFunction)
        {
            try
            {
                bool check = billEdit_TaxInvoiceBUS.DeleteTaxInvoice(TaxInvoiceId, TypeOfFunction);

                if (!check)
                {
                    throw new ArgumentException("Xóa hóa đơn lập lại không thành công.");
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
                respone.Message = $"{ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }

        //Hủy bỏ hóa đơn
        [HttpPost]
        [Route("CancelTaxInvoice")]
        public HttpResponseMessage CancelTaxInvoice(decimal taxInvoiceId)
        {
            try
            {
                var userId = TokenHelper.GetUserIdFromToken();

                var result = billEdit_TaxInvoiceBUS.CancelTaxInvoice(taxInvoiceId, userId);

                switch (result)
                {
                    case 0:
                        throw new ArgumentException("Hủy bỏ hóa đơn không thành công.");
                    case 1:
                        break;
                    case 2:
                        throw new ArgumentException("Không tồn tại hóa đơn trong bảng chấm nợ.");
                    default:
                        break;
                }

                if (result != 1)
                {
                    throw new ArgumentException("Hủy bỏ hóa đơn không thành công.");
                }
                else
                {
                    respone.Status = 1;
                    respone.Message = "Hủy bỏ hóa đơn thành công.";
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

        //Lập lại hóa đơn
        [HttpPost]
        [Route("CreateReTaxInvoice")]
        public HttpResponseMessage CreateReTaxInvoice(CreateReTaxInvoiceInput input)
        {
            using (var db = new CCISContext())
            {
                using (var dbContextTransaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var userId = TokenHelper.GetUserIdFromToken();

                        var dateNow = DateTime.Now;

                        var taxinvoice = db.Bill_TaxInvoice.FirstOrDefault(r => r.TaxInvoiceId == input.ReBillAdjustment.TaxInvoiceId);

                        var taxinvoiceDetail = db.Bill_TaxInvoiceDetail.Where(r => r.TaxInvoiceId == input.ReBillAdjustment.TaxInvoiceId).FirstOrDefault();

                        var subTotal = Math.Round(input.ListBillTaxAdjust.AsEnumerable().Sum(x => x.Total));
                        var Vat = Math.Round(subTotal * input.ReBillAdjustment.TaxRatio / 100);
                        var Total = Math.Round(subTotal + Vat);

                        var contractid = input.ReBillAdjustment.ContractId;
                        var contractdetailid = db.Concus_ContractDetail.FirstOrDefault(x => x.ContractId == contractid && x.EndDateCV == null)?.ContractDetailId;

                        /*
                        * Thêm mới dữ liệu bảng Bill_TaxInvoice
                        */
                        var bill_taxinvoice = new Bill_TaxInvoice
                        {
                            CustomerCode = input.ReBillAdjustment.CustomerCode,
                            ContractId = input.ReBillAdjustment.ContractId,
                            CreateUser = userId,
                            DepartmentId = input.ReBillAdjustment.DepartmentId,
                            IdDevice = null/*0*/,
                            SubTotal = subTotal,
                            VAT = Vat,
                            Year = input.ReBillAdjustment.Year,
                            Month = input.ReBillAdjustment.Month,
                            CreateDate = dateNow,
                            TaxRatio = input.ReBillAdjustment.TaxRatio,
                            BillType = input.ReBillAdjustment.BillType,
                            Total = Total,
                            CustomerId = input.ReBillAdjustment.CustomerId,
                            CustomerCode_Pay = input.ReBillAdjustment.CustomerCode_Pay,
                            TaxCode = input.ReBillAdjustment.TaxCode,
                            TaxInvoiceAddress = input.ReBillAdjustment.TaxInvoiceAddress,
                            Address_Pay = input.ReBillAdjustment.Address_Pay,
                            BankName = input.ReBillAdjustment.BankName,
                            BankAccount = input.ReBillAdjustment.BankAccount,
                            CustomerName = input.ReBillAdjustment.CustomerName,
                            CustomerId_Pay = input.ReBillAdjustment.CustomerId_Pay,
                            CustomerName_Pay = input.ReBillAdjustment.CustomerName_Pay
                        };

                        db.Bill_TaxInvoice.Add(bill_taxinvoice);

                        db.SaveChanges();

                        /*
                        * Thêm mới dữ liệu bảng Bill_TaxInvoiceDetail
                        */
                        foreach (var item in input.ListBillTaxAdjust)
                        {
                            var bill_TaxInvoiceDetail = new Bill_TaxInvoiceDetail
                            {
                                TaxInvoiceId = bill_taxinvoice.TaxInvoiceId,
                                DepartmentId = input.ReBillAdjustment.DepartmentId,
                                CustomerId = input.ReBillAdjustment.CustomerId,
                                CustomerCode = input.ReBillAdjustment.CustomerCode,
                                ServiceTypeId = item.ServiceTypeId,
                                FigureBookId = taxinvoiceDetail.FigureBookId,
                                Month = input.ReBillAdjustment.Month,
                                Year = input.ReBillAdjustment.Year,
                                Total = item.Total,
                                CreateUser = userId,
                                Amount = item.Amount,
                                TypeOfUnit = item.TypeOfUnit,
                                Price = item.Price,
                                ServiceName = item.ServiceName,
                                CreateDate = dateNow,
                                ContractDetailId = contractdetailid,
                            };

                            db.Bill_TaxInvoiceDetail.Add(bill_TaxInvoiceDetail);
                            db.SaveChanges();
                        }

                        /*
                         * Thêm mới bảng trung gian
                         */
                        var taxInvoiceAdjustment = new Bill_Taxinvoice_Adjustment
                        {
                            TaxInvoiceId = input.ReBillAdjustment.TaxInvoiceId,
                            TaxInvoice_AdjustmentId = bill_taxinvoice.TaxInvoiceId,
                            CustomerId = input.ReBillAdjustment.CustomerId,
                            DepartmentId = input.ReBillAdjustment.DepartmentId,
                            TypeOfFunction = "LL",
                            CreateDate = dateNow,
                            CreateUser = userId,
                        };

                        db.Bill_Taxinvoice_Adjustment.Add(taxInvoiceAdjustment);

                        //Cập nhật trang thái Liabilities_TrackDebt_TaxInvoice
                        var liaTax = db.Liabilities_TrackDebt_TaxInvoice.Where(x => x.TaxInvoiceId == input.ReBillAdjustment.TaxInvoiceId).FirstOrDefault();
                        liaTax.Status = 3;

                        db.SaveChanges();
                        dbContextTransaction.Commit();

                        respone.Status = 1;
                        respone.Message = "Lập lại hóa đơn thành công.";
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
            }
        }

        //Lập lại hóa đơn giống như chức năng thêm mới hóa đơn GTGT
        [HttpGet]
        [Route("CreateTaxInvoiceEditBill")]
        public HttpResponseMessage CreateTaxInvoiceEditBill(decimal taxInvoiceId)
        {
            try
            {
                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                if (taxInvoiceId != 0)
                {
                    using (var db = new CCISContext())
                    {
                        var _customerCode = (from taxInvoice in db.Bill_TaxInvoice
                                             where taxInvoice.TaxInvoiceId == taxInvoiceId
                                             select new { taxInvoice.CustomerCode }).FirstOrDefault();

                        var concusCustomer = (from customer in db.Concus_Customer.Where(x => x.CustomerCode == _customerCode.CustomerCode && x.Status == 1 && x.DepartmentId == departmentId)
                                              join contract in db.Concus_Contract
                                              on customer.CustomerId equals contract.CustomerId
                                              join service in db.Concus_ServicePoint
                                              on contract.ContractId equals service.ContractId
                                              select new { customer, contract, service }).FirstOrDefault();

                        var contractDetail = (from detail in db.Concus_ContractDetail
                                              join contract in db.Concus_Contract
                                              on detail.ContractId equals contract.ContractId
                                              where contract.CustomerId == concusCustomer.customer.CustomerId
                                              select detail).ToList();

                        List<CreateTaxInvoiceEditBillItem> billTax = new List<CreateTaxInvoiceEditBillItem>();

                        if (contractDetail.Count > 0)
                        {
                            contractDetail.ForEach(item =>
                            {
                                var bill = new CreateTaxInvoiceEditBillItem
                                {
                                    ContractId = concusCustomer.contract.ContractId,
                                    CustomCode = _customerCode.CustomerCode,
                                    CustomerId = concusCustomer.customer.CustomerId,
                                    ServicePointId = concusCustomer.service.PointId,
                                    TaxInvoiceId = taxInvoiceId,
                                    TaxCode = concusCustomer.customer.TaxCode,
                                    FigureBook = concusCustomer.service.FigureBookId,
                                    CustomName = concusCustomer.customer.Name,
                                    Address = concusCustomer.customer.Address,
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
                            var bill = new CreateTaxInvoiceEditBillItem
                            {
                                ContractId = concusCustomer.contract.ContractId,
                                CustomCode = _customerCode.CustomerCode,
                                CustomerId = concusCustomer.customer.CustomerId,
                                ServicePointId = concusCustomer.service.PointId,
                                TaxInvoiceId = taxInvoiceId,
                                TaxCode = concusCustomer.customer.TaxCode,
                                FigureBook = concusCustomer.service.FigureBookId,
                                CustomName = concusCustomer.customer.Name,
                                Address = concusCustomer.customer.Address,
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
                }
                else
                {
                    throw new ArgumentException("Dữ liệu đầu vào không thỏa mãn.");
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
        [Route("CreateTaxInvoiceEditBill")]
        public HttpResponseMessage CreateTaxInvoiceEditBill(CreateTaxInVoiceEditBillModel billTaxInvoiceItem)
        {
            try
            {
                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                bool result = business_Index_Value.CreateTaxInvoiceEditBill(billTaxInvoiceItem.listCreateEditBillTaxInvoiceItem, billTaxInvoiceItem.VAT, billTaxInvoiceItem.Total, User.Identity.Name, departmentId);

                if (result)
                {
                    respone.Status = 1;
                    respone.Message = "Thêm hóa đơn lập lại thành công.";
                    respone.Data = null;
                    return createResponse();
                }
                else
                {
                    throw new ArgumentException("Thêm hóa đơn lập lại không thành công yêu cầu thao tác lại.");
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
    }
}
