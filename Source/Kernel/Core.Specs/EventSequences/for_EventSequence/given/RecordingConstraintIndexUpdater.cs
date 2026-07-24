// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

/// <summary>
/// An <see cref="IUpdateConstraintIndex"/> that records the <see cref="EventSequenceNumber"/> it is asked to index with.
/// </summary>
/// <param name="recordedSequenceNumbers">The sink that captures every sequence number the index is updated with.</param>
public class RecordingConstraintIndexUpdater(IList<EventSequenceNumber> recordedSequenceNumbers) : IUpdateConstraintIndex
{
    /// <inheritdoc/>
    public Task Update(EventSequenceNumber eventSequenceNumber)
    {
        recordedSequenceNumbers.Add(eventSequenceNumber);
        return Task.CompletedTask;
    }
}
