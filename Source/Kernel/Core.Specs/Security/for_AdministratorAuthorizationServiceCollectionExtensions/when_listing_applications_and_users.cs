// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Chronicle.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorizationServiceCollectionExtensions;

public class when_listing_applications_and_users : given.the_kernel_authorization
{
    IAuthorizationDecisions _decisions;
    bool _administratorAllowed;
    bool _nonAdministratorAllowed;

    void Establish()
    {
        _decisions = Substitute.For<IAuthorizationDecisions>();
        _decisions.Decide(Arg.Any<ClaimsPrincipal?>(), WellKnownCapabilities.AdministerSecurity, AuthorizationScope.Kernel)
            .Returns(call => call.Arg<ClaimsPrincipal?>()?.HasClaim("role", "administrator") == true
                ? AuthorizationDecision.Allow(AuthorizationReason.Granted, call.ArgAt<Capability>(1), call.ArgAt<AuthorizationScope>(2))
                : AuthorizationDecision.Deny(AuthorizationReason.NotGranted, call.ArgAt<Capability>(1), call.ArgAt<AuthorizationScope>(2)));
        _services.AddSingleton(_decisions);
        _services.AddChronicleAdministratorAuthorization();

        // The administrator policy for operations is opted out; listing stays administrator-only regardless.
        BuildProvider(enforceOperations: false);
    }

    async Task Because()
    {
        await using var scope = _provider.CreateAsyncScope();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        _administratorAllowed = (await authorization.AuthorizeAsync(Principal("administrator"), new object(), WellKnownAuthorizationPolicies.Administrator)).Succeeded;
        _nonAdministratorAllowed = (await authorization.AuthorizeAsync(Principal("other"), new object(), WellKnownAuthorizationPolicies.Administrator)).Succeeded;
    }

    [Fact] void should_require_the_administrator_policy_for_the_application_read_model() => Policy(typeof(Application)).ShouldEqual(WellKnownAuthorizationPolicies.Administrator);
    [Fact] void should_require_the_administrator_policy_for_the_user_read_model() => Policy(typeof(User)).ShouldEqual(WellKnownAuthorizationPolicies.Administrator);
    [Fact] void should_require_the_administrator_policy_for_the_grpc_application_listing() => GrpcPolicy(typeof(Applications), "AllApplications").ShouldEqual(WellKnownAuthorizationPolicies.Administrator);
    [Fact] void should_require_the_administrator_policy_for_the_grpc_user_listing() => GrpcPolicy(typeof(Users), "AllUsers").ShouldEqual(WellKnownAuthorizationPolicies.Administrator);
    [Fact] void should_allow_an_administrator() => _administratorAllowed.ShouldBeTrue();
    [Fact] void should_refuse_a_non_administrator_even_with_the_operations_opt_out() => _nonAdministratorAllowed.ShouldBeFalse();

    static ClaimsPrincipal Principal(string role) => new(new ClaimsIdentity([new Claim("role", role)], "spec"));

    static string? Policy(MemberInfo artifact) => artifact.GetCustomAttribute<Cratis.Arc.Authorization.AuthorizeAttribute>()?.Policy;

    static string? GrpcPolicy(Type service, string method) => service.GetMethod(method)!.GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()?.Policy;
}
