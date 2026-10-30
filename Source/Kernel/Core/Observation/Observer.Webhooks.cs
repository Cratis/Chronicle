// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    bool _storedWebhookPolicyWarningLogged;

    async Task<bool> ValidateStoredWebhookTarget()
    {
        if (!await IsStoredWebhook())
        {
            return true;
        }

        var definition = await storage.GetEventStore(_observerKey.EventStore).Webhooks.Get((WebhookId)_observerId.Value);
        if (!definition.IsActive)
        {
            _subscriptionSetupFailed = false;
            return false;
        }

        var validation = await webhookTargetPolicy.EvaluateForDelivery(definition.Target.Url, _observerKey.EventStore);
        var warnings = new List<string>();
        if (validation.IsAllowed && validation.Reason.Length > 0) warnings.Add(validation.Reason);

        // Managed identity token acquisition uses Azure's pipeline, not the webhook target policy.
        // The receiver URL above remains subject to the delivery policy for every authorization type.
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

        return await RejectStoredWebhookTarget(validation);
    }

    async Task<bool> RejectStoredWebhookTarget(WebhookTargetValidation validation)
    {
        if (validation.IsIndeterminate)
        {
            if (IsQuarantined) return false;
            await Unsubscribe();
            _subscriptionSetupFailed = true;
            return false;
        }

        logger.StoredWebhookTargetRejected(validation.Reason);
        _subscriptionSetupFailed = false;
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

    /// <summary>
    /// Determines whether this observer delivers to a stored webhook. Other kernel-owned external observers,
    /// such as event store subscriptions, share the same observer type and owner and must not be evaluated.
    /// </summary>
    /// <returns>True when the observer delivers to a stored webhook.</returns>
    async Task<bool> IsStoredWebhook()
    {
        if (_subscription.IsSubscribed)
        {
            return typeof(IWebhookObserverSubscriber).IsAssignableFrom(_subscription.SubscriberType);
        }

        return Definition.Type == ObserverType.External &&
            Definition.Owner == ObserverOwner.Kernel &&
            await storage.GetEventStore(_observerKey.EventStore).Webhooks.Has((WebhookId)_observerId.Value);
    }
}
