// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_an_unavailable_receipt_hash : given.an_acknowledged_append
{
    AppendResult _result;

    void Establish() => _response.Receipt.Hash = EventHash.NotSet;

    async Task Because() => _result = await _eventSequence.Append(_source, "event");

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_the_unavailable_hash() => _result.Receipt.Hash.ShouldEqual(EventHash.NotSet);
    [Fact] void should_notify_with_the_receipt() => _notifications.Single().Event.Context.ShouldEqual(_result.Receipt);
}
