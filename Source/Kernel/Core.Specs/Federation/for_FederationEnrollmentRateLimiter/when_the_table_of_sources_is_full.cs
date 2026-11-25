// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Federation.for_FederationEnrollmentRateLimiter;

public class when_the_table_of_sources_is_full : Specification
{
    given.ControllableTimeProvider _time;
    FederationEnrollmentRateLimiter _limiter;
    bool _newSource;
    bool _theOldestSourceAfterEviction;
    bool _renewalWhileDiscoveryIsExhausted;
    bool _discoveryAfterTheBudgetWasUsedUp;

    static IPAddress Source(int index) => new([10, (byte)(index >> 16), (byte)(index >> 8), (byte)index]);

    void Establish()
    {
        _time = new();
        _limiter = new(_time);
    }

    void Because()
    {
        // Ten thousand distinct sources, each opened a little later than the one before it, fill the table.
        for (var i = 1; i <= 10_000; i++)
        {
            _limiter.TryAcquire(Source(i), 5);
            _time.Advance(TimeSpan.FromMilliseconds(1));
        }

        _newSource = _limiter.TryAcquire(IPAddress.Parse("203.0.113.9"), 5);
        _theOldestSourceAfterEviction = _limiter.TryAcquire(Source(1), 5);

        var flooder = IPAddress.Parse("203.0.113.10");
        for (var i = 0; i < 20; i++)
        {
            _limiter.TryAcquire(flooder, 5, FederationEnrollmentBudget.Discovery);
        }

        _discoveryAfterTheBudgetWasUsedUp = _limiter.TryAcquire(flooder, 5, FederationEnrollmentBudget.Discovery);
        _renewalWhileDiscoveryIsExhausted = _limiter.TryAcquire(flooder, 5, FederationEnrollmentBudget.Renewal);
    }

    [Fact] void should_still_admit_a_new_source() => _newSource.ShouldBeTrue();
    [Fact] void should_have_made_room_by_dropping_the_oldest_sources() => _theOldestSourceAfterEviction.ShouldBeTrue();
    [Fact] void should_still_limit_a_source_that_goes_over_its_budget() => _discoveryAfterTheBudgetWasUsedUp.ShouldBeFalse();
    [Fact] void should_keep_the_renewal_budget_apart_from_the_discovery_budget() => _renewalWhileDiscoveryIsExhausted.ShouldBeTrue();
}
