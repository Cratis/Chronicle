// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_appending_an_event_carrying_pii;

/// <summary>
/// Encrypting at rest is only half the contract — reading the event back has to release the value again,
/// under the same subject and the same key. Asserting the round trip together with what sits in the store
/// is what separates "the value is protected" from "the value was quietly dropped".
/// </summary>
public class and_the_event_is_read_back : Specification, IDisposable
{
    const string EmailAddress = "grace@example.com";

    EventScenario _scenario;
    EventSourceId _eventSourceId;
    IImmutableList<AppendedEvent> _readBack;
    string _contentAtRest;

    void Establish()
    {
        _scenario = new EventScenario();
        _eventSourceId = EventSourceId.New();
    }

    async Task Because()
    {
        await _scenario.EventLog.Append(_eventSourceId, new MemberEnrolledWithEmail(EmailAddress));
        _contentAtRest = await _scenario.ReadContentAtRest(EventSequenceNumber.First);
        _readBack = await _scenario.EventSequence.GetFromSequenceNumber(EventSequenceNumber.First, _eventSourceId);
    }

    [Fact] void should_release_the_value_on_read() => ((MemberEnrolledWithEmail)_readBack[0].Content).EmailAddress.Value.ShouldEqual(EmailAddress);
    [Fact] void should_not_have_read_it_straight_out_of_the_store() => Assert.DoesNotContain(EmailAddress, _contentAtRest, StringComparison.Ordinal);

    public void Dispose() => _scenario.Dispose();
}
