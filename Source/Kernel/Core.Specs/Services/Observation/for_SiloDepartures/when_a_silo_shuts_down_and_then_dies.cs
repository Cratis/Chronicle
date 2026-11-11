// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_SiloDepartures;

/// <summary>
/// A silo is signalled when it starts shutting down and again when it is declared dead, which is when the surviving
/// silos deactivate the activations whose directory entries it owned. Moving on to stopping signals nothing new.
/// </summary>
public class when_a_silo_shuts_down_and_then_dies : given.two_silos
{
    Task _shuttingDown;
    Task _stopping;
    Task _dead;
    bool _stoppingSignalled;

    void Because()
    {
        _shuttingDown = _departures.Next;
        Publish((_first, SiloStatus.Active), (_second, SiloStatus.ShuttingDown));
        _stopping = _departures.Next;
        Publish((_first, SiloStatus.Active), (_second, SiloStatus.Stopping));
        _stoppingSignalled = _stopping.IsCompleted;
        _dead = _departures.Next;
        Publish((_first, SiloStatus.Active), (_second, SiloStatus.Dead));
    }

    [Fact] void should_tell_when_it_starts_shutting_down() => _shuttingDown.IsCompleted.ShouldBeTrue();
    [Fact] void should_not_tell_again_when_it_is_stopping() => _stoppingSignalled.ShouldBeFalse();
    [Fact] void should_tell_again_when_it_is_dead() => _dead.IsCompleted.ShouldBeTrue();
}
