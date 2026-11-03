// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Observation.for_PartitionRebuildAdmission;

public class when_a_binding_appears_before_publication : given.an_admitted_rebuilder
{
    async Task Because()
    {
        _store.GenerationFloor.Get().Returns(_floor);
        _bindings.Get(_key.EventStore, _materialization).Returns(new MaterializationBinding(_materialization, new("models"), new(1), new("models"), new(1), new(2), null, null, []));
        _result = await _rebuilder.Publish(50UL);
    }

    [Fact] void should_have_initially_admitted_the_replay() => _wasAdmitted.ShouldBeTrue();
    [Fact] void should_refuse_the_now_ineligible_publication() => _result.ShouldBeFalse();
    [Fact] void should_not_replace_any_live_state() => _sink.DidNotReceive().ReplaceIsolated(_request.Key, Arg.Any<ExpandoObject?>(), Arg.Any<EventSequenceNumber>());
}
