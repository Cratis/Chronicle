// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class concurrently_with_a_removal : given.a_lagging_legacy_registration
{
    void Establish() => TheScopedAppendFailsWithAConcurrencyViolation();

    /// <summary>Source equivalence: base 130c50244a WebhookRegistrar.cs:85-109 and :421-428 do not fence a legacy registration.</summary>
    /// <returns>Awaitable task.</returns>
    async Task Because() => _error = await Catch.Exception(() => _registrar.Add("store", [_definition]));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_append_the_registration_and_authorization() => AppendCount.ShouldEqual(2);
    [Fact] void should_not_use_the_failing_scoped_append() => ScopedAppends.ShouldBeEmpty();
}
