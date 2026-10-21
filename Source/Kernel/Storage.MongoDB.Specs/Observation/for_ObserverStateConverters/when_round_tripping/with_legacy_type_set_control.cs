// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateConverters.when_round_tripping;

public class with_legacy_type_set_control : Specification
{
    KernelObserverState _saved;
    KernelObserverState _joined;

    void Because()
    {
        var control = new ObserverTypeSetControl(new(7), new("reactor-subscriber", Concepts.Observation.ObserverFilters.None), new Dictionary<EventTypeId, ObserverTypeSetMember> { [new("event-a")] = new(new(3), new(42)) }, []);
        var document = new KernelObserverState { Identifier = "observer", TypeSetControl = control }.ToMongoDB().ToBsonDocument();
        var storedControl = document["typeSetControl"].AsBsonDocument;
        storedControl.Remove("retirementResults");
        storedControl.Remove("pendingChange");
        _saved = BsonSerializer.Deserialize<ObserverState>(document).ToKernel();
        _joined = BsonSerializer.Deserialize<ObserverStateWithFailedPartitions>(document).ToKernel();
    }

    [Fact] void should_keep_the_committed_revision() => _saved.TypeSetControl.Revision.Value.ShouldEqual(7);
    [Fact] void should_keep_the_existing_member() => _saved.TypeSetControl.Members[new("event-a")].Incarnation.Value.ShouldEqual(3);
    [Fact] void should_not_invent_retirement_results() => _saved.TypeSetControl.RetirementResults.ShouldBeNull();
    [Fact] void should_not_invent_a_pending_change() => _saved.TypeSetControl.PendingChange.ShouldBeNull();
    [Fact] void should_not_invent_results_on_the_joined_read() => _joined.TypeSetControl.RetirementResults.ShouldBeNull();
    [Fact] void should_not_invent_an_intent_on_the_joined_read() => _joined.TypeSetControl.PendingChange.ShouldBeNull();
}
