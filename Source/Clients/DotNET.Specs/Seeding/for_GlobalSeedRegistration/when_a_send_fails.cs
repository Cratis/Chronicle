// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_GlobalSeedRegistration;

public class when_a_send_fails : Specification
{
    GlobalSeedRegistration _registration;
    Exception _failure;
    bool _includeGlobals;

    void Establish() => _registration = new();

    async Task Because()
    {
        _failure = await Catch.Exception(() => _registration.Run("store", _ => Task.FromException<bool>(new Exception("send failed"))));
        await _registration.Run("store", includeGlobals =>
        {
            _includeGlobals = includeGlobals;
            return Task.FromResult(true);
        });
    }

    [Fact] void should_propagate_the_failure() => _failure.ShouldNotBeNull();
    [Fact] void should_offer_globals_again_on_retry() => _includeGlobals.ShouldBeTrue();
}
