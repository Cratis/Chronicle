// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ObserverRegistrationEstablishment.when_establishing;

public class and_an_attempt_fails_for_another_reason : Specification
{
    readonly InvalidOperationException _failure = new("not a timeout");
    int _calls;
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(() => ObserverRegistrationEstablishment.Establish(
        () =>
        {
            _calls++;
            throw _failure;
        },
        (_, _) => { },
        CancellationToken.None));

    [Fact] void should_not_ask_again() => _calls.ShouldEqual(1);
    [Fact] void should_surface_the_failure() => _error.ShouldEqual(_failure);
}
