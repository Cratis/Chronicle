// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_a_guid_source_uses_an_alias : Specification
{
    EventStoreForTesting _store;
    string _upperKey;
    DecisionReadRefused _error;

    async Task Establish()
    {
        _store = new EventStoreForTesting();
        _upperKey = Guid.NewGuid().ToString("D").ToUpperInvariant();
        await _store.EventLog.Append((EventSourceId)_upperKey, new ModuleCreated("Aliased"));
    }

    async Task Because() => _error = await Record.ExceptionAsync(
        () => _store.GetDecisionReads().GetDetached<SimpleModule>((ReadModelKey)_upperKey)) as DecisionReadRefused;

    [Fact] void should_refuse_a_noncanonical_guid_key() => _error.Reason.ShouldEqual(DecisionReadRefusalReason.InvalidKey);
    [Fact] async Task should_leave_the_uppercase_history_intact() =>
        (await _store.EventLog.HasEventsFor((EventSourceId)_upperKey)).ShouldBeTrue();
}
