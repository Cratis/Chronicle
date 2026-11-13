// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.Observation.for_SiloDepartures;

/// <summary>
/// Without the membership the keepers only have the safety net, so a failure to follow it is retried after a backoff
/// rather than given up on, and no departure is made up in the meantime.
/// </summary>
public class when_following_membership_fails : Specification
{
    readonly TaskCompletionSource _followedAgain = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Observation.given.ManualClock _clock;
    SiloDepartures _departures;
    TimeSpan _retryAfter;

    void Establish()
    {
        _clock = new();
        var membership = Substitute.For<IClusterMembershipService>();
        membership.MembershipUpdates.Returns(
            _ => throw new InvalidOperationException("Membership is not available"),
            _ =>
            {
                _followedAgain.TrySetResult();
                return Channel.CreateUnbounded<ClusterMembershipSnapshot>().Reader.ReadAllAsync();
            });
        _departures = new(membership, NullLogger<SiloDepartures>.Instance, _clock);
    }

    async Task Because()
    {
        _retryAfter = await _clock.Elapse();
        await Observation.given.ManualClock.Within(_followedAgain.Task);
    }

    void Destroy() => _departures.Dispose();

    [Fact] void should_retry_after_the_first_backoff() => _retryAfter.ShouldEqual(SiloDepartures.FirstRetry);
    [Fact] void should_not_tell_that_a_silo_left() => _departures.Next.IsCompleted.ShouldBeFalse();
}
