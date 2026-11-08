// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorizationServiceCollectionExtensions;

public class when_both_policies_are_evaluated_through_the_decision_point : given.the_kernel_authorization
{
    IAuthorizationDecisions _decisions;
    bool _administrator;
    bool _operations;

    void Establish()
    {
        _decisions = Substitute.For<IAuthorizationDecisions>();
        _decisions.Decide(Arg.Any<ClaimsPrincipal?>(), Arg.Any<Capability>(), Arg.Any<AuthorizationScope>())
            .Returns(call => AuthorizationDecision.Allow(AuthorizationReason.Granted, call.ArgAt<Capability>(1), call.ArgAt<AuthorizationScope>(2)));
        _services.AddSingleton(_decisions);
        _services.AddChronicleAdministratorAuthorization();
        BuildProvider(enforceOperations: true);
    }

    async Task Because()
    {
        await using var scope = _provider.CreateAsyncScope();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity("spec"));
        _administrator = (await authorization.AuthorizeAsync(principal, new object(), WellKnownAuthorizationPolicies.Administrator)).Succeeded;
        _operations = (await authorization.AuthorizeAsync(principal, new object(), WellKnownAuthorizationPolicies.AdministratorOperations)).Succeeded;
    }

    [Fact] void should_succeed_for_both_policies() => (_administrator && _operations).ShouldBeTrue();
    [Fact] void should_ask_for_administering_security() =>
        _decisions.Received().Decide(Arg.Any<ClaimsPrincipal?>(), WellKnownCapabilities.AdministerSecurity, AuthorizationScope.Kernel);
    [Fact] void should_ask_for_operating_the_event_store() =>
        _decisions.Received().Decide(Arg.Any<ClaimsPrincipal?>(), WellKnownCapabilities.OperateEventStore, AuthorizationScope.Kernel);
}
