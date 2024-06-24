using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Web;
using Microsoft.Owin;

namespace ES.CCIS.Host.Helpers
{
    public class UserInfo
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string DepartmentId { get; set; }
    }

    public static class TokenHelper
    {
        /// <summary>
        /// Lấy thông tin từ token
        /// </summary>
        /// <param name="identity">ClaimsIdentity từ token</param>
        /// <returns>Thông tin người dùng</returns>
        public static UserInfo GetUserInfoFromToken(ClaimsIdentity identity)
        {
            if (identity == null)
            {
                return null;
            }

            return new UserInfo
            {
                UserId = identity.FindFirst("UserId")?.Value,
                UserName = identity.FindFirst(ClaimTypes.Name)?.Value,
                DepartmentId = identity.FindFirst("DepartmentId")?.Value,
                FullName = identity.FindFirst("FullName")?.Value
            };
        }

        /// <summary>
        /// Lấy thông tin người dùng từ HttpRequest
        /// </summary>
        /// <returns>Thông tin người dùng hoặc null nếu không tìm thấy</returns>
        public static UserInfo GetUserInfoFromRequest()
        {
            var context = HttpContext.Current;
            if (context == null || context.User == null)
            {
                return null;
            }

            var identity = context.User.Identity as ClaimsIdentity;
            return GetUserInfoFromToken(identity);
        }        

        public static int GetDepartmentIdFromToken()
        {
            var userInfo = GetUserInfoFromRequest();
            if (int.TryParse(userInfo.DepartmentId, out int departmentId))
            {
                return departmentId;
            }
            else
            {
                throw new ArgumentException("Có lỗi xảy ra trong quá trình lấy thông tin departmentId từ token.");
            }
        }
    }
}