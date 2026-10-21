// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class with_oauth_scope : given.an_oauth_registration
{
    async Task Because() => await _registrar.Add("store", [_definition]);

    [Fact] void should_append_both_facts_atomically() => _batch.Length.ShouldEqual(2);
    [Fact] void should_append_authorization_first() => _batch[0].EventType.ShouldEqual(typeof(OAuthAuthorizationSetForWebhook).GetEventType());
    [Fact] void should_append_scope_second() => _batch[1].EventType.ShouldEqual(typeof(OAuthScopeSetForWebhook).GetEventType());
    [Fact] void should_keep_secrets_encrypted() => _batch[0].Content["clientSecret"]!.GetValue<string>().ShouldEqual("encrypted-secret");
    [Fact] void should_persist_scope() => _batch[1].Content["scope"]!.GetValue<string>().ShouldEqual("api://resource/.default");
}
