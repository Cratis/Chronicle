// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit.Sdk;

namespace Aksio.Cratis.Specifications.when_asserting_it_should_contain_events;

public class and_there_are_no_events : given.no_events
{
    Exception result;

    void Because() => result = Catch.Exception(() => events.ShouldContainEvents(
        new MyEvent(42, "forty two"),
        new MyOtherEvent(42, "forty two")));

    [Fact] void should_assert_that_the_event_should_contain() => result.ShouldBeOfExactType<ContainsException>();
}
