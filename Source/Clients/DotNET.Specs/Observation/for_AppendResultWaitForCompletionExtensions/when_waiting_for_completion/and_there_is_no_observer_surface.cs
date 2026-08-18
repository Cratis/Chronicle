// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.for_AppendResultWaitForCompletionExtensions.when_waiting_for_completion;

public class and_there_is_no_observer_surface : Specification
{
    AppendResult _appendResult;
    Exception _exception;

    void Establish() => _appendResult = new AppendResult
    {
        EventStore = "event-store",
        EventStoreNamespace = "event-store-namespace",
        EventSequenceId = EventSequenceId.Log,
        SequenceNumber = 42UL
    };

    async Task Because() => _exception = await Catch.Exception(async () => await _appendResult.WaitForCompletion());

    [Fact] void should_not_report_that_observers_completed() => _exception.ShouldBeOfExactType<CannotWaitForObserverCompletion>();
}
