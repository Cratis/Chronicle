// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.EventSequences;
using Orleans.Storage;

namespace Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.given;

public class FailingEventSequenceStateStorage(EventSequencesStorageProvider inner, PatternCaptureControl control) : IGrainStorage
{
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => inner.ClearStateAsync(stateName, grainId, grainState);

    public Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => inner.ReadStateAsync(stateName, grainId, grainState);

    public Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        if (control.ShouldFailStateWrite(EventSequenceKey.Parse(grainId.Key.ToString())))
        {
            throw new IOException("The first event-sequence state snapshot write failed.");
        }

        return inner.WriteStateAsync(stateName, grainId, grainState);
    }
}
