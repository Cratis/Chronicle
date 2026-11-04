// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintKeyedEncoder.when_encoding;

public class and_values_match_the_static_encoder : Specification
{
    readonly byte[] _key = Encoding.ASCII.GetBytes("slice-1-reference-key");
    string _keyedValue;
    string _staticValue;
    string _keyedScope;
    string _staticScope;

    void Because()
    {
        var encoder = new UniqueConstraintKeyedEncoder(_key);
        UniqueConstraintComponent[] components = [new("I"), new(1.25d), new(Guid.Empty, "guid"), new(new[] { "a", "b" }), new(null)];
        var scope = new ResolvedConstraintScope("type", null, "stream");
        _keyedValue = encoder.EncodeValue(components, true).AsT0.Value;
        _staticValue = UniqueConstraintEncoder.Encode(_key, components, true).AsT0.Value;
        _keyedScope = encoder.EncodeScope(scope).Value;
        _staticScope = UniqueConstraintScopeToken.Encode(_key, scope).Value;
    }

    [Fact] void should_produce_the_same_value() => _keyedValue.ShouldEqual(_staticValue);
    [Fact] void should_produce_the_same_scope_token() => _keyedScope.ShouldEqual(_staticScope);
}
