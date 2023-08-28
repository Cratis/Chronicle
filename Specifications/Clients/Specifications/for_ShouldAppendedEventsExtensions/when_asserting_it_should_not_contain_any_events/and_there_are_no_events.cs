// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Specifications.when_asserting_it_should_not_contain_any_events;

public class and_there_are_no_events : given.no_events
{
    Exception result;

    void Because() => result = Catch.Exception(events.ShouldNotContainAnyEvents);

    [Fact] void should_not_assert_that_the_event_should_contain() => result.ShouldBeNull();
}
