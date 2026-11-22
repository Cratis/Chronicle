// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Patterns;

namespace Cratis.Chronicle.Observation.for_ObserverSubscriptionDurability;

public class when_getting_the_kind_of_a_pattern_capture_subscriber : Specification
{
    // The capture observer has no durable subscription record today, so it is never released when dormant.
    [Fact] void should_not_recognize_it() => typeof(IPatternCaptureSubscriber).GetSubscriptionKind().ShouldBeNull();
}
