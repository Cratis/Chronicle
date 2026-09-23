// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_appending_an_event_carrying_a_globally_scoped_encrypted_value;

/// <summary>
/// Proves EncryptionScope.Global end to end through the public EventScenario surface: the schema-driven metadata
/// resolution and JsonComplianceManager dispatch route a globally-scoped value to its own handler rather than
/// accidentally falling back to the subject- or namespace-scoped one, which would still happen to round-trip
/// correctly for a single document and hide the wiring defect.
/// </summary>
public class and_the_event_is_read_back : Specification, IDisposable
{
    const string Token = "lic_installation_wide_token";

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
        await _scenario.EventLog.Append(_eventSourceId, new LicenseActivated(Token));
        _contentAtRest = await _scenario.ReadContentAtRest(EventSequenceNumber.First);
        _readBack = await _scenario.EventSequence.GetFromSequenceNumber(EventSequenceNumber.First, _eventSourceId);
    }

    [Fact] void should_release_the_value_on_read() => ((LicenseActivated)_readBack[0].Content).Token.Value.ShouldEqual(Token);
    [Fact] void should_not_have_read_it_straight_out_of_the_store() => Assert.DoesNotContain(Token, _contentAtRest, StringComparison.Ordinal);

    public void Dispose() => _scenario.Dispose();
}
