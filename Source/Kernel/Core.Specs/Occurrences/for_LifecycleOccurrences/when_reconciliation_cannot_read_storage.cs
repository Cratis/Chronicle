// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_LifecycleOccurrences;

public class when_reconciliation_cannot_read_storage : given.a_lifecycle_grain
{
    Exception? _error;

    void Establish()
    {
        var intents = Substitute.For<ILifecycleFanOutStorage>();
        intents.GetPending(Arg.Any<int>()).Returns<Task<IEnumerable<LifecycleFanOutIntent>>>(_ => throw new IOException("storage unavailable"));
        _store.LifecycleFanOut.Returns(intents);
    }

    async Task Because() => _error = await Catch.Exception(_grain.Ensure);

    [Fact] void should_not_fail_kernel_startup() => _error.ShouldBeNull();
    [Fact] void should_keep_a_retry_armed() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
}
