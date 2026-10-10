using ArnoldOne.Shared.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArnoldOne.Server.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[TypeFilter(typeof(AdminOnlyFilter))]
public abstract class AdminApiControllerBase : ApiControllerBase
{
}
