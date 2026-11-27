// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Observation.for_IObserver;

public class when_declaring_the_released_catch_up_acknowledgement : Specification
{
    [Fact] void should_keep_the_interleavable_awaited_acknowledgement() => typeof(IObserver)
        .GetMethod(nameof(IObserver.CaughtUp), [typeof(JobId), typeof(EventSequenceNumber), typeof(EventSequenceNumber)])!
        .IsDefined(typeof(AlwaysInterleaveAttribute), false).ShouldBeTrue();
}
