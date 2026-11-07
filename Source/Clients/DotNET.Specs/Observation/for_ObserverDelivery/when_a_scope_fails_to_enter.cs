// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverDelivery;

public class when_a_scope_fails_to_enter : given.a_delivery_context
{
    Exception _exception;

    void Because()
    {
        var failing = Substitute.For<IObserverDeliveryScope>();
        failing.Enter(Arg.Any<ObserverDeliveryContext>()).Returns(_ => throw new InvalidOperationException("refused"));
        _exception = Catch.Exception(() => ObserverDeliveryScope.Enter(ProviderOf(RecordingScope("a"), failing, RecordingScope("c")), _context));
    }

    [Fact] void should_surface_the_original_exception() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_leave_the_scopes_already_entered() => _log.ShouldEqual(["enter:a", "leave:a"]);
    [Fact] void should_not_leave_a_context_behind() => ObserverDeliveryContext.Current.ShouldBeNull();
}
