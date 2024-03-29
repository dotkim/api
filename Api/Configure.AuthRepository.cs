using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Redis;
using ServiceStack.Web;

namespace Api
{
  // Custom UserAuth Data Model with extended Metadata properties
  public class AppUser : UserAuth
  {
    public string ProfileUrl { get; set; }
    public string LastLoginIp { get; set; }
    public DateTime? LastLoginDate { get; set; }
  }

  public class AppUserAuthEvents : AuthEvents
  {
    public override void OnAuthenticated(IRequest req, IAuthSession session, IServiceBase authService,
        IAuthTokens tokens, Dictionary<string, string> authInfo)
    {
      var authRepo = HostContext.AppHost.GetAuthRepository(req);
      using (authRepo as IDisposable)
      {
        var userAuth = (AppUser)authRepo.GetUserAuth(session.UserAuthId);
        userAuth.ProfileUrl = session.GetProfileUrl();
        userAuth.LastLoginIp = req.UserHostAddress;
        userAuth.LastLoginDate = DateTime.UtcNow;
        authRepo.SaveUserAuth(userAuth);
      }
    }
  }

  public class ConfigureAuthRepository : IConfigureAppHost, IConfigureServices, IPreInitPlugin
  {
    public void Configure(IServiceCollection services)
    {
      services.AddSingleton<IAuthRepository>(c =>
          new RedisAuthRepository<AppUser, UserAuthDetails>(c.Resolve<IRedisClientsManager>()));
    }

    public void Configure(IAppHost appHost)
    {
      var authRepo = appHost.Resolve<IAuthRepository>();
      authRepo.InitSchema();

      var builder = new ConfigurationBuilder().AddXmlFile($"./config/config.xml", true, true);
      AppConfig config = builder.Build().Get<AppConfig>();
      CreateUser(authRepo,
        config.Email,
        config.Name,
        config.Password,
        roles: new[] { RoleNames.Admin });
    }

    public void BeforePluginsLoaded(IAppHost appHost)
    {
      appHost.AssertPlugin<AuthFeature>().AuthEvents.Add(new AppUserAuthEvents());
    }

    // Add initial Users to the configured Auth Repository
    public void CreateUser(IAuthRepository authRepo, string email, string name, string password, string[] roles)
    {
      if (authRepo.GetUserAuthByUserName(email) == null)
      {
        var newAdmin = new AppUser { Email = email, DisplayName = name };
        var user = authRepo.CreateUserAuth(newAdmin, password);
        authRepo.AssignRoles(user, roles);
      }
    }
  }
}
