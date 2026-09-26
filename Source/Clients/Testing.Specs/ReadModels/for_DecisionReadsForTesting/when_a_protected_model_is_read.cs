// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_a_protected_model_is_read : Specification
{
    EventStoreForTesting _store;
    EventSourceId _source;
    DecisionRead<SimpleModule> _read;

    async Task Establish()
    {
        _store = new EventStoreForTesting();
        _source = EventSourceId.New();
        await _store.EventLog.Append(_source, new ModuleCreated("Test"));
    }

    async Task Because() => _read = await _store.GetDecisionReads().GetDetached<SimpleModule>(_source);

    [Fact] void should_return_the_model() => _read.Instance!.Name.ShouldEqual("Test");
    [Fact] void should_protect_the_read() => _read.IsProtected.ShouldBeTrue();
}
