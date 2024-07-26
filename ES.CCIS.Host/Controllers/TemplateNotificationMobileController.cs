using CCIS_BusinessLogic;
using CCIS_BusinessLogic.DTO.Sms;
using CCIS_BusinessLogic.DTO.SmsAppCskh;
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
using System.Web.Configuration;
using System.Web.Http;
using static CCIS_BusinessLogic.DefaultBusinessValue;

namespace ES.CCIS.Host.Controllers
{
    [Authorize]
    [RoutePrefix("api/TemplateNotificationMobile")]
    public class TemplateNotificationMobileController : ApiBaseController
    {
        private readonly Business_Administrator_Department businessDepartment = new Business_Administrator_Department();
        private readonly CCISContext _ccisContext;
        private readonly Business_Sms _businessSms;

        public TemplateNotificationMobileController()
        {
            _ccisContext = new CCISContext();
            _businessSms = new Business_Sms(_ccisContext);
        }

        [HttpGet]
        [Route("EditNotificationMobileTemplate")]
        public HttpResponseMessage EditNotificationMobileTemplate(int templateId)
        {
            try
            {
                var template = _ccisContext.Sms_Template.Where(p => p.SmsTemplateId == templateId).FirstOrDefault();
                TemplateNotificationsMobileModel model = new TemplateNotificationsMobileModel();
                if (template != null)
                {
                    model.Content = JsonConvert.DeserializeObject<TemplateNotificationsMobileContent>(template.TemplateContent);
                    model.TemplateId = templateId;
                    model.SmsTypeId = template.SmsTypeId;
                    model.TemplateName = template.TemplateName;
                }

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

        [HttpPost]
        [Route("CreateTemplateZalo")]
        public HttpResponseMessage CreateTemplateZalo(TemplateNotificationsMobileModel template)
        {
            try
            {
                string validateMsg = "";
                validateMsg = validateTemplate(template);

                if (!string.IsNullOrEmpty(validateMsg))
                {
                    throw new ArgumentException($"{validateMsg}");
                }

                var userId = TokenHelper.GetUserIdFromToken();

                AddOrUpdateTemplateModel model = new AddOrUpdateTemplateModel
                {
                    SmsTemplateId = template.TemplateId,
                    AppSend = APP_SEND.AppCSKH,
                    UnicodeSend = true,
                    DepartmentId = template.DepartmentId,
                    SmsTypeId = template.SmsTypeId,
                    TemplateName = template.TemplateName,
                    ModifiedBy = userId,
                    TemplateContent = JsonConvert.SerializeObject(template.Content)
                };
                _businessSms.EditSms_Template(model);

                respone.Status = 1;
                respone.Message = "Tạo mẫu thành công.";
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

        [HttpPost]
        [Route("EditTemplateZalo")]
        public HttpResponseMessage EditTemplateZalo(TemplateNotificationsMobileModel template)
        {
            try
            {
                string validateMsg = "";
                validateMsg = validateTemplate(template);

                if (!string.IsNullOrEmpty(validateMsg))
                {
                    throw new ArgumentException($"{validateMsg}");
                }

                var userId = TokenHelper.GetUserIdFromToken();

                AddOrUpdateTemplateModel model = new AddOrUpdateTemplateModel
                {
                    SmsTemplateId = template.TemplateId,
                    AppSend = APP_SEND.AppCSKH,
                    UnicodeSend = true,
                    DepartmentId = template.DepartmentId,
                    SmsTypeId = template.SmsTypeId,
                    TemplateName = template.TemplateName,
                    ModifiedBy = userId,
                    TemplateContent = JsonConvert.SerializeObject(template.Content)
                };
                _businessSms.EditSms_Template(model);

                respone.Status = 1;
                respone.Message = "OK";
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

        private string validateTemplate(TemplateNotificationsMobileModel template)
        {
            // Check nội dung
            if (string.IsNullOrEmpty(template.Content.Title))
            {
                return "Vui lòng không để trống tiêu đề";
            }
            else if (string.IsNullOrEmpty(template.Content.Body))
            {
                return "Nội dung phải có dữ liệu vui lòng không để trống";
            }
            return null;
        }

        #region Class
        public class TemplateNotificationsMobileModel
        {
            public int TemplateId { get; set; }
            public int SmsTypeId { get; set; }
            public int DepartmentId { get; set; }
            public string TemplateName { get; set; }
            public TemplateNotificationsMobileContent Content { get; set; }
        }
        #endregion
    }
}
