// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_appending_an_event;

public class and_no_hash_is_available : given.an_event_sequence_storage
{
    AppendedEvent _result;

    async Task Because() => _result = (await Append(EventSequenceNumber.First, "source")).AsT0;

    [Fact] void should_preserve_the_not_set_hash() => _result.Context.Hash.ShouldEqual(EventHash.NotSet);
}
