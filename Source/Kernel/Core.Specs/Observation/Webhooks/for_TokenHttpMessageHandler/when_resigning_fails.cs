// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_TokenHttpMessageHandler;

public class when_resigning_fails : given.a_signed_token_delivery
{
    void Establish()
    {
        var calls = 0;
        _deliverySigner = Substitute.For<IWebhookDeliverySigner>();
        _deliverySigner.Sign(Arg.Any<EventStoreName>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(call => ++calls == 1
                ? _signer.Sign(call.Arg<EventStoreName>(), call.Arg<string>(), call.Arg<long>(), call.Arg<ReadOnlyMemory<byte>>(), call.Arg<CancellationToken>())
                : Task.FromException<WebhookDeliverySignature>(new InvalidOperationException("private-material-must-not-escape")));
    }

    Task Because() => Deliver();

    [Fact] void should_not_send_a_stale_signature() => _receiver.Bodies.Count.ShouldEqual(1);
    [Fact] void should_fail_closed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_sanitize_the_error() => _result.Error.ShouldBeOfExactType<WebhookSigningUnavailable>();
    [Fact] void should_not_retain_the_cause() => _result.Error!.InnerException.ShouldBeNull();
}
