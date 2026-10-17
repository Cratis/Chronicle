// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using OneOf.Types;

namespace Cratis.Chronicle.Observation.Jobs.for_TransientTimeouts;

public class when_a_handling_attempt_times_out_once : Specification
{
    int _attempts;
    int _retries;
    Catch<int, None> _result;

    async Task Because() => _result = await TransientTimeouts.RetryOnceOnTimeout<int>(
        () =>
        {
            _attempts++;
            return Task.FromResult(_attempts == 1 ? (Catch<int, None>)new TimeoutException("slow") : 42);
        },
        TimeSpan.Zero,
        _ => _retries++,
        CancellationToken.None);

    [Fact] void should_attempt_twice() => _attempts.ShouldEqual(2);
    [Fact] void should_signal_the_retry_once() => _retries.ShouldEqual(1);
    [Fact] void should_return_the_result_of_the_second_attempt() => _result.TryGetResult(out _).ShouldBeTrue();
    [Fact] void should_not_have_failed() => _result.TryGetException(out _).ShouldBeFalse();
}
