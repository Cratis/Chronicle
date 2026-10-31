// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_observing;

public class with_a_late_subscriber : given.a_collection_stream
{
    IEnumerable<Row> _latest;
    IDisposable _late;

    async Task Because()
    {
        _observation = _readModels.Observe<Row>();
        Subscribe();
        await Emit(new Row("one", "Before"));
        await Emit(new Row("one", "Latest"));
        _late = _observation.Subscribe(set => _latest = set);
    }

    void Destroy() => _late.Dispose();

    [Fact] void should_replay_only_the_latest_set() => _latest.ShouldContainOnly(new Row("one", "Latest"));
    [Fact] void should_share_one_stream_within_the_observation() => _service.Received(1).ObserveInstances(Arg.Any<Contracts.ReadModels.ObserveInstancesRequest>());
}
