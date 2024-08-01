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
        private readonly CCISContext _dbContext;

        public RelationPointManagerController()
        {
            _dbContext = new CCISContext();
        }

        [HttpGet]
        [Route("")]
        public HttpResponseMessage Index([DefaultValue(0)] int departmentId, [DefaultValue(0)] int regionId, [DefaultValue("")] string search, [DefaultValue(1)] int pageNumber)
        {
            try
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
                    lstPoint = (from cs in _dbContext.Concus_ServicePoint
                                join ct in _dbContext.Concus_Contract.Where(x => lstDep.Contains(x.DepartmentId)) on cs.ContractId equals ct.ContractId
                                join cr in _dbContext.Category_Regions.Where(x => lstDep.Contains(x.DepartmentId)) on cs.RegionId equals cr.RegionId
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
                IEnumerable<RelationPointManagerModel> lstPoint;
                var lstDep = DepartmentHelper.GetChildDepIds(departmentId);
                var depCombo = _dbContext.Administrator_Department.Where(x => lstDep.Contains(x.DepartmentId)).ToList();

                var lstPointRe = _dbContext.Concus_SPRelation_ManagementFee.Where(x => x.ContractId == ContractId).Select(x => x.PointId).ToList();

                var lstPointInRe = _dbContext.Concus_SPRelation_ManagementFee.Where(x => !lstPointRe.Contains(x.PointId)).Select(x => x.PointId).Distinct().ToList();

                if (departmentId == 0)
                {
                    lstPoint = new List<RelationPointManagerModel>();
                }
                else
                {
                    lstPoint = (from cs in _dbContext.Concus_ServicePoint.Where(x => x.PointId != PointId && x.Status == true && !lstPointInRe.Contains(x.PointId))
                                join ct in _dbContext.Concus_Contract.Where(x => lstDep.Contains(x.DepartmentId)) on cs.ContractId equals ct.ContractId
                                join cr in _dbContext.Category_Regions.Where(x => lstDep.Contains(x.DepartmentId)) on cs.RegionId equals cr.RegionId
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
                var lstPointDetail = _dbContext.Concus_ServicePoint.Where(x => lstPoint.Contains(x.PointId)).ToList();
                var lstPointIdDelete = _dbContext.Concus_SPRelation_ManagementFee.Where(x => x.ContractId == ContractId).Select(x => x.PointId).ToList();
                var lstDelete = _dbContext.Concus_SPRelation_ManagementFee.Where(x => lstPointIdDelete.Contains(x.PointId)).Select(x => x).ToList();

                _dbContext.Concus_SPRelation_ManagementFee.RemoveRange(lstDelete);
                _dbContext.SaveChanges();

                foreach (var item in lstPointDetail)
                {
                    Concus_SPRelation_ManagementFee spr = new Concus_SPRelation_ManagementFee();
                    spr.ContractId = item.ContractId;
                    spr.DepartmentId = item.DepartmentId;
                    spr.FigureBookId = item.FigureBookId;
                    spr.PointId = item.PointId;

                    _dbContext.Concus_SPRelation_ManagementFee.Add(spr);
                    _dbContext.SaveChanges();

                    foreach (var x in lstPointDetail)
                    {
                        if (x.PointId != item.PointId)
                        {
                            Concus_SPRelation_ManagementFee spr2 = new Concus_SPRelation_ManagementFee();
                            spr2.ContractId = item.ContractId;
                            spr2.DepartmentId = item.DepartmentId;
                            spr2.FigureBookId = item.FigureBookId;
                            spr2.PointId = x.PointId;

                            _dbContext.Concus_SPRelation_ManagementFee.Add(spr2);
                            _dbContext.SaveChanges();
                        }
                    }
                }

                respone.Status = 1;
                respone.Message = "Thiết lập quan hệ thành công.";
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
