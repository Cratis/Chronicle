// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope.for_WebhookMediator.when_reading_a_result;

public class with_managed_identity_and_receiver_text : given.a_batch_delivery
{
    const string Secret = "sentinel-token-secret";
    WebhookDeliveryResult _result;

    void Establish()
    {
        _request = _request with
        {
            Target = _request.Target with { Authorization = new ManagedIdentityAuthorization("client", "api://receiver/.default") },
            AccessToken = Secret
        };
        _handler.ResponseBody = Result("failed", 42, extra: $",\"exceptionMessages\":[\"{Secret}\"],\"exceptionStackTrace\":\"{Secret}\"");
    }

    async Task Because() => _result = await _mediator.DeliverEnvelope(_request);

    [Fact] void should_not_succeed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_preserve_the_checkpoint() => _result.LastHandled.Value.ShouldEqual(42UL);
    [Fact] void should_carry_the_failure() => _result.Error.ShouldBeOfExactType<DeliveryNotFullyAcknowledged>();
    [Fact] void should_not_retain_receiver_messages() => ((DeliveryNotFullyAcknowledged)_result.Error!).ReceiverMessages.ShouldBeEmpty();
    [Fact] void should_not_retain_the_secret_in_the_message() => _result.Error!.Message.ShouldNotContain(Secret);
    [Fact] void should_not_retain_a_receiver_stack_trace() => (_result.Error!.StackTrace ?? string.Empty).ShouldNotContain(Secret);
    [Fact] void should_not_expose_the_secret_in_exception_text() => _result.Error!.ToString().ShouldNotContain(Secret);
    [Fact] void should_not_retain_an_inner_exception() => _result.Error!.InnerException.ShouldBeNull();
}
