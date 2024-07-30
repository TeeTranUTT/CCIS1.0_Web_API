using CCIS_BusinessLogic;
using CCIS_BusinessLogic.DTO.Sms;
using CCIS_BusinessLogic.DTO.Zalo;
using CCIS_DataAccess;
using ES.CCIS.Host.Helpers;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using static CCIS_BusinessLogic.DefaultBusinessValue;

namespace ES.CCIS.Host.Controllers.CauHinh
{
    [Authorize]
    [RoutePrefix("api/TemplateZalo")]
    public class TemplateZaloController : ApiBaseController
    {
        private readonly CCISContext _dbContext;
        private readonly Business_Sms _businessSms;

        public TemplateZaloController()
        {
            _dbContext = new CCISContext();
            _businessSms = new Business_Sms(_dbContext);
        }

        [HttpGet]
        [Route("EditZaloTemplate")]
        public HttpResponseMessage EditZaloTemplate(int templateId)
        {
            try
            {
                var template = _dbContext.Sms_Template.Where(p => p.SmsTemplateId == templateId).FirstOrDefault();
                ZaloTemplateModel model = new ZaloTemplateModel();
                if (template != null)
                {
                    model.Content = JsonConvert.DeserializeObject<ZaloTemplateContent>(template.TemplateContent);
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
        public HttpResponseMessage CreateTemplateZalo(ZaloTemplateModel template)
        {
            try
            {
                string validateMsg = "";
                validateMsg = validateTemplate(template);
                if (!string.IsNullOrEmpty(validateMsg))
                {
                    throw new ArgumentException($"{validateMsg}");
                }

                var userCreated = TokenHelper.GetUserIdFromToken();
                AddOrUpdateTemplateModel model = new AddOrUpdateTemplateModel
                {
                    AppSend = APP_SEND.ZALO,
                    UnicodeSend = true,
                    DepartmentId = template.DepartmentId,
                    SmsTypeId = template.SmsTypeId,
                    TemplateName = template.TemplateName,
                    CreateBy = userCreated,
                    TemplateContent = JsonConvert.SerializeObject(template.Content)
                };
                _businessSms.AddSms_Template(model);

                var templateZalo = _dbContext.Sms_Template.Where(p => p.TemplateName == model.TemplateName).FirstOrDefault();

                if (templateZalo != null)
                {
                    respone.Status = 1;
                    respone.Message = "Thêm cấu hình zalo thành công.";
                    respone.Data = templateZalo.SmsTemplateId;
                    return createResponse();
                }
                else
                {
                    throw new ArgumentException("Thêm cấu hình zalo không thành công.");
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
        [Route("EditTemplateZalo")]
        public HttpResponseMessage EditTemplateZalo(ZaloTemplateModel template)
        {
            try
            {
                string validateMsg = "";
                if (!string.IsNullOrEmpty(validateMsg))
                {
                    throw new ArgumentException($"{validateMsg}");
                }
                var user = TokenHelper.GetUserIdFromToken();
                AddOrUpdateTemplateModel model = new AddOrUpdateTemplateModel
                {
                    SmsTemplateId = template.TemplateId,
                    AppSend = APP_SEND.ZALO,
                    UnicodeSend = true,
                    DepartmentId = template.DepartmentId,
                    SmsTypeId = template.SmsTypeId,
                    TemplateName = template.TemplateName,
                    ModifiedBy = user,
                    TemplateContent = JsonConvert.SerializeObject(template.Content)
                };
                _businessSms.EditSms_Template(model);

                respone.Status = 1;
                respone.Message = "Chỉnh sửa cấu hình zalo thành công.";
                respone.Data = model.SmsTemplateId;
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
        private string validateTemplate(ZaloTemplateModel template)
        {
            // Check nội dung
            if (template.Content.Texts == null || !template.Content.Texts.Any())
            {
                return "Bắt buộc phải có nội dung";
            }
            else if (template.Content.Texts.Any(value => string.IsNullOrEmpty(value)))
            {
                return "Nội dung phải có dữ liệu vui lòng không để trống";
            }
            // Check bảng
            if (template.Content.Table != null && template.Content.Table.Any())
            {
                if (template.Content.Table.Any(value => string.IsNullOrEmpty(value.Key) || string.IsNullOrEmpty(value.Params)))
                {
                    return "Tên hàng hoặc tham số thêm vào không được để trống";
                }
            }
            // Check nút bấm
            if (template.Content.Buttons != null && template.Content.Buttons.Any())
            {
                if (template.Content.Buttons.Any(value => string.IsNullOrEmpty(value.Name) || string.IsNullOrEmpty(value.Value)))
                {
                    return "Tên nút hoặc đường dẫn thêm vào không được để trống";
                }
            }
            return null;
        }

        #region Class
        public class ZaloTemplateModel
        {
            public int TemplateId { get; set; }
            public int SmsTypeId { get; set; }
            public int DepartmentId { get; set; }
            public string TemplateName { get; set; }
            public ZaloTemplateContent Content { get; set; }

        }
        #endregion

    }
}
