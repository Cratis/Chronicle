// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Expressions.Keys.for_CompositeKeyExpressionResolver;

public class when_resolving_composite_key_with_literal_value : given.a_resolver
{
    bool _result;
    Key _key;

    async Task Because()
    {
        const string expression = "$composite(Key, first=$value(abc-def),second=second)";
        _eventValueProviderResolvers.Resolve(Arg.Any<JsonSchemaProperty>(), "$value(abc-def)").Returns(_ => "abc-def");
        _eventValueProviderResolvers.Resolve(Arg.Any<JsonSchemaProperty>(), "second").Returns(_ => "two");
        _result = _resolver.CanResolve(expression);
        var resolved = await _resolver.Resolve(_projection, expression, string.Empty)(null!, null!, null!);
        _key = ((ResolvedKey)resolved).Key;
    }

    [Fact] void should_accept_literal_value_expressions() => _result.ShouldBeTrue();
    [Fact] void should_resolve_the_value_with_a_hyphen() => ((IDictionary<string, object>)_key.Value)["first"].ShouldEqual("abc-def");
}
