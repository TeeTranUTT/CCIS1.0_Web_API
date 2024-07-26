using CCIS_BusinessLogic;
using CCIS_BusinessLogic.DTO.Hilo;
using CCIS_DataAccess;
using ES.CCIS.Host.Helpers;
using ES.CCIS.Host.Models;
using ES.CCIS.Host.Models.EnumMethods;
using Newtonsoft.Json;
using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web.Configuration;
using System.Web.Http;
using static CCIS_BusinessLogic.DefaultBusinessValue;

namespace ES.CCIS.Host.Controllers.CauHinh
{
    [Authorize]
    [RoutePrefix("api/TemplateXslt")]
    public class TemplateXsltController : ApiBaseController
    {
        private readonly CCISContext _dbContext;

        public TemplateXsltController()
        {
            _dbContext = new CCISContext();
        }

        [HttpGet]
        [Route("TemplateXsltManage")]
        public HttpResponseMessage TemplateXsltManage([DefaultValue(-1)] int departmentID)
        {
            try
            {
                var list = new List<TemplateXsltModel>();
                if (departmentID != -1)
                {
                    var hilo = new Business_Hilo();
                    list = (from a in _dbContext.Category_ElectronicBillForm
                            join b in _dbContext.Category_Serial
                            on new { p1 = a.DepartmentId, p2 = a.BillType } equals new { p1 = b.DepartmentId, p2 = b.BillType }
                            join c in _dbContext.Administrator_Department on a.DepartmentId equals c.DepartmentId
                            where (departmentID == -1 || a.DepartmentId == departmentID) && a.Status == 1 && b.Status
                            select new TemplateXsltModel
                            {
                                Bill_FormId = a.Bill_FormId,
                                DepartmentId = a.DepartmentId,
                                DepartmentName = c.DepartmentName,
                                BillType = a.BillType,
                                VersionBillForm = a.VersionBillForm,
                                XML_Bill = a.XML_Bill,
                                XML_Notification = a.XML_Notification,
                                XML_BillConvert = a.XML_BillConvert,
                                SpecimenNumber = b.SpecimenNumber,
                                SpecimenCode = b.SpecimenCode,
                                TaxCode = b.TaxCode,
                                StartDate = b.ActiveDate,
                                EndDate = b.EndDate,
                                Status = a.Status == 1 ? true : false
                            }
                               ).OrderBy(i => i.DepartmentId).ToList();
                    var loginInfo = _dbContext.Administrator_Parameter.FirstOrDefault(i => i.DepartmentId == departmentID && i.ParameterName == Administrator_Parameter_Common.HILO_AUTHENTICATION);
                    if (loginInfo == null)
                    {
                        throw new ArgumentException("Chưa cấu hình Parameter đăng nhập vào Hilo, vui lòng kiểm tra lại.");                       
                    }
                    var hiloVersion = hilo.GetInfoRealseXsltByTaxCode(new HeadersRequest
                    {
                        Username = loginInfo.ParameterValue.Split(';')[0],
                        Password = loginInfo.ParameterValue.Split(';')[1],
                        Taxcode = list.Select(i => i.TaxCode).FirstOrDefault()
                    });
                    if (hiloVersion.status)
                    {
                        var result = JsonConvert.DeserializeObject<List<HiloVersionTemplate>>(hiloVersion.result);                        
                    }                                                            
                }               

                respone.Status = 1;
                respone.Message = "Lấy danh sách khách hàng thành công.";
                respone.Data = list;
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
        [Route("CreatOrEditTemplateXslt")]
        public HttpResponseMessage CreatOrEditTemplateXslt(CreatOrEditTemplateXsltInput input)
        {
            try
            {
                var billForm = (from a in _dbContext.Category_ElectronicBillForm
                                join b in _dbContext.Category_Serial
                                on new { p1 = a.DepartmentId, p2 = a.BillType } equals new { p1 = b.DepartmentId, p2 = b.BillType }
                                join c in _dbContext.Administrator_Department on a.DepartmentId equals c.DepartmentId
                                where (a.DepartmentId == input.DepartmentId) && a.Status == EnumMethod.TrangThai.KichHoat && a.Bill_FormId == input.IdBillForm && b.Status
                                select new
                                {
                                    b.SpecimenNumber,
                                    a.VersionBillForm,
                                    b.BillType,
                                    a.XML_Bill,
                                    b.TaxCode
                                }).FirstOrDefault();
                if (billForm == null)
                {
                    throw new ArgumentException($"Không có mẫu tương ứng với ID là: {input.IdBillForm}");                    
                }
                var hilo = new Business_Hilo();
                var requestTemplate = new CreateTemplateXsltRequest
                {
                    InvPattern = billForm.SpecimenNumber,
                    TempName = billForm.BillType,
                    Version = billForm.VersionBillForm,
                    Xslt = Convert.ToBase64String(Encoding.UTF8.GetBytes(billForm.XML_Bill))
                };
                var loginInfo = _dbContext.Administrator_Parameter.FirstOrDefault(i => i.DepartmentId == input.DepartmentId && i.ParameterName == Administrator_Parameter_Common.HILO_AUTHENTICATION);
                if (loginInfo == null)
                {
                    throw new ArgumentException("Chưa cấu hình Parameter đăng nhập vào Hilo, vui lòng kiểm tra lại.");                    
                }
                var headers = new HeadersRequest
                {
                    Username = loginInfo.ParameterValue.Split(';')[0],
                    Password = loginInfo.ParameterValue.Split(';')[1],
                    Taxcode = billForm.TaxCode
                };
                HiloResponseRaw result = null;
                if (input.TypeOfAction == "C")
                {
                    result = hilo.CreateTemplateXsltToHilo(requestTemplate, headers);
                }
                else if (input.TypeOfAction == "U")
                {
                    result = hilo.EditTemplateXsltToHilo(requestTemplate, headers);
                }
                if (result.status)
                {
                    respone.Status = 1;
                    respone.Message = "Cập nhật thành công.";
                    respone.Data = null;
                    return createResponse();                    
                }
                else
                {
                    throw new ArgumentException($"Cập nhật mẫu không thành công {result.result}");                   
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
        [Route("EditTemplateXslt")]
        public HttpResponseMessage EditTemplateXslt(EditTemplateXsltInput input)
        {
            try
            {
                var hilo = new Business_Hilo();
                var bill = _dbContext.Category_ElectronicBillForm.Where(i => i.DepartmentId == input.DepartmentId && i.Bill_FormId == input.Bill_FormId).FirstOrDefault();
                if (bill != null)
                {
                    switch (input.Type)
                    {
                        case 11:
                            bill.XML_Bill = input.Xslt;
                            break;
                        case 12:
                            bill.XML_Notification = input.Xslt;
                            break;
                        case 13:
                            bill.XML_BillConvert = input.Xslt;
                            break;
                    }
                    _dbContext.SaveChanges();

                    respone.Status = 1;
                    respone.Message = "Cập nhật thành công.";
                    respone.Data = null;
                    return createResponse();                    
                }
                else
                {
                    throw new ArgumentException($"Không tìm thấy mẫu Xslt với ID tương ứng là: {input.Bill_FormId}");                    
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
        [Route("EditTemplateXsltHilo")]
        public HttpResponseMessage EditTemplateXsltHilo(EditTemplateXsltHiloInput input)
        {
            try
            {
                var hilo = new Business_Hilo();
                var requestTemplate = new CreateTemplateXsltRequest
                {
                    InvPattern = input.InvPattern,
                    TempName = input.TempName,
                    Version = input.Version,
                    Xslt = Convert.ToBase64String(Encoding.UTF8.GetBytes(input.XsltHilo))
                };
                var loginInfo = _dbContext.Administrator_Parameter.FirstOrDefault(i => i.DepartmentId == input.DepartmentId && i.ParameterName == Administrator_Parameter_Common.HILO_AUTHENTICATION);
                if (loginInfo == null)
                {
                    throw new ArgumentException("Chưa cấu hình Parameter đăng nhập vào Hilo, vui lòng kiểm tra lại.");                    
                }
                var headers = new HeadersRequest
                {
                    Username = loginInfo.ParameterValue.Split(';')[0],
                    Password = loginInfo.ParameterValue.Split(';')[1],
                    Taxcode = input.Taxcode
                };
                var result = hilo.EditTemplateXsltToHilo(requestTemplate, headers);
                if (result.status)
                {
                    respone.Status = 1;
                    respone.Message = "Cập nhật thành công.";
                    respone.Data = null;
                    return createResponse();                   
                }
                else
                {
                    throw new ArgumentException($"Cập nhật mẫu không thành công {result.result}");                    
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
        [Route("GetXsltFormHilo")]
        public HttpResponseMessage GetXsltFormHilo(GetXsltFormHiloInput input)
        {
            try
            {
                var hilo = new Business_Hilo();
                var loginInfo = _dbContext.Administrator_Parameter.FirstOrDefault(i => i.DepartmentId == input.DepartmentId && i.ParameterName == Administrator_Parameter_Common.HILO_AUTHENTICATION);
                if (loginInfo == null)
                {
                    throw new ArgumentException("Chưa cấu hình Parameter đăng nhập vào Hilo, vui lòng kiểm tra lại.");                    
                }
                var headers = new HeadersRequest
                {
                    Username = loginInfo.ParameterValue.Split(';')[0],
                    Password = loginInfo.ParameterValue.Split(';')[1],
                    Taxcode = input.Request.taxcode
                };

                var response = hilo.GetInfoTemplate(input.Request.data, headers);

                respone.Status = 1;
                respone.Message = "OK";
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
        [Route("GetListXsltVersionHilo")]
        public HttpResponseMessage GetListXsltVersionHilo(HeadersRequest request)
        {
            try
            {
                var hilo = new Business_Hilo();
                var listByTaxCode = hilo.GetInfoRealseXsltByTaxCode(request);

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = listByTaxCode;
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

        #region class
        public class TemplateXsltModel
        {
            public int Bill_FormId { get; set; }
            public int DepartmentId { get; set; }
            public string DepartmentName { get; set; }
            public string BillType { get; set; }
            public string VersionBillForm { get; set; }
            public string XML_Bill { get; set; }
            public string XML_Notification { get; set; }
            public string XML_BillConvert { get; set; }
            public string SpecimenNumber { get; set; }
            public string SpecimenCode { get; set; }
            public string TaxCode { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public bool Status { get; set; }
        }

        public class CreatOrEditTemplateXsltInput
        {
            public int IdBillForm { get; set; }
            public int DepartmentId { get; set; }
            public string TypeOfAction { get; set; }
        }

        public class EditTemplateXsltInput
        {
            public int DepartmentId { get; set; }
            public int Bill_FormId { get; set; }
            public string Xslt { get; set; }
            public int Type { get; set; }
        }

        public class EditTemplateXsltHiloInput
        {
            public string InvPattern { get; set; }
            public string TempName { get; set; }
            public string Version { get; set; }
            public string Taxcode { get; set; }
            public string XsltHilo { get; set; }
            public int DepartmentId { get; set; }
        }

        public class GetXsltFormHiloInput
        {
            public RequestHilo Request { get; set; }
            public int DepartmentId { get; set; }
        }

        public class RequestHilo
        {
            public GetTemplateXsltRequest data { get; set; }
            public string taxcode { get; set; }
        }
        #endregion
    }
}
