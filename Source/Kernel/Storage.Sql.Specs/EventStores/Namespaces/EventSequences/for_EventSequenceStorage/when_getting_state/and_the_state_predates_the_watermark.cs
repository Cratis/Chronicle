// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_getting_state;

public class and_the_state_predates_the_watermark : given.a_state_storage
{
    Chronicle.Storage.EventSequences.EventSequenceState _result;

    void Establish()
    {
        using var context = CreateContext();
        context.EventSequences.Add(new EventSequenceState { EventSequenceId = EventSequenceId.Log.Value, SequenceNumber = 42 });
        context.SaveChanges();
    }

    async Task Because() => _result = await _storage.GetState();

    [Fact] void should_preserve_the_sequence_number() => _result.SequenceNumber.Value.ShouldEqual(42UL);
    [Fact] void should_not_invent_a_watermark() => _result.ConstraintIndexWatermark.ShouldBeNull();
    [Fact] void should_not_invent_a_boundary() => _result.ConstraintIndexCatchUpBelow.ShouldBeNull();
    [Fact] void should_not_invent_a_refusal() => _result.ConstraintIndexCatchUpRefusing.ShouldBeNull();
}
