// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// Verifies that <see cref="ReadModelScenario{TReadModel}"/> assigns increasing per-event sequence numbers
/// to a reducer's <see cref="EventContext"/> rather than a constant, so the second of two events is seen as
/// sequence number 1.
/// </summary>
public class when_a_reducer_reads_the_event_sequence_number : Specification
{
    ReadModelScenario<Tally> _scenario;
    EventSourceId _id;

    void Establish()
    {
        _id = EventSourceId.New();
        _scenario = new ReadModelScenario<Tally>();
    }

    async Task Because() =>
        await _scenario.Given.ForEventSource(_id).Events(new SequenceProbed(), new SequenceProbed());

    [Fact] void should_assign_increasing_sequence_numbers() => _scenario.Instance!.Count.ShouldEqual(1);
}
