// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.for_BoundedEventCursor;

public class when_disposing : Specification
{
    IEventCursor _inner;

    void Establish() => _inner = Substitute.For<IEventCursor>();

    void Because() => new BoundedEventCursor(_inner, 10).Dispose();

    [Fact] void should_dispose_the_cursor_it_wraps() => _inner.Received(1).Dispose();
}
