// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Setup.Authentication.for_AuthenticationService;

public class when_the_development_client_is_created_while_storage_is_stale : Specification
{
    ConfiguredGrant[] _grants;
    string _applicationId;
    AuthenticationService _service;

    void Establish()
    {
        var applications = Substitute.For<IApplicationStorage>();
        applications.GetAll().Returns([]);
        var eventLog = Substitute.For<IEventSequence>();
        eventLog.Append(Arg.Any<EventSourceId>(), Arg.Any<object>()).Returns(call =>
        {
            _applicationId = call.ArgAt<EventSourceId>(0).Value;
            return Task.FromResult(new AppendResult());
        });
        var grains = Substitute.For<IGrainFactory>();
        grains.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(eventLog);
        var entitlements = Substitute.For<IStoreEntitlements>();
        grains.GetGrain<IStoreEntitlements>(IStoreEntitlements.Key).Returns(entitlements);
        entitlements.Reconcile(Arg.Do<IEnumerable<ConfiguredGrant>>(grants => _grants = grants.ToArray())).Returns(Task.CompletedTask);
        var options = new ChronicleOptions
        {
            Clients = [new() { ClientId = "client", ClientSecret = "configured", EventStores = [new() { EventStore = "orders", Access = StoreAccess.Read }] }]
        };
        _service = new(Substitute.For<IUserStorage>(), applications, grains, Options.Create(options), Substitute.For<IEventSerializer>(), Substitute.For<IInitialAdminSetupTokens>(), NullLogger<AuthenticationService>.Instance);
    }

    async Task Because()
    {
        await _service.EnsureBootstrapClients();
        await _service.EnsureDefaultClientCredentials();
    }

    [Fact] void should_keep_the_grant_of_the_bootstrap_client() => _grants.ShouldContain(new ConfiguredGrant(new(PrincipalKey.LocalIssuer, _applicationId), "orders", null, StoreAccess.Read));
}
#endif
