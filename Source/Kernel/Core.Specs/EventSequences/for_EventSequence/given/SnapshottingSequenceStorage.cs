// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class SnapshottingSequenceStorage(Func<EventSequenceState, Task> save) : global::Orleans.Core.IStorage<EventSequenceState>
{
    public string Etag => string.Empty;
    public bool RecordExists { get; private set; }
    public EventSequenceState State { get; set; } = new();
    public EventSequenceState? Written { get; private set; }
    public List<EventSequenceState> Snapshots { get; } = [];
    public Func<Task> BeforeWrite { get; set; } = () => Task.CompletedTask;

    public Task ReadStateAsync() => Task.CompletedTask;
    public Task ClearStateAsync()
    {
        State = new();
        Written = null;
        RecordExists = false;
        return Task.CompletedTask;
    }

    public async Task WriteStateAsync()
    {
        var snapshot = new EventSequenceState
        {
            SequenceNumber = State.SequenceNumber,
            ConstraintIndexWatermark = State.ConstraintIndexWatermark,
            ConstraintIndexCatchUpBelow = State.ConstraintIndexCatchUpBelow,
            ConstraintIndexCatchUpRefusing = State.ConstraintIndexCatchUpRefusing?.ToArray(),
            TailSequenceNumberPerEventType = new Dictionary<Concepts.Events.EventTypeId, Concepts.Events.EventSequenceNumber>(State.TailSequenceNumberPerEventType)
        };
        await BeforeWrite();
        await save(snapshot);
        Written = snapshot;
        Snapshots.Add(snapshot);
        RecordExists = true;
    }
}
