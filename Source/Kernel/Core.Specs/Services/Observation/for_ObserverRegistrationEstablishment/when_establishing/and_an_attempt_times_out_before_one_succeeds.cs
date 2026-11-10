// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ObserverRegistrationEstablishment.when_establishing;

public class and_an_attempt_times_out_before_one_succeeds : Specification
{
    readonly List<int> _timedOutAttempts = [];
    int _calls;
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(() => ObserverRegistrationEstablishment.Establish(
        () => ++_calls < 3 ? throw new TimeoutException() : Task.CompletedTask,
        (attempt, _) => _timedOutAttempts.Add(attempt),
        CancellationToken.None));

    [Fact] void should_ask_again_on_the_same_stream() => _calls.ShouldEqual(3);
    [Fact] void should_report_each_timed_out_attempt() => _timedOutAttempts.ShouldContainOnly(1, 2);
    [Fact] void should_succeed() => _error.ShouldBeNull();
}
