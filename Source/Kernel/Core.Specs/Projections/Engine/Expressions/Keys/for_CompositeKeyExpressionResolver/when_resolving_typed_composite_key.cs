// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Expressions.Keys.for_CompositeKeyExpressionResolver;

public class when_resolving_typed_composite_key : given.a_resolver
{
    Key _result;
    ValueProvider<AppendedEvent> _first = _ => "one";
    ValueProvider<AppendedEvent> _second = _ => "two";

    void Establish()
    {
        _eventValueProviderResolvers.Resolve(Arg.Any<JsonSchemaProperty>(), "first").Returns(_first);
        _eventValueProviderResolvers.Resolve(Arg.Any<JsonSchemaProperty>(), "second").Returns(_second);
    }

    async Task Because()
    {
        var resolved = await _resolver.Resolve(_projection, "$composite(Key, first=first,second=second)", string.Empty)(null!, null!, null!);
        _result = ((ResolvedKey)resolved).Key;
    }

    [Fact] void should_ignore_type_and_resolve_first_property() => ((IDictionary<string, object>)_result.Value)["first"].ShouldEqual("one");
    [Fact] void should_resolve_second_property() => ((IDictionary<string, object>)_result.Value)["second"].ShouldEqual("two");
}
