using ES.CCIS.Host.Helpers;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Owin;
using Microsoft.Owin.Security.OAuth;
using Owin;
using System;
using System.Collections.Generic;
using System.Web.Http;

[assembly: OwinStartup(typeof(ES.CCIS.Host.Startup))]

namespace ES.CCIS.Host
{
    public class Startup
    {
        private IEnumerable<IDisposable> GetHangfireServers()
        {
            //Cấu hình HangFire
            Hangfire.GlobalConfiguration.Configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseFilter(new AutomaticRetryAttribute { Attempts = 0 }) // Tắt chế độ retry khi lỗi xảy ra
                .UseSqlServerStorage("DefaultConnection", new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true,

                });

            yield return new BackgroundJobServer();
        }

        public void Configuration(IAppBuilder app)
        {
            // Any connection or hub wire up and configuration should go here 
            app.UseHangfireAspNet(GetHangfireServers);
            app.UseHangfireDashboard();

            // Cấu hình Authentication và Authorization
            app.UseCors(Microsoft.Owin.Cors.CorsOptions.AllowAll);

            OAuthAuthorizationServerOptions options = new OAuthAuthorizationServerOptions
            {
                AllowInsecureHttp = true,
                //The Path For generating the Toekn
                TokenEndpointPath = new PathString("/token"),
                //Setting the Token Expired Time (24 hours)
                AccessTokenExpireTimeSpan = TimeSpan.FromDays(30),
                //MyAuthorizationServerProvider class will validate the user credentials
                Provider = new AuthorizationServerProvider()
            };
            //Token Generations
            app.UseOAuthAuthorizationServer(options);
            app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions());

            HttpConfiguration config = new HttpConfiguration();
             
            WebApiConfig.Register(config);
        }
    }
}
