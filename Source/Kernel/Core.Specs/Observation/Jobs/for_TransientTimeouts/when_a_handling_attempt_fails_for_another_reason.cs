// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using OneOf.Types;

namespace Cratis.Chronicle.Observation.Jobs.for_TransientTimeouts;

public class when_a_handling_attempt_fails_for_another_reason : Specification
{
    int _attempts;
    Catch<int, None> _result;

    async Task Because() => _result = await TransientTimeouts.RetryOnceOnTimeout<int>(
        () =>
        {
            _attempts++;
            return Task.FromResult((Catch<int, None>)new InvalidOperationException("boom"));
        },
        TimeSpan.Zero,
        _ => { },
        CancellationToken.None);

    [Fact] void should_attempt_once() => _attempts.ShouldEqual(1);
    [Fact] void should_return_the_failure() => _result.TryGetException(out _).ShouldBeTrue();
}
