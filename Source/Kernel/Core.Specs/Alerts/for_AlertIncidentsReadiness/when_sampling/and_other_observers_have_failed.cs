// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsReadiness.when_sampling;

public class and_other_observers_have_failed : given.a_health_sampler
{
    AlertIncidentsReadinessState _result;

    void Establish() => _storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default)
        .FailedPartitions.GetFor((ObserverId)"other").Returns(new FailedPartitions { Partitions = [new FailedPartition()] });
    async Task Because() => _result = await _readiness.Get();

    [Fact] void should_only_consider_the_incidents_observer() => _result.ShouldEqual(AlertIncidentsReadinessState.Ready);
}
