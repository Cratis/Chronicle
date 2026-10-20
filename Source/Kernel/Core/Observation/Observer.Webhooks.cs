// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    bool _storedWebhookPolicyWarningLogged;

    async Task<bool> ValidateStoredWebhookTarget()
    {
        if (_subscription.SubscriberType != typeof(IWebhookObserverSubscriber) && (Definition.Type != ObserverType.External || Definition.Owner != ObserverOwner.Kernel)) return true;

        var definition = await storage.GetEventStore(_observerKey.EventStore).Webhooks.Get((WebhookId)_observerId.Value);
        if (!definition.IsActive) return false;

        var validation = await webhookTargetPolicy.EvaluateForDelivery(definition.Target.Url, _observerKey.EventStore);
        var warnings = new List<string>();
        if (validation.IsAllowed && validation.Reason.Length > 0) warnings.Add(validation.Reason);
        if (validation.IsAllowed && definition.Target.Authorization.IsT2)
        {
            validation = await webhookTargetPolicy.EvaluateForDelivery(definition.Target.Authorization.AsT2.Authority, _observerKey.EventStore);
        }
        if (validation.IsAllowed)
        {
            if (validation.Reason.Length > 0) warnings.Add(validation.Reason);
            if (warnings.Count > 0 && !_storedWebhookPolicyWarningLogged)
            {
                logger.StoredWebhookTargetWarning(string.Join(' ', warnings.Distinct()));
                _storedWebhookPolicyWarningLogged = true;
            }
            return true;
        }

        logger.StoredWebhookTargetRejected(validation.Reason);
        _isPreparingCatchup = false;
        NotePreparationEnded();
        if (await GetCurrentState() is Disconnected disconnected)
        {
            await disconnected.QuarantineUnrecoverableSubscription();
        }
        else if (!IsQuarantined)
        {
            await TransitionTo<QuarantinedObserver>();
        }

        return false;
    }
}
