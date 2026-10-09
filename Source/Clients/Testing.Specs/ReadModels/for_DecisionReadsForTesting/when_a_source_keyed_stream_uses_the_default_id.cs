// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_a_source_keyed_stream_uses_the_default_id : Specification
{
    EventStoreForTesting _store;
    EventSourceId _source;
    DecisionRead<SimpleModule> _read;

    async Task Establish()
    {
        _store = new EventStoreForTesting();
        _source = EventSourceId.New();
        await _store.EventLog.Append(_source, new ModuleCreated("Selected"), "modules", EventStreamId.Default);
        await _store.EventLog.Append(_source, new ModuleCreated("Other stream type"), "other", EventStreamId.Default);
    }

    async Task Because() => _read = await _store.GetDecisionReads().GetDetached<SimpleModule>(_source, "modules", EventStreamId.Default);

    [Fact] void should_fold_the_default_id_of_the_selected_type() => _read.Instance!.Name.ShouldEqual("Selected");
    [Fact] void should_keep_the_source_id_as_the_instance_key() => _read.Key.Value.ShouldEqual(_source.Value);
}
