// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookDeliverySigner.when_signing;

public class with_cancellation : given.a_delivery_signer
{
    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _error = await Catch.Exception(() => _signer.Sign("store", "message", 1000, Body, cancellation.Token));
    }

    [Fact] void should_preserve_cancellation() => _error.ShouldBeOfExactType<OperationCanceledException>();
}
