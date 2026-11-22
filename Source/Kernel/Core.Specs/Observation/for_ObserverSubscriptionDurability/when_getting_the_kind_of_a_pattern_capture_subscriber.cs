// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverSubscriptionDurability;

public class when_getting_the_kind_of_a_pattern_capture_subscriber : Specification
{
    [Fact] void should_recognize_it_as_pattern_capture() => typeof(IPatternCaptureSubscriber).GetSubscriptionKind().ShouldEqual(ObserverSubscriptionKind.PatternCapture);
    [Fact] void should_be_released_when_dormant() => ObserverSubscriptionKind.PatternCapture.CanBeReleasedWhenDormant().ShouldBeTrue();
}
