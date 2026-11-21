// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Signing;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Observation.Webhooks.for_RotateWebhookSigningKey.when_authorizing;

public class when_checking_authorization : Specification
{
    [Fact] void should_require_an_administrator() => typeof(RotateWebhookSigningKey).GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldEqual(WellKnownAuthorizationPolicies.Administrator);

    [Fact]
    void should_route_to_the_addressed_store()
    {
        var routing = KernelCommandRouting.For(typeof(RotateWebhookSigningKey)).Single();
        routing.Name.ShouldEqual("EventStore");
        routing.IsStore.ShouldBeTrue();
        routing.Get(new RotateWebhookSigningKey("store")).ShouldEqual("store");
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task should_only_rotate_for_an_authenticated_administrator(bool isAdministrator, bool isAuthenticated)
    {
        await using var scenario = ChronicleCommandScenario.For<RotateWebhookSigningKey>(isAdministrator);
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        scenario.Services.AddSingleton(storage);
        scenario.Services.AddChronicleAdministratorAuthorization();
        var factory = Substitute.For<IGrainFactory>();
        var ring = Substitute.For<IWebhookSigningKeyRing>();
        factory.GetGrain<IWebhookSigningKeyRing>("store").Returns(ring);
        scenario.Services.AddSingleton(factory);
        if (!isAuthenticated)
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity());
            var accessor = Substitute.For<ICurrentPrincipalAccessor>();
            accessor.Current.Returns(principal);
            scenario.Services.AddSingleton(accessor);
            scenario.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = principal } });
        }

        var result = await scenario.Execute(new RotateWebhookSigningKey("store"));
        result.ShouldNotHaveExceptions();
        if (isAdministrator && isAuthenticated)
        {
            result.ShouldBeSuccessful();
            await ring.Received(1).Rotate(WebhookSigningKeyOverlap.Default);
        }
        else
        {
            result.ShouldNotBeAuthorized();
            await ring.DidNotReceive().Rotate(Arg.Any<WebhookSigningKeyOverlap>());
        }
    }
}
