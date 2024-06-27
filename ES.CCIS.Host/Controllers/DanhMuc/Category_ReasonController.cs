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
    [RoutePrefix("api/DanhMuc/LyDo")]
    public class Category_ReasonController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_Administrator_Department administrator_Department = new Business_Administrator_Department();
        private readonly Business_Category_Reason businessReason = new Business_Category_Reason();

        [HttpGet]
        [Route("Category_ReasonManager")]
        public HttpResponseMessage Category_ReasonManager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search, int? group)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    var query = db.Category_Reason.Select(item => new Category_ReasonModel
                    {
                        Group = item.Group,
                        ReasonCode = item.ReasonCode,
                        ReasonId = item.ReasonId,
                        ReasonName = item.ReasonName
                    });

                    if (!string.IsNullOrEmpty(search))
                    {
                        query = (IQueryable<Category_ReasonModel>)query.Where(item => item.ReasonName.Contains(search) || item.ReasonCode.Contains(search));
                    }

                    if (group != null)
                    {
                        query = (IQueryable<Category_ReasonModel>)query.Where(item => item.Group == group);
                    }

                    var pagedReason = (IPagedList<Category_ReasonModel>)query.OrderBy(p => p.ReasonId).ToPagedList(pageNumber, pageSize);

                    var response = new
                    {
                        pagedReason.PageNumber,
                        pagedReason.PageSize,
                        pagedReason.TotalItemCount,
                        pagedReason.PageCount,
                        pagedReason.HasNextPage,
                        pagedReason.HasPreviousPage,
                        Reasons = pagedReason.ToList()
                    };
                    respone.Status = 1;
                    respone.Message = "Lấy danh sách lý do thành công.";
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
        public HttpResponseMessage GetReasonById(int reasonId)
        {
            try
            {
                if (reasonId < 0 || reasonId == 0)
                {
                    throw new ArgumentException($"ReasonId {reasonId} không hợp lệ.");
                }
                using (var dbContext = new CCISContext())
                {
                    var reason = dbContext.Category_Reason.Where(p => p.ReasonId == reasonId).Select(item => new Category_ReasonModel
                    {
                        Group = item.Group,
                        ReasonCode = item.ReasonCode,
                        ReasonId = item.ReasonId,
                        ReasonName = item.ReasonName
                    });

                    if (reason?.Any() == true)
                    {
                        var response = reason.FirstOrDefault();

                        respone.Status = 1;
                        respone.Message = "Lấy thông tin lý do thành công.";
                        respone.Data = response;
                        return createResponse();

                    }
                    else
                    {
                        throw new ArgumentException($"Lý do có ReasonId {reasonId} không tồn tại.");
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
        [Route("ThemMoi")]
        public HttpResponseMessage AddCategory_Reason(Category_ReasonModel model)
        {
            try
            {
                businessReason.AddCategory_Reason(model);

                using (var dbContext = new CCISContext())
                {
                    var lyDo = dbContext.Category_Reason.Where(p => p.ReasonName == model.ReasonName && p.ReasonCode == model.ReasonCode).FirstOrDefault();
                    if (lyDo != null)
                    {
                        respone.Status = 1;
                        respone.Message = "Thêm mới lý do thành công.";
                        respone.Data = lyDo.ReasonId;
                        return createResponse();
                    }
                    else
                    {
                        respone.Status = 0;
                        respone.Message = "Thêm mới lý do không thành công.";
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
        public HttpResponseMessage EditCategory_Reason(Category_ReasonModel model)
        {
            try
            {
                using (var dbContext = new CCISContext())
                {
                    var lyDo = dbContext.Category_Reason.Where(p => p.ReasonId == model.ReasonId).FirstOrDefault();
                    if (lyDo == null)
                    {
                        throw new ArgumentException($"Không tồn tại ReasonId {model.ReasonId}");
                    }

                    businessReason.EditCategory_Reason(model);

                    respone.Status = 1;
                    respone.Message = "Chỉnh sửa lý do thành công.";
                    respone.Data = model.ReasonId;

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
        public HttpResponseMessage DeleteCategory_Reason(int reasonId)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    var target = db.Category_Reason.Where(item => item.ReasonId == reasonId).FirstOrDefault();
                    db.Category_Reason.Remove(target);
                    db.SaveChanges();
                }
                respone.Status = 1;
                respone.Message = "Xóa lý do thành công.";
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
