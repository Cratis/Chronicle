// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Concepts.Observation.Webhooks.for_WebhookAuthorization;

public class when_adding_managed_identity : Specification
{
    WebhookAuthorization _authorization;

    void Because() => _authorization = WebhookAuthorization.FromT4(new("client", "api://resource/.default"));

    /// <summary>
    /// OneOfSerializer writes the index, so None stays at T3 for rolling upgrades.
    /// </summary>
    [Fact] void should_keep_none_at_index_three() => WebhookAuthorization.None.IsT3.ShouldBeTrue();
    [Fact] void should_append_managed_identity_at_index_four() => _authorization.IsT4.ShouldBeTrue();
    [Fact] void should_preserve_basic_at_index_zero() => ((WebhookAuthorization)new BasicAuthorization("user", "password")).IsT0.ShouldBeTrue();
    [Fact] void should_preserve_bearer_at_index_one() => ((WebhookAuthorization)new BearerTokenAuthorization("token")).IsT1.ShouldBeTrue();
    [Fact] void should_preserve_oauth_at_index_two() => ((WebhookAuthorization)new OAuthAuthorization("authority", "client", "secret")).IsT2.ShouldBeTrue();
}
