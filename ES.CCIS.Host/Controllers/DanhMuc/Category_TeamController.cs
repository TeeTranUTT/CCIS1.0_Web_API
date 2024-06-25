using CCIS_BusinessLogic;
using CCIS_DataAccess;
using ES.CCIS.Host.Helpers;
using PagedList;
using System;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Web.Configuration;
using System.Web.Http;

namespace ES.CCIS.Host.Controllers.DanhMuc
{
    [Authorize]
    [RoutePrefix("api/DanhMuc/Doi")]
    public class Category_TeamController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_Administrator_Department business_Administrator_Department = new Business_Administrator_Department();
        private readonly Business_Category_Team businessTeam = new Business_Category_Team();
        public Category_TeamController()
        {

        }
        #region Danh mục đội
        [HttpGet]
        [Route("Category_TeamManager")]
        public HttpResponseMessage Category_TeamManager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search, [DefaultValue(0)] int departmentId)
        {
            try
            {
                //Thong tin user from token                
                var userInfo = TokenHelper.GetUserInfoFromRequest();
                if (departmentId == 0)
                    departmentId = TokenHelper.GetDepartmentIdFromToken();
                //list đơn vị con của đơn vị được search
                var listDepartments = DepartmentHelper.GetChildDepIds(departmentId);

                //list đơn vị con của user đăng nhập
                var lstDepCombo = DepartmentHelper.GetChildDepIds(business_Administrator_Department.GetIddv(userInfo.UserName));

                using (var db = new CCISContext())
                {
                    var query = db.Category_Team.Where(item => listDepartments.Contains(item.DepartmentId)).Select(item => new Category_TeamModel
                    {
                        TeamId = item.TeamId,
                        DepartmentId = item.DepartmentId,
                        TeamName = item.TeamName,
                        TeamCode = item.TeamCode,
                        PhoneNumber = item.PhoneNumber,
                        Status = item.Status
                    });


                    if (!string.IsNullOrEmpty(search))
                    {
                        query = (IQueryable<Category_TeamModel>)query.Where(item => item.TeamName.Contains(search) || item.TeamCode.Contains(search));
                    }
                    var pagedTeam = (IPagedList<Category_TeamModel>)query.OrderBy(p => p.TeamId).ToPagedList(pageNumber, pageSize);
                    var response = new
                    {
                        pagedTeam.PageNumber,
                        pagedTeam.PageSize,
                        pagedTeam.TotalItemCount,
                        pagedTeam.PageCount,
                        pagedTeam.HasNextPage,
                        pagedTeam.HasPreviousPage,
                        Teams = pagedTeam.ToList()
                    };

                    respone.Status = 1;
                    respone.Message = "Lấy danh sách đội thành công.";
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
        [Route("")]
        public HttpResponseMessage GetTeamById(int teamId)
        {
            try
            {
                if (teamId < 0 || teamId == 0)
                {
                    throw new ArgumentException($"teamId {teamId} không hợp lệ.");
                }
                using (var dbContext = new CCISContext())
                {
                    var team = dbContext.Category_Team.Where(p => p.TeamId == teamId).Select(p => new Category_TeamModel
                    {
                        TeamId = p.TeamId,
                        DepartmentId = p.DepartmentId,
                        TeamCode = p.TeamCode,                        
                        TeamName = p.TeamName,
                        PhoneNumber = p.PhoneNumber,
                        Status = p.Status
                    });

                    if (team?.Any() == true)
                    {
                        var response = team.FirstOrDefault();
                        if (response.Status)
                        {
                            respone.Status = 1;
                            respone.Message = "Lấy thông tin đội thành công.";
                            respone.Data = response;
                            return createResponse();
                        }
                        else
                        {
                            throw new ArgumentException($"Đội {response.TeamName} đã bị vô hiệu.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"Đội có TeamId {teamId} không tồn tại.");
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

        [HttpGet]
        [Route("GetAllTeamByDepartmentId")]
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
        [Route("ThemMoi")]
        public HttpResponseMessage AddCategory_Team(Category_TeamModel team)
        {
            try
            {
                #region Get DepartmentId From Token

                var departmentId = TokenHelper.GetDepartmentIdFromToken();

                team.DepartmentId = departmentId;
                #endregion                
                
                businessTeam.AddCategory_Team(team);

                using (var dbContext = new CCISContext())
                {
                    var doi = dbContext.Category_Team.Where(p => p.TeamName == team.TeamName && p.TeamCode == team.TeamCode).FirstOrDefault();
                    if (doi != null)
                    {
                        respone.Status = 1;
                        respone.Message = "Thêm mới đội thành công.";
                        respone.Data = doi.TeamId;
                        return createResponse();
                    }
                    else
                    {
                        respone.Status = 0;
                        respone.Message = "Thêm mới đội không thành công.";
                        respone.Data = null;
                        return createResponse();
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
        [Route("Sua")]
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
                  
                    businessTeam.EditCategory_Team(team);

                    respone.Status = 1;
                    respone.Message = "Chỉnh sửa đội thành công.";
                    respone.Data = team.TeamId;

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
        [Route("Xoa")]
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
