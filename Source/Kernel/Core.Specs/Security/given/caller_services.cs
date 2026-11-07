// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.Security.given;

[IgnoreConvention]
public static class caller_services
{
    public static IHttpContextAccessor For(IAdministratorAuthorization administrator)
    {
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IAdministratorAuthorization)).Returns(administrator);
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext { RequestServices = services });
        return accessor;
    }
}
