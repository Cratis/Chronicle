// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintDefinitionComparison;

public class when_a_closing_constraint_is_added : Specification
{
    IReadOnlyCollection<ConstraintDefinitionChange> _changes;

    void Because() => _changes = ConstraintDefinitionComparison.GetReindexChanges([], [new ClosesStreamConstraintDefinition("closing", ["Closed"], ClosedStreamDimensions.EventSourceId, [])], EventSequenceId.Log);

    [Fact] void should_not_request_automatic_reindexing() => _changes.ShouldBeEmpty();
}
