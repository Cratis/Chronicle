// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.EventStores.for_EnsureEventStore.when_ensuring;

public class and_the_notification_cannot_be_appended : given.an_ensure_command
{
    Exception _failure;

    void Establish() => _systemSequence.Append(Arg.Any<EventSourceId>(), Arg.Any<object>(), default, default, default, default, default, default, default)
        .Returns(new AppendResult { Errors = ["Notification failed"] });

    async Task Because() => _failure = await Catch.Exception(Ensure);

    [Fact] void should_not_report_success() => _failure.ShouldBeOfExactType<EventStoreInitializationFailed>();
    [Fact] async Task should_leave_initialization_retryable() => await _store.DidNotReceive().MarkInitialized();
}
