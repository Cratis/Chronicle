// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContext;

public class when_memoizing_a_resolved_key : given.a_context
{
    KeyResolver _resolver;
    KeyResolverResult _expected;
    KeyResolverResult _actual;
    bool _found;

    void Establish()
    {
        _expected = KeyResolverResult.Resolved(_context.Key);
        _resolver = (_, _, _) => Task.FromResult(_expected);
    }

    void Because()
    {
        _context.MemoizeResolvedKey(_resolver, _expected);
        var copy = _context with { NeedsInitialState = true };
        _found = copy.TryGetResolvedKey(_resolver, out var result);
        _actual = result!;
    }

    [Fact] void should_find_the_memo_in_a_record_copy() => _found.ShouldBeTrue();
    [Fact] void should_return_the_resolved_result() => _actual.ShouldEqual(_expected);
}
