// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Application = Cratis.Chronicle.Storage.Security.Application;
using ApplicationId = Cratis.Chronicle.Concepts.Security.ApplicationId;

namespace Cratis.Chronicle.Setup.Authentication.for_AuthenticationService;

public class when_the_development_client_is_created_concurrently_by_another_node : Specification
{
    readonly ApplicationId _winner = ClientApplicationIds.For("chronicle-dev-client");
    IApplicationStorage _applications;
    ConfiguredGrant[] _grants;
    AuthenticationService _service;
    Exception _error;

    void Establish()
    {
        _applications = Substitute.For<IApplicationStorage>();
        _applications.GetAll().Returns(
            [],
            [new Application { Id = _winner, ClientId = "chronicle-dev-client" }]);
        _applications.Create(Arg.Any<Application>()).Returns(Task.FromException(new InvalidOperationException("duplicate key")));
        var grains = Substitute.For<IGrainFactory>();
        var entitlements = Substitute.For<IStoreEntitlements>();
        grains.GetGrain<IStoreEntitlements>(IStoreEntitlements.Key).Returns(entitlements);
        entitlements.Reconcile(Arg.Do<IEnumerable<ConfiguredGrant>>(grants => _grants = grants.ToArray())).Returns(Task.CompletedTask);
        _service = new(Substitute.For<IUserStorage>(), _applications, grains, Options.Create(new ChronicleOptions()), Substitute.For<IEventSerializer>(), Substitute.For<IInitialAdminSetupTokens>(), NullLogger<AuthenticationService>.Instance);
    }

    async Task Because() => _error = await Catch.Exception(_service.EnsureDefaultClientCredentials);

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_create_only_once() => _applications.Received(1).Create(Arg.Any<Application>());
    [Fact] void should_create_with_the_deterministic_application_id() => _applications.Received(1).Create(Arg.Is<Application>(application => application.Id == _winner));
    [Fact] void should_resolve_the_stored_application() => _grants.Single().Holder.ShouldEqual(new PrincipalKey(PrincipalKey.LocalIssuer, _winner.ToString()));
}
#endif
