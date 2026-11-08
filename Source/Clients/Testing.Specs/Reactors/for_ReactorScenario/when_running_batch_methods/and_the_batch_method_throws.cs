// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_running_batch_methods;

public class and_the_batch_method_throws : Specification
{
    ReactorScenario<ThrowingSeatReactor> _scenario;
    Exception _exception;

    void Establish() => _scenario = new();

    async Task Because() => _exception = await Catch.Exception(() => _scenario.Given.ForEventSource(EventSourceId.New()).Events(new SeatReleased("1A")));

    void Destroy() => _scenario.Dispose();

    [Fact] void should_surface_what_the_batch_method_threw() => _exception.ShouldBeOfExactType<SeatUnavailable>();
}
