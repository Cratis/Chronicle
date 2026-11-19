// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_RotateWebhookSigningKey.when_rotating;

public class with_zero_overlap : given.a_key_rotation
{
    void Establish() => _command = new("store", true, 0);
    Task Because() => _command.Handle(_grainFactory);

    [Fact] void should_request_immediate_retirement() => _overlap.ShouldEqual(WebhookSigningKeyOverlap.None);
}
