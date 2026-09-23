// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_appending_an_event_carrying_an_encrypted_value;

/// <summary>
/// The [Encrypted] counterpart to when_appending_an_event_carrying_pii/and_the_stored_content_is_inspected - the
/// same guarantee (an unhandled marker must never pass a value through as plaintext) proved through the public
/// EventScenario surface an application's own specs use, not through kernel internals.
/// </summary>
public class and_the_stored_content_is_inspected : Specification, IDisposable
{
    const string ApiKey = "sk_live_partner_signing_secret";

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
        await _scenario.EventLog.Append(_eventSourceId, new PartnerIntegrationConfigured(ApiKey));
        _contentAtRest = await _scenario.ReadContentAtRest(EventSequenceNumber.First);
    }

    [Fact] void should_not_store_the_plaintext() => Assert.DoesNotContain(ApiKey, _contentAtRest, StringComparison.Ordinal);
    [Fact] void should_still_store_the_property() => Assert.Contains("apiKey", _contentAtRest, StringComparison.Ordinal);

    public void Dispose() => _scenario.Dispose();
}
