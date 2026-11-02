// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class after_the_webhook_was_removed : given.a_pinned_registration
{
    void Establish() => ALogWithLastLifecycleEvent(ALifecycleEvent<WebhookRemoved>(7));

    /// <summary>Source equivalence: base 130c50244a WebhookRegistrar.cs:85-109 and :421-428 append an unfenced New registration.</summary>
    /// <returns>Awaitable task.</returns>
    async Task Because() => _error = await Catch.Exception(() => _registrar.Add("store", [_definition]));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_append_the_registration() => _appended.OfType<WebhookAdded>().Count().ShouldEqual(1);
    [Fact] void should_not_fence_the_append() => ScopedAppends.ShouldBeEmpty();
}
