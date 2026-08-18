// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_appending_an_event_carrying_pii;

/// <summary>
/// A <c>[PII]</c> value must never reach the store as plaintext. The harness used to construct the kernel's
/// compliance manager with no property value handlers at all, which made every marked value pass straight
/// through — invisibly, because an unhandled value and a value with no marker look identical downstream.
/// </summary>
public class and_the_stored_content_is_inspected : Specification, IDisposable
{
    const string EmailAddress = "ada@example.com";

    EventScenario _scenario;
    EventSourceId _eventSourceId;
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
    }

    [Fact] void should_not_store_the_plaintext() => Assert.DoesNotContain(EmailAddress, _contentAtRest, StringComparison.Ordinal);
    [Fact] void should_still_store_the_property() => Assert.Contains("emailAddress", _contentAtRest, StringComparison.Ordinal);

    public void Dispose() => _scenario.Dispose();
}
