// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintKeyedEncoder.when_encoding;

public class and_a_scope_repeats : Specification
{
    readonly byte[] _key = Encoding.ASCII.GetBytes("slice-1-reference-key");
    UniqueConstraintScopeToken _first;
    UniqueConstraintScopeToken _second;
    UniqueConstraintScopeToken _otherDimension;
    UniqueConstraintScopeToken _expected;
    int _cached;

    void Because()
    {
        var encoder = new UniqueConstraintKeyedEncoder(_key);
        _first = encoder.EncodeScope(new ResolvedConstraintScope(EventStreamId: "branch-1"));
        _second = encoder.EncodeScope(new ResolvedConstraintScope(EventStreamId: "branch-1"));
        _otherDimension = encoder.EncodeScope(new ResolvedConstraintScope(EventStreamType: "branch-1"));
        _expected = UniqueConstraintScopeToken.Encode(_key, new ResolvedConstraintScope(EventStreamId: "branch-1"));
        _cached = encoder.CachedScopeCount;
    }

    [Fact] void should_return_an_equal_token_from_the_cache() => _second.ShouldEqual(_first);
    [Fact] void should_equal_the_static_token() => _first.ShouldEqual(_expected);
    [Fact] void should_not_alias_the_same_text_in_another_dimension() => _otherDimension.ShouldNotEqual(_first);
    [Fact] void should_cache_each_distinct_scope_once() => _cached.ShouldEqual(2);
}
