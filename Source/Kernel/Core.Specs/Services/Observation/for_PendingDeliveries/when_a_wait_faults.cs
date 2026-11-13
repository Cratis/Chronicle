// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_PendingDeliveries;

public class when_a_wait_faults : given.tracked_deliveries
{
    bool _removed;

    void Establish() => _pending.Add("partition", _completion);

    void Because()
    {
        _completion.SetException(new InvalidOperationException("Client wait failed"));
        _ = _completion.Task.Exception;
        _removed = HasEntryCount("_byId", 0) && HasEntryCount("_byPartition", 0);
    }

    [Fact] void should_remove_the_faulted_wait() => _removed.ShouldBeTrue();
}
