// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceRecovery.when_rehydrating;

public class and_one_trigger_cannot_wake : Specification
{
    readonly IOccurrencesStorage _storage = Substitute.For<IOccurrencesStorage>();
    readonly IGrainFactory _factory = Substitute.For<IGrainFactory>();
    readonly IOccurrenceQueue _failed = Substitute.For<IOccurrenceQueue>();
    readonly IOccurrenceQueue _healthy = Substitute.For<IOccurrenceQueue>();

    void Establish()
    {
        _storage.GetPendingTriggers().Returns(Task.FromResult<IEnumerable<TriggerId>>([new("failed"), new("healthy")]));
        OccurrenceQueueKey.Resolve(_factory, new("store"), null, "failed").Returns(_failed);
        OccurrenceQueueKey.Resolve(_factory, new("store"), null, "healthy").Returns(_healthy);
        _failed.Wake().Returns(Task.FromException(new IOException("Simulated wake failure")));
    }

    Task Because() => OccurrenceRecovery.Rehydrate(_factory, _storage, new("store"), null, NullLogger.Instance);
    [Fact] async Task should_wake_the_other_trigger() => await _healthy.Received(1).Wake();
}
