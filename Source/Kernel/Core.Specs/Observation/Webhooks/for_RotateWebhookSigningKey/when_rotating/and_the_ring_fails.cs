// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_RotateWebhookSigningKey.when_rotating;

public class and_the_ring_fails : given.a_key_rotation
{
    void Establish() => _ring.Rotate(Arg.Any<WebhookSigningKeyOverlap>()).Returns(_ => Task.FromException<WebhookSigningKey>(new InvalidOperationException("encrypted-private-material")));
    async Task Because() => _error = await Catch.Exception(() => _command.Handle(_grainFactory));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<WebhookSigningUnavailable>();
    [Fact] void should_not_retain_the_cause() => _error!.InnerException.ShouldBeNull();
    [Fact] void should_not_expose_the_failure_message() => _error!.ToString().Contains("encrypted-private-material", StringComparison.Ordinal).ShouldBeFalse();
}
