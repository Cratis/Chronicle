// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_reminded.given;

public class an_observer_with_event_types_a_reminder_and_two_failing_partitions : an_observer_with_event_types_and_reminder
{
    protected const string first_partition = "0fbbbd40-1380-4a9d-bbbe-7b46b324f537";
    protected const string second_partition = "db5fa43e-32eb-4481-9e74-d13b7a501ab3";
    protected const ulong first_partition_failed_sequence = 42;
    protected const ulong second_partition_failed_sequence = 43;

    void Establish()
    {
        state.FailPartition(first_partition, first_partition_failed_sequence, Array.Empty<string>(), string.Empty);
        state.FailPartition(second_partition, second_partition_failed_sequence, Array.Empty<string>(), string.Empty);
    }
}
