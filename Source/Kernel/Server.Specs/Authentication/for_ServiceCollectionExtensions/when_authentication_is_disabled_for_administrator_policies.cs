// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_authentication_is_disabled_for_administrator_policies : given.chronicle_authentication_services
{
    bool _administratorAllowed;
    bool _operationAllowed;
    bool _plainAuthorizeAllowed;
    IServiceCollection _services;

    async Task Because()
    {
        var authenticationServices = BuildServices(
            authenticationEnabled: false,
            configureServices: services =>
            {
                services.AddSingleton<IOptions<ChronicleOptions>>(Options.Create(new ChronicleOptions { Authentication = new Configuration.Authentication { Enabled = false } }));
                services.AddSingleton(Substitute.For<IAdministratorPrivileges>());
            });
        _services = authenticationServices.Services;
        await using var provider = authenticationServices.ServiceProvider;
        await using var scope = provider.CreateAsyncScope();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        _administratorAllowed = (await authorization.AuthorizeAsync(principal, new object(), WellKnownAuthorizationPolicies.Administrator)).Succeeded;
        _operationAllowed = (await authorization.AuthorizeAsync(principal, new object(), WellKnownAuthorizationPolicies.AdministratorOperations)).Succeeded;
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(principal);
        var evaluator = new AuthorizationEvaluator(
            accessor,
            new KnownInstancesOf<IAnonymousEvaluator>(new AnonymousEvaluator()),
            new KnownInstancesOf<IAuthorizationAttributeEvaluator>(new AuthorizationAttributeEvaluator()));
        _plainAuthorizeAllowed = evaluator.IsAuthorized(typeof(RetryQuarantinedOccurrence));
    }

    [Fact] void should_allow_anonymous_administrator_management_in_authentication_off_mode() => _administratorAllowed.ShouldBeTrue();
    [Fact] void should_allow_anonymous_operational_recovery_in_authentication_off_mode() => _operationAllowed.ShouldBeTrue();
    [Fact] void should_not_make_an_existing_plain_authorize_declaration_anonymous() => _plainAuthorizeAllowed.ShouldBeFalse();
    [Fact] void should_explicitly_opt_both_named_policies_into_arc_anonymous_execution() => _services
        .Count(descriptor => descriptor.ServiceType == typeof(AnonymousAspNetAuthorizationPolicyRegistration)).ShouldEqual(2);
}
