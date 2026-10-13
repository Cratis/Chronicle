// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_resetting;

public class and_events_were_held_back : given.a_detector
{
    void Because()
    {
        HoldBack(_q, 10);
        _detector.Reset();
        _detector.Evaluate(11);
    }

    [Fact] void should_not_report() => _reports.ShouldBeEmpty();
}
