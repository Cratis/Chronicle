// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence;

public class when_completing_an_import_commit : given.an_event_sequence_with_imports
{
    async Task Because() => await _eventSequence.CompleteImportCommit(_importId);

    [Fact] async Task should_release_the_hold_so_held_observers_catch_up_once() => await _appendedEventsQueues.Received(1).ReleaseHold($"import-{_importId.Value:D}");
}
