// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ObserverRegistrationEstablishment.when_establishing;

public class and_the_stream_is_cancelled_when_an_attempt_times_out : Specification
{
    readonly CancellationTokenSource _cancellation = new();
    int _calls;
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(() => ObserverRegistrationEstablishment.Establish(
        async () =>
        {
            _calls++;
            await _cancellation.CancelAsync();
            throw new TimeoutException();
        },
        (_, _) => { },
        () => Task.CompletedTask,
        _cancellation.Token));

    void Destroy() => _cancellation.Dispose();

    [Fact] void should_not_ask_again() => _calls.ShouldEqual(1);
    [Fact] void should_surface_the_timeout() => _error.ShouldBeOfExactType<TimeoutException>();
}
