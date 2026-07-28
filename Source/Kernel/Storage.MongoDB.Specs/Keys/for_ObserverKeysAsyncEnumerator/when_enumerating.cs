// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.MongoDB.Keys;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_ObserverKeysAsyncEnumerator;

public class when_enumerating : Specification
{
    ObserverKeysAsyncEnumerator _enumerator;
    IAsyncCursor<EventSourceId> _cursor;
    int _cursorFactoryInvocations;
    int _invocationsBeforeMoveNext;

    void Establish()
    {
        _cursor = Substitute.For<IAsyncCursor<EventSourceId>>();
        _cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(false);
        _enumerator = new ObserverKeysAsyncEnumerator(
            _ =>
            {
                _cursorFactoryInvocations++;
                return Task.FromResult(_cursor);
            },
            CancellationToken.None);
    }

    async Task Because()
    {
        _invocationsBeforeMoveNext = _cursorFactoryInvocations;
        await _enumerator.MoveNextAsync();
        await _enumerator.MoveNextAsync();
    }

    [Fact] void should_not_open_the_cursor_before_the_first_move_next() => _invocationsBeforeMoveNext.ShouldEqual(0);
    [Fact] void should_open_the_cursor_only_once_across_enumeration() => _cursorFactoryInvocations.ShouldEqual(1);
}
