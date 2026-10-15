// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceRecovery.when_rehydrating;

public class with_pending_application_work : Specification
{
    readonly IOccurrencesStorage _storage = Substitute.For<IOccurrencesStorage>();
    readonly IGrainFactory _factory = Substitute.For<IGrainFactory>();
    readonly IOccurrenceQueue _queue = Substitute.For<IOccurrenceQueue>();
    string _key;

    void Establish()
    {
        _storage.GetPendingTriggers().Returns(Task.FromResult<IEnumerable<TriggerId>>([new("trigger")]));
        _factory.GetGrain<IOccurrenceQueue>(Arg.Any<string>()).Returns(call =>
        {
            _key = call.ArgAt<string>(0);
            return _queue;
        });
    }

    async Task Because() => await OccurrenceRecovery.Rehydrate(_factory, _storage, new("store"), null);
    [Fact] async Task should_wake_the_durable_queue_without_event_log_data() => await _queue.Received(1).Wake();
    [Fact] void should_not_route_application_work_into_a_namespace() => JsonSerializer.Deserialize<OccurrenceQueueKey>(_key)!.Namespace.ShouldBeNull();
}
