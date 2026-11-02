// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Recommendations;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.given;

public class a_reactor_with_a_newer_owner : a_reactor_grain_with_a_stored_definition
{
    protected IObserver _owner;
    protected IRecommendationsManager _recommendations;
    protected ObserverKey _ownerKey;

    void Establish()
    {
        _client = _client with { ClientType = ".NET", Version = "1.0.0" };
        _connectedClients.GetConnectedClient(_client.ConnectionId).Returns(_client);
        _ownerKey = new(_connectionDefinition.Identifier, "the-event-store", "the-namespace", "the-owner-sequence");
        _storage.GetEventStore(_ownerKey.EventStore).Observers.Get(_connectionDefinition.Identifier).Returns(new ObserverDefinition
        {
            Identifier = _connectionDefinition.Identifier,
            EventSequenceId = _ownerKey.EventSequenceId
        });
        _owner = Substitute.For<IObserver>();
        _owner.GetSubscription().Returns(new ObserverSubscription(_connectionDefinition.Identifier, _ownerKey, [], typeof(IReactorObserverSubscriber), SiloAddress.Zero)
        {
            Targets = [new(SiloAddress.Zero, _client with { Version = "2.0.0" })]
        });
        _ownerObserver = _owner;
        _recommendations = Substitute.For<IRecommendationsManager>();
        _silo.AddProbe(_ => _recommendations);
    }

    protected static Task<RecommendationId> ReplayRecommendedOn(IRecommendationsManager recommendations) => recommendations.Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(Arg.Any<RecommendationDescription>(), Arg.Any<ReplayCandidateRequest>());

    protected bool Subscribed => (bool)typeof(Reactor).GetField("_subscribed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(_grain)!;

    protected void ShouldWarnOnce() => _logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "Log" && (LogLevel)call.GetArguments()[0]! == LogLevel.Warning).ShouldEqual(1);
}
