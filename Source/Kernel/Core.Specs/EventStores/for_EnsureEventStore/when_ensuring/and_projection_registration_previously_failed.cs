// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.EventStores.for_EnsureEventStore.when_ensuring;

public class and_projection_registration_previously_failed : given.an_ensure_command
{
    Exception _failure;

    async Task Establish()
    {
        _storage.HasEventStore(_name).Returns(true);
        _projections.DiscoverAndRegister(_name).ThrowsAsync(new Exception("Registration failed"));
        _failure = await Catch.Exception(Ensure);
        _projections.DiscoverAndRegister(_name).Returns(Task.CompletedTask);
    }

    Task Because() => Ensure();

    [Fact] void should_surface_the_first_failure() => _failure.ShouldNotBeNull();
    [Fact] async Task should_retry_projection_registration() => await _projections.Received(2).DiscoverAndRegister(_name);
}
