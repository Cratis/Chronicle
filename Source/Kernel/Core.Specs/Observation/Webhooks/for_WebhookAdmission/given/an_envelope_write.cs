// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookAdmission.given;

public class an_envelope_write : for_WebhookRegistrar.given.a_pinned_registration
{
    private protected WebhookAdmission _admission;
    private protected WebhookAdmissionContext _context;
    protected WebhookDefinition _webhook;
    protected IEnumerable<object> _events;
    protected WebhookAuthorization? _authorization;
    protected int _comparisons;

    void Establish()
    {
        _definition.DeliveryFormat = Contracts.Observation.Webhooks.WebhookDeliveryFormat.EnvelopeV1;
        _webhook = _definition.ToChronicle();
        _admission = new(_grainFactory, _storage, _eventSerializer);
        _eventSequence.AppendMany(Arg.Any<IEnumerable<EventToAppend>>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Causation>>(), Arg.Any<Identity>(), Arg.Any<ConcurrencyScopes>())
            .Returns(AppendManyResult.Success(CorrelationId.New(), []));
        _context = new("store");
        _events = [new WebhookAdded(_webhook.Owner, _webhook.EventSequenceId, _webhook.EventTypes, _webhook.Target.Url, _webhook.Target.Headers, _webhook.IsReplayable, _webhook.IsActive) { DeliveryFormat = _webhook.DeliveryFormat }];
        TheStoredRegistrationHasFormat(WebhookDeliveryFormat.EnvelopeV1);
    }

    protected Task Write() => _admission.Write(_eventSequence, _webhook, _context, () =>
    {
        _comparisons++;
        return Task.FromResult((_events, _authorization));
    });
}
