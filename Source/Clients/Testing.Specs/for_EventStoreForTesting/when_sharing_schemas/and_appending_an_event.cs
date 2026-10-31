// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_sharing_schemas;

public class and_appending_an_event : given.schema_sharing
{
    AppendResult _result;

    void Establish() => _other = new(null, _artifacts);

    async Task Because() => _result = await _store.EventLog.Append(_source, new StoreIdentityRecorded("isolated"));

    [Fact] void should_share_the_underlying_generator() => GeneratorFor(_store).ShouldBeSame(GeneratorFor(_other));
    [Fact] void should_append_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_store_the_event_in_the_first_store() => (await _store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Count.ShouldEqual(1);
    [Fact] async Task should_leave_the_other_store_empty() => (await _other.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).ShouldBeEmpty();
}
