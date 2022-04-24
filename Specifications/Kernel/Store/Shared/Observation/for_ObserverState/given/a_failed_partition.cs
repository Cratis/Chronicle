// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Observation.for_ObserverState.given;

public class a_failed_partition : Specification
{
    protected static EventSourceId partition = Guid.NewGuid();
    protected static EventSequenceNumber sequence_number = 42;
    protected static string[] messages = new[]
    {
        "first line",
        "second line"
    };
    protected static string stack_trace = "The stack trace";
    protected ObserverState state;

    void Establish()
    {
        state = new();
        state.FailPartition(partition, sequence_number, messages, stack_trace);
    }
}
