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

namespace ES.CCIS.Host.Controllers.KhachHang_HopDong_DiemDo
{
    [Authorize]
    [RoutePrefix("api/KhachHang")]
    public class Concus_CustomerController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_Administrator_Department administrator_Department = new Business_Administrator_Department();
        private readonly Business_Concus_Customer business_Concus_Customer = new Business_Concus_Customer();
        private readonly Business_Administrator_Parameter vParameters = new Business_Administrator_Parameter();
        private readonly CCISContext _dbContext;

        public Concus_CustomerController()
        {
            _dbContext = new CCISContext();
        }

        #region Quản lý khách hàng
        [HttpGet]
        [Route("Concus_CustomerManager")]
        public HttpResponseMessage Concus_CustomerManager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search,
            [DefaultValue(0)] int departmentId)
        {
            try
            {
                //Thong tin user from token                
                var userInfo = TokenHelper.GetUserInfoFromRequest();
                if (departmentId == 0)
                    departmentId = TokenHelper.GetDepartmentIdFromToken();
                //list đơn vị con của đơn vị được search
                var listDepartments = DepartmentHelper.GetChildDepIds(departmentId);

                var query = _dbContext.Concus_Customer.Where(item => listDepartments.Contains(item.DepartmentId) && item.Status == 1).Select(item => new Concus_CustomerModel
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
                    Status = item.Status,
                    CreateDate = item.CreateDate,
                    CreateUser = item.CreateUser,
                    PhoneCustomerCare = item.PhoneCustomerCare,
                    ZaloCustomerCare = item.ZaloCustomerCare
                });

                if (!string.IsNullOrEmpty(search))
                {
                    query = (IQueryable<Concus_CustomerModel>)query.Where(item => item.Name.Contains(search) || item.CustomerCode.Contains(search) || item.PhoneCustomerCare.Contains(search) || item.Address.Contains(search) || item.TaxCode.Contains(search));
                }

                var pagedCustomer = (IPagedList<CustomerCodeModel>)query.OrderBy(p => p.CustomerCode).ToPagedList(pageNumber, pageSize);

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
                respone.Message = "Lấy danh sách khách hàng thành công.";
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
        public HttpResponseMessage GetCustomerById(int customerId)
        {
            try
            {
                if (customerId < 0 || customerId == 0)
                {
                    throw new ArgumentException($"CustomerId {customerId} không hợp lệ.");
                }
                var customer = _dbContext.Concus_Customer.Where(p => p.CustomerId == customerId).Select(item => new Concus_CustomerModel
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
                    Fax = item.Fax,
                    BankAccount = item.BankAccount,
                    BankName = item.BankName,
                    CreateDate = item.CreateDate,
                    CreateUser = item.CreateUser,
                    PhoneCustomerCare = item.PhoneCustomerCare,
                    OccupationsGroupCode = item.OccupationsGroupCode,
                    PaymentMethodsCode = item.PaymentMethodsCode,
                    PurposeOfUse = item.PurposeOfUse,
                    BuyerName = item.BuyerName,
                    RelationsShip = _dbContext.Concus_Customer_Relationship.Where(x => x.CustomerId == item.CustomerId && x.DepartmentId == item.DepartmentId && !x.IsDelete).Select(
                                x => new Concus_Customer_RelationshipModel
                                {
                                    CustomerId = item.CustomerId,
                                    DepartmentId = item.DepartmentId,
                                    Address = x.Address,
                                    Email = x.Email,
                                    CustomerRelationshipId = x.CustomerRelationshipId,
                                    FullName = x.FullName,
                                    Phone = x.Phone,
                                    Relationship = x.Relationship,
                                    CMT = x.CMT,
                                    NoiCap = x.NoiCap,
                                    NgayCap = x.NgayCap,
                                    NguoiDaiDien = x.NguoiDaiDien,
                                    NgayKyUyQuyen = x.NgayKyUyQuyen,
                                    MST = x.MST,
                                    GiayUyQuyen = x.GiayUyQuyen
                                }).ToList()
                });
                if (customer?.Any() == true)
                {
                    var response = customer.FirstOrDefault();
                    if (response.Status == EnumMethod.TrangThai.KichHoat)
                    {
                        var concusCustomerAdd = _dbContext.Concus_CustomerAdditionalInfo.FirstOrDefault(x => x.CustomerId == response.CustomerId);
                        if (concusCustomerAdd != null)
                        {
                            response.CMT = concusCustomerAdd.CMT;
                            response.NoiCap = concusCustomerAdd.NoiCap;
                            response.NgayCap = concusCustomerAdd.NgayCap;
                            response.NguoiDaiDien = concusCustomerAdd.NguoiDaiDien;
                            response.GiayUyQuyen = concusCustomerAdd.GiayUyQuyen;
                            response.NgayKyUyQuyen = concusCustomerAdd.NgayKyUyQuyen;
                        }

                        respone.Status = 1;
                        respone.Message = "Lấy thông tin khách hàng thành công.";
                        respone.Data = response;
                        return createResponse();
                    }
                    else
                    {
                        throw new ArgumentException($"Khách hàng {response.Name} đã bị vô hiệu.");
                    }
                }
                else
                {
                    throw new ArgumentException($"Khách hàng có customerId {customerId} không tồn tại.");
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
        [Route("ThemMoi")]
        public HttpResponseMessage AddConcus_Customer(Concus_CustomerModel model)
        {
            try
            {
                #region Get DepartmentId From Token

                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var userId = TokenHelper.GetUserIdFromToken();
                #endregion

                //Kiểm tra tồn tại mã khách hàng hay chưa
                int customerId = 0;

                if (ModelState.IsValid)
                {
                    string strCheckInfor = business_Concus_Customer.CheckCustomerModal(model);
                    if (strCheckInfor != "OK")
                    {
                        throw new ArgumentException($"{strCheckInfor}");
                    }
                    else if (business_Concus_Customer.CheckExistCustomerCode(model.CustomerCode))
                    {
                        throw new ArgumentException($"Mã khách hàng {model.CustomerCode} đã tồn tại.");
                    }
                    else
                    {
                        model.DepartmentId = departmentId;
                        model.CreateUser = userId;
                        customerId = business_Concus_Customer.AddConcus_Customer(model);

                        if (customerId > 0)
                        {
                            var add = new Concus_CustomerAdditionalInfo
                            {
                                CustomerId = customerId,
                                CMT = model.CMT,
                                NgayCap = model.NgayCap,
                                NoiCap = model.NoiCap,
                                NguoiDaiDien = model.NguoiDaiDien,
                                GiayUyQuyen = model.GiayUyQuyen,
                                NgayKyUyQuyen = model.NgayKyUyQuyen
                            };

                            _dbContext.Concus_CustomerAdditionalInfo.Add(add);
                            _dbContext.SaveChanges();

                            respone.Status = 1;
                            respone.Message = "Thêm mới khách hàng thành công.";
                            respone.Data = customerId;
                            return createResponse();
                        }
                        else
                        {
                            respone.Status = 0;
                            respone.Message = "Thêm mới khách hàng không thành công.";
                            respone.Data = null;
                            return createResponse();
                        }
                    }
                }
                else
                {
                    respone.Status = 0;
                    respone.Message = "Thêm mới khách hàng không thành công.";
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

        [HttpPost]
        [Route("Sua")]
        public HttpResponseMessage EditConcus_Customer(Concus_CustomerModel model)
        {
            try
            {
                var customer = _dbContext.Concus_Customer.Where(p => p.CustomerId == model.CustomerId).FirstOrDefault();
                if (customer == null)
                {
                    throw new ArgumentException($"Không tồn tại CustomerId {model.CustomerId}");
                }

                #region Get DepartmentId From Token

                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var userId = TokenHelper.GetUserIdFromToken();

                model.DepartmentId = departmentId;
                #endregion
                string strCheckInfor = business_Concus_Customer.CheckCustomerModal(model);
                if (strCheckInfor != "OK")
                {
                    throw new ArgumentException($"{strCheckInfor}");
                }
                else if (business_Concus_Customer.CheckExistCustomerCode_Edit(model.CustomerCode, model.CustomerId))
                {
                    business_Concus_Customer.EditConcus_Customer(model);
                    var add = _dbContext.Concus_CustomerAdditionalInfo.FirstOrDefault(x => x.CustomerId == model.CustomerId);
                    if (add != null)
                    {
                        // Add Log
                        var log = new Concus_CustomerAdditionalInfo_Log
                        {
                            CustomerId = add.CustomerId,
                            CMT = add.CMT,
                            NgayCap = add.NgayCap,
                            NoiCap = add.NoiCap,
                            NguoiDaiDien = add.NguoiDaiDien,
                            GiayUyQuyen = add.GiayUyQuyen,
                            NgayKyUyQuyen = add.NgayKyUyQuyen,
                            CreatedDate = DateTime.Now,
                            CreatedUser = userId
                        };
                        _dbContext.Concus_CustomerAdditionalInfo_Log.Add(log);

                        // Cập nhật thay đổi bảng chính
                        add.CMT = model.CMT;
                        add.NoiCap = model.NoiCap;
                        add.NgayCap = model.NgayCap;
                        add.NguoiDaiDien = model.NguoiDaiDien;
                        add.GiayUyQuyen = model.GiayUyQuyen;
                        add.NgayKyUyQuyen = model.NgayKyUyQuyen;


                    }
                    else
                    {
                        _dbContext.Concus_CustomerAdditionalInfo.Add(new Concus_CustomerAdditionalInfo
                        {
                            CustomerId = model.CustomerId,
                            CMT = model.CMT,
                            NgayCap = model.NgayCap,
                            NoiCap = model.NoiCap,
                            NguoiDaiDien = model.NguoiDaiDien,
                            GiayUyQuyen = model.GiayUyQuyen,
                            NgayKyUyQuyen = model.NgayKyUyQuyen
                        }); ;
                    }
                    _dbContext.SaveChanges();

                    respone.Status = 1;
                    respone.Message = "Chỉnh sửa khách hàng thành công.";
                    respone.Data = model.CustomerId;

                    return createResponse();
                }
                else
                {
                    respone.Status = 0;
                    respone.Message = "Chỉnh sửa khách hàng không thành công.";
                    respone.Data = model.CustomerId;

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
        [Route("Reset_ZaloID")]
        public HttpResponseMessage Reset_ZaloID(int customerId)
        {
            try
            {
                var customer = _dbContext.Concus_Customer.Where(o => o.CustomerId == customerId).FirstOrDefault();
                if (customer != null)
                {
                    customer.ZaloCustomerCare = null;
                    _dbContext.SaveChanges();
                    respone.Status = 1;
                    respone.Message = "OK";
                    respone.Data = null;
                    return createResponse();
                }
                else
                {
                    throw new ArgumentException("Không tim thấy khách hàng này trong hệ thống");
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

        #region Bổ sung form thêm kh, hợp đồng, điểm đo, áp giá trên 1 màn hình
        [HttpGet]
        [Route("GetAllData")]
        public HttpResponseMessage GetAllData()
        {
            try
            {
                var departmentId = TokenHelper.GetDepartmentIdFromToken();
                var userId = TokenHelper.GetUserIdFromToken();
                var lstDepartmentId = DepartmentHelper.GetChildDepIds(departmentId);
                var TDN = vParameters.GetParameterValue("CheDoTinhTienNuoc", "KHONGCO", departmentId);
                List<object> data = new List<object>();

                // lấy danh sách loại hợp đồng : thông tin hợp đồng
                var Ds_HopDong = _dbContext.Category_ContractType.Where(item => item.Status == true).ToList();

                // lấy danh sách loại điểm đo: điểm đo
                var Ds_LoaiDiemDo = _dbContext.Category_ServicePointType.Select(item => new Category_ServicePointTypeModel
                {
                    ServicePointType = item.ServicePointType,
                    Description = (_dbContext.Category_ServicePointType
                        .Where(a => a.ServicePointTypeId.Equals(item.ServicePointType)).Select(a => a.Description)
                        .FirstOrDefault())

                }).Distinct().ToList();

                // Lấy danh sách cấp điện áp: điểm đo
                List<Category_PotentialModel> potential = new List<Category_PotentialModel>();
                if (TDN == "TINH_PHI_THOAT_NUOC" || TDN == "TINH_PHI_BVE_MTRUONG" || TDN == "PHI_BVE_MTRUONG_0DONG")
                {
                    potential = _dbContext.Category_Potential.Where(item => item.Status == true).Select(item =>
                    new Category_PotentialModel
                    {
                        PotentialCode = item.PotentialCode,
                        PotentialName = item.PotentialCode + "-" + item.PotentialName

                    }).ToList();
                }
                else
                {
                    potential = _dbContext.Category_Potential.Where(item => item.Status == true && item.PotentialCode != "0").Select(item =>
                    new Category_PotentialModel
                    {
                        PotentialCode = item.PotentialCode,
                        PotentialName = item.PotentialCode + "-" + item.PotentialName

                    }).ToList();
                }

                // lấy sanh sách tổ: điểm đo
                var team = _dbContext.Category_Team.Where(item => item.Status == true && lstDepartmentId.Contains(item.DepartmentId)).Select(item => new Category_TeamModel
                {
                    TeamId = item.TeamId,
                    TeamName = item.TeamCode + "-" + item.TeamName,
                    TeamCode = item.TeamCode,
                    PhoneNumber = item.PhoneNumber
                }).ToList();

                // lấy danh sách trạm : điểm đo
                var satiton = _dbContext.Category_Satiton.Where(item => item.Status == true && lstDepartmentId.Contains(item.DepartmentId)).Select(item =>
                    new Category_SatitonModel
                    {
                        StationId = item.StationId,
                        StationCode = item.StationCode,
                        StationName = item.StationCode + "-" + item.StationName,
                        Type = item.Type,
                        Power = item.Power
                    }).ToList();

                // lấy danh sách lộ: điểm đo
                var route = _dbContext.Category_Route.Where(item => item.Status == true && lstDepartmentId.Contains(item.DepartmentId)).Select(item => new Category_RouteModel
                {
                    RouteId = item.RouteId,
                    RouteCode = item.RouteCode,
                    RouteName = item.RouteCode + "-" + item.RouteName,
                    Type = item.Type,
                    PotentialCode = item.PotentialCode
                }).ToList();

                // lấy danh sách sổ ghi chỉ số: điểm đo
                var figureBook = DepartmentHelper.GetFigureBook(userId, lstDepartmentId);
                figureBook = figureBook.Select(item => new Category_FigureBookModel
                {
                    FigureBookId = item.FigureBookId,
                    BookName = item.BookCode + "-" + item.BookName,
                    BookType = item.BookType,
                    TeamId = item.TeamId
                }).ToList();

                // lấy danh sách khu vực: điểm đo
                var khuvuc = _dbContext.Category_Regions.ToList();
                var tilethue = _dbContext.Category_TaxRatio.ToList().OrderBy(o => o.Seq);

                // lấy danh sách điểm đo chính: điểm đo
                // lấy danh sách  loại bộ chỉ số: áp giá
                // lấy danh sách đối tượng giá: áp giá
                // lấy ra ngành ngê: áp giá
                // lấy ra đơn giá: áp giá
                data.Add(Ds_HopDong);
                data.Add(Ds_LoaiDiemDo);
                data.Add(potential);
                data.Add(team);
                data.Add(satiton);
                data.Add(route);
                data.Add(figureBook);
                data.Add(khuvuc);
                data.Add(tilethue);

                respone.Status = 1;
                respone.Message = "Lấy thông tin danh mục thành công.";
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

        [HttpGet]
        [Route("MainMeasurementPointInformation")]
        public HttpResponseMessage MainMeasurementPointInformation(int figureBookId)
        {
            try
            {
                List<object> data = new List<object>();
                // lấy danh sách loại điểm đo chính: điểm đo
                var listServicePointOfFigureBook = _dbContext.Concus_ServicePoint
                    .Where(item => item.FigureBookId == figureBookId && item.Status == true).Select(item =>
                        new Concus_ServicePointModel
                        {
                            PointId = item.PointId,
                            PointCode = item.PointCode
                        }).ToList();
                data.Add(listServicePointOfFigureBook);

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

        // Chọn sổ theo trạm với điều kiện mã trạm = mã sổ
        [HttpGet]
        [Route("GetFigueBookSelected")]
        public HttpResponseMessage GetFigueBookSelected([DefaultValue(1)] int matchvalue)
        {
            try
            {
                var userInfo = TokenHelper.GetUserIdFromToken();
                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                var listDepartment = DepartmentHelper.GetChildDepIds(departmentId);
                var figureBook = DepartmentHelper.GetFigureBook(userInfo, listDepartment);

                // Truy vấn cơ sở dữ liệu
                var getStationCode = _dbContext.Category_Satiton
                                       .Where(item => item.StationId == matchvalue)
                                       .Select(item => item.StationCode)
                                       .FirstOrDefault();

                // Lấy phần tử được chọn
                var selectedItem = _dbContext.Category_FigureBook
                                     .Where(item => item.BookCode == getStationCode)
                                     .Select(item => new Category_FigureBookModel
                                     {
                                         FigureBookId = item.FigureBookId,
                                         BookName = item.BookCode + "-" + item.BookName,
                                         BookType = item.BookType
                                     })
                                     .FirstOrDefault();

                if (selectedItem != null)
                {
                    figureBook = figureBook.Select(item => new Category_FigureBookModel
                    {
                        FigureBookId = item.FigureBookId,
                        BookName = item.BookCode + "-" + item.BookName,
                        BookType = item.BookType
                    }).ToList();

                    // Đảm bảo không có phần tử nào trùng với phần tử đã chọn
                    figureBook = figureBook.Where(item => item.FigureBookId != selectedItem.FigureBookId).ToList();

                    // Chèn phần tử đã chọn vào đầu danh sách
                    figureBook.Insert(0, selectedItem);
                }
                respone.Status = 1;
                respone.Message = "OK";
                respone.Data = figureBook.ToArray();
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

        // lấy thông tin bộ chỉ số trong áp giá khi thay đổi loại điểm đo
        [HttpGet]
        [Route("GetBCS")]
        public HttpResponseMessage GetBCS(int value)
        {
            try
            {
                List<object> data = new List<object>();
                // lấy danh sách loại điểm đo chính: điểm đo

                var list = _dbContext.Category_ServicePointType.Where(item => item.ServicePointType == value).Select(item =>
                    new Category_ServicePointTypeModel
                    {
                        TimeOfUse = item.TimeOfUse
                    }).Distinct().ToList();
                data.Add(list);

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

        // lấy thông tin đối tượng áp giá
        [HttpGet]
        [Route("GetInformationPrice")]
        public HttpResponseMessage GetInformationPrice(string value)
        {
            try
            {
                List<object> data = new List<object>();
                var categoryPrice = (from D in _dbContext.Category_PotentialReference
                                     join E in _dbContext.Category_Price on D.PotentialSpace equals E.PotentialSpace
                                     where D.OccupationsGroupCode == E.OccupationsGroupCode
                                        && D.PotentialCode == value
                                        && E.ActiveDate <= DateTime.Now && DateTime.Now < (DateTime)E.EndDate.Value
                                     select new Category_PriceModel
                                     {
                                         OccupationsGroupCode = E.OccupationsGroupCode + "-" + E.Time + "-" + E.Price + "   [" + E.Description + "]",
                                         PriceId = E.PriceId,
                                         Description = E.Description,
                                         Time = E.Time
                                     }).ToList();
                data.Add(categoryPrice);

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

        [HttpGet]
        [Route("ChangePrice")]
        public HttpResponseMessage ChangePrice(int value)
        {
            try
            {
                // lấy danh sách loại điểm đo chính: điểm đo
                var OccupationsGroup =
                    _dbContext.Category_Price.Where(item =>
                            item.PriceId.Equals(value) && item.ActiveDate <= DateTime.Now &&
                            item.EndDate > DateTime.Now)
                        .Select(item => new Category_PriceModel
                        {
                            OccupationsGroupCode = item.OccupationsGroupCode,
                            Price = item.Price,
                            PotentialCode = item.PotentialSpace,
                            PriceGroupCode = item.PriceGroupCode,
                            Time = item.Time,
                            ActiveDate = item.ActiveDate
                        }).FirstOrDefault();

                var data = new ChangePriceModel()
                {
                    OccupationsGroupCode = OccupationsGroup.OccupationsGroupCode,
                    Price = OccupationsGroup.Price,
                    PotentialCode = OccupationsGroup.PotentialCode,
                    PriceGroupCode = OccupationsGroup.PriceGroupCode,
                    Time = OccupationsGroup.Time,
                    OccupationsGroupName = OccupationsGroup != null
                        ? null
                        : _dbContext.Category_OccupationsGroup.Where(item =>
                                item.OccupationsGroupCode.Equals(OccupationsGroup.OccupationsGroupCode))
                            .Select(item => item.OccupationsGroupName)
                            .FirstOrDefault(),
                    ActiveDate = OccupationsGroup.ActiveDate.Day + "/" + OccupationsGroup.ActiveDate.Month + "/" + OccupationsGroup.ActiveDate.Year
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

        //TODO: còn api thêm nhanh khách hàng hợp đồng điểm đo áp giá chưa viết
        #endregion

        #region Sinh mã khách hàng tự động
        [HttpPost]
        [Route("GenerateCustomerCode")]
        public HttpResponseMessage GenerateCustomerCode(GenerateCustomerCodeInput model)
        {
            try
            {
                if (model != null)
                {
                    var result = business_Concus_Customer.GenerateCustomerCode(model.TeamId, model.CodeCustom, model.RegionId, model.ServiceType);

                    respone.Status = 1;
                    respone.Message = "OK";
                    respone.Data = result;
                    return createResponse();
                }
                else
                {
                    throw new ArgumentException("Dữ liệu đầu vào không được để trống.");
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

        #region Khởi tạo khách hàng bằng file
        [HttpPost]
        [Route("AddCustomerAndElectricityMeterByFile")]
        public HttpResponseMessage Add_Customer_And_ElectricityMeter_ByFile(List<ConcusCustomerAndElectricityMeter_FromFileModel> model)
        {
            try
            {
                string strKQ = "OK";

                //Lấy userId, deparmentId từ token
                var userId = TokenHelper.GetUserIdFromToken();
                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                //Đặt lại số thứ tự
                for (int i = 0; i < model.Count; i++)
                {
                    model[i].SoThuTu = i + 1;
                }

                //Kiểm tra dữ liệu?
                strKQ = business_Concus_Customer.CheckCustomerInfoAndElectricityMeterFromFile(model, departmentId);

                if (strKQ != "OK")
                {
                    throw new ArgumentException($"{strKQ}");
                }

                strKQ = business_Concus_Customer.InsertCustomerInfoAndElectricityMeterFromFile(model, departmentId, userId);

                if (strKQ != "OK")
                {
                    throw new ArgumentException($"{strKQ}");
                }
                else
                {
                    respone.Status = 1;
                    respone.Message = "Thêm khách hàng và công tơ bằng file thành công.";
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

        [HttpPost]
        [Route("AddCustomerByFile")]
        public HttpResponseMessage Add_Customer_ByFile(List<Concus_FromFileModel> model)
        {
            try
            {
                string strKQ = "OK";

                //Lấy userId, deparmentId từ token
                var userId = TokenHelper.GetUserIdFromToken();
                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                //Đặt lại số thứ tự
                for (int i = 0; i < model.Count; i++)
                {
                    model[i].SoThuTu = i + 1;
                }
                //Kiểm tra dữ liệu?
                strKQ = business_Concus_Customer.CheckCustomerInfoFromFile(model, departmentId);

                if (strKQ != "OK")
                {
                    throw new ArgumentException($"{strKQ}");
                }

                strKQ = business_Concus_Customer.InsertCustomerInfoFromFile(model, departmentId, userId);

                if (strKQ != "OK")
                {
                    throw new ArgumentException($"{strKQ}");
                }
                else
                {
                    respone.Status = 1;
                    respone.Message = "Thêm khách hàng bằng file thành công.";
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

        [HttpPost]
        [Route("AddElectricityMeterByFile")]
        public HttpResponseMessage Add_ElectricityMeter_ByFile(List<ElectricityMeter_FromFileModel> model)
        {
            try
            {
                string strKQ = "OK";

                //Lấy userId, deparmentId từ token
                var userId = TokenHelper.GetUserIdFromToken();
                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                //Đặt lại số thứ tự
                for (int i = 0; i < model.Count; i++)
                {
                    model[i].SoThuTu = i + 1;
                }

                //Kiểm tra dữ liệu?
                strKQ = business_Concus_Customer.CheckElectricityMeterFromFile(model, departmentId);

                if (strKQ != "OK")
                {
                    throw new ArgumentException($"{strKQ}");
                }

                strKQ = business_Concus_Customer.InsertElectricityMeterFromFile(model, departmentId, userId);

                if (strKQ != "OK")
                {
                    throw new ArgumentException($"{strKQ}");
                }
                else
                {
                    respone.Status = 1;
                    respone.Message = "Thêm công tơ bằng file thành công.";
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

        [HttpPost]
        [Route("AddCustomerRelationship")]
        public HttpResponseMessage Add_Customer_Relationship(Concus_Customer_RelationshipModel model)
        {
            try
            {
                using (var _dbContext = new CCISContext())
                {
                    var checkExistCustomer = _dbContext.Concus_Customer.Any(x => x.CustomerId == model.CustomerId && x.DepartmentId == model.DepartmentId);
                    if (checkExistCustomer)
                    {
                        var relationship = new Concus_Customer_Relationship
                        {
                            CustomerId = model.CustomerId,
                            DepartmentId = model.DepartmentId,
                            Address = model.Address,
                            FullName = model.FullName,
                            Email = model.Email,
                            Phone = model.Phone,
                            IsDelete = false,
                            Relationship = model.Relationship,
                            CreatedDate = DateTime.Now,
                            MST = model.MST,
                            CMT = model.CMT,
                            NgayCap = model.NgayCap,
                            NoiCap = model.NoiCap,
                            GiayUyQuyen = model.GiayUyQuyen,
                            NgayKyUyQuyen = model.NgayKyUyQuyen,
                            NguoiDaiDien = model.NguoiDaiDien
                        };

                        _dbContext.Concus_Customer_Relationship.Add(relationship);
                        _dbContext.SaveChanges();

                        respone.Status = 1;
                        respone.Message = "Thêm thành công.";
                        respone.Data = relationship.CustomerRelationshipId;
                        return createResponse();
                    }
                    else
                    {
                        throw new ArgumentException("Không tìm thấy khách hàng tương ứng.");
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
        [Route("DeleteCustomerRelationship")]
        public HttpResponseMessage Delete_Customer_Relationship(int id, int departmentId, int customerId)
        {
            try
            {
                var relation = _dbContext.Concus_Customer_Relationship.Where(x => x.CustomerRelationshipId == id && x.DepartmentId == departmentId && x.CustomerId == customerId).FirstOrDefault();
                relation.IsDelete = true;
                _dbContext.SaveChanges();

                respone.Status = 1;
                respone.Message = "Đã xóa thành công.";
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
