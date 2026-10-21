// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_empty_sequence_with_a_failing_first_snapshot : an_event_sequence
{
    protected SnapshottingSequenceStorage _snapshots;
    protected int _stateWriteAttempts;

    protected override async Task<EventSequence> CreateEventSequence()
    {
        _silo = new();
        ConfigureSilo();
        _snapshots = new(_ => Task.CompletedTask)
        {
            BeforeWrite = () => ++_stateWriteAttempts == 1
                ? Task.FromException(new SimulatedStateWriteError())
                : Task.CompletedTask
        };
        _silo.Options.StorageFactory = _ => _snapshots;
        _stateStorage = _silo.StorageManager.GetStorage<EventSequenceState>(typeof(EventSequence).FullName!);
        return await base.CreateEventSequence();
    }
}
