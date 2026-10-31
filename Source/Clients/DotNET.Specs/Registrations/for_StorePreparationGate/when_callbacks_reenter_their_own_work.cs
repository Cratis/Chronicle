// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_StorePreparationGate;

public class when_callbacks_reenter_their_own_work : Specification
{
    Exception _registrationReentry;
    Exception _discoveryReentry;

    async Task Because()
    {
        var preparing = new StorePreparationGate();
        await preparing.Discover(async () => _registrationReentry = await Catch.Exception(() => preparing.Register(() => Task.CompletedTask, () => Task.CompletedTask)))
            .WaitAsync(TimeSpan.FromSeconds(10));
        var registering = new StorePreparationGate();
        await registering.Register(() => Task.CompletedTask, async () => _discoveryReentry = await Catch.Exception(() => registering.Discover(() => Task.CompletedTask)))
            .WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_reject_registration_from_a_preparation_callback() => _registrationReentry.ShouldBeOfExactType<ArtifactPreparationReentered>();
    [Fact] void should_reject_discovery_from_a_registration_payload_callback() => _discoveryReentry.ShouldBeOfExactType<ArtifactPreparationReentered>();
}
