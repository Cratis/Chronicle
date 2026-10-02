// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_a_model_is_created_by_an_event_without_properties : Specification
{
    EventStoreForTesting _store;
    Guid _id;
    DecisionRead<MarkerState> _read;

    async Task Establish()
    {
        _store = new EventStoreForTesting();
        _id = Guid.NewGuid();
        await _store.EventLog.Append(new EventSourceId(_id), new Marked());
    }

    async Task Because() => _read = await _store.GetDecisionReads().GetDetached<MarkerState>(new EventSourceId(_id));

    [Fact] void should_return_the_model() => _read.Instance.ShouldNotBeNull();
    [Fact] void should_identify_the_model_by_the_event_source() => _read.Instance!.Id.ShouldEqual(_id);
}
