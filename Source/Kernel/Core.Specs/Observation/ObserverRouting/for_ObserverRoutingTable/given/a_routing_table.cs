// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.ObserverRouting.for_ObserverRoutingTable.given;

public class a_routing_table : Specification
{
    protected static readonly EventType _firstEventType = new("first-event", 1);
    protected static readonly EventType _secondEventType = new("second-event", 1);
    protected static readonly ObserverId _firstObserver = "first-observer";
    protected static readonly ObserverId _secondObserver = "second-observer";
    protected static readonly ObserverId _everythingObserver = "everything-observer";
    protected static readonly ObserverId _outboxObserver = "outbox-observer";

    protected ObserverRoutingTable _table;

    void Establish() => _table = ObserverRoutingTable.From(new ObserverDefinitionRegistrySnapshot(
        7,
        new[]
        {
            DefinitionOf(_firstObserver, EventSequenceId.Log, _firstEventType),
            DefinitionOf(_secondObserver, EventSequenceId.Log, _firstEventType, _secondEventType),
            DefinitionOf(_everythingObserver, EventSequenceId.Log),
            DefinitionOf(_outboxObserver, EventSequenceId.Outbox, _firstEventType)
        }.ToFrozenDictionary(_ => _.Identifier)));

    static ObserverDefinition DefinitionOf(ObserverId id, EventSequenceId eventSequenceId, params EventType[] eventTypes) =>
        new(id, EventTypeSet.From(eventTypes), eventSequenceId, ObserverType.Reactor, ObserverOwner.Client, true) { Revision = 2 };
}
