using ES.CCIS.Host.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ES.CCIS.Host.Controllers
{
    public class ApiBaseController : ApiController
    {
        public ApiBaseController()
        {
            respone = new ResponseModel();
        }

        protected ResponseModel respone;

        public HttpResponseMessage createResponse() {
            return Request.CreateResponse(HttpStatusCode.OK, respone, Configuration.Formatters.JsonFormatter);
        }
    }
}
