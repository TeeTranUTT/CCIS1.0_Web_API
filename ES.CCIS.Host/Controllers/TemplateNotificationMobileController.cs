using CCIS_BusinessLogic;
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


    }
}
