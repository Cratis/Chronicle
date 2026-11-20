// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_delivering;

public class with_cancellation_during_signing : given.a_signed_delivery
{
    void Establish() => _signer.Sign(Arg.Any<EventStoreName>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
        .Returns(_ => Task.FromException<WebhookDeliverySignature>(new OperationCanceledException()));

    async Task Because() => _result = await Deliver(_identity);

    [Fact] void should_keep_the_existing_cancellation_failure_mapping() => _result.Error.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] void should_not_deliver() => _receiver.Bodies.ShouldBeEmpty();
}
