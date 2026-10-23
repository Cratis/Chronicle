// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Catch = Cratis.Monads.Catch;

namespace Cratis.Chronicle.Observation.Webhooks.for_AddWebhookValidator.given;

public class a_managed_identity_validator : for_WebhookRegistrar.given.a_webhook_registrar
{
    protected IManagedIdentityTokens _tokens;
    protected AddWebhookValidator _validator;
    protected AddWebhook _command;

    void Establish()
    {
        var policy = Substitute.For<IWebhookTargetPolicy>();
        policy.Evaluate(Arg.Any<string>(), Arg.Any<EventStoreName?>(), Arg.Any<CancellationToken>()).Returns(new WebhookTargetValidation(true, ""));
        _webhookMediator.OnNext(Arg.Any<EventStoreName?>(), Arg.Any<Concepts.Observation.Webhooks.WebhookTarget>(), Arg.Any<Concepts.Keys.Key>(), Arg.Any<IEnumerable<Concepts.Events.AppendedEvent>>(), Arg.Any<string?>(), Arg.Any<TimeSpan?>()).Returns(Catch.Success());
        _tokens = Substitute.For<IManagedIdentityTokens>();
        _tokens.AcquireToken(Arg.Any<Concepts.Security.ManagedIdentityAuthorization>(), Arg.Any<CancellationToken>()).Returns(new AccessTokenInfo("token", DateTimeOffset.MaxValue));
        _validator = new AddWebhookValidator(_registrar, _storage, policy, _tokens);
        _command = new AddWebhook("orders", "receiver", "https://receiver.example", "event-log", [], AuthorizationType.ManagedIdentity, "", "", "", "", "", "", new Dictionary<string, string>(), true, true)
        {
            ManagedIdentityScope = "api://receiver/.default"
        };
    }
}
