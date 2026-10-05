// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Orleans.Core;
using Orleans.TestKit.Storage;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class faultable_observer_state_storage : IStorage<ObserverState>, IStorageStats
{
    ObserverRunningState? _failNextWriteOf;
    Exception? _writeFailure;

    public ObserverState State { get; set; } = new();
    public TestStorageStats Stats { get; } = new() { Reads = -1 };
    public string Etag => string.Empty;
    public bool RecordExists { get; private set; }

    public void FailNextWriteOf(ObserverRunningState runningState, Exception failure)
    {
        _failNextWriteOf = runningState;
        _writeFailure = failure;
    }

    public Task ReadStateAsync()
    {
        Stats.Reads++;
        return Task.CompletedTask;
    }

    public Task WriteStateAsync()
    {
        if (_failNextWriteOf is { } runningState && State.RunningState == runningState)
        {
            _failNextWriteOf = null;
            return Task.FromException(_writeFailure!);
        }

        RecordExists = true;
        Stats.Writes++;
        return Task.CompletedTask;
    }

    public Task ClearStateAsync()
    {
        State = new();
        RecordExists = false;
        Stats.Clears++;
        return Task.CompletedTask;
    }
}
