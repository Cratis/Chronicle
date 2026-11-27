// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemover;

public class when_cleaning_legacy_namespace_reminders : given.a_namespace_remover
{
    static readonly EventStoreNamespaceName _legacyNamespace = "tenant,tenant";
    static readonly GrainId _lifecycleOwner = GrainId.Create("namespaceincarnation", "lifecycle-owner");
    static readonly GrainId _observerGrain = GrainId.Create("observer", new ObserverKey("observer", _eventStore, _legacyNamespace, EventSequenceId.Log).ToString());

    void Establish() => _reminderTable.ReadRows(Arg.Any<uint>(), Arg.Any<uint>()).Returns(call => Task.FromResult(call.ArgAt<uint>(0) == 0
        ? new ReminderTableData([
            new() { GrainId = _lifecycleOwner, ReminderName = "recovery", ETag = "owner-etag" },
            new() { GrainId = _observerGrain, ReminderName = "observer", ETag = "observer-etag" }])
        : new ReminderTableData([])));

    Task Because() => _remover.Execute(_eventStore, _legacyNamespace, NamespaceRemovalStage.Cleanup);

    [Fact] async Task should_remove_the_observer_reminder() => await _reminderTable.Received(1).RemoveRow(_observerGrain, "observer", "observer-etag");
    [Fact] async Task should_leave_the_lifecycle_owners_durable_wake_alone() => await _reminderTable.DidNotReceive().RemoveRow(_lifecycleOwner, Arg.Any<string>(), Arg.Any<string>());
    [Fact] async Task should_not_repeat_forget() => await _namespaces.DidNotReceive().Forget(_legacyNamespace);
}
