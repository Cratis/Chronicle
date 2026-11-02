// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ContractWebhookDefinition = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDefinition;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class with_changed_legacy_values_while_the_read_model_lags : given.a_lagging_legacy_registration
{
    ContractWebhookDefinition _second;

    void Establish()
    {
        _second = new()
        {
            Identifier = _definition.Identifier,
            Target = new()
            {
                Url = "https://example.com/second",
                Headers = new Dictionary<string, string> { ["version"] = "second" },
                Authorization = new(new Contracts.Security.BearerTokenAuthorization { Token = "second" })
            }
        };
    }

    /// <summary>Source equivalence: base 130c50244a WebhookRegistrar.cs:85-109 and :421-428 append each New payload, last write wins.</summary>
    /// <returns>Awaitable task.</returns>
    async Task Because() => _error = await Catch.Exception(() => _registrar.Add("store", [_definition, _second]));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_append_both_new_payloads() => _appended.OfType<WebhookAdded>().Count().ShouldEqual(2);
    [Fact] void should_keep_the_latest_url() => _appended.OfType<WebhookAdded>().Last().TargetUrl.ShouldEqual(_second.ToChronicle().Target.Url);
    [Fact] void should_keep_the_latest_headers() => _appended.OfType<WebhookAdded>().Last().TargetHeaders["version"].ShouldEqual("second");
    [Fact] void should_append_both_authorizations() => _appended.OfType<BearerTokenAuthorizationSetForWebhook>().Select(_ => _.Token.Value).ShouldEqual(["encrypted-first", "encrypted-second"]);
    [Fact] void should_not_fence_the_writes() => ScopedAppends.ShouldBeEmpty();
}
