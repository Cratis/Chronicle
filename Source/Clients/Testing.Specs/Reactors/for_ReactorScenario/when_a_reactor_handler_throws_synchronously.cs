// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario;

public class when_a_reactor_handler_throws_synchronously : Specification
{
    ReactorScenario<given.SynchronouslyThrowingReactor> _scenario;
    Defaults _defaults;
    Exception? _exception;

    void Establish()
    {
        _defaults = new(new InMemoryClientArtifactsProvider { EventTypes = [typeof(ReservationMade)] });
        _scenario = new(_defaults);
    }

    async Task Because() =>
        _exception = await Catch.Exception(() => _scenario.Given.ForEventSource(EventSourceId.New()).Events(new ReservationMade("Ada")));

    void Destroy()
    {
        _scenario.Dispose();
        ((IDisposable)_defaults.EventStore).Dispose();
    }

    [Fact] void should_surface_the_handlers_exception_without_a_reflection_wrapper() => _exception.ShouldBeOfExactType<ReservationNotYetVisible>();
    [Fact] void should_preserve_the_handler_in_the_stack_trace() => _exception!.StackTrace!.Contains(nameof(given.SynchronouslyThrowingReactor.Handle), StringComparison.Ordinal).ShouldBeTrue();
}
