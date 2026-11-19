// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_RotateWebhookSigningKey.when_rotating;

public class with_an_invalid_overlap : given.a_key_rotation
{
    void Establish() => _command = new("store", double.NaN);
    async Task Because() => _error = await Catch.Exception(() => _command.Handle(_grainFactory));

    [Fact] void should_fail_closed_even_without_validation() => _error.ShouldBeOfExactType<WebhookSigningUnavailable>();
    [Fact] void should_not_touch_the_ring() => _overlap.ShouldBeNull();
}
