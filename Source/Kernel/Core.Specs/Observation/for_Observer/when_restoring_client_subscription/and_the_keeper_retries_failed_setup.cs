// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.ClientSubscriptions;
using Cratis.Chronicle.Observation.Reactors;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Services.Observation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_client_subscription;

public class and_the_keeper_retries_failed_setup : given.an_observer_that_lost_its_client
{
    bool _failLookup = true;
    bool _installedAfterFailure;
    bool _establishedAfterFailure;
    bool _secondCheck;
    bool _thirdCheck;
    int _restorations;
    Exception? _firstError;
    Func<Task<bool>> _check;

    void Establish()
    {
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(_ => _failLookup
            ? Task.FromException<IImmutableList<JobState>>(new TimeoutException("Job lookup failed"))
            : Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        var subscription = CreateSubscription();
        var definition = new ReactorDefinition(_observerId.Value, ReactorOwner.Client, _observerKey.EventSequenceId, [new(event_type, Concepts.WellKnownExpressions.EventSourceId)]);
        _check = () => ClientSubscriptionKeeper.KeepSubscription(
            ClientSubscriptionKeeper.ConfirmFromObserver(subscription.IsSubscriptionIntact, _observer, _client.ConnectionId),
            () =>
            {
                _restorations++;
                return subscription.KeepSubscription(definition);
            });
    }

    ClientReactorSubscription CreateSubscription()
    {
        var connectedClients = Substitute.For<IConnectedClients>();
        connectedClients.IsConnected(Arg.Any<ConnectionId>()).Returns(true);
        connectedClients.GetConnectedClient(Arg.Any<ConnectionId>()).Returns(_client);
        var localSilo = Substitute.For<ILocalSiloDetails>();
        localSilo.SiloAddress.Returns(SiloAddress.Zero);
        return new ClientReactorSubscription(
            new ConnectedObserverKey(_observerId, _observerKey.EventStore, _observerKey.Namespace, _observerKey.EventSequenceId, _client.ConnectionId),
            ClientSubscriptionFakes.GrainFactory(_observer, connectedClients, Substitute.For<INamespaces>()),
            Substitute.For<IReactorDefinitionComparer>(),
            localSilo,
            Options.Create(new ChronicleOptions()),
            _storage,
            NullLogger<ClientReactorSubscription>.Instance);
    }

    async Task Because()
    {
        _firstError = await Catch.Exception(async () => await _check());
        _installedAfterFailure = await _observer.IsSubscribed();
        _establishedAfterFailure = await _observer.IsSubscriptionEstablished();
        _failLookup = false;
        _secondCheck = await _check();
        _thirdCheck = await _check();
    }

    [Fact] void should_fail_the_first_check_after_installation() => _firstError.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_keep_membership_after_failure() => _installedAfterFailure.ShouldBeTrue();
    [Fact] void should_not_confuse_membership_with_establishment() => _establishedAfterFailure.ShouldBeFalse();
    [Fact] void should_restore_on_the_second_check() => _secondCheck.ShouldBeFalse();
    [Fact] void should_find_the_third_check_healthy() => _thirdCheck.ShouldBeTrue();
    [Fact] void should_only_restore_twice() => _restorations.ShouldEqual(2);
    [Fact] async Task should_establish_setup() => (await _observer.IsSubscriptionEstablished()).ShouldBeTrue();
    [Fact] async Task should_keep_one_target() => (await _observer.GetSubscription()).Targets.Count.ShouldEqual(1);
}
