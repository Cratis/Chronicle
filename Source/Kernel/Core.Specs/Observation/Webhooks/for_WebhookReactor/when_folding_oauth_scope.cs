// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookReactor;

public class when_folding_oauth_scope : given.a_webhook_reactor
{
    WebhookDefinition _definition;
    readonly List<string> _scopes = [];

    void Establish()
    {
        _definition = new("webhook", WebhookOwner.Client, "log", [], new("https://target.example", WebhookAuthorization.None, new Dictionary<string, string>()));
        _webhooksManager.GetWebhookDefinitions().Returns(_ => [_definition]);
        _webhooksManager.SetAuthorization(Arg.Any<WebhookId>(), Arg.Any<WebhookAuthorization>()).Returns(call =>
        {
            var authorization = call.Arg<WebhookAuthorization>();
            _definition = _definition with { Target = _definition.Target with { Authorization = authorization } };
            _scopes.Add(authorization.AsT2.Scope);
            return Task.CompletedTask;
        });
    }

    async Task Because()
    {
        var context = EventContext.EmptyWithEventSourceId("webhook") with { EventStore = "store" };
        await _reactor.OAuthAuthorizationSet(new("https://authority.example", "client", "secret"), context);
        await _reactor.OAuthScopeSet(new("api://resource/.default"), context);
        await _reactor.OAuthAuthorizationSet(new("https://authority.example", "client", "new-secret"), context);
    }

    [Fact] void should_default_old_authorization_to_empty_scope() => _scopes[0].ShouldEqual(string.Empty);
    [Fact] void should_apply_the_separate_scope_fact() => _scopes[1].ShouldEqual("api://resource/.default");
    [Fact] void should_clear_scope_when_authorization_is_reset() => _scopes[2].ShouldEqual(string.Empty);
}
