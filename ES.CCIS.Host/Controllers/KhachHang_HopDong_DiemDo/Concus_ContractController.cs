using CCIS_BusinessLogic;
using CCIS_DataAccess;
using ES.CCIS.Host.Helpers;
using ES.CCIS.Host.Models.HopDong;
using ES.CCIS.Host.Models.EnumMethods;
using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Web.Configuration;
using System.Web.Http;
using static CCIS_BusinessLogic.DefaultBusinessValue;
using Newtonsoft.Json;
using System.Web;
using CCIS_DataAccess.ViewModels;

namespace ES.CCIS.Host.Controllers.KhachHang_HopDong_DiemDo
{
    [Authorize]
    [RoutePrefix("api/HopDong")]
    public class Concus_ContractController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_Administrator_Parameter vParameters = new Business_Administrator_Parameter();
        private readonly Business_Concus_Contract businessConcusContract = new Business_Concus_Contract();
        private readonly Business_Concus_ImposedPrice imposedPrice = new Business_Concus_ImposedPrice();
        private readonly Business_Concus_ServicePoint business_Concus_ServicePoint = new Business_Concus_ServicePoint();
        private readonly Business_Administrator_Department businessDepartment = new Business_Administrator_Department();
        private readonly Business_Concus_Customer business_Concus_Customer = new Business_Concus_Customer();
        private readonly Business_Concus_ContractDetail businessConcusContractDetail = new Business_Concus_ContractDetail();
        private readonly CCISContext _dbContext;

        public Concus_ContractController()
        {
            _dbContext = new CCISContext();
        }

        #region Quản lý hợp đồng
        //Chưa hoàn thiện được api module quản lý hợp đồng còn vướng phần đính kèm file
        [HttpGet]
        [Route("Concus_ContractManager")]
        public HttpResponseMessage Concus_ContractManager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search, [DefaultValue(0)] int departmentId)
        {
            try
            {
                //Lấy thông tin người dùng từ token
                var userInfo = TokenHelper.GetUserInfoFromRequest();
                if (departmentId == 0)
                    departmentId = TokenHelper.GetDepartmentIdFromToken();
                //list đơn vị con của đơn vị được search
                var listDepartments = DepartmentHelper.GetChildDepIds(departmentId);

                var query = _dbContext.Concus_Contract.Where(item => listDepartments.Contains(item.DepartmentId)).Select(item => new Concus_ContractModel
                {
                    CustomerId = item.CustomerId,
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
                    Name = item.Concus_Customer.Name,
                    TypeName = item.Category_ContractType.TypeName,
                    CustomerCode = item.Concus_Customer.CustomerCode
                });

                if (!string.IsNullOrEmpty(search))
                {
                    query = (IQueryable<Concus_ContractModel>)query.Where(item => item.Name.Contains(search) || item.CustomerCode.Contains(search) || item.CustomerCode.Contains(search));
                }

                var pagedCustomer = (IPagedList<Concus_ContractModel>)query.OrderBy(p => p.CustomerCode).ToPagedList(pageNumber, pageSize);

                var response = new
                {
                    pagedCustomer.PageNumber,
                    pagedCustomer.PageSize,
                    pagedCustomer.TotalItemCount,
                    pagedCustomer.PageCount,
                    pagedCustomer.HasNextPage,
                    pagedCustomer.HasPreviousPage,
                    Customers = pagedCustomer.ToList()
                };
                respone.Status = 1;
                respone.Message = "Lấy danh sách hợp đồng thành công.";
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

        [HttpGet]
        [Route("ContractManager")]
        public HttpResponseMessage ContractManager([DefaultValue(0)] int departmentId, [DefaultValue(0)] int figurebookId, [DefaultValue(0)] int contracttypeId,
             [DefaultValue("")] string search, [DefaultValue(1)] int pageNumber)
        {
            try
            {
                //Lấy thông tin người dùng từ token
                var userInfo = TokenHelper.GetUserInfoFromRequest();
                if (departmentId == 0)
                    departmentId = TokenHelper.GetDepartmentIdFromToken();
                //list đơn vị con của đơn vị được search
                var listDepartments = DepartmentHelper.GetChildDepIds(departmentId);

                var query = (from cc in _dbContext.Concus_Contract
                             join cs in _dbContext.Concus_ServicePoint on cc.ContractId equals cs.ContractId
                             where listDepartments.Contains(cc.DepartmentId)
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

                if (figurebookId != 0)
                {
                    query = (IQueryable<ContractManagerViewerModel>)query.Where(x => x.FigureBookId == figurebookId);
                }

                if (contracttypeId != 0)
                {
                    query = (IQueryable<ContractManagerViewerModel>)query.Where(x => x.ContractTypeId == contracttypeId);
                }

                if (!string.IsNullOrEmpty(search))
                {
                    query = (IQueryable<ContractManagerViewerModel>)query.Where(item => item.Name.Contains(search) || item.CustomerCode.Contains(search) || item.CustomerCode.Contains(search));
                }

                var pagedCustomer = (IPagedList<ContractManagerViewerModel>)query.OrderBy(p => p.CustomerId).ToPagedList(pageNumber, pageSize);

                var response = new
                {
                    pagedCustomer.PageNumber,
                    pagedCustomer.PageSize,
                    pagedCustomer.TotalItemCount,
                    pagedCustomer.PageCount,
                    pagedCustomer.HasNextPage,
                    pagedCustomer.HasPreviousPage,
                    Customers = pagedCustomer.ToList()
                };
                respone.Status = 1;
                respone.Message = "Lấy danh sách hợp đồng thành công.";
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

        [HttpGet]
        [Route("")]
        public HttpResponseMessage GetContractById(int contractId)
        {
            try
            {
                if (contractId < 0 || contractId == 0)
                {
                    throw new ArgumentException($"contractId {contractId} không hợp lệ.");
                }
                var route = _dbContext.Concus_Contract.Where(p => p.ContractId == contractId).Select(item => new Concus_ContractModel
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
                    CustomerId = item.CustomerId,
                    Note = item.Note,
                });

                if (route?.Any() == true)
                {
                    var response = route.FirstOrDefault();

                    respone.Status = 1;
                    respone.Message = "Lấy thông tin hợp đồng thành công.";
                    respone.Data = response;
                    return createResponse();

                }
                else
                {
                    throw new ArgumentException($"Lộ có contractId {contractId} không tồn tại.");
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
        [Route("DetailedContract")]
        public HttpResponseMessage DetailedContract(int contractId)
        {
            try
            {
                if (contractId < 0 || contractId == 0)
                {
                    throw new ArgumentException($"ContractId {contractId} không hợp lệ.");
                }
                var model = _dbContext.Concus_Contract.Where(item => item.ContractId.Equals(contractId))
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
                        FileName = _dbContext.Concus_ContractFile.FirstOrDefault(x => x.ContractId.Equals(item.ContractId)).FileName,//thêm filenam trong model
                        FileUrl = _dbContext.Concus_ContractFile.FirstOrDefault(x => x.ContractId.Equals(item.ContractId)).FileUrl,
                        TypeName = item.Category_ContractType.TypeName,
                        Note = item.Category_Reason.ReasonName,
                        CustomerId = item.CustomerId,
                        Custom1 = item.Custom1,
                        Custom2 = item.Custom2,
                        Custom3 = item.Custom3,
                        Custom4 = item.Custom4,
                        Custom5 = item.Custom5,
                        Custom6 = item.Custom6,
                        Custom7 = item.Custom7,
                        Custom8 = item.Custom8,
                        Custom9 = item.Custom9
                    }).FirstOrDefault();

                string signatureDate = model.SignatureDate.Day + "/" + model.SignatureDate.Month + "/" + model.SignatureDate.Year;
                string activeDate = model.ActiveDate.Day + "/" + model.ActiveDate.Month + "/" + model.ActiveDate.Year;
                string endDate = model.EndDate.Day + "/" + model.EndDate.Month + "/" + model.EndDate.Year;
                string createDate = model.CreateDate.Day + "/" + model.CreateDate.Month + "/" + model.CreateDate.Year;
                decimal TicksActiveDate = model.ActiveDate.Ticks;
                decimal TicksEndDate = model.EndDate.Ticks;
                decimal TicksNow = DateTime.Now.Ticks;
                var infoExplan = _dbContext.Administrator_CustomColumnsContract.Where(x => x.DepartmentId == model.DepartmentId).ToList();
                var lstContractMoreInfo = new List<ContractMoreInfo>();
                infoExplan.ForEach(item =>
                {
                    switch (item.ColName)
                    {
                        case "Custom1":
                            lstContractMoreInfo.Add(new ContractMoreInfo { Name = item.ColDesc, Value = model.Custom1 ?? "" });
                            break;
                        case "Custom2":
                            lstContractMoreInfo.Add(new ContractMoreInfo { Name = item.ColDesc, Value = model.Custom2 ?? "" });
                            break;
                        case "Custom3":
                            lstContractMoreInfo.Add(new ContractMoreInfo { Name = item.ColDesc, Value = model.Custom3 ?? "" });
                            break;
                        case "Custom4":
                            lstContractMoreInfo.Add(new ContractMoreInfo { Name = item.ColDesc, Value = model.Custom4 ?? "" });
                            break;
                        case "Custom5":
                            lstContractMoreInfo.Add(new ContractMoreInfo { Name = item.ColDesc, Value = model.Custom5 ?? "" });
                            break;
                        case "Custom6":
                            lstContractMoreInfo.Add(new ContractMoreInfo { Name = item.ColDesc, Value = model.Custom6 ?? "" });
                            break;
                        case "Custom7":
                            lstContractMoreInfo.Add(new ContractMoreInfo { Name = item.ColDesc, Value = model.Custom7 ?? "" });
                            break;
                        case "Custom8":
                            lstContractMoreInfo.Add(new ContractMoreInfo { Name = item.ColDesc, Value = model.Custom8 ?? "" });
                            break;
                        case "Custom9":
                            lstContractMoreInfo.Add(new ContractMoreInfo { Name = item.ColDesc, Value = model.Custom9 ?? "" });
                            break;
                    }
                });
                var ds = _dbContext.Concus_ContractFile.Where(item => item.ContractId.Equals(contractId)).Select(item => new
                {
                    filename = item.FileName,
                    fileurl = item.FileUrl,
                    ngay = item.CreateDate.Day + "/" + item.CreateDate.Month + "/" + item.CreateDate.Year
                }).ToList();

                // Kiểm tra xem đơn vị có phải của VSIP không các thông tin của bảng ContractExtendedInfo chỉ dành cho VSIP

                var ContractExtendedInfoVsip = vParameters.GetParameterValue(Administrator_Parameter_Common.ContractExtendedInfoVsip, "false");
                List<VsipContractExtend> VsipContractExtend = new List<VsipContractExtend>();
                if (ContractExtendedInfoVsip.ToLower() == "true")
                {
                    var contractExtendedInfo = _dbContext.AdditionInfos.Where(x => x.ObjectId == contractId && x.ObjectCode == VSIPKeys.ObjectCode).Select(x => x.Value).ToList();
                    if (contractExtendedInfo != null)
                    {
                        contractExtendedInfo.ForEach(item =>
                        {
                            VsipContractExtend.Add(JsonConvert.DeserializeObject<VsipContractExtend>(item));
                        });
                    }
                    else
                    {
                        VsipContractExtend.Add(new VsipContractExtend
                        {
                            Cap1 = new Cap1
                            {
                                Obj5051 = new Obj5051(),
                                Obj5051N = new Obj5051N()
                            },
                            Cap2 = new Cap2
                            {
                                Obj5051 = new Obj5051(),
                                Obj5051N = new Obj5051N()
                            }
                        });
                    }
                }
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
                    TicksNow = TicksNow,
                    lstContractMoreInfo = lstContractMoreInfo,
                    ContractExtendedInfo = VsipContractExtend
                };

                respone.Status = 1;
                respone.Message = "Lấy thông tin hợp đồng thành công.";
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

        //Todo: còn phần đính kèm file chưa xử lý được
        [HttpPost]
        [Route("ThemMoi")]
        public HttpResponseMessage AddConcus_Contract(Concus_ContractModel model)
        {
            try
            {
                //Thong tin user from token 
                var userId = TokenHelper.GetUserIdFromToken();
                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                if (model.CustomerId > 0)
                {
                    var i = model.CustomerId;
                }

                if (businessConcusContract.CheckExistContractCode(model.ContractCode))
                {
                    throw new ArgumentException($"Mã hợp đồng {model.ContractCode} đã tồn tại.");
                }
                else if (model.ActiveDate.Ticks > model.EndDate.Ticks)
                {
                    throw new ArgumentException("Ngày bắt đầu không được lớn hơn ngày kết thúc.");
                }
                else
                {
                    model.DepartmentId = departmentId;
                    model.CreateUser = userId;

                    int contractId = businessConcusContract.AddConcus_Contract(model);

                    //if (HttpContext.Current.Request.Files.Count != 0)
                    //{
                    //    using (var _dbContext = new CCISContext())
                    //    {
                    //        // Lấy file đầu tiên từ request
                    //        var files = HttpContext.Current.Request.Files[0];

                    //        //Đường dẫn lưu vào _dbContext
                    //        foreach (var item in files)
                    //        {
                    //            string pathFolder = "/UploadFoldel/Contract"; // Your code goes here
                    //            bool exists = Directory.Exists(Server.MapPath(pathFolder));
                    //            if (!exists)
                    //                Directory.CreateDirectory(Server.MapPath(pathFolder));

                    //            var extension = Path.GetExtension(item.FileName);
                    //            Guid fileName = Guid.NewGuid();
                    //            var physicalPath = "/UploadFoldel/Contract/" + fileName + extension;
                    //            var savePath = Path.Combine(Server.MapPath("~/UploadFoldel/Contract/"), fileName + extension);
                    //            item.SaveAs(savePath);

                    //            Concus_ContractFile target = new Concus_ContractFile();
                    //            target.FileExtension = extension;
                    //            target.ContractId = contractId;
                    //            target.FileName = item.FileName;
                    //            target.FileUrl = physicalPath;
                    //            target.CreateDate = DateTime.Now;
                    //            target.CreateUser = userId;
                    //            _dbContext.Concus_ContractFile.Add(target);
                    //            _dbContext.SaveChanges();
                    //        }
                    //    }
                    //}

                    respone.Status = 1;
                    respone.Message = "Thêm mới hợp đồng thành công.";
                    respone.Data = contractId;
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
                var contractLog = _dbContext.Concus_Contract_Log.Where(item => item.ContractId == contractId).OrderByDescending(item => item.Id).Take(1).ToList();

                var contract = _dbContext.Concus_Contract.Where(item => item.ContractId == contractId).FirstOrDefault();
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
                _dbContext.SaveChanges();

                contractLog.Remove(contractLog[0]);
                _dbContext.SaveChanges();

                respone.Status = 1;
                respone.Message = "Khôi phục hợp đồng thành công.";
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

        #region Áp giá
        [HttpGet]
        [Route("ImposedPrice")]
        public HttpResponseMessage ImposedPrice(int pointId)
        {
            try
            {
                Customer_ContractModel cusContract = new Customer_ContractModel();
                //Lấy thông tin điểm đo
                var concusServicePoint =
                    _dbContext.Concus_ServicePoint.Where(item => item.PointId.Equals(pointId))
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
                            Description = item.ServicePointType + " - " + _dbContext.Category_ServicePointType.Where(a => a.ServicePointType == item.ServicePointType).Select(a => a.Description).FirstOrDefault(),
                            PotentialName = item.PotentialCode + " - " + _dbContext.Category_Potential.Where(a => a.PotentialCode == item.PotentialCode).Select(a => a.PotentialName).FirstOrDefault(),

                        }).FirstOrDefault();
                cusContract.ServicePoint = concusServicePoint;

                //Lấy thông tin hợp đồng
                int contractId = Convert.ToInt32(concusServicePoint.ContractId);
                var concusContract =
                    _dbContext.Concus_Contract.Where(item => item.ContractId == contractId)
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
                    _dbContext.Concus_Customer.Where(item => item.CustomerId.Equals(concusContract.CustomerId))
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
                    _dbContext.Concus_ImposedPrice.Where(item => item.PointId.Equals(pointId))
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
                            Price = _dbContext.Category_Price.Where(a => a.OccupationsGroupCode.Equals(item.OccupationsGroupCode) &&
                                a.PotentialSpace == (_dbContext.Category_PotentialReference.Where(b => b.PotentialCode.Equals(item.PotentialCode) && b.OccupationsGroupCode == item.OccupationsGroupCode).Select(b => b.PotentialSpace).FirstOrDefault()) &&
                                a.Time.Equals(item.TimeOfSale) &&
                                a.PriceGroupCode.Equals(item.GroupCode) && a.ActiveDate <= DateTime.Now && a.EndDate > DateTime.Now).Select(a => a.Price).FirstOrDefault(),
                        }).ToList();

                //nếu chưa có dòng áp giá nào thì phải lấy ngày hiệu lực điểm đo (hoặc ngày có chỉ số đầu tiên)
                if (Concus_ImposedPrice == null || Concus_ImposedPrice.Count == 0)
                {
                    cusContract.dActivedate = cusContract.ServicePoint.ActiveDate;
                    cusContract.isFixActivedate = true;
                }
                else
                {
                    //lấy ngày đầu kỳ đã tính hóa đơn + 1
                    decimal maxBillID = 0;
                    maxBillID = _dbContext.Bill_ElectricityBillDetail.Where(o => o.DepartmentId == concusContract.DepartmentId
                                && o.PointId == cusContract.ServicePoint.PointId).Select(o2 => o2.BillId).DefaultIfEmpty(0).Max();
                    if (maxBillID == 0)
                    {
                        //trường hợp là điểm đo đầu nguồn
                        maxBillID = _dbContext.Loss_ElectricityBillDetail.Where(o => o.DepartmentId == concusContract.DepartmentId
                                && o.PointId == cusContract.ServicePoint.PointId).Select(o2 => o2.BillId).DefaultIfEmpty(0).Max();
                        if (maxBillID == 0)
                        {
                            cusContract.dActivedate = cusContract.ServicePoint.ActiveDate;
                            cusContract.isFixActivedate = true;
                        }
                        else
                        {
                            var ngayhdon = _dbContext.Loss_ElectricityBill.Where(o => o.BillId == maxBillID).FirstOrDefault().EndDate;
                            cusContract.dActivedate = ngayhdon.AddDays(1);
                            //cusContract.isFixActivedate = true;
                        }
                    }
                    else
                    {
                        var ngayhdon = _dbContext.Bill_ElectricityBill.Where(o => o.BillId == maxBillID).FirstOrDefault().EndDate;
                        cusContract.dActivedate = ngayhdon.AddDays(1);
                        //cusContract.isFixActivedate = true;
                    }
                }

                respone.Status = 1;
                respone.Message = "Lấy thông tin áp giá thành công.";
                respone.Data = cusContract;
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
        [Route("GetCategory_Occupations")]
        public HttpResponseMessage GetCategory_Occupations(int priceId)
        {
            try
            {
                var OccupationsGroup =
                    _dbContext.Category_Price.Where(item => item.PriceId.Equals(priceId) && item.ActiveDate <= DateTime.Now && item.EndDate > DateTime.Now)
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
                    _dbContext.Category_OccupationsGroup.Where(item => item.OccupationsGroupCode.Equals(OccupationsGroup.OccupationsGroupCode))
                        .Select(item => item.OccupationsGroupName)
                        .FirstOrDefault();

                var data = new
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

        [HttpPost]
        [Route("AddImposedPrice")]
        public HttpResponseMessage AddImposedPrice(List<Concus_ImposedPriceModel> models)
        {
            try
            {
                //Get userinfo from token
                var userId = TokenHelper.GetUserIdFromToken();
                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                string strAG = business_Concus_ServicePoint.CheckImposedPrice(models);
                if (strAG != "OK")
                {
                    throw new ArgumentException($"{strAG}");
                }

                using (var _dbContextTransaction = _dbContext.Database.BeginTransaction())
                {
                    try
                    {
                        for (int i = 0; i < models.Count; i++)
                        {
                            Concus_ImposedPriceModel prime = models[i];
                            prime.CreateDate = DateTime.Now;
                            prime.CreateUser = userId;
                            prime.DepartmentId = departmentId;

                            imposedPrice.AddConcus_ImposedPrice(prime, _dbContext);
                        }
                        _dbContextTransaction.Commit();

                        respone.Status = 1;
                        respone.Message = "Thêm biên bản áp giá thành công.";
                        respone.Data = null;
                        return createResponse();
                    }
                    catch (Exception ex)
                    {
                        _dbContextTransaction.Rollback();

                        throw new ArgumentException($"{ex.Message}");
                    }
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
        [Route("EditImposedPrice")]
        public HttpResponseMessage EditImposedPrice(List<Concus_ImposedPriceModel> models)
        {
            try
            {
                //Get userinfo from token
                var userId = TokenHelper.GetUserIdFromToken();
                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                string error = "";

                //Kiểm tra xem áp đủ tỷ lệ BCS chưa
                var listBCS = models.Select(o => o.TimeOfUse).Distinct().ToList();
                foreach (var strBCS in listBCS)
                {
                    var listPrice = models.Where(o => o.TimeOfUse == strBCS).ToList();
                    decimal tyle = 0;
                    bool conlai = false;
                    bool kwh = false;

                    foreach (var vPrice in listPrice)
                    {
                        if (vPrice.RatedType == "%")
                        {
                            if (vPrice.Rated == "C")
                                conlai = true;
                            else
                                tyle += Convert.ToDecimal(vPrice.Rated.Replace(".00", ""));
                        }
                        else //kwh
                            kwh = true;
                    }

                    if (tyle > 100)
                    {
                        throw new ArgumentException($"Tỷ lệ áp giá bộ chỉ số {strBCS} không được > 100%. Kiểm tra lại!");
                    }
                    else if (tyle < 100 && tyle > 0 && conlai == false)
                    {
                        throw new ArgumentException($"Tỷ lệ áp giá bộ chỉ số {strBCS} không được < 100%. Kiểm tra lại!");
                    }
                    else if (tyle == 0 && kwh == false)
                    {
                        throw new ArgumentException($"Tỷ lệ áp giá bộ chỉ số {strBCS} chưa được áp tỷ lệ. Kiểm tra lại!");
                    }
                }

                bool success = imposedPrice.EditConcus_ImposedPrice(models, models[0].PointId, userId, departmentId, ref error);
                if (!success)
                {
                    throw new ArgumentException($"{error}");
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
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return createResponse();
            }
        }
        #endregion

        #region Thanh lý hợp đồng
        [HttpPost]
        [Route("ContractLiquidation")]
        public HttpResponseMessage ContractLiquidation(ContractLiquidationInput model)
        {
            try
            {
                DateTime Day = DateTime.Now.Date;
                if (model.Liquidation != "")
                {
                    DateTime.TryParseExact(model.Liquidation, "dd-MM-yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out Day);
                }
                model.Contract.EndDate = Day;
                model.Contract.ReasonId = model.ReasonId;
                businessConcusContract.Liquidation_Contract(model.Contract);

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
        public HttpResponseMessage ContractExtension(ContractExtensionInput model)
        {
            try
            {
                DateTime Day = DateTime.Now;
                if (model.Extend != "")
                {
                    DateTime.TryParseExact(model.Extend, "dd-MM-yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out Day);
                }
                if (Day < DateTime.Now)
                {
                    throw new ArgumentException("Gia hạn hợp đồng không thành công, ngày gia hạn thêm hợp đồng phải lớn hơn hoặc bằng ngày hiện tại");
                }
                else
                {
                    model.Contract.CreateDate = DateTime.Now;
                    model.Contract.EndDate = Day;
                    businessConcusContract.Extension_Contract(model.Contract);

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

        #region Tra cứu khách hàng
        //Đã có api danh sách phân trang khách hàng

        [HttpGet]
        [Route("EditConcus_Customer")]
        public HttpResponseMessage EditConcus_Customer(int customerId)
        {
            try
            {
                var concuscustomerEdit =
                   _dbContext.Concus_Customer.Where(item => item.CustomerId.Equals(customerId))
                       .Select(item => new Concus_CustomerModel
                       {
                           CustomerId = item.CustomerId,
                           CustomerCode = item.CustomerCode,
                           DepartmentId = item.DepartmentId,
                           Name = item.Name,
                           Address = item.Address,
                           InvoiceAddress = item.InvoiceAddress,
                           Gender = item.Gender,
                           Email = item.Email,
                           PhoneNumber = item.PhoneNumber,
                           TaxCode = item.TaxCode,
                           Ratio = item.Ratio,
                           BankAccount = item.BankAccount,
                           BankName = item.BankName,
                           CreateDate = item.CreateDate,
                           CreateUser = item.CreateUser,
                           PhoneCustomerCare = item.PhoneCustomerCare,
                           OccupationsGroupCode = item.OccupationsGroupCode,
                           PaymentMethodsCode = item.PaymentMethodsCode,
                           PurposeOfUse = item.PurposeOfUse
                       }).FirstOrDefault();

                Customer_ContractModel customerContract = new Customer_ContractModel();
                customerContract.Customer = concuscustomerEdit;

                respone.Status = 1;
                respone.Message = "Lấy thông tin khách hàng thành công.";
                respone.Data = customerContract.Customer;
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
        [Route("EditConcus_Customer")]
        public HttpResponseMessage EditConcus_Customer(EditConcus_CustomerInput model)
        {
            try
            {
                model.ConCusConTract.Customer.OccupationsGroupCode = model.OccupationsGroupName;
                model.ConCusConTract.Customer.Gender = Convert.ToInt32(model.Gender);
                Concus_CustomerModel customer = new Concus_CustomerModel();
                customer = model.ConCusConTract.Customer;

                business_Concus_Customer.EditConcus_Customer(customer);

                respone.Status = 1;
                respone.Message = "Chỉnh sửa thông tin khách hàng thành công.";
                respone.Data = customer.CustomerId;
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

        #region Bảng giá
        [HttpGet]
        [Route("Category_Price")]
        public HttpResponseMessage Category_Price(string OccupationsGroupCode)
        {
            try
            {
                var categoryPrice = _dbContext.Category_Price.Where(item => item.OccupationsGroupCode.Equals(OccupationsGroupCode)).Select(item => new Category_PriceModel
                {
                    OccupationsGroupCode = item.OccupationsGroupCode,
                    Price = item.Price,
                    Time = item.Time,
                    PriceGroupCode = item.PriceGroupCode,
                    PotentialCode = item.PotentialSpace,
                    PriceId = item.PriceId,
                }).ToList();

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = categoryPrice;
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

        #region Sửa hợp đồng
        //Todo: chưa viết 
        #endregion

        #region Quản lý dịch vụ giá trị gia tăng
        [HttpPost]
        [Route("EditConcus_ContractDetail")]
        public HttpResponseMessage EditConcus_ContractDetail(Concus_ContractDetailModel model)
        {
            try
            {
                var userId = TokenHelper.GetUserIdFromToken();

                model.CreateDate = DateTime.Now;
                model.CreateUser = userId;
                if (model.ServiceTypeId == 7)
                {
                    model.Price = model.Price_7;
                    model.Po = model.Po_7;
                }
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

        [HttpPost]
        [Route("AddConcus_ContractDetail")]
        public HttpResponseMessage AddConcus_ContractDetail(Concus_ContractDetailModel model)
        {
            try
            {
                var userId = TokenHelper.GetUserIdFromToken();

                model.CreateDate = DateTime.Now;
                model.CreateUser = userId;
                if (model.ServiceTypeId == 7)
                {
                    model.Price = model.Price_7;
                    model.Po = model.Po_7;
                }
                businessConcusContractDetail.AddConcus_ContractDetail(model);

                respone.Status = 1;
                respone.Message = "Thêm mới dịch vụ thành công.";
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

        [HttpPost]
        [Route("DeleteConcus_ContractDetail")]
        public HttpResponseMessage DeleteConcus_ContractDetail(int contractDetailId)
        {
            try
            {
                var target = _dbContext.Concus_ContractDetail.Where(item => item.ContractDetailId == contractDetailId).FirstOrDefault();
                var contractId = target.ContractId;
                //20201121: Xuân ĐT: kiểm tra nếu có hóa đơn dịch vụ rồi thì chỉ cập nhật không sử dụng nữa (EndDateCV ! null)
                var hdct = _dbContext.Bill_TaxInvoiceDetail.Where(item => item.ContractDetailId == contractDetailId).FirstOrDefault();
                if (hdct != null)
                { //đã có hóa đơn
                    target.EndDateCV = DateTime.Now;
                }
                else
                {
                    _dbContext.Concus_ContractDetail.Remove(target);
                }
                _dbContext.SaveChanges();

                respone.Status = 1;
                respone.Message = "Xóa dịch vụ thành công!";
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

        [HttpGet]
        [Route("GetCustomerInfoByContract")]
        public HttpResponseMessage GetCustomerInfoByContract(int contractId)
        {
            try
            {
                Concus_ContractDetailViewModel viewModel = new Concus_ContractDetailViewModel();

                viewModel.ContractDetail = new Concus_ContractDetailModel();
                viewModel.ContractDetail.ContractId = contractId;

                //get customerId
                int customerId = _dbContext.Concus_Contract.Where(item => item.ContractId == contractId).FirstOrDefault().CustomerId;

                var customerModel = _dbContext.Concus_Customer.Where(item => item.CustomerId == customerId).Select(item => new Concus_CustomerModel()
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

                var listContractDetail = _dbContext.Concus_ContractDetail.Where(item => item.ContractId == contractId && item.EndDateCV == null).Select(item => new Concus_ContractDetailModel()
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

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = viewModel;
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
        [Route("GetCustomerInfoByContract")]
        public HttpResponseMessage GetCustomerInfoByContract(int contractId, int contractDetailId)
        {
            try
            {
                Concus_ContractDetailViewModel viewModel = new Concus_ContractDetailViewModel();

                viewModel.ContractDetail = new Concus_ContractDetailModel();
                viewModel.ContractDetail.ContractId = contractId;

                //get customerId
                int customerId = _dbContext.Concus_Contract.Where(item => item.ContractId == contractId).FirstOrDefault().CustomerId;

                var customerModel = _dbContext.Concus_Customer.Where(item => item.CustomerId == customerId).Select(item => new Concus_CustomerModel()
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

                var listContractDetail = _dbContext.Concus_ContractDetail.Where(item => item.ContractId == contractId && item.EndDateCV == null).Select(item => new Concus_ContractDetailModel()
                {
                    ServiceName = item.Bill_ServiceType.ServiceName,
                    Price = item.Price,
                    Description = item.Description,
                    ContractDetailId = item.ContractDetailId,
                    ContractId = item.ContractId
                }).ToList();
                viewModel.ListContractDetail = listContractDetail;

                var contractDetail = _dbContext.Concus_ContractDetail.Where(item => item.ContractDetailId == contractDetailId && item.EndDateCV == null).FirstOrDefault();

                var contractDetailModel = new Concus_ContractDetailModel();
                contractDetailModel.ContractId = contractDetail.ContractId;
                contractDetailModel.ActiveDate = contractDetail.ActiveDate;
                contractDetailModel.ContractDetailId = contractDetail.ContractDetailId;
                contractDetailModel.Description = contractDetail.Description;
                contractDetailModel.EndDate = contractDetail.EndDate;
                contractDetailModel.Po = contractDetail.Po;
                contractDetailModel.Po_7 = contractDetail.Po;
                contractDetailModel.PointId = contractDetail.PointId?.Split(',').ToList();
                contractDetailModel.Price = contractDetail.Price;
                contractDetailModel.Price_7 = contractDetail.Price;
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

                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = viewModel;
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
    }
}
