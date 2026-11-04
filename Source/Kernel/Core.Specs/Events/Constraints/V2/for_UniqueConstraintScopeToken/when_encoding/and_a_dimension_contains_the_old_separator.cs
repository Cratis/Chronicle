// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintScopeToken.when_encoding;

public class and_a_dimension_contains_the_old_separator : Specification
{
    readonly byte[] _key = Encoding.ASCII.GetBytes("slice-1-reference-key");
    string _joined;
    string _split;
    string _typeMarker;
    string _typeAndStream;

    void Because()
    {
        _joined = Token(new(EventStreamId: "a|esid:b"));
        _split = Token(new(EventStreamType: "a", EventStreamId: "b"));
        _typeMarker = Token(new(EventStreamId: "est:b"));
        _typeAndStream = Token(new(EventStreamType: "b"));
    }

    string Token(ResolvedConstraintScope scope) => UniqueConstraintScopeToken.Encode(_key, scope).Value;

    [Fact] void should_not_alias_a_stream_id_containing_the_separator_with_a_split_tuple() => _joined.ShouldNotEqual(_split);
    [Fact] void should_not_alias_a_dimension_containing_a_marker_with_another_dimension() => _typeMarker.ShouldNotEqual(_typeAndStream);
}
