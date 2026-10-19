// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using FluentValidation.Results;
using Catch = Cratis.Monads.Catch;

namespace Cratis.Chronicle.Observation.Webhooks.for_AddWebhookValidator.when_validating;

public class and_policy_rejects_the_target : for_WebhookRegistrar.given.a_webhook_registrar
{
    AddWebhookValidator _validator;
    ValidationResult _result;

    void Establish()
    {
        var policy = Substitute.For<IWebhookTargetPolicy>();
        policy.Evaluate(Arg.Any<string>(), Arg.Any<EventStoreName?>(), Arg.Any<CancellationToken>()).Returns(new WebhookTargetValidation(false, "Webhook target must use HTTPS."));
        _webhookMediator.OnNext(Arg.Any<Concepts.Observation.Webhooks.WebhookTarget>(), Arg.Any<Concepts.Keys.Key>(), Arg.Any<IEnumerable<Concepts.Events.AppendedEvent>>(), Arg.Any<string?>(), Arg.Any<TimeSpan?>()).Returns(Catch.Success());
        _validator = new AddWebhookValidator(_registrar, _storage, policy);
    }

    async Task Because() => _result = await _validator.ValidateAsync(new AddWebhook("orders", "receiver", "http://receiver.example", "event-log", [], AuthorizationType.None, "", "", "", "", "", "", new Dictionary<string, string>(), true, true));

    [Fact] void should_reject_registration() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_not_probe_the_target() => _webhookMediator.DidNotReceive().OnNext(Arg.Any<Concepts.Observation.Webhooks.WebhookTarget>(), Arg.Any<Concepts.Keys.Key>(), Arg.Any<IEnumerable<Concepts.Events.AppendedEvent>>(), Arg.Any<string?>(), Arg.Any<TimeSpan?>());
}
