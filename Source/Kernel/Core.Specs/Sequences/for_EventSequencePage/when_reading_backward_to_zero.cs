// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_EventSequencePage;

public class when_reading_backward_to_zero : given.a_page_query
{
    void Establish() => _window = [EventAt(0)];

    async Task Because() => _page = await Query(from: 0, count: 1, backward: true);

    [Fact] void should_return_zero() => _page.Events.Single().Id.ShouldEqual("0");
    [Fact] void should_not_underflow_the_continuation() => _page.Next.ShouldBeNull();
}
