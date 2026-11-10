// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Application = Cratis.Chronicle.Storage.Security.Application;

namespace Cratis.Chronicle.Setup.Authentication.for_AuthenticationService;

public class when_bootstrapping_while_a_client_is_registered_twice : Specification
{
    Exception? _exception;
    ConfiguredGrant[] _grants;
    AuthenticationService _service;

    void Establish()
    {
        var applications = Substitute.For<IApplicationStorage>();
        var lowest = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var highest = Guid.Parse("ffffffff-0000-0000-0000-000000000002");

        // Storage returns the records in no particular order, so the higher id comes first here.
        applications.GetAll().Returns(
        [
            new Application { Id = highest, ClientId = (Cratis.Chronicle.Concepts.Security.ClientId)"client" },
            new Application { Id = lowest, ClientId = (Cratis.Chronicle.Concepts.Security.ClientId)"client" }
        ]);
        var grains = Substitute.For<IGrainFactory>();
        var entitlements = Substitute.For<IStoreEntitlements>();
        grains.GetGrain<IStoreEntitlements>(IStoreEntitlements.Key).Returns(entitlements);
        entitlements.Reconcile(Arg.Do<IEnumerable<ConfiguredGrant>>(grants => _grants = grants.ToArray())).Returns(Task.CompletedTask);
        var options = new ChronicleOptions
        {
            Clients = [new() { ClientId = "client", ClientSecret = "configured", EventStores = [new() { EventStore = "orders", Access = StoreAccess.Read }] }]
        };
        _lowest = lowest.ToString();
        _service = new(Substitute.For<IUserStorage>(), applications, grains, Options.Create(options), Substitute.For<IEventSerializer>(), Substitute.For<IInitialAdminSetupTokens>(), NullLogger<AuthenticationService>.Instance);
    }

    string _lowest;

    async Task Because() => _exception = await Catch.Exception(_service.EnsureBootstrapClients);

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_grant_the_registration_with_the_lowest_id() => _grants.ShouldContain(new ConfiguredGrant(new(PrincipalKey.LocalIssuer, _lowest), "orders", null, StoreAccess.Read));
}
