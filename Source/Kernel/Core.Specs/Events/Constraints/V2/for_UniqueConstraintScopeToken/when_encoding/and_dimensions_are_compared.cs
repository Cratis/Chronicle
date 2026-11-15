// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintScopeToken.when_encoding;

public class and_dimensions_are_compared : Specification
{
    readonly byte[] _key = Encoding.ASCII.GetBytes("slice-1-reference-key");
    Dictionary<string, string> _tokens;
    string _repeated;
    string _otherKey;
    string _valueDigest;
    string _nullScope;

    void Because()
    {
        var scopes = new Dictionary<string, ResolvedConstraintScope>
        {
            ["left-pipe"] = new("a|b", "c"),
            ["right-pipe"] = new("a", "b|c"),
            ["left-colon"] = new("a:b", "c"),
            ["right-colon"] = new("a", "b:c"),
            ["type-only"] = new("x"),
            ["stream-type-only"] = new(EventStreamType: "x"),
            ["stream-id-only"] = new(EventStreamId: "x"),
            ["all-null"] = new(),
            ["empty-type"] = new(""),
            ["empty-stream-type"] = new(EventStreamType: ""),
            ["empty-stream-id"] = new(EventStreamId: ""),
            ["case-upper"] = new("I"),
            ["case-lower"] = new("i"),
            ["unicode"] = new("é"),
            ["decomposed"] = new("e\u0301"),
            ["all-dimensions"] = new("type|:é", "stream|:", "id|:😀")
        };
        _tokens = scopes.ToDictionary(_ => _.Key, _ => UniqueConstraintScopeToken.Encode(_key, _.Value).Value);
        _repeated = UniqueConstraintScopeToken.Encode(_key, scopes["all-dimensions"]).Value;
        _nullScope = UniqueConstraintScopeToken.Encode(_key, null).Value;
        _otherKey = UniqueConstraintScopeToken.Encode(Encoding.ASCII.GetBytes("a-different-fixture-key"), scopes["all-dimensions"]).Value;
        _valueDigest = UniqueConstraintEncoder.Encode(_key, [new("type|:é"), new("stream|:"), new("id|:😀")]).AsT0.Value;
    }

    [Fact] void should_not_alias_pipes_inside_dimensions() => _tokens["left-pipe"].ShouldNotEqual(_tokens["right-pipe"]);
    [Fact] void should_not_alias_colons_inside_dimensions() => _tokens["left-colon"].ShouldNotEqual(_tokens["right-colon"]);
    [Fact] void should_preserve_source_and_stream_type_positions() => _tokens["type-only"].ShouldNotEqual(_tokens["stream-type-only"]);
    [Fact] void should_preserve_stream_type_and_id_positions() => _tokens["stream-type-only"].ShouldNotEqual(_tokens["stream-id-only"]);
    [Fact] void should_preserve_source_type_and_stream_id_positions() => _tokens["type-only"].ShouldNotEqual(_tokens["stream-id-only"]);
    [Fact] void should_distinguish_empty_source_type_from_unnarrowed() => _tokens["empty-type"].ShouldNotEqual(_tokens["all-null"]);
    [Fact] void should_distinguish_empty_stream_type_from_unnarrowed() => _tokens["empty-stream-type"].ShouldNotEqual(_tokens["all-null"]);
    [Fact] void should_distinguish_empty_stream_id_from_unnarrowed() => _tokens["empty-stream-id"].ShouldNotEqual(_tokens["all-null"]);
    [Fact] void should_not_lowercase_dimensions() => _tokens["case-upper"].ShouldNotEqual(_tokens["case-lower"]);
    [Fact] void should_not_normalize_unicode() => _tokens["unicode"].ShouldNotEqual(_tokens["decomposed"]);
    [Fact] void should_encode_an_absent_scope_as_all_unnarrowed() => _nullScope.ShouldEqual(_tokens["all-null"]);
    [Fact] void should_be_deterministic() => _repeated.ShouldEqual(_tokens["all-dimensions"]);
    [Fact] void should_be_keyed() => _otherKey.ShouldNotEqual(_tokens["all-dimensions"]);
    [Fact] void should_use_a_distinct_domain_from_value_hashing() => _tokens["all-dimensions"].ShouldNotEqual(_valueDigest[..32]);
    [Fact] void should_return_128_bits_of_hex() => _tokens["all-dimensions"].Length.ShouldEqual(32);
    [Fact] void should_return_lowercase_hex() => _tokens["all-dimensions"].All(_ => _ is >= '0' and <= '9' or >= 'a' and <= 'f').ShouldBeTrue();
}
