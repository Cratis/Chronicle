// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_appending_an_event_carrying_an_encrypted_value;

public class and_the_event_is_read_back : Specification, IDisposable
{
    const string ApiKey = "sk_live_another_partner_secret";

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
        await _scenario.EventLog.Append(_eventSourceId, new PartnerIntegrationConfigured(ApiKey));
        _contentAtRest = await _scenario.ReadContentAtRest(EventSequenceNumber.First);
        _readBack = await _scenario.EventSequence.GetFromSequenceNumber(EventSequenceNumber.First, _eventSourceId);
    }

    [Fact] void should_release_the_value_on_read() => ((PartnerIntegrationConfigured)_readBack[0].Content).ApiKey.Value.ShouldEqual(ApiKey);
    [Fact] void should_not_have_read_it_straight_out_of_the_store() => Assert.DoesNotContain(ApiKey, _contentAtRest, StringComparison.Ordinal);

    public void Dispose() => _scenario.Dispose();
}
