// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class with_the_same_identifier_twice_and_the_same_delivery_format : given.a_lagging_legacy_registration
{
    /// <summary>Source equivalence: base 130c50244a WebhookRegistrar.cs:85-109 and :421-428 append for each request entry.</summary>
    /// <returns>Awaitable task.</returns>
    async Task Because() => _error = await Catch.Exception(() => _registrar.Add("store", [_definition, _definition]));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_append_both_registrations_with_authorization() => _appended.Select(_ => _.GetType()).ShouldEqual([typeof(WebhookAdded), typeof(BearerTokenAuthorizationSetForWebhook), typeof(WebhookAdded), typeof(BearerTokenAuthorizationSetForWebhook)]);
    [Fact] void should_not_fence_the_appends() => ScopedAppends.ShouldBeEmpty();
}
