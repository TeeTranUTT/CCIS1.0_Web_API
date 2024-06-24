using System;
using System.Linq;
using System.Threading;
using System.Web.Mvc;
using WebMatrix.WebData;

namespace ES.CCIS.Host.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class InitializeAdministratorAttribute : ActionFilterAttribute
    {
        private static AdministratorInitializer _initializer;
        private static object _initializerLock = new object();
        private static bool _isInitialized;

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Ensure ASP.NET Simple Membership is initialized only once per app start
            LazyInitializer.EnsureInitialized(ref _initializer, ref _isInitialized, ref _initializerLock);
        }

        private class AdministratorInitializer
        {
            public AdministratorInitializer()
            {
                try
                {
                    if (!WebSecurity.Initialized)
                        WebSecurity.InitializeDatabaseConnection("DefaultConnection", "UserProfile", "UserId", "UserName", autoCreateTables: true);
                    if (!WebSecurity.UserExists("administrator"))
                        WebSecurity.CreateUserAndAccount("administrator", "admin@123", false);

                    //todo: hieulv do phiên bản migration đang lấy ở bản có dev nên tạm rem đoạn này
                    //var migrator = new DbMigrator(new Configuration());
                    //if (migrator.GetPendingMigrations().Any())
                    //    migrator.Update();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("The ASP.NET Simple Membership database could not be initialized. For more information, please see http://go.microsoft.com/fwlink/?LinkId=256588", ex);
                }
            }
        }
    }
}
