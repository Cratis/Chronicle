// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_a_stream_scope_includes_source_type : Specification
{
    EventStoreForTesting _store;
    EventSourceId _source;
    DecisionRead<SimpleModule> _read;

    async Task Establish()
    {
        _store = new EventStoreForTesting();
        _source = EventSourceId.New();
        await _store.EventLog.Append(_source, new ModuleCreated("Selected"), "modules", "selected", "first-source-type");
        await _store.EventLog.Append(_source, new ModuleCreated("Other source type"), "modules", "selected", "second-source-type");
    }

    async Task Because() => _read = await _store.GetDecisionReads().GetDetached<SimpleModule>(_source, "modules", "selected", "first-source-type");

    [Fact] void should_fold_only_the_selected_source_type() => _read.Instance!.Name.ShouldEqual("Selected");
    [Fact] void should_keep_the_source_id_as_the_instance_key() => _read.Key.Value.ShouldEqual(_source.Value);
}
