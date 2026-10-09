// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Sinks.for_EventSequenceSink.when_applying_changes;

public class and_a_custom_sequence_is_configured : given.a_sink
{
    static readonly EventSequenceId _custom = new("public-totals");

    protected override EventSequenceId? Configured => _custom;

    async Task Because() => await _sink.ApplyChanges("customer-1", Fold(new(), 10, 1, 5), 5);

    [Fact] void should_publish_to_the_configured_sequence() => _destinations.Events(Store, Tenant, _custom).Count.ShouldEqual(1);
    [Fact] void should_not_publish_to_the_outbox() => _destinations.Events(Store, Tenant, EventSequenceId.Outbox).ShouldBeEmpty();
}
