// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_RotateWebhookSigningKey.when_rotating;

public class with_the_default_overlap : given.a_key_rotation
{
    Task Because() => _command.Handle(_grainFactory, _storage);

    [Fact] void should_use_the_default_overlap() => _overlap.ShouldEqual(WebhookSigningKeyOverlap.Default);
    [Fact] void should_return_no_key_material() => typeof(RotateWebhookSigningKey).GetMethod(nameof(RotateWebhookSigningKey.Handle))!.ReturnType.ShouldEqual(typeof(Task));
    [Fact] void should_rotate_only_the_requested_store() => _grainFactory.Received(1).GetGrain<IWebhookSigningKeyRing>("store");
}
