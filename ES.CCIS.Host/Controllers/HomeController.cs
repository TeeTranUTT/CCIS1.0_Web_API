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
    [RoutePrefix("api/Home")]
    public class HomeController : ApiBaseController
    {
        private readonly Business_Administrator_Department businessDepartment = new Business_Administrator_Department();
        private readonly CCISContext _dbContext;

        public HomeController()
        {
            _dbContext = new CCISContext();
        }

        [HttpGet]
        [Route("Index")]
        public HttpResponseMessage Index()
        {
            try
            {
                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var lstDepIds = DepartmentHelper.GetChildDepIds(departmentId);

                List<EquipmentMT_ElectricityMeterModel> listCTo = new List<EquipmentMT_ElectricityMeterModel>();
                List<Concus_ContractModel> lstContract = new List<Concus_ContractModel>();

                int TotalCongTo = 0;
                var date = DateTime.Now.Date;

                // lấy danh sách công tơ hết hạn kiểm định
                var table_cto = (from table1 in
                                  (from EquipmentMT_ElectricityMeter in _dbContext.EquipmentMT_ElectricityMeter.Where(i => lstDepIds.Contains(i.DepartmentId) && i.TestingDate != null && i.EndTestingDate != null && i.EndTestingDate <= date)
                                   where (EquipmentMT_ElectricityMeter.ActionCode == "B"
                                            || EquipmentMT_ElectricityMeter.ActionCode == "A"
                                            || EquipmentMT_ElectricityMeter.ActionCode == "E"
                                         )
                                    && EquipmentMT_ElectricityMeter.IsRoot == false
                                   select new
                                   {
                                       ElectricityMeterId = EquipmentMT_ElectricityMeter.ElectricityMeterId,
                                       ElectricityMeterCode = EquipmentMT_ElectricityMeter.ElectricityMeterCode, // mã công tơ
                                       ElectricityMeterNumber = EquipmentMT_ElectricityMeter.ElectricityMeterNumber, // số công tơ
                                       ElectricityMeterTypeId = EquipmentMT_ElectricityMeter.ElectricityMeterTypeId,
                                       TestingDate = EquipmentMT_ElectricityMeter.TestingDate, // ngày kiểm định
                                       EndTestingDate = EquipmentMT_ElectricityMeter.EndTestingDate, // ngày kiểm định
                                       ActionCode = EquipmentMT_ElectricityMeter.ActionCode,
                                       TypeName = "Công tơ",
                                   })
                                 select new EquipmentMT_ElectricityMeterModel
                                 {
                                     ActionCode = table1.ActionCode,
                                     TypeName = table1.TypeName,
                                     ElectricityMeterId = table1.ElectricityMeterId,
                                     ElectricityMeterCode = table1.ElectricityMeterCode, // mã công tơ
                                     ElectricityMeterNumber = table1.ElectricityMeterNumber, // số công tơ
                                     ElectricityMeterTypeId = table1.ElectricityMeterTypeId,
                                     TestingDate = table1.TestingDate, // ngày kiểm định
                                     EndTestingDate = table1.EndTestingDate.Value, // thời hạn kiểm định
                                 }).OrderBy(item => item.TestingDate).ToList();
                TotalCongTo = table_cto.Count();
                // lấy danh sách TU hết hạn kiểm định
                var table_tu = (from table1 in
                                  (from Equipment in _dbContext.EquipmentVT_VoltageTransformer.Where(i => lstDepIds.Contains(i.DepartmentId) && i.TestingDate != null && i.EndTestingDate != null && i.EndTestingDate <= date)
                                   where (Equipment.ActionCode == "B"
                                            || Equipment.ActionCode == "A"
                                            || Equipment.ActionCode == "E"
                                         )
                                   select new
                                   {
                                       ElectricityMeterId = Equipment.VoltageTransformerId,
                                       ElectricityMeterCode = Equipment.VTCode, // mã công tơ
                                       ElectricityMeterNumber = Equipment.VTNumber, // số công tơ
                                       ElectricityMeterTypeId = Equipment.VTTypeId,
                                       TestingDate = Equipment.TestingDate, // ngày kiểm định
                                       EndTestingDate = Equipment.EndTestingDate.Value, // ngày kiểm định
                                       ActionCode = Equipment.ActionCode,
                                       TypeName = "TU"
                                   })
                                select new EquipmentMT_ElectricityMeterModel
                                {
                                    ActionCode = table1.ActionCode,
                                    TypeName = table1.TypeName,
                                    ElectricityMeterId = table1.ElectricityMeterId,
                                    ElectricityMeterCode = table1.ElectricityMeterCode, // mã công tơ
                                    ElectricityMeterNumber = table1.ElectricityMeterNumber, // số công tơ
                                    ElectricityMeterTypeId = table1.ElectricityMeterTypeId,
                                    TestingDate = table1.TestingDate, // ngày kiểm định
                                    EndTestingDate = table1.EndTestingDate, // thời hạn kiểm định
                                }).OrderBy(item => item.TestingDate).ToList();

                // lấy danh sách TI hết hạn kiểm định
                var table_ti = (from table1 in
                                  (from Equipment in _dbContext.EquipmentCT_CurrentTransformer.Where(i => lstDepIds.Contains(i.DepartmentId) && i.TestingDate != null && i.EndTestingDate != null && i.EndTestingDate <= date)
                                   where (Equipment.ActionCode == "B"
                                            || Equipment.ActionCode == "A"
                                            || Equipment.ActionCode == "E"
                                         )
                                   select new
                                   {
                                       ElectricityMeterId = Equipment.CurrentTransformerId,
                                       ElectricityMeterCode = Equipment.CTCode, // mã công tơ
                                       ElectricityMeterNumber = Equipment.CTNumber, // số công tơ
                                       ElectricityMeterTypeId = Equipment.CTTypeId,
                                       TestingDate = Equipment.TestingDate, // ngày kiểm định
                                       EndTestingDate = Equipment.EndTestingDate.Value, // ngày kiểm định
                                       ActionCode = Equipment.ActionCode,
                                       TypeName = "TI"
                                   })
                                select new EquipmentMT_ElectricityMeterModel
                                {
                                    ActionCode = table1.ActionCode,
                                    TypeName = table1.TypeName,
                                    ElectricityMeterId = table1.ElectricityMeterId,
                                    ElectricityMeterCode = table1.ElectricityMeterCode, // mã công tơ
                                    ElectricityMeterNumber = table1.ElectricityMeterNumber, // số công tơ
                                    ElectricityMeterTypeId = table1.ElectricityMeterTypeId,
                                    TestingDate = table1.TestingDate, // ngày kiểm định
                                    EndTestingDate = table1.EndTestingDate, // thời hạn kiểm định
                                }).OrderBy(item => item.TestingDate).ToList();

                if (table_cto != null)
                {
                    listCTo.AddRange(table_cto.Take(1000));
                }
                if (table_tu != null)
                {
                    listCTo.AddRange(table_tu);
                }
                if (table_ti != null)
                {
                    listCTo.AddRange(table_ti);
                }

                #region hợp đồng hết hạn
                // var date = DateTime.Now;
                var endday = DateTime.Now.AddDays(60);
                lstContract = _dbContext.Concus_Contract.Where(item => item.EndDate < endday && item.EndDate >= DateTime.Now).Select(item => new Concus_ContractModel()
                {
                    ContractId = item.ContractId,
                    ContractCode = item.ContractCode,
                    SignatureDate = item.SignatureDate,
                    EndDate = item.EndDate,
                    DepartmentId = item.DepartmentId,
                    Name = item.Concus_Customer.Name,
                    Status = item.ReasonId != null ? "Đã thanh lý" : ((item.EndDate <= DateTime.Now || item.ActiveDate >= DateTime.Now) ? "Hết hiệu lực" : "Còn hiệu lực")
                }).Where(i => lstDepIds.Contains(i.DepartmentId))
                .OrderBy(item => item.EndDate).ToList();

                #endregion

                var data = new
                {
                    DanhSachCongTo = listCTo,
                    DanhSachHopDong = lstContract,
                    TotalCongTo = TotalCongTo
                };

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = data;
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

        //công tơ quá hạn
        [HttpGet]
        [Route("OverdueMeters")]
        public HttpResponseMessage OverdueMeters()
        {
            try
            {
                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var lstDepIds = DepartmentHelper.GetChildDepIds(departmentId);

                DateTime date1 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                DateTime date2 = date1.AddMonths(3);
                //Công tơ quá hạn
                var countA = _dbContext.EquipmentMT_ElectricityMeter.Where(item => item.ActionCode == "B" && lstDepIds.Contains(item.DepartmentId) && item.EndTestingDate < date1).Count();
                //Công tơ quá hạn trong 3 tháng
                var countB = _dbContext.EquipmentMT_ElectricityMeter.Where(item => item.ActionCode == "B" && lstDepIds.Contains(item.DepartmentId) && item.EndTestingDate >= date1 && item.EndTestingDate < date2).Count();
                //lấy ra list công tơ trong kho
                var countE = _dbContext.EquipmentMT_ElectricityMeter.Where(item => item.ActionCode == "B" && lstDepIds.Contains(item.DepartmentId) && item.EndTestingDate >= date2).Count();

                List<ChartItem> listChartItem = new List<ChartItem>();
                listChartItem.Add(new ChartItem { Count = countA, Name = "Đã quá hạn" });
                listChartItem.Add(new ChartItem { Count = countB, Name = "Hết hạn trong 3 tháng" });
                listChartItem.Add(new ChartItem { Count = countE, Name = "Hết hạn sau 3 tháng" });

                respone.Status = 1;
                respone.Message = "Lấy danh sách khách hàng thành công.";
                respone.Data = listChartItem;
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
        [Route("GetElectricMeterInfo")]
        public HttpResponseMessage GetElectricMeterInfo()
        {
            try
            {
                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var lstDepIds = DepartmentHelper.GetChildDepIds(departmentId);
                var list = _dbContext.EquipmentMT_ElectricityMeter.Where(item => item.ActionCode != "F" && lstDepIds.Contains(item.DepartmentId)).Select(item => item.ActionCode).ToList();
                //lấy ra list công tơ trong kho
                var countA = _dbContext.EquipmentMT_ElectricityMeter.Where(item => item.ActionCode == "A" && lstDepIds.Contains(item.DepartmentId)).Select(i => i.ElectricityMeterId).Distinct().Count();
                //lấy ra list công tơ trong kho
                var countB = _dbContext.EquipmentMT_ElectricityMeter.Where(item => item.ActionCode == "B" && lstDepIds.Contains(item.DepartmentId)).Select(i => i.ElectricityMeterId).Distinct().Count();
                //lấy ra list công tơ trong kho
                var countE = _dbContext.EquipmentMT_ElectricityMeter.Where(item => item.ActionCode == "E" && lstDepIds.Contains(item.DepartmentId)).Select(i => i.ElectricityMeterId).Distinct().Count();

                List<ChartItem> listChartItem = new List<ChartItem>();
                listChartItem.Add(new ChartItem { Count = countB, Name = "Đang treo" });
                listChartItem.Add(new ChartItem { Count = countA, Name = "Trong kho" });
                listChartItem.Add(new ChartItem { Count = countE, Name = "Dưới lưới" });

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = listChartItem;
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
        public class ChartItem
        {
            public string Name { get; set; }
            public int Count { get; set; }
        }

        [HttpGet]
        [Route("GetDepartmentInfo")]
        public HttpResponseMessage GetDepartmentInfo(string userName)
        {
            try
            {
                var departmentId = _dbContext.UserProfile.Where(item => item.UserName == userName.Trim()).Select(it2 => it2.DepartmentId).FirstOrDefault();
                string departmentName = _dbContext.Administrator_Department.Where(item => item.DepartmentId == departmentId).Select(it2 => it2.DepartmentName).FirstOrDefault();

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = departmentName;
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
