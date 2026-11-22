// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_getting_state;

public class and_no_state_exists : given.a_state_storage
{
    Chronicle.Storage.EventSequences.EventSequenceState _result;

    void Establish()
    {
        using var context = CreateContext();
        context.EventSequences.Add(new EventSequenceState { EventSequenceId = "another-sequence", SequenceNumber = 99 });
        context.SaveChanges();
    }

    async Task Because() => _result = await _storage.GetState();

    [Fact] void should_return_the_initial_sequence_number() => _result.SequenceNumber.ShouldEqual(new Chronicle.Storage.EventSequences.EventSequenceState().SequenceNumber);
    [Fact] void should_have_no_tails() => _result.TailSequenceNumberPerEventType.ShouldBeEmpty();
    [Fact] void should_not_invent_a_watermark() => _result.ConstraintIndexWatermark.ShouldBeNull();
}
