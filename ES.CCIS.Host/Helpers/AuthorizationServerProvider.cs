using CCIS_DataAccess;
using Microsoft.Owin.Security.OAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using WebMatrix.WebData;

namespace ES.CCIS.Host.Helpers
{
    public class AuthorizationServerProvider: OAuthAuthorizationServerProvider
    {
        public override async Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            context.Validated();
        }
        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {            
            if (!WebMatrix.WebData.WebSecurity.Initialized) WebMatrix.WebData.WebSecurity.InitializeDatabaseConnection("DefaultConnection", "UserProfile", "UserId", "UserName", autoCreateTables: true);
            var membership = (WebMatrix.WebData.SimpleMembershipProvider)System.Web.Security.Membership.Provider;
            var checkLogin = membership.ValidateUser(context.UserName, context.Password);
            if (checkLogin)
            {
                using (var dbContext = new CCISContext())
                {
                    var user = dbContext.UserProfile.Where(p => p.UserName == context.UserName).FirstOrDefault();
                    if (user == null)
                    {
                        context.SetError("invalid_grant", "Tài khoản hoặc mật khẩu không đúng.");
                        return;
                    }
                    var identity = new ClaimsIdentity(context.Options.AuthenticationType);
                    identity.AddClaim(new Claim(ClaimTypes.Name, user.UserName));
                    identity.AddClaim(new Claim("FullName", user.FullName));
                    identity.AddClaim(new Claim("DepartmentId", user.DepartmentId.ToString()));
                    identity.AddClaim(new Claim("UserId", user.UserId.ToString()));
                    context.Validated(identity);
                }
            }
            else
            {
                context.SetError("invalid_grant", "Tài khoản hoặc mật khẩu không đúng.");
                return;
            }
        }
    }
}