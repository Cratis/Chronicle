// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorizationServiceCollectionExtensions;

public class when_a_transient_check_is_registered : Specification
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task should_resolve_the_check_for_each_decision(bool before)
    {
        var count = 0;
        var check = Substitute.For<IAdministratorAuthorization>();
        check.IsAuthorized(Arg.Any<ClaimsPrincipal>()).Returns(true);
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton(Options.Create(new Configuration.ChronicleOptions()));
        services.AddSingleton(Substitute.For<IAdministratorPrivileges>());
        void Register() => services.AddTransient<IAdministratorAuthorization>(_ =>
        {
            count++;
            return check;
        });
        if (before) Register();
        services.AddChronicleAdministratorAuthorization();
        if (!before) Register();
        await using var root = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        await using var scope = root.CreateAsyncScope();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "caller")], "spec"));
        root.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext { User = principal, RequestServices = scope.ServiceProvider };
        var decisions = root.GetRequiredService<IAuthorizationDecisions>();
        (await decisions.Decide(principal, WellKnownCapabilities.AdministerSecurity, AuthorizationScope.Kernel)).IsAllowed.ShouldBeTrue();
        (await decisions.Decide(principal, WellKnownCapabilities.AdministerSecurity, AuthorizationScope.Kernel)).IsAllowed.ShouldBeTrue();
        count.ShouldEqual(2);
    }
}
