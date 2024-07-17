using CCIS_BusinessLogic.CustomBusiness.LiabilitiesTrans;
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


namespace ES.CCIS.Host.Controllers.HoaDon.CongNo
{
    [Authorize]
    [RoutePrefix("api/LiabilitiesTrans")]
    public class LiabilitiesTransController : ApiBaseController
    {
        private readonly LiabilitiesTransBUS liabilitiesTransBUS = new LiabilitiesTransBUS();

        [HttpGet]
        [Route("Confirm_Transfer")]
        public HttpResponseMessage Confirm_Transfer(DateTime? DateFrom, DateTime? DateTo, string customerCode, string customerName, string EmployeeCode)
        {
            try
            {
                int departmentId = TokenHelper.GetDepartmentIdFromToken();
                var listDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);

                IEnumerable<ConfirmTransfer_ViewModel> lstTransfer;                

                if (DateTo == null || DateFrom == null)
                {
                    lstTransfer = new List<ConfirmTransfer_ViewModel>();
                }
                else
                {
                    lstTransfer = liabilitiesTransBUS.GetLiabilitiesTrans(DateFrom.Value, DateTo.Value, customerCode, customerName, EmployeeCode, listDepartmentId);
                }

                var model = lstTransfer.ToList();

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
        [Route("ConfirmBillTransfer")]
        public HttpResponseMessage ConfirmBillTransfer(List<decimal> listAdjustId)
        {
            try
            {
                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var userInfo = TokenHelper.GetUserInfoFromRequest();
                var result = liabilitiesTransBUS.DebtRelief(listAdjustId, departmentId, userInfo.UserName);
                if (Convert.ToBoolean(result["status"]))
                {
                    respone.Status = 1;
                    respone.Message = "Cập nhật công nợ thành công.";
                    respone.Data = null;
                    return createResponse();                    
                }
                else
                {
                    throw new ArgumentException($"{result["message"].ToString()}");                    
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

        [HttpGet]
        [Route("GetResultMappingEBank")]
        public HttpResponseMessage GetResultMappingEBank(List<EBankingReq> lstRequest)
        {
            try
            {
                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                
                if (departmentId != 0)
                {
                    var model = liabilitiesTransBUS.GetEBanking(lstRequest, departmentId).ToList();

                    respone.Status = 1;
                    respone.Message = "OK";
                    respone.Data = model;
                    return createResponse();                    
                }
                else
                {
                    throw new ArgumentException("Không xác định được đơn vị");                   
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
    }
}
