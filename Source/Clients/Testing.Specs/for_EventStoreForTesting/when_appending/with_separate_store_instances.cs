// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_appending;

public class with_separate_store_instances : given.store_identity
{
    EventStoreForTesting _other;
    AppendResult _result;

    void Establish()
    {
        _store = new(null, _artifacts, null, new ScenarioTime(), _name, _namespace);
        _other = new(null, _artifacts, null, new ScenarioTime(), "other-store", _namespace);
    }

    async Task Because() => _result = await _store.EventLog.Append(_source, new StoreIdentityRecorded("isolated"));

    [Fact] void should_append_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_store_the_event_in_the_first_store() => (await _store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Count.ShouldEqual(1);
    [Fact] async Task should_leave_the_other_store_empty() => (await _other.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).ShouldBeEmpty();

    void Destroy() => _other.Dispose();
}
