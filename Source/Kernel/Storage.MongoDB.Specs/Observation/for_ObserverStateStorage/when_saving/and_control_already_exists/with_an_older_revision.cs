// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateStorage.when_saving.and_control_already_exists;

[Collection(MongoDBCollection.Name)]
public class with_an_older_revision(MongoDBFixture fixture) : given.a_persisted_observer(fixture)
{
    async Task Because()
    {
        var stale = _control with
        {
            Revision = new(6),
            Members = new Dictionary<EventTypeId, ObserverTypeSetMember> { [_type] = new(new(3), new(12)) },
            Registrations = [],
            RetirementResults = null,
            PendingChange = null
        };
        await _storage.Save(new() { Identifier = "observer", TypeSetControl = stale, NextEventSequenceNumber = new(123) });
        _saved = await _storage.Get("observer");
    }

    [Fact] void should_preserve_the_membership_revision() => _saved.TypeSetControl.Revision.Value.ShouldEqual(7);
    [Fact] void should_preserve_the_rejoined_member_incarnation() => _saved.TypeSetControl.Members[_type].Incarnation.Value.ShouldEqual(7);
    [Fact] void should_preserve_the_member_fence() => _saved.TypeSetControl.Members[_type].JoinFence.Value.ShouldEqual(42UL);
    [Fact] void should_preserve_the_connection_capabilities() => _saved.TypeSetControl.Registrations.Single().DeclaredTypes.Single().Generation.Value.ShouldEqual(2U);
    [Fact] void should_preserve_retirement_results() => _saved.TypeSetControl.RetirementResults.Single().OperationId.ShouldEqual(_control.RetirementResults.Single().OperationId);
    [Fact] void should_preserve_the_pending_change() => _saved.TypeSetControl.PendingChange.Revision.Value.ShouldEqual(8);
    [Fact] void should_preserve_literal_subscriber_names() => _saved.TypeSetControl.Contract.SubscriberType.ShouldEqual("$reactor-subscriber");
    [Fact] void should_preserve_literal_connection_names() => _saved.TypeSetControl.Registrations.Single().ConnectionId.Value.ShouldEqual("$connection-new");
    [Fact] void should_save_the_other_state_changes() => _saved.NextEventSequenceNumber.Value.ShouldEqual(123UL);
}
