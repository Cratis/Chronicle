// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class a_rebuilding_constraint_index : a_catching_up_sequence
{
    protected readonly ConstraintIndexRebuildRunId _runId = new(Guid.NewGuid());
    protected readonly ConstraintIndexRebuildRunId _otherRunId = new(Guid.NewGuid());
    protected override EventSequenceNumber? Watermark => 2;
    protected override EventSequenceNumber InitialNext => 2;

    async Task Establish()
    {
        await Seed(0, "source", email: "old");
        await Seed(1, "source", email: "current");
        await Hold("source", "current");
    }
}
