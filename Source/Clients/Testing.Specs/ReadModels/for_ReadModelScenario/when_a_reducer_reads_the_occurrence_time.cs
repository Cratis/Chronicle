// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

public class when_a_reducer_reads_the_occurrence_time : Specification
{
    ReadModelScenario<RecordedOccurrence> _scenario;

    void Establish() => _scenario = new();

    async Task Because() => await _scenario.Given.ForEventSourceId(EventSourceId.New()).Events(new ThingOpened("Acme"));

    void Destroy() => _scenario.Dispose();

    [Fact] void should_deliver_occurred_in_utc() => _scenario.Instance!.Occurred.Offset.ShouldEqual(TimeSpan.Zero);
}
