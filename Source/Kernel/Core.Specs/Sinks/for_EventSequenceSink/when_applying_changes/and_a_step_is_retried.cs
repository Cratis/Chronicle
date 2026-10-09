// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Sinks.for_EventSequenceSink.when_applying_changes;

public class and_a_step_is_retried : given.a_sink
{
    async Task Because()
    {
        await _sink.ApplyChanges("customer-1", Fold(new(), 10, 1, 5), 5);

        // The same processing step after a crash, in a different sink instance (e.g. another silo).
        var other = new EventSequenceSink(Store, Tenant, _definition, _configuration, _destinations.Grains, _destinations.Storage, _destinations.Converter);
        await other.ApplyChanges("customer-1", Fold(new(), 10, 1, 5), 5);
        await other.ApplyChanges("customer-1", Fold(new(), 10, 1, 5), 5, Storage.Sinks.SinkWriteMode.OnlyWhenAdvancingWatermark);
    }

    [Fact] void should_attempt_the_append_again_with_the_same_identity() => _destinations.AppendAttempts.ShouldEqual(2);
    [Fact] void should_not_create_a_new_public_instance() => _destinations.Events(Store, Tenant, EventSequenceId.Outbox).Count.ShouldEqual(1);
}
