// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_delivering;

public class and_signing_fails : given.a_signed_delivery
{
    void Establish() => _signer.Sign(Arg.Any<EventStoreName>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
        .Returns(_ => Task.FromException<WebhookDeliverySignature>(new InvalidOperationException(Convert.ToBase64String(_seed))));

    async Task Because() => _result = await Deliver(_identity);

    [Fact] void should_fail_closed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_send_any_request() => _receiver.Bodies.ShouldBeEmpty();
    [Fact] void should_return_only_a_sanitized_error() => (_result.Error?.ToString().Contains(Convert.ToBase64String(_seed), StringComparison.Ordinal) ?? false).ShouldBeFalse();
    [Fact] void should_not_log_the_seed() => _logs.Entries.All(entry => !entry.Text.Contains(Convert.ToBase64String(_seed), StringComparison.Ordinal)).ShouldBeTrue();
}
