// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventContext;

public class when_using_the_empty_context : Specification
{
    EventContext _context;

    void Because() => _context = EventContext.Empty;

    [Fact] void should_have_occurred_in_utc() => _context.Occurred.Offset.ShouldEqual(TimeSpan.Zero);
}
