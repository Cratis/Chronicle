// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_EventStoreInitialization.when_ensuring;

public class and_a_previous_attempt_failed : given.an_event_store_initialization
{
    Exception _error;
    int _attempts;

    void Establish() => _projections.DiscoverAndRegister(_name).Returns(_ =>
        ++_attempts == 1 ? Task.FromException(new EventStoreInitializationFailed(_name)) : Task.CompletedTask);

    async Task Because()
    {
        _error = await Catch.Exception(() => _initialization.Ensure(_name));
        await _initialization.Ensure(_name);
    }

    [Fact] void should_surface_the_first_failure() => _error.ShouldBeOfExactType<EventStoreInitializationFailed>();
    [Fact] void should_retry_the_initializer() => _initializerReference.Received(2).Ensure();
    [Fact] void should_retry_projection_registration() => _projections.Received(2).DiscoverAndRegister(_name);
    [Fact] void should_mark_the_store_initialized() => _initialized.ShouldBeTrue();
}
