// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_checking;

/// <summary>
/// The negative control: registering the input the check leaves alone does change the store,
/// so the snapshot used by the read-only specification would notice a write.
/// </summary>
public class and_registering_the_same_input_changes_the_store : given.a_store_to_leave_unchanged
{
    string _before;
    string _after;

    async Task Because()
    {
        _before = await Snapshot();
        await _subject.Register("test-store", [_newEventType, _newGeneration], false, _storage, _eventTypesCacheClient, _patternCapture);
        _after = await Snapshot();
    }

    [Fact] void should_change_the_stored_definitions() => _after.ShouldNotEqual(_before);
}
