// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using OneOf.Types;

namespace Cratis.Chronicle.Observation.Jobs.for_TransientTimeouts;

public class when_a_handling_attempt_times_out_twice : Specification
{
    int _attempts;
    Catch<int, None> _result;

    async Task Because() => _result = await TransientTimeouts.RetryOnceOnTimeout<int>(
        () =>
        {
            _attempts++;
            return Task.FromResult((Catch<int, None>)new TimeoutException("slow"));
        },
        TimeSpan.Zero,
        _ => { },
        CancellationToken.None);

    [Fact] void should_give_up_after_two_attempts() => _attempts.ShouldEqual(2);
    [Fact] void should_return_the_timeout_so_the_failure_is_classified_as_a_timeout() => _result.TryGetException(out _).ShouldBeTrue();
}
