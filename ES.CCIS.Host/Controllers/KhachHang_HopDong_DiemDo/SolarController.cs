using CCIS_BusinessLogic;
using CCIS_DataAccess;
using CCIS_DataAccess.ViewModels;
using ES.CCIS.Host.Helpers;
using ES.CCIS.Host.Models;
using ES.CCIS.Host.Models.EnumMethods;
using ES.CCIS.Host.Models.HopDong;
using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Web.Configuration;
using System.Web.Http;

namespace ES.CCIS.Host.Controllers.KhachHang_HopDong_DiemDo
{
    [Authorize]
    [RoutePrefix("api/Solar")]
    public class SolarController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_Concus_ImposedPrice business_Concus_ImposedPrice = new Business_Concus_ImposedPrice();
        private readonly Business_Concus_Contract Concus_Contract = new Business_Concus_Contract();
        private readonly Business_Concus_ContractDetail businessConcusContractDetail = new Business_Concus_ContractDetail();

        #region Danh sách hợp đồng
        [HttpGet]
        [Route("Solar_ContractManager")]
        public HttpResponseMessage Solar_ContractManager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search, [DefaultValue(0)] int departmentId)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    IEnumerable<Solar_ContractModel> lists;
                    if (departmentId == 0)
                    {
                        lists = new List<Solar_ContractModel>();
                    }
                    else
                    {
                        var listDepartments = DepartmentHelper.GetChildDepIds(departmentId);

                        lists = (from item in db.Solar_Contract
                               .Where(item => listDepartments.Contains(item.DepartmentId))
                                 select new Solar_ContractModel
                                 {
                                     CustomerId = item.CustomerId,
                                     ContractId = item.ContractId,
                                     DepartmentId = item.DepartmentId,
                                     ReasonId = item.ReasonId,
                                     ContractCode = item.ContractCode,
                                     SignatureDate = item.SignatureDate,
                                     ActiveDate = item.ActiveDate,
                                     EndDate = item.EndDate,
                                     CreateDate = item.CreateDate,
                                     CreateUser = item.CreateUser,
                                     ContractName = item.ContractName,
                                     CustomerCode = item.ContractCode,
                                     ContractAdress = item.ContractAdress
                                 });
                        if (!string.IsNullOrEmpty(search))
                        {
                            lists = (IQueryable<Solar_ContractModel>)lists.Where(item => item.ContractName.Contains(search) || item.CustomerCode.Contains(search) || item.ContractAdress.Contains(search));
                        }
                    }
                    var paged = (IPagedList<Solar_ContractModel>)lists.OrderBy(p => p.CustomerCode).ToPagedList(pageNumber, pageSize);

                    var response = new
                    {
                        paged.PageNumber,
                        paged.PageSize,
                        paged.TotalItemCount,
                        paged.PageCount,
                        paged.HasNextPage,
                        paged.HasPreviousPage,
                        SolarContracts = paged.ToList()
                    };
                    respone.Status = 1;
                    respone.Message = "OK";
                    respone.Data = response;
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

        //Lấy chi tiết thông tin hợp đồng
        [HttpPost]
        [Route("DetailedContract")]
        public HttpResponseMessage DetailedContract(int contractId)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    var model = db.Concus_Contract.Where(item => item.ContractId.Equals(contractId))
                    .Select(item => new Concus_ContractModel
                    {
                        ContractId = item.ContractId,
                        DepartmentId = item.DepartmentId,
                        ReasonId = item.ReasonId,
                        ContractCode = item.ContractCode,
                        ContractTypeId = item.ContractTypeId,
                        SignatureDate = item.SignatureDate,
                        ActiveDate = item.ActiveDate,
                        EndDate = item.EndDate,
                        CreateDate = item.CreateDate,
                        CreateUser = item.CreateUser,
                        Name = item.Concus_Customer.Name, //thêm name
                        FileName = db.Concus_ContractFile.FirstOrDefault(x => x.ContractId.Equals(item.ContractId)).FileName,//thêm filenam trong model
                        FileUrl = db.Concus_ContractFile.FirstOrDefault(x => x.ContractId.Equals(item.ContractId)).FileUrl,
                        TypeName = item.Category_ContractType.TypeName,
                        Note = item.Category_Reason.ReasonName
                    }).FirstOrDefault();

                    string signatureDate = model.SignatureDate.Day + "/" + model.SignatureDate.Month + "/" + model.SignatureDate.Year;
                    string activeDate = model.ActiveDate.Day + "/" + model.ActiveDate.Month + "/" + model.ActiveDate.Year;
                    string endDate = model.EndDate.Day + "/" + model.EndDate.Month + "/" + model.EndDate.Year;
                    string createDate = model.CreateDate.Day + "/" + model.CreateDate.Month + "/" + model.CreateDate.Year;
                    decimal TicksActiveDate = model.ActiveDate.Ticks;
                    decimal TicksEndDate = model.EndDate.Ticks;
                    decimal TicksNow = DateTime.Now.Ticks;

                    var ds = db.Concus_ContractFile.Where(item => item.ContractId.Equals(contractId)).Select(item => new
                    {
                        filename = item.FileName,
                        fileurl = item.FileUrl,
                        ngay = item.CreateDate.Day + "/" + item.CreateDate.Month + "/" + item.CreateDate.Year
                    }).ToList();

                    var response = new
                    {
                        ContractId = model.ContractId,
                        DepartmentId = model.DepartmentId,
                        ReasonId = model.ReasonId,
                        ContractCode = model.ContractCode,
                        ContractTypeId = model.ContractTypeId,
                        SignatureDate = signatureDate,
                        ActiveDate = activeDate,
                        EndDate = endDate,
                        CreateDate = createDate,
                        CreateUser = model.CreateUser,
                        Name = model.Name,
                        TypeName = model.TypeName,
                        ReasonName = model.Note,
                        FileName = model.FileName,
                        FileUrl = model.FileUrl,
                        ds = ds,
                        TicksActiveDate = TicksActiveDate,
                        TicksEndDate = TicksEndDate,
                        TicksNow = TicksNow
                    };

                    respone.Status = 1;
                    respone.Message = "OK";
                    respone.Data = response;
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
        #endregion

        #region Thêm mới hợp đồng
        //Todo: api phần này chưa viết
        #endregion

        #region Form áp giá
        [HttpGet]
        [Route("ImposedPrice")]
        public HttpResponseMessage ImposedPrice(int pointId)
        {
            try
            {
                Customer_ContractModel cusContract = new Customer_ContractModel();

                using (var db = new CCISContext())
                {
                    // lấy thông tin  hợp đồng
                    //Lấy thông tin điểm đo
                    var concusServicePoint =
                        db.Concus_ServicePoint.Where(item => item.PointId.Equals(pointId))
                            .Select(item => new Concus_ServicePointModel
                            {
                                PointId = item.PointId,
                                PointCode = item.PointCode,
                                DepartmentId = item.DepartmentId,
                                ContractId = item.ContractId,
                                PotentialCode = item.PotentialCode,
                                Address = item.Address,
                                ReactivePower = item.ReactivePower,
                                Power = item.Power,
                                NumberOfPhases = item.NumberOfPhases,
                                ActiveDate = item.ActiveDate,
                                Status = item.Status,
                                CreateDate = item.CreateDate,
                                CreateUser = item.CreateUser,
                                HouseholdNumber = item.HouseholdNumber,
                                StationId = item.StationId,
                                RouteId = item.RouteId,
                                TeamId = item.TeamId,
                                BoxNumber = item.BoxNumber,
                                PillarNumber = item.PillarNumber,
                                FigureBookId = item.FigureBookId,
                                Index = item.Index,
                                ServicePointType = item.ServicePointType,
                                Description = item.ServicePointType + " - " + db.Category_ServicePointType.Where(a => a.ServicePointType == item.ServicePointType).Select(a => a.Description).FirstOrDefault(),
                                PotentialName = item.PotentialCode + " - " + db.Category_Potential.Where(a => a.PotentialCode == item.PotentialCode).Select(a => a.PotentialName).FirstOrDefault(),

                            }).FirstOrDefault();
                    cusContract.ServicePoint = concusServicePoint;

                    //Lấy thông tin hợp đồng
                    int contractId = Convert.ToInt32(concusServicePoint.ContractId);
                    var concusContract =
                        db.Concus_Contract.Where(item => item.ContractId == contractId)
                            .Select(item => new Concus_ContractModel
                            {
                                ContractId = item.ContractId,
                                DepartmentId = item.DepartmentId,
                                CustomerId = item.CustomerId,
                                ReasonId = item.ReasonId,
                                ContractCode = item.ContractCode,
                                ContractTypeId = item.ContractTypeId,
                                SignatureDate = item.SignatureDate,
                                ActiveDate = item.ActiveDate,
                                EndDate = item.EndDate,
                                CreateDate = item.CreateDate,
                                CreateUser = item.CreateUser,
                                Note = item.Note
                            }).FirstOrDefault();
                    cusContract.Contract = concusContract;

                    //Lấy danh sách khách hàng
                    var concusCustomer =
                        db.Concus_Customer.Where(item => item.CustomerId.Equals(concusContract.CustomerId))
                            .Select(item => new Concus_CustomerModel
                            {
                                CustomerId = item.CustomerId,
                                CustomerCode = item.CustomerCode,
                                DepartmentId = item.DepartmentId,
                                Name = item.Name,
                                Address = item.Address,
                                InvoiceAddress = item.InvoiceAddress,
                                Fax = item.Fax,
                                Gender = item.Gender,
                                Email = item.Email,
                                PhoneNumber = item.PhoneNumber,
                                TaxCode = item.TaxCode,
                                Ratio = item.Ratio,
                                BankAccount = item.BankAccount,
                                BankName = item.BankName,
                                Status = item.Status,
                                CreateDate = item.CreateDate,
                                CreateUser = item.CreateUser,
                                OccupationsGroupCode = item.OccupationsGroupCode,
                                PhoneCustomerCare = item.PhoneCustomerCare
                            }).FirstOrDefault();
                    cusContract.Customer = concusCustomer;

                    // lay danh sach ap gia 
                    var Concus_ImposedPrice =
                        db.Concus_ImposedPrice.Where(item => item.PointId.Equals(pointId))
                            .Select(item => new Concus_ImposedPriceModel
                            {
                                ImposedPriceId = item.ImposedPriceId,
                                DepartmentId = item.DepartmentId,
                                PointId = item.PointId,
                                ActiveDate = item.ActiveDate,
                                TimeOfSale = item.TimeOfSale,
                                TimeOfUse = item.TimeOfUse,
                                OccupationsGroupCode = item.OccupationsGroupCode,
                                GroupCode = item.GroupCode,
                                PotentialCode = item.PotentialCode,
                                Index = item.Index,
                                Rated = item.Rated,
                                RatedType = item.RatedType,
                                CreateDate = item.CreateDate,
                                CreateUser = item.CreateUser,
                                HouseholdNumber = concusServicePoint.HouseholdNumber,
                                Describe = item.Describe,
                                Price = db.Category_Price.Where(a => a.OccupationsGroupCode.Equals(item.OccupationsGroupCode) &&
                                    a.PotentialSpace == (db.Category_PotentialReference.Where(b => b.PotentialCode.Equals(item.PotentialCode)).Select(b => b.PotentialSpace).FirstOrDefault()) &&
                                    a.Time.Equals(item.TimeOfSale) &&
                                    a.PriceGroupCode.Equals(item.GroupCode) && a.ActiveDate <= DateTime.Now && a.EndDate > DateTime.Now).Select(a => a.Price).FirstOrDefault(),
                            }).ToList();

                    //nếu chưa có dòng áp giá nào thì phải lấy ngày hiệu lực điểm đo (hoặc ngày có chỉ số đầu tiên)
                    if (Concus_ImposedPrice?.Any() != true)
                    {
                        cusContract.dActivedate = cusContract.ServicePoint.ActiveDate;
                        cusContract.isFixActivedate = true;
                    }
                    else
                    {
                        //lấy ngày đầu kỳ đã tính hóa đơn + 1
                        decimal maxBillID = 0;
                        maxBillID = db.Bill_ElectricityBillDetail.Where(o => o.DepartmentId == concusContract.DepartmentId
                                    && o.PointId == cusContract.ServicePoint.PointId).Select(o2 => o2.BillId).DefaultIfEmpty(0).Max();
                        if (maxBillID == 0)
                        {
                            //trường hợp là điểm đo đầu nguồn
                            maxBillID = db.Loss_ElectricityBillDetail.Where(o => o.DepartmentId == concusContract.DepartmentId
                                    && o.PointId == cusContract.ServicePoint.PointId).Select(o2 => o2.BillId).DefaultIfEmpty(0).Max();
                            if (maxBillID == 0)
                            {
                                cusContract.dActivedate = cusContract.ServicePoint.ActiveDate;
                                cusContract.isFixActivedate = true;
                            }
                            else
                            {
                                var ngayhdon = db.Loss_ElectricityBill.Where(o => o.BillId == maxBillID).FirstOrDefault().EndDate;
                                cusContract.dActivedate = ngayhdon.AddDays(1);
                            }
                        }
                        else
                        {
                            var ngayhdon = db.Bill_ElectricityBill.Where(o => o.BillId == maxBillID).FirstOrDefault().EndDate;
                            cusContract.dActivedate = ngayhdon.AddDays(1);
                        }


                    }

                    // lấy ra danh sách bộ chỉ số
                    var servicePointTypes = db.Category_ServicePointType.Where(item => item.ServicePointType == (cusContract.ServicePoint.ServicePointType)).Select(item => new Category_ServicePointTypeModel
                    {
                        TimeOfUse = item.TimeOfUse
                    }).Distinct().ToList();

                    // lay ra danh sach gia, phải lọc theo giá đang áp dụng, giá theo cấp điện áp.
                    var categoryPrice = (from D in db.Category_PotentialReference
                                         join E in db.Category_Price on D.PotentialSpace equals E.PotentialSpace
                                         where D.OccupationsGroupCode == E.OccupationsGroupCode
                                            && D.PotentialCode == cusContract.ServicePoint.PotentialCode.ToString()
                                            && E.ActiveDate <= DateTime.Now && DateTime.Now < (DateTime)E.EndDate.Value
                                         select new Category_PriceModel
                                         {
                                             OccupationsGroupCode = E.OccupationsGroupCode + "-" + E.Time + "-" + E.Price + "   [" + E.Description + "]",
                                             PriceId = E.PriceId,
                                             Description = E.Description,
                                             Time = E.Time
                                         }).ToList();

                    var response = new
                    {
                        Customer_Contract = cusContract,
                        Concus_ImposedPrices = Concus_ImposedPrice,
                        ServicePointTypes = servicePointTypes,
                        CategoryPrices = categoryPrice
                    };

                    respone.Status = 1;
                    respone.Message = "OK";
                    respone.Data = response;
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
        [Route("GetCategory_Occupations")]
        public HttpResponseMessage GetCategory_Occupations(int PriceId)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    var OccupationsGroup =
                        db.Category_Price.Where(item => item.PriceId.Equals(PriceId) && item.ActiveDate <= DateTime.Now && item.EndDate > DateTime.Now)
                            .Select(item => new Category_PriceModel
                            {
                                OccupationsGroupCode = item.OccupationsGroupCode,
                                Price = item.Price,
                                PotentialCode = item.PotentialSpace,
                                PriceGroupCode = item.PriceGroupCode,
                                Time = item.Time,
                                ActiveDate = item.ActiveDate
                            }).FirstOrDefault();

                    var OccupationsGroupName =
                        db.Category_OccupationsGroup.Where(item => item.OccupationsGroupCode.Equals(OccupationsGroup.OccupationsGroupCode))
                            .Select(item => item.OccupationsGroupName)
                            .FirstOrDefault();

                    var response = new
                    {
                        OccupationsGroupCode = OccupationsGroup.OccupationsGroupCode,
                        Price = OccupationsGroup.Price,
                        PotentialCode = OccupationsGroup.PotentialCode,
                        PriceGroupCode = OccupationsGroup.PriceGroupCode,
                        Time = OccupationsGroup.Time,
                        OccupationsGroupName = OccupationsGroupName,
                        ActiveDate = OccupationsGroup.ActiveDate.Day + "/" + OccupationsGroup.ActiveDate.Month + "/" + OccupationsGroup.ActiveDate.Year,

                    };

                    respone.Status = 1;
                    respone.Message = "OK";
                    respone.Data = response;
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
        [Route("AddImposedPrice")]
        public HttpResponseMessage AddImposedPrice(List<Concus_ImposedPriceModel> myArray)
        {
            using (var db = new CCISContext())
            {
                using (var dbContextTransaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var departmentId = TokenHelper.GetDepartmentIdFromToken();
                        var userId = TokenHelper.GetUserIdFromToken();

                        for (int i = 0; i < myArray.Count; i++)
                        {
                            Concus_ImposedPriceModel prime = myArray[i];
                            prime.CreateDate = DateTime.Now;
                            prime.CreateUser = userId;
                            prime.DepartmentId = departmentId;
                            business_Concus_ImposedPrice.AddConcus_ImposedPrice(prime, db);
                        }
                        dbContextTransaction.Commit();

                        respone.Status = 1;
                        respone.Message = "Thêm biên bản áp giá thành công.";
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

        [HttpPost]
        [Route("EditImposedPrice")]
        public HttpResponseMessage EditImposedPrice(List<Concus_ImposedPriceModel> myArray)
        {
            try
            {
                string error = "";
                var createUser = TokenHelper.GetUserIdFromToken();
                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                bool success = business_Concus_ImposedPrice.EditConcus_ImposedPrice(myArray, myArray[0].PointId, createUser, departmentId, ref error);
                if (!success)
                {
                    throw new ArgumentException($"Áp giá điểm đo lỗi, chi tiết: {error}");
                }
                else
                {
                    respone.Status = 1;
                    respone.Message = "Áp giá điểm đo thành công.";
                    respone.Data = null;
                    return createResponse();
                }
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Áp giá điểm đo lỗi, chi tiết: {ex.Message} {ex.StackTrace}";
                respone.Data = null;
                return createResponse();
            }
        }
        #endregion

        #region Thanh lý hợp đồng
        [HttpPost]
        [Route("ContractLiquidation")]
        public HttpResponseMessage ContractLiquidation(ContractLiquidationInput input)
        {
            try
            {
                DateTime Day = DateTime.Now.Date;
                if (!string.IsNullOrEmpty(input.Liquidation))
                {
                    DateTime.TryParseExact(input.Liquidation, "dd-MM-yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out Day);
                }
                input.Contract.EndDate = Day;
                input.Contract.ReasonId = input.ReasonId;
                Concus_Contract.Liquidation_Contract(input.Contract);

                respone.Status = 1;
                respone.Message = "Thanh lý hợp đồng thành công.";
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
        #endregion

        #region Gia hạn hợp đồng
        [HttpPost]
        [Route("ContractExtension")]
        public HttpResponseMessage ContractExtension(ContractExtensionInput input)
        {
            try
            {
                DateTime Day = DateTime.Now;
                if (!string.IsNullOrEmpty(input.Extend))
                {
                    DateTime.TryParseExact(input.Extend, "dd-MM-yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out Day);
                }
                if (Day < DateTime.Now)
                {
                    throw new ArgumentException("Gia hạn hợp đồng không thành công, ngày gia hạn thêm hợp đồng phải lớn hơn hoặc bằng ngày hiện tại");
                }
                else
                {
                    input.Contract.CreateDate = DateTime.Now;
                    input.Contract.EndDate = Day;
                    Concus_Contract.Extension_Contract(input.Contract);

                    respone.Status = 1;
                    respone.Message = "Gia hạn hợp đồng thành công.";
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
        #endregion

        // Xóa file đính kèm(sửa thông tin hợp đồng)
        //[HttpPost]
        //[Route("DeleteFile")]
        //public HttpResponseMessage DeleteFile(int fileId)
        //{
        //    try
        //    {
        //        using (var db = new CCISContext())
        //        {
        //            var a = Convert.ToInt32(fileId);
        //            var fileurl = db.Concus_ContractFile.Where(item => item.FileId == a).Select(item => item.FileUrl).FirstOrDefault();
        //            string fullPath = Request.MapPath("~" + fileurl);
        //            if (System.IO.File.Exists(fullPath))
        //            {
        //                System.IO.File.Delete(fullPath);
        //            }
        //            var b = db.Concus_ContractFile.Where(item => item.FileId == a).FirstOrDefault();
        //            db.Concus_ContractFile.Remove(b);
        //            db.SaveChanges();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        respone.Status = 0;
        //        respone.Message = $"Lỗi: {ex.Message.ToString()}";
        //        respone.Data = null;
        //        return createResponse();
        //    }
        //}

        #region Tra cứu khách hàng
        //Todo: api SearchCustomer giống api phân trang khách hàng

        [HttpPost]
        [Route("EditConcus_Customer")]
        public HttpResponseMessage EditConcus_Customer(EditConcus_CustomerInput input)
        {
            try
            {
                input.ConCusConTract.Customer.OccupationsGroupCode = input.OccupationsGroupName;
                input.ConCusConTract.Customer.Gender = Convert.ToInt32(input.Gender);
                Concus_CustomerModel customer = new Concus_CustomerModel();
                customer = input.ConCusConTract.Customer;
                Business_Concus_Customer business = new Business_Concus_Customer();
                business.EditConcus_Customer(customer);

                respone.Status = 1;
                respone.Message = "Chỉnh sửa khách hàng thành công.";
                respone.Data = input.ConCusConTract.Customer.CustomerId;

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
        #endregion

        #region Quản lý dịch vụ giá trị gia tăng
        [HttpGet]
        [Route("EditConcus_ContractDetail")]
        public HttpResponseMessage EditConcus_ContractDetail(int contractId, int contractDetailId)
        {
            try
            {
                var response = GetCustomerInfoByContract(contractId, contractDetailId);

                respone.Status = 1;
                respone.Message = "Lấy thông tin dịch vụ thành công.";
                respone.Data = response;
                return createResponse();
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = "Lấy thông tin dịch vụ không thành công.";
                respone.Data = null;

                return createResponse();
            }
        }

        [HttpPost]
        [Route("EditConcus_ContractDetail")]
        public HttpResponseMessage EditConcus_ContractDetail(Concus_ContractDetailModel model)
        {
            try
            {
                var userId = TokenHelper.GetUserIdFromToken();

                model.CreateDate = DateTime.Now;
                model.CreateUser = userId;
                businessConcusContractDetail.EditConcus_ContractDetail(model);

                respone.Status = 1;
                respone.Message = "Cập nhật dịch vụ thành công.";
                respone.Data = model.ContractDetailId;
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
        [Route("AddConcus_ContractDetail")]
        public HttpResponseMessage AddConcus_ContractDetail(int contractId)
        {
            try
            {
                var response = GetCustomerInfoByContract(contractId);

                respone.Status = 1;
                respone.Message = "OK";
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

        [HttpPost]
        [Route("DeleteConcus_ContractDetail")]
        public HttpResponseMessage DeleteConcus_ContractDetail(int contractDetailId)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    int contractId = 0;
                    var target = db.Concus_ContractDetail.Where(item => item.ContractDetailId == contractDetailId).FirstOrDefault();
                    contractId = target.ContractId;
                    db.Concus_ContractDetail.Remove(target);
                    db.SaveChanges();
                }

                respone.Status = 1;
                respone.Message = "Xóa dịch vụ thành công.";
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

        //Thay đổi giá trị non tải hàng tháng
        [HttpGet]
        [Route("EditQuantityService")]
        public HttpResponseMessage EditQuantityService([DefaultValue("")] string taxCode, [DefaultValue("")] string customerCode = "")
        {
            try
            {
                using (var db = new CCISContext())
                {
                    var model = db.Concus_ContractDetail.Where(item => item.ServiceTypeId == EnumMethod.ServiceType.NONTAI
                        && item.Concus_Contract.Concus_Customer.TaxCode.Contains(taxCode)
                        && item.Concus_Contract.Concus_Customer.CustomerCode.Contains(customerCode)
                        && item.Concus_Contract.ActiveDate <= DateTime.Now
                        && item.Concus_Contract.EndDate >= DateTime.Now
                        && item.Concus_Contract.ReasonId == null).Select(item => new Concus_ContractDetailModel()
                        {
                            ContractDetailId = item.ContractDetailId,
                            Price = item.Price,
                            Description = item.Description,
                            TaxCode = item.Concus_Contract.Concus_Customer.TaxCode,
                            CustomerCode = item.Concus_Contract.Concus_Customer.CustomerCode,
                            CustomerName = item.Concus_Contract.Concus_Customer.Name,
                        }).ToList();

                    respone.Status = 1;
                    respone.Message = "OK";
                    respone.Data = model;
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
        [Route("RollBack_Contract")]
        public HttpResponseMessage RollBack_Contract(int contractId)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    var contractLog = db.Concus_Contract_Log.Where(item => item.ContractId == contractId).OrderByDescending(item => item.Id).Take(1).ToList();

                    var contract = db.Concus_Contract.Where(item => item.ContractId == contractId).FirstOrDefault();
                    contract.ContractId = contractLog[0].ContractId;
                    contract.ContractCode = contractLog[0].ContractCode;
                    contract.ContractTypeId = contractLog[0].ContractTypeId;
                    contract.DepartmentId = contractLog[0].DepartmentId;
                    contract.ReasonId = contractLog[0].ReasonId;
                    contract.SignatureDate = contractLog[0].SignatureDate;
                    contract.ActiveDate = contractLog[0].ActiveDate;
                    contract.EndDate = contractLog[0].EndDate;
                    contract.CreateDate = contractLog[0].CreateDate;
                    contract.CreateUser = contractLog[0].CreateUser;
                    contract.Note = contractLog[0].Note;
                    db.SaveChanges();

                    contractLog.Remove(contractLog[0]);
                    db.SaveChanges();
                }

                respone.Status = 1;
                respone.Message = "Khôi phục hợp đồng thành công.";
                respone.Data = contractId;
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
        private Concus_ContractDetailViewModel GetCustomerInfoByContract(int contractId)
        {
            using (var db = new CCISContext())
            {                
                Concus_ContractDetailViewModel viewModel = new Concus_ContractDetailViewModel();

                viewModel.ContractDetail = new Concus_ContractDetailModel();
                viewModel.ContractDetail.ContractId = contractId;

                //get customerId
                int customerId = db.Concus_Contract.Where(item => item.ContractId == contractId).FirstOrDefault().CustomerId;
                
                var customerModel = db.Concus_Customer.Where(item => item.CustomerId == customerId).Select(item => new Concus_CustomerModel()
                {
                    Address = item.Address,
                    BankAccount = item.BankAccount,
                    BankName = item.BankName,
                    CustomerCode = item.CustomerCode,
                    CustomerId = item.CustomerId,
                    DepartmentId = item.DepartmentId,
                    Email = item.Email,
                    Fax = item.Fax,
                    Gender = item.Gender,
                    InvoiceAddress = item.InvoiceAddress,
                    Name = item.Name,
                    OccupationsGroupCode = item.OccupationsGroupCode,
                    PhoneCustomerCare = item.PhoneCustomerCare,
                    PhoneNumber = item.PhoneNumber,
                    Ratio = item.Ratio,
                    Status = item.Status,
                    TaxCode = item.TaxCode
                }).FirstOrDefault();
                viewModel.Customer = customerModel;

                var listContractDetail = db.Concus_ContractDetail.Where(item => item.ContractId == contractId).Select(item => new Concus_ContractDetailModel()
                {
                    ServiceName = item.Bill_ServiceType.ServiceName,
                    Price = item.Price,
                    Description = item.Description,
                    ContractDetailId = item.ContractDetailId,
                    ContractId = item.ContractId
                }).ToList();
                if (listContractDetail == null)
                {
                    listContractDetail = new List<Concus_ContractDetailModel>();
                }
                viewModel.ListContractDetail = listContractDetail;

                return viewModel;
            }
        }
        private Concus_ContractDetailViewModel GetCustomerInfoByContract(int contractId, int contractDetailId)
        {
            using (var db = new CCISContext())
            {

                Concus_ContractDetailViewModel viewModel = new Concus_ContractDetailViewModel();

                viewModel.ContractDetail = new Concus_ContractDetailModel();
                viewModel.ContractDetail.ContractId = contractId;

                //get customerId
                int customerId = db.Concus_Contract.Where(item => item.ContractId == contractId).FirstOrDefault().CustomerId;

                var customerModel = db.Concus_Customer.Where(item => item.CustomerId == customerId).Select(item => new Concus_CustomerModel()
                {
                    Address = item.Address,
                    BankAccount = item.BankAccount,
                    BankName = item.BankName,
                    CustomerCode = item.CustomerCode,
                    CustomerId = item.CustomerId,
                    DepartmentId = item.DepartmentId,
                    Email = item.Email,
                    Fax = item.Fax,
                    Gender = item.Gender,
                    InvoiceAddress = item.InvoiceAddress,
                    Name = item.Name,
                    OccupationsGroupCode = item.OccupationsGroupCode,
                    PhoneCustomerCare = item.PhoneCustomerCare,
                    PhoneNumber = item.PhoneNumber,
                    Ratio = item.Ratio,
                    Status = item.Status,
                    TaxCode = item.TaxCode
                }).FirstOrDefault();
                viewModel.Customer = customerModel;

                var listContractDetail = db.Concus_ContractDetail.Where(item => item.ContractId == contractId).Select(item => new Concus_ContractDetailModel()
                {
                    ServiceName = item.Bill_ServiceType.ServiceName,
                    Price = item.Price,
                    Description = item.Description,
                    ContractDetailId = item.ContractDetailId,
                    ContractId = item.ContractId
                }).ToList();
                viewModel.ListContractDetail = listContractDetail;

                var contractDetail = db.Concus_ContractDetail.Where(item => item.ContractDetailId == contractDetailId).FirstOrDefault();

                var contractDetailModel = new Concus_ContractDetailModel();
                contractDetailModel.ContractId = contractDetail.ContractId;
                contractDetailModel.ActiveDate = contractDetail.ActiveDate;
                contractDetailModel.ContractDetailId = contractDetail.ContractDetailId;
                contractDetailModel.Description = contractDetail.Description;
                contractDetailModel.EndDate = contractDetail.EndDate;
                contractDetailModel.Po = contractDetail.Po;
                contractDetailModel.PointId = contractDetail.PointId.Split(',').ToList();
                contractDetailModel.Price = contractDetail.Price;
                contractDetailModel.S = contractDetail.S;
                contractDetailModel.ServiceName = contractDetail.Bill_ServiceType.ServiceName;
                contractDetailModel.ServiceTypeId = contractDetail.ServiceTypeId;
                contractDetailModel.WorkDay = contractDetail.WorkDay;
                contractDetailModel.WorkHour = contractDetail.WorkHour;

                if (contractDetail.ServiceTypeId == CommonDefault.ServiceType.QLVH)
                {
                    var QL = contractDetail.Formula != null ? contractDetail.Formula : "";
                    var formula = QL.Split(';');
                    if (formula.Count() == 1 && formula[0] != "")
                    {
                        var result = formula[0].Split('|');

                        contractDetailModel.PercentCD = result[1];
                    }
                    else if (formula.Count() > 1)
                    {
                        var result1 = formula[0].Split('|');
                        var result2 = formula[1].Split('|');
                        var result3 = formula[2].Split('|');
                        var result4 = formula[3].Split('|');

                        contractDetailModel.PercentB1 = result1[1];
                        contractDetailModel.PercentB2 = result2[1];
                        contractDetailModel.PercentB3 = result3[1];
                        contractDetailModel.PercentBC = result4[1];
                        contractDetailModel.QuotaB1 = result1[2];
                        contractDetailModel.QuotaB2 = result2[2];
                        contractDetailModel.QuotaB3 = result3[2];
                    }
                }

                viewModel.ContractDetail = contractDetailModel;

                return viewModel;
            }
        }
        #endregion

        #region Quản lý hợp đồng
        [HttpGet]
        [Route("ContractManager")]
        public HttpResponseMessage ContractManager([DefaultValue(0)] int departmentId, [DefaultValue(0)] int figurebookId, [DefaultValue(0)] int contracttypeId,
             [DefaultValue("")] string search, [DefaultValue(1)] int pageNumber)
        {
            try
            {
                List<int> lstDep = new List<int>();

                lstDep = DepartmentHelper.GetChildDepIds(departmentId);

                var userInfo = TokenHelper.GetUserInfoFromRequest();

                if (departmentId == 0)
                {
                    lstDep = DepartmentHelper.GetChildDepIdsByUser(userInfo.UserName);
                }

                using (var db = new CCISContext())
                {
                    IEnumerable<ContractManagerViewerModel> listContract;

                    if (departmentId == 0)
                    {
                        listContract = new List<ContractManagerViewerModel>();
                    }
                    else
                    {
                        listContract = (from cc in db.Concus_Contract
                                        join cs in db.Concus_ServicePoint on cc.ContractId equals cs.ContractId
                                        where lstDep.Contains(cc.DepartmentId)
                                        select new ContractManagerViewerModel
                                        {
                                            CustomerId = cc.CustomerId,
                                            ContractId = cc.ContractId,
                                            DepartmentId = cc.DepartmentId,
                                            ReasonId = cc.ReasonId,
                                            ContractCode = cc.ContractCode,
                                            ContractTypeId = cc.ContractTypeId,
                                            SignatureDate = cc.SignatureDate,
                                            ActiveDate = cc.ActiveDate,
                                            EndDate = cc.EndDate,
                                            CreateDate = cc.CreateDate,
                                            CreateUser = cc.CreateUser,
                                            Name = cc.Concus_Customer.Name,
                                            TypeName = cc.Category_ContractType.TypeName,
                                            CustomerCode = cc.Concus_Customer.CustomerCode,
                                            FigureBookId = cs.FigureBookId,
                                            NumberOfPhases = cs.NumberOfPhases
                                        });
                    }

                    if (figurebookId != 0)
                    {
                        listContract = listContract.Where(x => x.FigureBookId == figurebookId);
                    }

                    if (contracttypeId != 0)
                    {
                        listContract = listContract.Where(x => x.ContractTypeId == contracttypeId);
                    }

                    if (search != "")
                    {
                        listContract = listContract.Where(x => x.CustomerCode == search || x.ContractCode == search);
                    }

                    var paged = (IPagedList<ContractManagerViewerModel>)listContract.OrderByDescending(p => p.CustomerId).ToPagedList(pageNumber, pageSize);

                    var response = new
                    {
                        paged.PageNumber,
                        paged.PageSize,
                        paged.TotalItemCount,
                        paged.PageCount,
                        paged.HasNextPage,
                        paged.HasPreviousPage,
                        Customers = paged.ToList()
                    };
                    respone.Status = 1;
                    respone.Message = "Lấy danh sách hợp đồng thành công.";
                    respone.Data = response;
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

        [HttpGet]
        [Route("PrintContract")]
        public HttpResponseMessage PrintContract(int contractId, int departmentid, int ContractTypeId)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    var lstTemplate = db.Category_ContractTemplate.Where(x => x.DepartmentId == departmentid).ToList();

                    var response = new
                    {
                        contractId = contractId,
                        lstTemplate = lstTemplate,
                        ContractTypeId = ContractTypeId,
                    };

                    respone.Status = 1;
                    respone.Message = "OK";
                    respone.Data = response;
                    return createResponse();
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        //Todo: chưa viết api ViewContract, DownloadContract, SaveFileContract, DeleteFileContract
        #endregion

        #region Class
        public class ContractLiquidationInput
        {
            public Concus_ContractModel Contract { get; set; }
            public string Liquidation { get; set; }
            public int ReasonId { get; set; }
        }

        public class ContractExtensionInput
        {
            public Concus_ContractModel Contract { get; set; }
            public string Extend { get; set; }
        }

        public class EditConcus_CustomerInput
        {
            public Customer_ContractModel ConCusConTract { get; set; }
            public string Gender { get; set; }
            public string OccupationsGroupName { get; set; }
        }
        #endregion
    }
}
