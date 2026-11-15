// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationEnrollmentRateLimiter;

public class when_too_many_requests_are_being_served : Specification
{
    FederationEnrollmentRateLimiter _limiter;
    IDisposable? _first;
    IDisposable? _second;
    IDisposable? _overTheCap;
    IDisposable? _afterOneFinished;

    void Establish() => _limiter = new(TimeProvider.System);

    void Because()
    {
        _first = _limiter.TryEnter(2);
        _second = _limiter.TryEnter(2);
        _overTheCap = _limiter.TryEnter(2);
        _first!.Dispose();
        _first.Dispose();
        _afterOneFinished = _limiter.TryEnter(2);
    }

    [Fact] void should_serve_up_to_the_cap() => (_first is not null && _second is not null).ShouldBeTrue();
    [Fact] void should_refuse_beyond_the_cap() => _overTheCap.ShouldBeNull();
    [Fact] void should_serve_again_when_one_has_finished() => _afterOneFinished.ShouldNotBeNull();
}
