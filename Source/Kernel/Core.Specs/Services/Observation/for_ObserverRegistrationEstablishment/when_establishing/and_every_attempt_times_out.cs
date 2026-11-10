// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ObserverRegistrationEstablishment.when_establishing;

public class and_every_attempt_times_out : Specification
{
    int _calls;
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(() => ObserverRegistrationEstablishment.Establish(
        () =>
        {
            _calls++;
            throw new TimeoutException();
        },
        (_, _) => { },
        () => Task.CompletedTask,
        CancellationToken.None));

    [Fact] void should_stop_after_the_bounded_number_of_attempts() => _calls.ShouldEqual(ObserverRegistrationEstablishment.MaxAttempts);
    [Fact] void should_surface_the_timeout() => _error.ShouldBeOfExactType<TimeoutException>();
}
