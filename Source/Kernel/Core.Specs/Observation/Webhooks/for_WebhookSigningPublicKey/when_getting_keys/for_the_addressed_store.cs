// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSigningPublicKey.when_getting_keys;

public class for_the_addressed_store : given.a_public_key_query
{
    IWebhookSigningKeyRing _otherRing;

    void Establish()
    {
        _otherRing = Substitute.For<IWebhookSigningKeyRing>();
        _grainFactory.GetGrain<IWebhookSigningKeyRing>("other").Returns(_otherRing);
    }

    Task Because() => Query();

    [Fact] void should_return_the_addressed_stores_key() => _result.Single().KeyId.ShouldEqual("active");
    [Fact] void should_read_only_the_addressed_ring() => _ring.Received(1).GetSigningKeys();
    [Fact] void should_not_read_another_store() => _otherRing.DidNotReceive().GetSigningKeys();
    [Fact] void should_not_add_a_query_policy() => typeof(WebhookSigningPublicKey).GetCustomAttribute<AuthorizeAttribute>().ShouldBeNull();
    [Fact] void should_not_add_a_method_policy() => typeof(WebhookSigningPublicKey).GetMethod(nameof(WebhookSigningPublicKey.GetWebhookSigningKeys))!.GetCustomAttribute<AuthorizeAttribute>().ShouldBeNull();
    [Fact] void should_expose_only_public_key_metadata() => typeof(WebhookSigningPublicKey).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(_ => _.Name).Order().ShouldEqual(new[] { "Algorithm", "CreatedAt", "KeyId", "OverlapEndsAt", "PublicKey", "Status" });
    [Fact] void should_not_expose_public_fields() => typeof(WebhookSigningPublicKey).GetFields(BindingFlags.Public | BindingFlags.Instance).ShouldBeEmpty();
}
