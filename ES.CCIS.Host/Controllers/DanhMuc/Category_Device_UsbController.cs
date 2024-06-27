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
    [RoutePrefix("api/DanhMuc/ChungThuSo")]
    public class Category_Device_UsbController : ApiBaseController
    {
        private int pageSize = int.Parse(WebConfigurationManager.AppSettings["PageSize"]);
        private readonly Business_Administrator_Department administrator_Department = new Business_Administrator_Department();

        [HttpGet]
        [Route("Category_Device_UsbManager")]
        public HttpResponseMessage Category_Device_UsbManager([DefaultValue(1)] int pageNumber, [DefaultValue("")] string search, [DefaultValue(0)] int departmentId)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    var query = db.Category_Device_Usb.Where(item => item.Status == true).Select(item => new Category_Device_UsbModel
                    {
                        IdDevice = item.IdDevice,
                        Name = item.Name,
                        Seri = item.Seri,
                        ActiveDate = item.ActiveDate,
                        EndDate = item.EndDate,
                        Status = item.Status
                    });

                    if (!string.IsNullOrEmpty(search))
                    {
                        query = (IQueryable<Category_Device_UsbModel>)query.Where(item => item.Name.Contains(search) || item.Seri.Contains(search));
                    }

                    var pagedDeviceUsb = (IPagedList<Category_Device_UsbModel>)query.OrderBy(p => p.IdDevice).ToPagedList(pageNumber, pageSize);

                    var response = new
                    {
                        pagedDeviceUsb.PageNumber,
                        pagedDeviceUsb.PageSize,
                        pagedDeviceUsb.TotalItemCount,
                        pagedDeviceUsb.PageCount,
                        pagedDeviceUsb.HasNextPage,
                        pagedDeviceUsb.HasPreviousPage,
                        DeviceUsbs = pagedDeviceUsb.ToList()
                    };
                    respone.Status = 1;
                    respone.Message = "Lấy danh sách chứng thư số thành công.";
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
        public HttpResponseMessage GetDeviceUsbById(int deviceId)
        {
            try
            {
                if (deviceId < 0 || deviceId == 0)
                {
                    throw new ArgumentException($"DeviceId {deviceId} không hợp lệ.");
                }
                using (var dbContext = new CCISContext())
                {
                    var chungThuSo = dbContext.Category_Device_Usb.Where(p => p.IdDevice == deviceId).Select(item => new Category_Device_UsbModel
                    {
                        IdDevice = item.IdDevice,
                        Name = item.Name,
                        Seri = item.Seri,
                        ActiveDate = item.ActiveDate,
                        EndDate = item.EndDate,
                        Status = item.Status
                    });

                    if (chungThuSo?.Any() == true)
                    {
                        var response = chungThuSo.FirstOrDefault();
                        if (response.Status)
                        {
                            respone.Status = 1;
                            respone.Message = "Lấy thông tin chứng thư số thành công.";
                            respone.Data = response;
                            return createResponse();
                        }
                        else
                        {
                            throw new ArgumentException($"Chứng thư số {response.Name} đã bị vô hiệu.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"Chứng thư số có IdDevice {deviceId} không tồn tại.");
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
        public HttpResponseMessage AddCategory_Device_Usb(Category_Device_UsbModel model)
        {
            try
            {
                if (model != null && model.EndDate < model.ActiveDate)
                {
                    throw new ArgumentException("Ngày hết hạn phải lớn hơn ngày hiệu lực.");
                }
                using (var dbContext = new CCISContext())
                {
                    Category_Device_Usb usb = new Category_Device_Usb();
                    usb.ActiveDate = model.ActiveDate;
                    usb.EndDate = model.EndDate;
                    usb.Name = model.Name;
                    usb.Seri = model.Seri;
                    usb.Status = true;
                    dbContext.Category_Device_Usb.Add(usb);
                    dbContext.SaveChanges();
                    model.IdDevice = usb.IdDevice;

                    var chungLoaiTU = dbContext.Category_Device_Usb.Where(p => p.Name == model.Name).FirstOrDefault();
                    if (chungLoaiTU != null)
                    {
                        respone.Status = 1;
                        respone.Message = "Thêm mới chứng thư số thành công.";
                        respone.Data = chungLoaiTU.IdDevice;
                        return createResponse();
                    }
                    else
                    {
                        respone.Status = 0;
                        respone.Message = "Thêm mới chứng thư số không thành công.";
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
        public HttpResponseMessage EditCategory_Device_Usb(Category_Device_UsbModel model)
        {
            try
            {
                using (var dbContext = new CCISContext())
                {
                    var chungThuSo = dbContext.Category_Device_Usb.Where(p => p.IdDevice == model.IdDevice).FirstOrDefault();
                    if (chungThuSo == null)
                    {
                        throw new ArgumentException($"Không tồn tại IdDevice {model.IdDevice}");
                    }

                    if (model != null && model.EndDate < model.ActiveDate)
                    {
                        throw new ArgumentException("Ngày hết hạn phải lớn hơn ngày hiệu lực.");
                    }

                    var target = dbContext.Category_Device_Usb.Where(item => item.IdDevice == model.IdDevice).FirstOrDefault();
                    target.Name = model.Name;
                    target.Seri = model.Seri;
                    target.ActiveDate = model.ActiveDate;
                    target.EndDate = model.EndDate;
                    target.Status = model.Status;
                    dbContext.SaveChanges();  

                    respone.Status = 1;
                    respone.Message = "Chỉnh sửa chứng thư số thành công.";
                    respone.Data = model.IdDevice;

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
        public HttpResponseMessage DeleteCategory_Device_Usb(int deviceId)
        {
            try
            {
                using (var db = new CCISContext())
                {
                    var target = db.Category_Device_Usb.Where(item => item.IdDevice == deviceId).FirstOrDefault();
                    db.Category_Device_Usb.Remove(target);
                    db.SaveChanges();
                }
                respone.Status = 1;
                respone.Message = "Xóa chứng thư số thành công.";
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
