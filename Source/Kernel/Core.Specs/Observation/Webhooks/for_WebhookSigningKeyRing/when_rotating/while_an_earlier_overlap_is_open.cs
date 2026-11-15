// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.when_rotating;

public class while_an_earlier_overlap_is_open : given.a_signing_key_ring
{
    async Task Establish() => await CreateRing(Key("active", WebhookSigningKeyStatus.Active), Key("earlier", WebhookSigningKeyStatus.Overlap, Now.AddHours(1)));

    async Task Because() => await _ring.Rotate(TimeSpan.FromHours(5));

    [Fact] void should_keep_the_end_time_of_the_earlier_overlap() => State.Keys.Single(_ => _.KeyId.Value == "earlier").OverlapEndsAt.ShouldEqual(Now.AddHours(1));
    [Fact] void should_keep_the_earlier_key_in_overlap() => State.Keys.Single(_ => _.KeyId.Value == "earlier").Status.ShouldEqual(WebhookSigningKeyStatus.Overlap);
}
