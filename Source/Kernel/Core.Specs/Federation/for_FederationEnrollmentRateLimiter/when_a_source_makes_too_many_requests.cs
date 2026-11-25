// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Federation.for_FederationEnrollmentRateLimiter;

public class when_a_source_makes_too_many_requests : Specification
{
    given.ControllableTimeProvider _time;
    FederationEnrollmentRateLimiter _limiter;
    bool[] _first;
    bool _overTheLimit;
    bool _otherSource;
    bool _otherHostInTheSameV6Prefix;
    bool _afterTheWindow;

    void Establish()
    {
        _time = new();
        _limiter = new(_time);
    }

    void Because()
    {
        var source = IPAddress.Parse("203.0.113.7");
        _first = [.. Enumerable.Range(0, 3).Select(_ => _limiter.TryAcquire(source, 3))];
        _overTheLimit = _limiter.TryAcquire(source, 3);
        _otherSource = _limiter.TryAcquire(IPAddress.Parse("203.0.113.8"), 3);

        var v6 = IPAddress.Parse("2001:db8:1:2::1");
        for (var i = 0; i < 3; i++)
        {
            _limiter.TryAcquire(v6, 3);
        }

        _otherHostInTheSameV6Prefix = _limiter.TryAcquire(IPAddress.Parse("2001:db8:1:2:ffff::9"), 3);

        _time.Advance(TimeSpan.FromMinutes(1));
        _afterTheWindow = _limiter.TryAcquire(source, 3);
    }

    [Fact] void should_admit_requests_up_to_the_limit() => _first.All(_ => _).ShouldBeTrue();
    [Fact] void should_refuse_requests_over_the_limit() => _overTheLimit.ShouldBeFalse();
    [Fact] void should_not_affect_another_source() => _otherSource.ShouldBeTrue();
    [Fact] void should_treat_one_ipv6_prefix_as_one_source() => _otherHostInTheSameV6Prefix.ShouldBeFalse();
    [Fact] void should_admit_again_in_the_next_window() => _afterTheWindow.ShouldBeTrue();
}
