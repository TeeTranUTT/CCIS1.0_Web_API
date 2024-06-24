using CCIS_BusinessLogic;
using CCIS_DataAccess;
using ES.CCIS.Host.Commons;
using ES.CCIS.Host.Helpers;
using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Configuration;
using System.Web.Http;

namespace ES.CCIS.Host.Controllers
{
    [Authorize]
    public class CategoryController : ApiBaseController
    {
        private int PageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        #region Cấp điện áp
        [HttpPost]
        [Route("api/DanhMuc/CapDienAp/ThemMoi")]
        public HttpResponseMessage AddCategory_Potential(Category_PotentialModel model)
        {
            try
            {
                Business_Category_Potential business = new Business_Category_Potential();
                business.AddCategory_Potential(model);
                respone.Status = 1;
                respone.Message = "Thêm mới cấp điện áp thành công.";
                respone.Data = model;
                return Request.CreateResponse(HttpStatusCode.OK, respone, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return Request.CreateResponse(HttpStatusCode.OK, respone, Configuration.Formatters.JsonFormatter);
            }
        }
        #endregion

        #region Danh mục đội
        [HttpGet]
        [Route("api/DanhMuc/Doi/Category_TeamManager")]
        public HttpResponseMessage Category_TeamManager([DefaultValue(1)] int page, [DefaultValue("")] string search, [DefaultValue(0)] int departmentId)
        {
            try
            {
                Business_Administrator_Department Administrator_Department = new Business_Administrator_Department();
                //Thong tin user from token                
                var userInfo = TokenHelper.GetUserInfoFromRequest();
                if (departmentId == 0)
                    departmentId = TokenHelper.GetDepartmentIdFromToken();
                //list đơn vị con của đơn vị được search
                var listDepartments = DepartmentHelper.GetChildDepIds(departmentId);
                
                //list đơn vị con của user đăng nhập
                var lstDepCombo = DepartmentHelper.GetChildDepIds(Administrator_Department.GetIddv(userInfo.UserName));

                using (var db = new CCISContext())
                {
                    var model = new StaticPagedList<Category_TeamModel>(db.Category_Team.Where(item => (item.TeamName.Contains(search) || item.TeamCode.Contains(search)) && listDepartments.Contains(item.DepartmentId)).Select(item => new Category_TeamModel
                    {
                        TeamId = item.TeamId,
                        DepartmentId = item.DepartmentId,
                        TeamName = item.TeamName,
                        TeamCode = item.TeamCode,
                        PhoneNumber = item.PhoneNumber,
                        Status = item.Status
                    }).OrderBy(item => item.TeamId).Skip((page - 1) * PageSize).Take(PageSize).ToList(), page, PageSize, db.Category_Team.Where(item => (item.TeamName.Contains(search) || item.TeamCode.Contains(search)) && listDepartments.Contains(item.DepartmentId)).Count());

                    respone.Status = 1;
                    respone.Message = "Lấy danh sách đội thành công.";
                    respone.Data = model;
                    return Request.CreateResponse(HttpStatusCode.OK, respone, Configuration.Formatters.JsonFormatter);
                }
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return Request.CreateResponse(HttpStatusCode.OK, respone, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpGet]
        [Route("api/DanhMuc/Doi/GetAllTeamByDepartmentId")]
        public HttpResponseMessage GetAllTeamByDepartmentId([DefaultValue(0)] int? departmentId)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    var listCategory = db.Category_Team
                        .Where(item => item.DepartmentId == departmentId && item.Status == true)
                        .Select(item => new Category_TeamModel
                        {
                            TeamId = item.TeamId,
                            TeamCode = item.TeamCode,
                            TeamName = item.TeamName,
                            DepartmentId = item.DepartmentId,
                            PhoneNumber = item.PhoneNumber,
                            Status = item.Status                            
                        }).ToList();

                    respone.Status = 1;
                    respone.Message = "Lấy thông tin đội thành công.";
                    respone.Data = listCategory;
                    return Request.CreateResponse(HttpStatusCode.OK, respone, Configuration.Formatters.JsonFormatter);
                }
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return Request.CreateResponse(HttpStatusCode.OK, respone, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpPost]
        [Route("api/DanhMuc/Doi/ThemMoi")]
        public HttpResponseMessage AddCategory_Team(Category_TeamModel team)
        {
            try
            {
                #region Get DepartmentId From Token
               
                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                team.DepartmentId = departmentId;
                #endregion                

                Business_Category_Team businessTeam = new Business_Category_Team();
                businessTeam.AddCategory_Team(team);

                using (var dbContext = new CCISContext())
                {
                    var doi = dbContext.Category_Team.Where(p => p.TeamName == team.TeamName && p.TeamCode == team.TeamCode).FirstOrDefault();
                    if (doi != null)
                    {
                        respone.Status = 1;
                        respone.Message = "Thêm mới đội thành công.";
                        respone.Data = doi.TeamId;
                        return Request.CreateResponse(HttpStatusCode.OK, respone, Configuration.Formatters.JsonFormatter);
                    }
                    else
                    {
                        respone.Status = 0;
                        respone.Message = "Thêm mới đội không thành công.";
                        respone.Data = null;
                        return Request.CreateResponse(HttpStatusCode.OK, respone, Configuration.Formatters.JsonFormatter);
                    }
                }                
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return Request.CreateResponse(HttpStatusCode.OK, respone, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpPost]
        [Route("api/DanhMuc/Doi/Sua")]
        public HttpResponseMessage EditCategory_Team(Category_TeamModel team)
        {
            try
            {
                using (var dbContext = new CCISContext())
                {
                    var doi = dbContext.Category_Team.Where(p => p.TeamId == team.TeamId).FirstOrDefault();
                    if (doi == null)
                    {
                        throw new ArgumentException($"Không tồn tại TeamId {team.TeamId}"); 
                    }

                    #region Get DepartmentId From Token

                    var departmentId = TokenHelper.GetDepartmentIdFromToken();

                    team.DepartmentId = departmentId;
                    #endregion

                    Business_Category_Team businessTeam = new Business_Category_Team();
                    businessTeam.EditCategory_Team(team);
                    
                    respone.Status = 1;
                    respone.Message = "Chỉnh sửa đội thành công.";
                    respone.Data = team.TeamId;

                    return Request.CreateResponse(HttpStatusCode.OK, respone, Configuration.Formatters.JsonFormatter);
                }
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return Request.CreateResponse(HttpStatusCode.OK, respone, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpPost]
        [Route("api/DanhMuc/Doi/Xoa")]
        public HttpResponseMessage DeleteCategory_Team(int teamId)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    var target = db.Category_Team.Where(item => item.TeamId == teamId).FirstOrDefault();
                    target.Status = false;
                    db.SaveChanges();
                }
                respone.Status = 1;
                respone.Message = "Xóa đội thành công.";
                respone.Data = null;
                return Request.CreateResponse(HttpStatusCode.OK, respone, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return Request.CreateResponse(HttpStatusCode.OK, respone, Configuration.Formatters.JsonFormatter);
            }
        }
        #endregion

        #region Trạm
        [HttpGet]
        [Route("api/DanhMuc/Tram/Category_SatitonManager")]
        public HttpResponseMessage Category_SatitonManager([DefaultValue(1)] int page, [DefaultValue("")] string search, [DefaultValue(0)] int departmentId)
        {
            try
            {
                Business_Administrator_Department Administrator_Department = new Business_Administrator_Department();
                //Thong tin user from token                
                var userInfo = TokenHelper.GetUserInfoFromRequest();
                if (departmentId == 0)
                    departmentId = TokenHelper.GetDepartmentIdFromToken();
                //list đơn vị con của đơn vị được search
                var listDepartments = DepartmentHelper.GetChildDepIds(departmentId);

                //list đơn vị con của user đăng nhập
                var lstDepCombo = DepartmentHelper.GetChildDepIds(Administrator_Department.GetIddv(userInfo.UserName));

                using (var db = new CCISContext())
                {
                    var model = new StaticPagedList<Category_SatitonModel>(db.Category_Satiton.Where(item => (item.StationName.Contains(search) || item.StationCode.Contains(search)) && listDepartments.Contains(item.DepartmentId)).Select(item => new Category_SatitonModel
                    {
                        StationId = item.StationId,
                        DepartmentId = item.DepartmentId,
                        StationName = item.StationName,
                        StationCode = item.StationCode,
                        Type = item.Type,
                        Power = item.Power,
                        Status = item.Status
                    }).OrderBy(item => item.StationId).Skip((page - 1) * PageSize).Take(PageSize).ToList(), page, PageSize,
                           db.Category_Satiton.Where(item => (item.StationName.Contains(search) || item.StationCode.Contains(search)) && listDepartments.Contains(item.DepartmentId)).Count());

                    respone.Status = 1;
                    respone.Message = "Lấy danh sách trạm thành công.";
                    respone.Data = model;
                    return Request.CreateResponse(HttpStatusCode.OK, respone, Configuration.Formatters.JsonFormatter);
                }
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return Request.CreateResponse(HttpStatusCode.OK, respone, Configuration.Formatters.JsonFormatter);
            }
        }
        
        [HttpPost]
        [Route("api/DanhMuc/Tram/ThemMoi")]
        public HttpResponseMessage AddCategory_Satiton(Category_SatitonModel station)
        {
            try
            {
                #region Get DepartmentId From Token

                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                station.DepartmentId = departmentId;
                #endregion                

                Business_Category_Satiton businessstation = new Business_Category_Satiton();
                businessstation.AddCategory_Satiton(station);

                using (var dbContext = new CCISContext())
                {
                    var tram = dbContext.Category_Satiton.Where(p => p.StationName == station.StationName && p.StationCode == station.StationCode).FirstOrDefault();
                    if (tram != null)
                    {
                        respone.Status = 1;
                        respone.Message = "Thêm mới trạm thành công.";
                        respone.Data = tram.StationId;
                        return Request.CreateResponse(HttpStatusCode.OK, respone, Configuration.Formatters.JsonFormatter);
                    }
                    else
                    {
                        respone.Status = 0;
                        respone.Message = "Thêm mới trạm không thành công.";
                        respone.Data = null;
                        return Request.CreateResponse(HttpStatusCode.OK, respone, Configuration.Formatters.JsonFormatter);
                    }
                }
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return Request.CreateResponse(HttpStatusCode.OK, respone, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpPost]
        [Route("api/DanhMuc/Tram/Sua")]
        public HttpResponseMessage EditCategory_Satiton(Category_SatitonModel station)
        {
            try
            {
                using (var dbContext = new CCISContext())
                {
                    var doi = dbContext.Category_Satiton.Where(p => p.StationId == station.StationId).FirstOrDefault();
                    if (doi == null)
                    {
                        throw new ArgumentException($"Không tồn tại SatitonId {station.StationId}");
                    }

                    #region Get DepartmentId From Token

                    var departmentId = TokenHelper.GetDepartmentIdFromToken();

                    station.DepartmentId = departmentId;
                    #endregion

                    Business_Category_Satiton businessstation = new Business_Category_Satiton();
                    businessstation.EditCategory_Satiton(station);

                    
                    respone.Status = 1;
                    respone.Message = "Chỉnh sửa trạm thành công.";
                    respone.Data = station.StationId;

                    return Request.CreateResponse(HttpStatusCode.OK, respone, Configuration.Formatters.JsonFormatter);
                }
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return Request.CreateResponse(HttpStatusCode.OK, respone, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpPost]
        [Route("api/DanhMuc/Tram/Xoa")]
        public HttpResponseMessage DeleteCategory_Satiton(int stationId)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    //kiểm tra điều kiện xóa: đảm bảo không có điểm đo nào trong trạm này
                    var kiemtra = db.Concus_ServicePoint.Where(item => item.StationId == stationId)
                        .Select(item2 => new Concus_ServicePointModel
                        {
                            PointCode = item2.PointCode
                        }).ToList();
                    if (kiemtra.Count > 0)
                    {
                        throw new ArgumentException($"Đã có điểm đo " + kiemtra[0].PointCode + " trong trạm, không xóa được.");                        
                    }
                    var kiemtra2 = db.Concus_ServicePoint_Log.Where(item => item.StationId == stationId)
                        .Select(item2 => new Concus_ServicePoint_LogModel
                        {
                            PointCode = item2.PointCode
                        }).ToList();
                    if (kiemtra2.Count > 0)
                    {
                        throw new ArgumentException($"Đã có điểm đo " + kiemtra2[0].PointCode + " trong trạm, không xóa được.");                        
                    }

                    var target = db.Category_Satiton.Where(item => item.StationId == stationId).FirstOrDefault();
                    target.Status = false;
                    db.SaveChanges();
                }
                respone.Status = 1;
                respone.Message = "Xóa trạm thành công.";
                respone.Data = null;
                return Request.CreateResponse(HttpStatusCode.OK, respone, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                respone.Status = 0;
                respone.Message = $"Lỗi: {ex.Message.ToString()}";
                respone.Data = null;
                return Request.CreateResponse(HttpStatusCode.OK, respone, Configuration.Formatters.JsonFormatter);
            }
        }
        #endregion
    }
}
