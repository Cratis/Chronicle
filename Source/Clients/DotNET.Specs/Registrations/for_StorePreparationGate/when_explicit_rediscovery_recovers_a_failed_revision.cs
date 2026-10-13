// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_StorePreparationGate;

public class when_explicit_rediscovery_recovers_a_failed_revision : Specification
{
    StorePreparationGate _gate;
    int _registered;

    async Task Establish()
    {
        _gate = new();
        await Catch.Exception(() => _gate.Discover(() => throw new Exception("first revision failed")));
    }

    async Task Because()
    {
        await _gate.Discover(() => Task.CompletedTask);
        await _gate.Register(() => throw new Exception("A completed successful revision must be reused."), () =>
        {
            _registered++;
            return Task.CompletedTask;
        });
    }

    [Fact] void should_register_against_the_new_successful_revision() => _registered.ShouldEqual(1);
    [Fact] void should_publish_the_successful_verdict() => _gate.LatestVerdict.IsCompletedSuccessfully.ShouldBeTrue();
}
