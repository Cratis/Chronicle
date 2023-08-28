// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Specifications.when_asserting_it_should_contain_events;

public class and_it_has_the_events_amongst_others : given.no_events
{
    Exception result;
    MyEvent first_event;
    MyOtherEvent second_event;

    void Establish()
    {
        first_event = new MyEvent(42, "forty two");
        second_event = new MyOtherEvent(42, "forty two");
        events.Add(new AppendedEventForSpecifications(null!, null!, null!, new MyEvent(43, "something")));
        events.Add(new AppendedEventForSpecifications(null!, null!, null!, first_event));
        events.Add(new AppendedEventForSpecifications(null!, null!, null!, second_event));
        events.Add(new AppendedEventForSpecifications(null!, null!, null!, new MyOtherEvent(43, "something")));
    }

    void Because() => result = Catch.Exception(() => events.ShouldContainEvents(first_event, second_event));

    [Fact] void should_not_assert_that_the_event_should_contain() => result.ShouldBeNull();
}
