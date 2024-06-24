using CCIS_DataAccess;
using ES.CCIS.Host.Filters;
using ES.CCIS.Host.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;

namespace ES.CCIS.Host.Controllers
{
    [InitializeAdministrator]
    public class XacThucController : ApiController
    {
        [Authorize]
        [HttpGet]
        [Route("api/XacThuc/LayThongTinNguoiDung")]
        public IHttpActionResult GetUserInfo()
        {
            var userInfo = TokenHelper.GetUserInfoFromRequest();

            if (userInfo == null)
            {
                return Unauthorized();
            }

            return Ok(userInfo);

        }
    }
}
