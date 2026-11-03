// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Observation.for_PartitionRebuildAdmission;

public class when_the_materialization_is_bound : given.an_admission_check
{
    void Establish()
    {
        _store.GenerationFloor.Get().Returns(_floor);
        _bindings.Get(_key.EventStore, _materialization).Returns(new MaterializationBinding(_materialization, new("models"), new(1), new("models"), new(1), new(2), null, null, []));
    }

    async Task Because() => _result = await _admission.IsAdmitted(_request);

    [Fact] void should_refuse_isolated_replay() => _result.ShouldBeFalse();
    [Fact] void should_inspect_the_binding() => _bindings.Received(1).Get(_key.EventStore, _materialization);
}
