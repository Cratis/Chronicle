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

public class when_reconciling_client_access : Specification
{
    readonly ApplicationId _applicationId = Guid.NewGuid();
    IStoreEntitlements _entitlements;
    AuthenticationService _service;
    ConfiguredGrant[] _grants;

    void Establish()
    {
        var applications = Substitute.For<IApplicationStorage>();
        applications.GetAll().Returns([new Application { Id = _applicationId, ClientId = "client" }]);
        var grains = Substitute.For<IGrainFactory>();
        _entitlements = Substitute.For<IStoreEntitlements>();
        grains.GetGrain<IStoreEntitlements>(IStoreEntitlements.Key).Returns(_entitlements);
        _entitlements.Reconcile(Arg.Do<IEnumerable<ConfiguredGrant>>(grants => _grants = grants.ToArray())).Returns(Task.CompletedTask);
        var options = new ChronicleOptions
        {
            Authentication = new()
            {
                EventStoreAccess = [new() { Issuer = "external", Subject = "worker", IsClient = true, AllEventStores = true, Access = StoreAccess.Append }]
            },
            Clients = [new() { ClientId = "client", ClientSecret = "configured", EventStores = [new() { EventStore = "orders", Access = StoreAccess.Read }] }]
        };
        _service = new(Substitute.For<IUserStorage>(), applications, grains, Options.Create(options), Substitute.For<IEventSerializer>(), Substitute.For<IInitialAdminSetupTokens>(), NullLogger<AuthenticationService>.Instance);
    }

    async Task Because()
    {
        await _service.EnsureBootstrapClients();
        await _service.EnsureBootstrapClients();
    }

    [Fact] void should_resolve_the_local_application_subject() => _grants.ShouldContain(new ConfiguredGrant(new(PrincipalKey.LocalIssuer, _applicationId.ToString()), "orders", null, StoreAccess.Read));
    [Fact] void should_resolve_the_external_principal_exactly() => _grants.ShouldContain(new ConfiguredGrant(new("external", "worker", true), null, null, StoreAccess.Append));
    [Fact] void should_reconcile_once_per_start() => _entitlements.Received(2).Reconcile(Arg.Any<IEnumerable<ConfiguredGrant>>());
    [Fact] void should_repeat_the_same_configuration() => _grants.Length.ShouldEqual(2);
}
