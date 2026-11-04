// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Core.Specs.Patches.for_RemoveNotSetObserverReminders.when_applying_up;

public class and_reminders_exist : given.a_patch_with_reminders
{
    Task Because() => _patch.Up();

    [Fact] void should_remove_the_reminder_of_a_not_set_observer() => _reminderTable.Received(1).RemoveRow(_notSetObserver.GrainId, _notSetObserver.ReminderName, _notSetObserver.ETag);
    [Fact] void should_remove_a_reminder_hashed_in_the_second_range() => _reminderTable.Received(1).RemoveRow(_otherNotSetObserver.GrainId, _otherNotSetObserver.ReminderName, _otherNotSetObserver.ETag);
    [Fact] void should_keep_the_reminder_of_a_real_observer() => _reminderTable.DidNotReceive().RemoveRow(_realObserver.GrainId, Arg.Any<string>(), Arg.Any<string>());
    [Fact] void should_keep_the_reminder_of_a_grain_that_is_not_an_observer() => _reminderTable.DidNotReceive().RemoveRow(_notSetGrainOfAnotherType.GrainId, Arg.Any<string>(), Arg.Any<string>());
    [Fact] void should_remove_exactly_the_not_set_reminders() => _reminderTable.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(IReminderTable.RemoveRow)).ShouldEqual(2);
    [Fact] void should_run_after_the_released_version() => (_patch.Version > new Concepts.System.SemanticVersion(19, 117, 0)).ShouldBeTrue();
}
