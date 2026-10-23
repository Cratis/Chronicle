// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <inheritdoc/>
    /// <remarks>
    /// An observer that has never been persisted has no identifier in its stored state. That is the one moment the
    /// position can be chosen without discarding anything. The position and the identifier are written together, in
    /// the observer's own turn, so a concurrent registration sees an existing observer and leaves it alone.
    /// The identifier is written even when the sequence is empty, so a later registration is not mistaken for the first.
    /// </remarks>
    public async Task<bool> StartAtTailWhenNew()
    {
        ThrowIfSealed();
        var storageWasReset = await ReloadObserverState();
        if (!storageWasReset || IsRetired || _removed)
        {
            return false;
        }

        // Starting at the tail is the first step of registering, and registering decides which event sequence the
        // observer is on. Without the claim, a definition shared with another sequence would leave the position unwritten.
        ClaimDefinitionForKey();
        var tail = await _eventSequence.GetTailSequenceNumber();
        if (tail.IsActualValue)
        {
            State = State with { NextEventSequenceNumber = tail.Next(), TailEventSequenceNumber = tail };
        }

        // State is only persisted for an observer that exists. Creating it is what makes the position stick, and
        // what stops the next registration from being treated as the first.
        await EnsureAlertState();
        await WriteStateAsync();
        return true;
    }
}
