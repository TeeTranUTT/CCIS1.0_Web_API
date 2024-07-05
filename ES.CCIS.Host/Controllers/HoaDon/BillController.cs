using CCIS_BusinessLogic;
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
    [RoutePrefix("api/Bill")]
    public class BillController : ApiBaseController
    {
        private readonly Business_Administrator_Department administrator_Department = new Business_Administrator_Department();
        private readonly Business_Index_CalendarOfSaveIndex SaveAddIndex = new Business_Index_CalendarOfSaveIndex();

        //Tính hóa đơn
        [HttpPost]
        [Route("ElectricityBillCalculator")]
        public HttpResponseMessage ElectricityBillCalculator(ElectricityBillCalculatorInput model)
        {
            try
            {
                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var listDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);

                //Gán = ngày tháng hiện tại nếu chưa chọn tháng tính hóa đơn
                DateTime dFilterDate = DateTime.Now;
                int iMonthSearch = dFilterDate.Month;
                int iYearSearch = dFilterDate.Year;

                if (model.FilterDate != null)
                {
                    iMonthSearch = Convert.ToInt32(model.FilterDate.Substring(0, 2));
                    iYearSearch = Convert.ToInt32(model.FilterDate.Substring(3, 4));
                }
                else
                {
                    model.FilterDate = dFilterDate.ToString("MM-yyyy");
                }

                using (var db = new CCISContext())
                {                    
                    //Lấy danh mục sổ ghi chỉ số theo điều kiện lọc lập lịch
                    var listCalendarOfSaveIndex = db.Index_CalendarOfSaveIndex.Where(
                            item => listDepartmentId.Contains(item.DepartmentId)
                                    && (item.Status == 5 || item.Status == 7)
                                    && item.Term == model.Term
                                    && item.Year == iYearSearch
                                    && item.Month == iMonthSearch
                                    && ((model.FigureBookId == 0) ? item.FigureBookId != 0 : item.FigureBookId.Equals(model.FigureBookId))
                                    && ((model.SaveDate == "0") ? item.SaveDate != "0" : item.SaveDate.Equals(model.SaveDate)))
                        .Select(item => new Bill_Index_CalendarOfSaveIndexModel
                        {
                            FigureBookId = item.FigureBookId,
                            BookCode = item.Category_FigureBook.BookCode,
                            BookName = item.Category_FigureBook.BookName,
                            Term = item.Term,
                            Month = item.Month,
                            Year = item.Year,
                            SaveDate = item.SaveDate,
                            IsBillCalculator = false,
                            Status = item.Status
                        }).ToList();

                    respone.Status = 1;
                    respone.Message = "OK";
                    respone.Data = listCalendarOfSaveIndex;
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

        [HttpPost]
        [Route("CancelBillCalculator")]
        public HttpResponseMessage CancelBillCalculator(CancelBillCalculatorInput model)
        {
            try
            {
                if (model != null)
                {
                    string strKQ = SaveAddIndex.CalBillElectricityByBook(model.FigureBookId, model.Term, model.Month, model.Year);
                    if (strKQ == "OK")
                    {
                        respone.Status = 1;
                        respone.Message = "Hủy tính hóa đơn thành công.";
                        respone.Data = null;
                        return createResponse();                        
                    }
                    else
                    {
                        throw new ArgumentException($"{strKQ}");                        
                    }
                }
                else
                {
                    throw new ArgumentException("Hủy tính hóa đơn không thành công.");
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
