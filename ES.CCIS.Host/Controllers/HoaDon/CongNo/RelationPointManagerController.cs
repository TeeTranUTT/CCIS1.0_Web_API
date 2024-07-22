using CCIS_BusinessLogic;
using CCIS_DataAccess;
using ES.CCIS.Host.Helpers;
using ES.CCIS.Host.Models;
using ES.CCIS.Host.Models.EnumMethods;
using ES.CCIS.Host.Models.HoaDon.CongNo;
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
    [RoutePrefix("api/RelationPointManager")]
    public class RelationPointManagerController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);

        [HttpGet]
        [Route("")]
        public HttpResponseMessage Index([DefaultValue(0)] int departmentId, [DefaultValue(0)] int regionId, [DefaultValue("")] string search, [DefaultValue(1)] int pageNumber)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    List<int> lstDep = new List<int>();
                    var userInfo = TokenHelper.GetUserInfoFromRequest();
                    if (departmentId == 0)
                    {
                        lstDep = DepartmentHelper.GetChildDepIdsByUser(userInfo.UserName);
                    }
                    else
                    {
                        lstDep = DepartmentHelper.GetChildDepIds(departmentId);
                    }

                    IEnumerable<RelationPointManagerModel> lstPoint;

                    if (departmentId == 0)
                    {
                        lstPoint = new List<RelationPointManagerModel>();
                    }
                    else
                    {
                        lstPoint = (from cs in db.Concus_ServicePoint
                                    join ct in db.Concus_Contract.Where(x => lstDep.Contains(x.DepartmentId)) on cs.ContractId equals ct.ContractId
                                    join cr in db.Category_Regions.Where(x => lstDep.Contains(x.DepartmentId)) on cs.RegionId equals cr.RegionId
                                    where lstDep.Contains(cs.DepartmentId) && cs.Status == true
                                    select new RelationPointManagerModel
                                    {
                                        DepartmentId = cs.DepartmentId,
                                        PointCode = cs.PointCode,
                                        PointId = cs.PointId,
                                        ContractCode = ct.ContractCode,
                                        NumberOfPhase = cs.NumberOfPhases,
                                        RegionId = cs.RegionId ?? 0,
                                        RegionName = cr.RegionName,
                                        Status = cs.Status,
                                        ContractId = cs.ContractId
                                    });

                        if (regionId != 0)
                        {
                            lstPoint = lstPoint.Where(x => x.RegionId == regionId);
                        }

                        if (!string.IsNullOrEmpty(search))
                        {
                            lstPoint = lstPoint.Where(x => x.PointCode == search || x.ContractCode == search);
                        }
                    }

                    var paged = (IPagedList<RelationPointManagerModel>)lstPoint.OrderBy(p => p.PointCode).ToPagedList(pageNumber, pageSize);

                    var response = new
                    {
                        paged.PageNumber,
                        paged.PageSize,
                        paged.TotalItemCount,
                        paged.PageCount,
                        paged.HasNextPage,
                        paged.HasPreviousPage,
                        RelationPoints = paged.ToList()
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

        [HttpGet]
        [Route("ShowPointList")]
        public HttpResponseMessage ShowPointList([DefaultValue(0)] int departmentId, [DefaultValue(0)] int regionId, [DefaultValue("")] string search,
            [DefaultValue(0)] int ContractId, [DefaultValue(0)] int PointId)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    IEnumerable<RelationPointManagerModel> lstPoint;
                    var lstDep = DepartmentHelper.GetChildDepIds(departmentId);
                    var depCombo = db.Administrator_Department.Where(x => lstDep.Contains(x.DepartmentId)).ToList();

                    var lstPointRe = db.Concus_SPRelation_ManagementFee.Where(x => x.ContractId == ContractId).Select(x => x.PointId).ToList();

                    var lstPointInRe = db.Concus_SPRelation_ManagementFee.Where(x => !lstPointRe.Contains(x.PointId)).Select(x => x.PointId).Distinct().ToList();

                    if (departmentId == 0)
                    {
                        lstPoint = new List<RelationPointManagerModel>();
                    }
                    else
                    {
                        lstPoint = (from cs in db.Concus_ServicePoint.Where(x => x.PointId != PointId && x.Status == true && !lstPointInRe.Contains(x.PointId))
                                    join ct in db.Concus_Contract.Where(x => lstDep.Contains(x.DepartmentId)) on cs.ContractId equals ct.ContractId
                                    join cr in db.Category_Regions.Where(x => lstDep.Contains(x.DepartmentId)) on cs.RegionId equals cr.RegionId
                                    where lstDep.Contains(cs.DepartmentId)
                                    select new RelationPointManagerModel
                                    {
                                        DepartmentId = cs.DepartmentId,
                                        PointCode = cs.PointCode,
                                        PointId = cs.PointId,
                                        ContractCode = ct.ContractCode,
                                        NumberOfPhase = cs.NumberOfPhases,
                                        RegionId = cs.RegionId ?? 0,
                                        RegionName = cr.RegionName,
                                        Status = cs.Status,
                                        ContractId = ContractId
                                    });

                        if (regionId != 0)
                        {
                            lstPoint = lstPoint.Where(x => x.RegionId == regionId);
                        }

                        if (!string.IsNullOrEmpty(search))
                        {
                            lstPoint = lstPoint.Where(x => x.PointCode == search || x.ContractCode == search);
                        }
                    }

                    var lst = lstPoint.ToList();
                    lst.ForEach(x =>
                    {
                        if (lstPointRe.Contains(x.PointId))
                        {
                            x.Checked = true;
                        }
                    });

                    var response = new
                    {
                        ContractId = ContractId,
                        RegionId = regionId,
                        DepartmentId = departmentId,
                        PointKey = PointId,
                        ListPoint = lst
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
        [Route("SaveRelation")]
        public HttpResponseMessage SaveRelation(int[] lstPoint, int ContractId)
        {
            try
            {
                // lấy dang sách công nợ ứng với BillId;
                using (var db = new CCISContext())
                {
                    var lstPointDetail = db.Concus_ServicePoint.Where(x => lstPoint.Contains(x.PointId)).ToList();
                    var lstPointIdDelete = db.Concus_SPRelation_ManagementFee.Where(x => x.ContractId == ContractId).Select(x => x.PointId).ToList();
                    var lstDelete = db.Concus_SPRelation_ManagementFee.Where(x => lstPointIdDelete.Contains(x.PointId)).Select(x => x).ToList();

                    db.Concus_SPRelation_ManagementFee.RemoveRange(lstDelete);
                    db.SaveChanges();

                    foreach (var item in lstPointDetail)
                    {
                        Concus_SPRelation_ManagementFee spr = new Concus_SPRelation_ManagementFee();
                        spr.ContractId = item.ContractId;
                        spr.DepartmentId = item.DepartmentId;
                        spr.FigureBookId = item.FigureBookId;
                        spr.PointId = item.PointId;

                        db.Concus_SPRelation_ManagementFee.Add(spr);
                        db.SaveChanges();

                        foreach (var x in lstPointDetail)
                        {
                            if (x.PointId != item.PointId)
                            {
                                Concus_SPRelation_ManagementFee spr2 = new Concus_SPRelation_ManagementFee();
                                spr2.ContractId = item.ContractId;
                                spr2.DepartmentId = item.DepartmentId;
                                spr2.FigureBookId = item.FigureBookId;
                                spr2.PointId = x.PointId;

                                db.Concus_SPRelation_ManagementFee.Add(spr2);
                                db.SaveChanges();
                            }
                        }
                    }
                    
                    respone.Status = 1;
                    respone.Message = "Thiết lập quan hệ thành công.";
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
    }
}
