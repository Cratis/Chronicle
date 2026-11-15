// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Application = Cratis.Chronicle.Storage.Security.Application;
using ApplicationId = Cratis.Chronicle.Concepts.Security.ApplicationId;

namespace Cratis.Chronicle.Setup.Authentication.for_AuthenticationService;

public class when_bootstrapping_with_a_duplicate_holding_the_deterministic_id : Specification
{
    readonly ApplicationId _deterministic = ClientApplicationIds.For("client");
    ConfiguredGrant[] _grants;
    AuthenticationService _service;

    void Establish()
    {
        var applications = Substitute.For<IApplicationStorage>();
        applications.GetAll().Returns([
            new Application { Id = Guid.Empty, ClientId = "client" },
            new Application { Id = _deterministic, ClientId = "client" }
        ]);
        var grains = Substitute.For<IGrainFactory>();
        var entitlements = Substitute.For<IStoreEntitlements>();
        grains.GetGrain<IStoreEntitlements>(IStoreEntitlements.Key).Returns(entitlements);
        entitlements.Reconcile(Arg.Do<IEnumerable<ConfiguredGrant>>(grants => _grants = grants.ToArray())).Returns(Task.CompletedTask);
        var options = new ChronicleOptions
        {
            Clients = [new() { ClientId = "client", ClientSecret = "configured", EventStores = [new() { EventStore = "orders", Access = StoreAccess.Read }] }]
        };
        _service = new(Substitute.For<IUserStorage>(), applications, grains, Options.Create(options), Substitute.For<IEventSerializer>(), Substitute.For<IInitialAdminSetupTokens>(), NullLogger<AuthenticationService>.Instance);
    }

    Task Because() => _service.EnsureBootstrapClients();

    [Fact] void should_resolve_the_deterministic_application_id() => _grants.ShouldContain(new ConfiguredGrant(new(PrincipalKey.LocalIssuer, _deterministic.ToString()), "orders", null, StoreAccess.Read));
}
