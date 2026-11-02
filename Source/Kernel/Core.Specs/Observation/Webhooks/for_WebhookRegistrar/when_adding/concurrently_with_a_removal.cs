// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class concurrently_with_a_removal : given.a_pinned_registration
{
    void Establish()
    {
        ALogWithLastLifecycleEvent(Cratis.Monads.Option<AppendedEvent>.None(), ALifecycleEvent<WebhookRemoved>(7));
        TheScopedAppendFailsWithAConcurrencyViolation();
    }

    async Task Because() => _error = await Catch.Exception(() => _registrar.Add("store", [_definition]));

    [Fact] void should_fail_the_registration() => _error.ShouldBeOfExactType<WebhookRegistrationFailed>();
    [Fact] void should_not_attempt_a_second_append() => ScopedAppends.Count().ShouldEqual(1);
}
