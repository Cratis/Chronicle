// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_HandledEventsReportDeduplication.when_getting_the_window;

public class for_the_default_response_timeout : Specification
{
    TimeSpan _window;

    void Because() => _window = HandledEventsReportDeduplication.WindowFor(TimeSpan.FromSeconds(30));

    [Fact] void should_be_the_default_window() => _window.ShouldEqual(HandledEventsReportDeduplication.DefaultWindow);
}
