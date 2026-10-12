using Tyngd.Shared.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tyngd.Server.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[TypeFilter(typeof(AdminOnlyFilter))]
public abstract class AdminApiControllerBase : ApiControllerBase
{
}
