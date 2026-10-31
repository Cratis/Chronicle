// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.for_Defaults.when_constructing;

public class with_explicit_identity : given.store_identity
{
    AppendResult _append;
    bool _committed;

    async Task Because()
    {
        _defaults = new(_artifacts, null, null, null, _name, _namespace);
        _append = await _defaults.EventStore.EventLog.Append(_source, new DefaultsIdentityRecorded("direct"));
        using var unit = _defaults.EventStore.UnitOfWorkManager.Begin(CorrelationId.New());
        unit.AddEvent(EventSequenceId.Log, _source, new DefaultsIdentityRecorded("transactional"), Causation.Unknown());
        await unit.Commit();
        _committed = unit.IsSuccess;
    }

    [Fact] void should_use_the_name() => _defaults.EventStore.Name.ShouldEqual(_name);
    [Fact] void should_use_the_namespace() => _defaults.EventStore.Namespace.ShouldEqual(_namespace);
    [Fact] void should_append_through_the_event_log() => _append.IsSuccess.ShouldBeTrue();
    [Fact] void should_commit_through_the_unit_of_work_manager() => _committed.ShouldBeTrue();
    [Fact] async Task should_store_both_appends_in_the_same_log() => (await _defaults.EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Select(appended => ((DefaultsIdentityRecorded)appended.Content).Value).ShouldEqual(["direct", "transactional"]);
}
