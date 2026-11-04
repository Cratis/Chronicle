// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Patches;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RemoveNotSetObserverReminders.given;

public class a_patch_with_reminders : Specification
{
    protected RemoveNotSetObserverReminders _patch;
    protected IReminderTable _reminderTable;
    protected ReminderEntry _notSetObserver;
    protected ReminderEntry _otherNotSetObserver;
    protected ReminderEntry _realObserver;
    protected ReminderEntry _notSetGrainOfAnotherType;

    void Establish()
    {
        _notSetObserver = Reminder("observer", new ObserverKey("$system.patterns", EventStoreName.NotSet, EventStoreNamespaceName.Default, EventSequenceId.Log).ToString());
        _otherNotSetObserver = Reminder("observer", new ObserverKey("$system.other", EventStoreName.NotSet, EventStoreNamespaceName.Default, EventSequenceId.System).ToString());
        _realObserver = Reminder("observer", new ObserverKey("Reactor", "Direct", EventStoreNamespaceName.Default, EventSequenceId.Log).ToString());
        _notSetGrainOfAnotherType = Reminder("other", new ObserverKey("$system.patterns", EventStoreName.NotSet, EventStoreNamespaceName.Default, EventSequenceId.Log).ToString());

        _reminderTable = Substitute.For<IReminderTable>();
        _reminderTable.ReadRows(Arg.Any<uint>(), Arg.Any<uint>()).Returns(callInfo => Task.FromResult(callInfo.ArgAt<uint>(0) == 0
            ? new ReminderTableData([_notSetObserver, _realObserver, _notSetGrainOfAnotherType])
            : new ReminderTableData([_otherNotSetObserver])));
        _reminderTable.RemoveRow(Arg.Any<GrainId>(), Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(true));

        _patch = new RemoveNotSetObserverReminders(_reminderTable, NullLogger<RemoveNotSetObserverReminders>.Instance);
    }

    static ReminderEntry Reminder(string grainType, string key) => new()
    {
        GrainId = GrainId.Create(grainType, key),
        ReminderName = "chronicle-observer:alerts",
        ETag = $"etag-{key}"
    };
}
