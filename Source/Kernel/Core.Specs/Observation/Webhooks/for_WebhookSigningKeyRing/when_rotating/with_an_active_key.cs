// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.when_rotating;

public class with_an_active_key : given.a_signing_key_ring
{
    WebhookSigningKey _result;

    async Task Establish() => await CreateRing(Key("active", WebhookSigningKeyStatus.Active));

    async Task Because() => _result = await _ring.Rotate(TimeSpan.FromHours(2));

    [Fact] void should_append_superseded_then_generated() => _appended.Select(_ => _.Event.GetType()).ShouldEqual([typeof(WebhookSigningKeySuperseded), typeof(WebhookSigningKeyGenerated)]);
    [Fact] void should_supersede_the_old_key_until_the_end_of_the_overlap() => _appended[0].Event.ShouldEqual(new WebhookSigningKeySuperseded("active", Now.AddHours(2)));
    [Fact] void should_keep_the_old_key_in_overlap() => (State.Keys.Single(_ => _.KeyId.Value == "active").Status, State.Keys.Single(_ => _.KeyId.Value == "active").OverlapEndsAt).ShouldEqual((WebhookSigningKeyStatus.Overlap, (DateTimeOffset?)Now.AddHours(2)));
    [Fact] void should_return_a_new_active_key() => (_result.Status, _result.KeyId.Value).ShouldEqual((WebhookSigningKeyStatus.Active, _appended[1].EventSource.Value));
    [Fact] void should_have_a_different_key_than_before() => _result.KeyId.Value.ShouldNotEqual("active");
}
