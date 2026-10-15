// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_redelivering;

public class with_a_throwing_handler : Specification
{
    ReactorScenario<SynchronouslyThrowingReactor> _scenario;
    RecordedOccurrence _occurrence;
    Exception? _exception;

    async Task Establish()
    {
        _scenario = new();
        _occurrence = await _scenario.Given.ForEventSource("reservation").History(new ReservationMade("Ada"));
    }

    async Task Because() => _exception = await Catch.Exception(() => _scenario.Redeliver(_occurrence));

    void Destroy() => _scenario.Dispose();

    [Fact] void should_surface_the_handler_exception() => _exception.ShouldBeOfExactType<ReservationNotYetVisible>();
}
