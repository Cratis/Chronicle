// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Chronicle.EventStores;
using Cratis.Chronicle.Setup;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_StoreEntitlementCommandFilter;

public class when_ensuring_an_event_store_through_the_command_pipeline : Specification
{
    [Fact]
    public async Task should_authorize_manage_access_to_the_store_named_by_the_command()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "caller")], "spec"));
        var decisions = Substitute.For<IAuthorizationDecisions>();
        decisions.Decide(Arg.Any<ClaimsPrincipal?>(), Arg.Any<Capability>(), Arg.Any<AuthorizationScope>()).Returns(call =>
            AuthorizationDecision.Deny(AuthorizationReason.NotGranted, call.Arg<Capability>(), call.Arg<AuthorizationScope>()));
        var services = new ServiceCollection().AddLogging().AddOptions();
        services.Configure<ArcOptions>(_ => { });
        services.AddCratisArcCore();
        services.AddKernelSideCommandExtensionPoints();
        services.AddSingleton<IAuthorizationPolicyRuntime, AspNetAuthorizationPolicyRuntime>();
        var handlers = Substitute.For<ICommandHandlerProviders>();
        handlers.TryGetHandlerFor(Arg.Any<object>(), out Arg.Any<ICommandHandler>()).Returns(call =>
        {
            call[1] = Substitute.For<ICommandHandler>();
            return true;
        });
        services.AddSingleton(handlers);
        services.AddSingleton(Options.Create(new Configuration.ChronicleOptions { Authentication = new Configuration.Authentication { EnforceStoreEntitlements = true } }));
        services.AddSingleton(decisions);
        services.AddSingleton(Substitute.For<IAdministratorPrivileges>());
        services.AddSingleton(Substitute.For<IGrainFactory>());
        var current = Substitute.For<ICurrentPrincipalAccessor>();
        current.Current.Returns(principal);
        services.AddSingleton(current);
        var http = new HttpContextAccessor();
        services.AddSingleton<IHttpContextAccessor>(http);
        services.AddChronicleAdministratorAuthorization();
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var request = provider.CreateAsyncScope();
        http.HttpContext = new DefaultHttpContext { User = principal, RequestServices = request.ServiceProvider };

        var result = await provider.GetRequiredService<ICommandPipeline>().Execute(new EnsureEventStore("requested-store"));

        result.IsAuthorized.ShouldBeFalse();
        result.IsValid.ShouldBeTrue();
        result.ExceptionMessages.ShouldBeEmpty();
        await decisions.Received(1).Decide(Arg.Any<ClaimsPrincipal?>(), WellKnownCapabilities.ManageEventStore, AuthorizationScope.ForEventStore("requested-store"));
    }
}
