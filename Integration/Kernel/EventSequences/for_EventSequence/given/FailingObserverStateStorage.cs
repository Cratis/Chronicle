// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.given;

public class FailingObserverStateStorage(IStorage storage, PatternCaptureControl control) : ObserverStateGrainStorageProvider(storage)
{
    public override Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var key = new ObserverKey(PatternCapture.ObserverIdentifier, control.Key.EventStore, control.Key.Namespace, control.Key.EventSequenceId);
        if (control.FailObserverStateWrite && grainId.Key.ToString() == key.ToString() &&
            grainState.State is ObserverState { RunningState: Concepts.Observation.ObserverRunningState.Unknown })
        {
            control.FailObserverStateWrite = false;
            control.FailedObserverStateWrites++;
            throw new IOException("Persisting entry into in-flight catch-up failed.");
        }

        return base.WriteStateAsync(stateName, grainId, grainState);
    }
}
