// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class while_the_read_model_lags_behind_an_identical_webhook : given.a_lagging_legacy_registration
{
    /// <summary>Source equivalence: base 130c50244a WebhookRegistrar.cs:85-109 and :421-428 unconditionally append on New.</summary>
    /// <returns>Awaitable task.</returns>
    async Task Because() => _error = await Catch.Exception(() => _registrar.Add("store", [_definition]));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_append_the_registration_and_authorization_again() => _appended.Select(_ => _.GetType()).ShouldEqual([typeof(WebhookAdded), typeof(BearerTokenAuthorizationSetForWebhook)]);
    [Fact] void should_not_fence_the_append() => ScopedAppends.ShouldBeEmpty();
}
