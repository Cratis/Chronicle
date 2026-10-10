// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Constraints.for_ConstraintDefinitionConverters;

public class when_converting_a_closes_stream_definition : Specification
{
    ClosesStreamConstraintDefinition _definition;
    IConstraintDefinition _result;

    void Establish() => _definition = new("closing", ["Closed", "Cancelled"], ClosedStreamDimensions.EventSourceId | ClosedStreamDimensions.EventStreamId, ["Reopened"], "period")
    {
        EventSequences = [EventSequenceId.Log]
    };

    void Because() => _result = _definition.ToSql(1).ToKernel();

    [Fact] void should_round_trip_the_complete_declaration() => _result.ShouldEqual(_definition);
    [Fact] void should_not_request_automatic_reindexing() => _result.CompareWith(_definition).RequiresReindex.ShouldBeFalse();
}
