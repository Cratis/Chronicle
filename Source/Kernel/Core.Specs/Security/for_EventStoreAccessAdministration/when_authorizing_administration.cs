// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using ArcAuthorizeAttribute = Cratis.Arc.Authorization.AuthorizeAttribute;

namespace Cratis.Chronicle.Security.for_EventStoreAccessAdministration;

public class when_authorizing_administration : Specification
{
    [Theory]
    [InlineData(typeof(GrantEventStoreAccess))]
    [InlineData(typeof(RevokeEventStoreAccess))]
    [InlineData(typeof(EventStoreAccessGrants))]
    [InlineData(typeof(EventStoreAccessPreflight))]
    void should_require_the_administrator_policy(Type surface) => surface.GetCustomAttribute<ArcAuthorizeAttribute>().Policy.ShouldEqual(WellKnownAuthorizationPolicies.Administrator);

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    async Task should_enforce_at_kernel_scope_regardless_of_opt_outs(bool administrator, bool application, bool allowed)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "subject")], "spec"));
        var admins = Substitute.For<IAdministratorAuthorization>();
        admins.IsAuthorized(principal).Returns(administrator && !application);
        var options = Options.Create(new ChronicleOptions { Authentication = new() { EnforceAdministratorPolicyForOperations = false, EnforceDestructiveOperations = false, EnforceStoreEntitlements = false } });
        var decisions = new AuthorizationDecisions(admins, options, Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthorizationDecisions>.Instance, Substitute.For<IStoreEntitlementsCache>());
        var context = new AuthorizationHandlerContext([new AdministratorRequirement()], principal, null);
        await new AdministratorAuthorizationHandler(decisions).HandleAsync(context);
        context.HasSucceeded.ShouldEqual(allowed);
    }
}
