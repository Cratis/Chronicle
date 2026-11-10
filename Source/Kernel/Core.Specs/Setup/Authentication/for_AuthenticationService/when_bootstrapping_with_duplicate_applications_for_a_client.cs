// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Application = Cratis.Chronicle.Storage.Security.Application;
using ApplicationId = Cratis.Chronicle.Concepts.Security.ApplicationId;

namespace Cratis.Chronicle.Setup.Authentication.for_AuthenticationService;

public class when_bootstrapping_with_duplicate_applications_for_a_client : Specification
{
    readonly given.setup_logger _logger = new();
    readonly ApplicationId _lowest = new Guid("00000000-0000-0000-0000-000000000001");
    readonly ApplicationId _highest = new Guid("ffffffff-0000-0000-0000-000000000000");
    ConfiguredGrant[] _grants;
    AuthenticationService _service;
    Exception _error;

    void Establish()
    {
        var applications = Substitute.For<IApplicationStorage>();
        applications.GetAll().Returns([
            new Application { Id = _highest, ClientId = "client" },
            new Application { Id = _lowest, ClientId = "client" }
        ]);
        var grains = Substitute.For<IGrainFactory>();
        var entitlements = Substitute.For<IStoreEntitlements>();
        grains.GetGrain<IStoreEntitlements>(IStoreEntitlements.Key).Returns(entitlements);
        entitlements.Reconcile(Arg.Do<IEnumerable<ConfiguredGrant>>(grants => _grants = grants.ToArray())).Returns(Task.CompletedTask);
        var options = new ChronicleOptions
        {
            Clients = [new() { ClientId = "client", ClientSecret = "configured", EventStores = [new() { EventStore = "orders", Access = StoreAccess.Read }] }]
        };
        _service = new(Substitute.For<IUserStorage>(), applications, grains, Options.Create(options), Substitute.For<IEventSerializer>(), Substitute.For<IInitialAdminSetupTokens>(), new Logger<AuthenticationService>(LoggerFactoryFor(_logger.Logger)));
    }

    static ILoggerFactory LoggerFactoryFor(ILogger logger)
    {
        var factory = Substitute.For<ILoggerFactory>();
        factory.CreateLogger(Arg.Any<string>()).Returns(logger);
        return factory;
    }

    async Task Because()
    {
        _error = await Catch.Exception(_service.EnsureBootstrapClients);
        await _service.EnsureBootstrapClients();
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_resolve_the_lowest_application_id() => _grants.ShouldContain(new ConfiguredGrant(new(PrincipalKey.LocalIssuer, _lowest.ToString()), "orders", null, StoreAccess.Read));
    [Fact] void should_report_the_duplicate_once() => _logger.Messages.Count(message => message.Level == LogLevel.Warning && message.Message.Contains("Found 2 applications for ClientId: client")).ShouldEqual(1);
}
