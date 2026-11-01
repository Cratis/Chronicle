// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing_a_controlled_target;

public class and_the_client_declares_only_one_member : given.a_controlled_observer
{
    readonly ConnectedClient _client = new() { ConnectionId = "controlled-client", Version = "1.0.0" };
    ObserverSubscription _result;
    EventType[] _readerTypes;

    async Task Because()
    {
        await _observer.Subscribe<ObserverSubscriber>(ObserverType.Reactor, [event_type], SiloAddress.Zero, _client);
        _result = await _observer.GetSubscription();
        _readerTypes = (await _observer.GetEventTypes()).ToArray();
    }

    [Fact] void should_retain_the_full_committed_membership() => _result.EventTypes.Select(type => type.Id).ShouldContainOnly(event_type.Id, added_type.Id);
    [Fact] void should_read_both_members_despite_the_targets_declaration() => _readerTypes.Select(type => type.Id).ShouldContainOnly(event_type.Id, added_type.Id);
    [Fact] void should_keep_the_targets_declaration_separate() => _result.Targets.Single(target => target.ConnectedClient?.ConnectionId == _client.ConnectionId).DeclaredEventTypes.ShouldContainOnly(event_type);
    [Fact] void should_keep_the_committed_revision() => _result.TypeSetRevision.ShouldEqual(subscription.TypeSetRevision);
}
