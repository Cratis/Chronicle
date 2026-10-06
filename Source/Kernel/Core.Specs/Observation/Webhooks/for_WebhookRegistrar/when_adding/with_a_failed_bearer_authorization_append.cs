// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.EventSequences;
using ContractWebhookDefinition = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDefinition;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class with_a_failed_bearer_authorization_append : given.a_webhook_registrar
{
    IEventSequence _eventSequence;
    ContractWebhookDefinition _webhook;
    Exception? _exception;

    void Establish()
    {
        _eventSequence = Substitute.For<IEventSequence>();
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_eventSequence);
        var webhooks = Substitute.For<IWebhooks>();
        webhooks.GetWebhookDefinitions().Returns([]);
        _grainFactory.GetGrain<IWebhooks>(Arg.Any<string>()).Returns(webhooks);
        _webhookDefinitionComparer.Compare(Arg.Any<WebhookKey>(), Arg.Any<WebhookDefinition>(), Arg.Any<WebhookDefinition>())
            .Returns(new WebhookDefinitionComparisonResult(WebhookDefinitionCompareResult.New, null));
        _encryption.Encrypt(Arg.Any<string>()).Returns("synthetic-encrypted-token");
        _webhook = new ContractWebhookDefinition
        {
            Identifier = "bearer-webhook",
            Target = new()
            {
                Url = "https://target.invalid/events",
                Authorization = new(new Contracts.Security.BearerTokenAuthorization { Token = "synthetic-token" })
            }
        };
        _eventSequence.Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<Identity>(),
            Arg.Any<IEnumerable<Tag>>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>()).Returns(call => call.ArgAt<object>(1) is BearerTokenAuthorizationSetForWebhook
                ? AppendResult.Failed(CorrelationId.New(), [new AppendError("Missing authorization event schema")])
                : AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First));
    }

    async Task Because() => _exception = await Catch.Exception(async () => await _registrar.Add("non-system-store", [_webhook]));

    [Fact] void should_not_report_success_after_the_authorization_append_failed() => _exception.ShouldNotBeNull();
    [Fact] void should_fail_the_registration() => _exception.ShouldBeOfExactType<WebhookRegistrationFailed>();
    [Fact] void should_keep_the_token_out_of_the_failure() => _exception!.Message.ShouldNotContain("synthetic");
}
