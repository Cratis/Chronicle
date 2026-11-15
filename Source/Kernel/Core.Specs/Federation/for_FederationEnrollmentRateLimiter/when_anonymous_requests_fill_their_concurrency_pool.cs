// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationEnrollmentRateLimiter;

public class when_anonymous_requests_fill_their_concurrency_pool : Specification
{
    FederationEnrollmentRateLimiter _limiter;
    IDisposable? _anonymousOverTheCap;
    IDisposable? _renewal;
    IDisposable? _renewalOverItsOwnCap;

    void Establish() => _limiter = new(TimeProvider.System);

    void Because()
    {
        using var first = _limiter.TryEnter(1, FederationEnrollmentBudget.Discovery);
        _anonymousOverTheCap = _limiter.TryEnter(1, FederationEnrollmentBudget.Discovery);
        _renewal = _limiter.TryEnter(1, FederationEnrollmentBudget.Renewal);
        _renewalOverItsOwnCap = _limiter.TryEnter(1, FederationEnrollmentBudget.Renewal);
        _renewal?.Dispose();
    }

    [Fact] void should_refuse_the_anonymous_request_over_the_cap() => _anonymousOverTheCap.ShouldBeNull();
    [Fact] void should_still_serve_a_renewal() => _renewal.ShouldNotBeNull();
    [Fact] void should_cap_renewals_by_their_own_pool() => _renewalOverItsOwnCap.ShouldBeNull();
}
