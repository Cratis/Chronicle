// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_appending_an_event_carrying_an_encrypted_value;

/// <summary>
/// The two features coexist on one event, protecting different properties for the same event source id (the
/// default subject when none is set), each releasing correctly on read - proved through the public EventScenario
/// surface exactly as an application's own specs would exercise it.
/// </summary>
public class and_the_same_event_also_carries_pii : Specification, IDisposable
{
    const string ContactEmail = "partner-contact@example.com";
    const string ApiKey = "sk_live_shared_event_secret";

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
        await _scenario.EventLog.Append(_eventSourceId, new PartnerIntegrationConfiguredWithContact(ContactEmail, ApiKey));
        _contentAtRest = await _scenario.ReadContentAtRest(EventSequenceNumber.First);
        _readBack = await _scenario.EventSequence.GetFromSequenceNumber(EventSequenceNumber.First, _eventSourceId);
    }

    [Fact] void should_not_store_the_email_plaintext() => Assert.DoesNotContain(ContactEmail, _contentAtRest, StringComparison.Ordinal);
    [Fact] void should_not_store_the_api_key_plaintext() => Assert.DoesNotContain(ApiKey, _contentAtRest, StringComparison.Ordinal);
    [Fact] void should_use_different_ciphertext_for_the_two_properties() => Assert.NotEqual(
        ExtractCiphertext(_contentAtRest, "contactEmail"),
        ExtractCiphertext(_contentAtRest, "apiKey"));
    [Fact] void should_release_the_email_on_read() => ((PartnerIntegrationConfiguredWithContact)_readBack[0].Content).ContactEmail.Value.ShouldEqual(ContactEmail);
    [Fact] void should_release_the_api_key_on_read() => ((PartnerIntegrationConfiguredWithContact)_readBack[0].Content).ApiKey.Value.ShouldEqual(ApiKey);

    static string ExtractCiphertext(string json, string property)
    {
        var marker = $"\"{property}\":\"";
        var start = json.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = json.IndexOf('"', start);
        return json[start..end];
    }

    public void Dispose() => _scenario.Dispose();
}
