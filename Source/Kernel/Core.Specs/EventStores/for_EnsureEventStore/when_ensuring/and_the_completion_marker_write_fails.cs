// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute.ExceptionExtensions;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.EventStores.for_EnsureEventStore.when_ensuring;

public class and_the_completion_marker_write_fails : given.an_ensure_command
{
    Exception _failure;

    async Task Establish()
    {
        _store.Configure().MarkInitialized().ThrowsAsync(new Exception("Marker write failed"));
        _failure = await Catch.Exception(Ensure);
        _store.Configure().MarkInitialized().Returns(_ =>
        {
            _initialized = true;
            return Task.CompletedTask;
        });
    }

    Task Because() => Ensure();

    [Fact] void should_surface_the_failed_write() => _failure.ShouldNotBeNull();
    [Fact] async Task should_retry_setup_without_an_in_memory_success_flag() => await _projections.Received(2).DiscoverAndRegister(_name);
    [Fact] void should_complete_initialization_on_retry() => _initialized.ShouldBeTrue();
}
