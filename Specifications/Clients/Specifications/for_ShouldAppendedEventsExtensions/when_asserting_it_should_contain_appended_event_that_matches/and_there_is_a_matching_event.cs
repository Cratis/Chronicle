// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Specifications.when_asserting_it_should_contain_appended_event_that_matches;

public class and_there_is_a_matching_event : given.no_events
{
    Exception result;

    void Establish()
    {
        events.Add(new AppendedEventForSpecifications(null!, null!, null!, new MyOtherEvent(43, "something")));
        events.Add(new AppendedEventForSpecifications(null!, null!, null!, new MyEvent(43, "something")));
    }

    void Because() => result = Catch.Exception(() => events.ShouldContainEvent<MyEvent>((ev, _) => ev.SomeInteger == 43));

    [Fact] void should_not_assert_that_the_event_should_contain() => result.ShouldBeNull();
}
