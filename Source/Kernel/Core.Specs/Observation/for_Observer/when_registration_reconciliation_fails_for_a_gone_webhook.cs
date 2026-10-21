// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_registration_reconciliation_fails_for_a_gone_webhook : given.a_gone_retired_webhook
{
    Guid _retirement;
    Guid _intent;
    Exception _error;

    async Task Establish()
    {
        _retirement = _stateStorage.State.WebhookRetirement.Id;
        _intent = Guid.NewGuid();
        await _observer.AcceptWebhookRegistration(_intent, WebhookDeliveryAttempt.Fingerprint(_webhookDefinition));
        _eventStoreStorage.Webhooks.Get(_webhookDefinition.Identifier).Returns(Task.FromException<WebhookDefinition>(new InvalidOperationException("Definition storage unavailable")));
    }

    async Task Because()
    {
        _error = await Catch.Exception(_observer.ReconcileWebhookRegistration);
        await Crash();
    }

    [Fact] void should_surface_the_failure() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_keep_the_terminal_level() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Retired);
    [Fact] void should_keep_the_same_incident() => _stateStorage.State.WebhookRetirement.Id.ShouldEqual(_retirement);
    [Fact] void should_keep_the_accepted_intent_for_retry() => _stateStorage.State.WebhookRegistrationId.ShouldEqual(_intent);
    [Fact] void should_not_reset_the_failure_budget() => _failedPartitionsState.Partitions.Single().AttemptsInCurrentBudget.ShouldEqual(1);
}
