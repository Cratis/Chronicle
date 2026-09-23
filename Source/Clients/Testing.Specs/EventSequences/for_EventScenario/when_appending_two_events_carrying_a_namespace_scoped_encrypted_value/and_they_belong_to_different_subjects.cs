// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_appending_two_events_carrying_a_namespace_scoped_encrypted_value;

/// <summary>
/// Proves EncryptionScope.Namespace end to end through the public EventScenario surface, not just against the
/// kernel handler directly: two different event sources, each carrying its own namespace-scoped secret, both
/// still release to their own plaintext on read - the schema-driven metadata resolution and JsonComplianceManager
/// dispatch route a namespace-scoped value to the namespace handler rather than accidentally falling back to the
/// subject-scoped one, which would still happen to round-trip correctly and hide the wiring defect.
/// </summary>
public class and_they_belong_to_different_subjects : Specification, IDisposable
{
    const string FirstSecret = "whsec_first_partner_signing_secret";
    const string SecondSecret = "whsec_second_partner_signing_secret";

    EventScenario _scenario;
    EventSourceId _firstEventSourceId;
    EventSourceId _secondEventSourceId;
    string _contentAtRest;
    string _firstReleasedSecret;
    string _secondReleasedSecret;

    void Establish()
    {
        _scenario = new EventScenario();
        _firstEventSourceId = EventSourceId.New();
        _secondEventSourceId = EventSourceId.New();
    }

    async Task Because()
    {
        await _scenario.EventLog.Append(_firstEventSourceId, new PartnerWebhookConfigured("https://first.example/hook", FirstSecret));
        await _scenario.EventLog.Append(_secondEventSourceId, new PartnerWebhookConfigured("https://second.example/hook", SecondSecret));

        var firstReadBack = await _scenario.EventSequence.GetFromSequenceNumber(EventSequenceNumber.First, _firstEventSourceId);
        var secondReadBack = await _scenario.EventSequence.GetFromSequenceNumber(EventSequenceNumber.First, _secondEventSourceId);
        _firstReleasedSecret = ((PartnerWebhookConfigured)firstReadBack[0].Content).Secret.Value;
        _secondReleasedSecret = ((PartnerWebhookConfigured)secondReadBack[0].Content).Secret.Value;

        _contentAtRest = await _scenario.ReadContentAtRest(EventSequenceNumber.First) + await _scenario.ReadContentAtRest(EventSequenceNumber.First + 1);
    }

    [Fact] void should_release_the_first_subjects_own_secret() => _firstReleasedSecret.ShouldEqual(FirstSecret);
    [Fact] void should_release_the_second_subjects_own_secret() => _secondReleasedSecret.ShouldEqual(SecondSecret);
    [Fact] void should_not_have_stored_either_secret_in_plaintext()
    {
        Assert.DoesNotContain(FirstSecret, _contentAtRest, StringComparison.Ordinal);
        Assert.DoesNotContain(SecondSecret, _contentAtRest, StringComparison.Ordinal);
    }

    public void Dispose() => _scenario.Dispose();
}
