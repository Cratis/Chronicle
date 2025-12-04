// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Expressions.Keys.for_CompositeKeyExpressionResolver;

public class when_resolving_expression_with_two_properties_mapped : given.a_resolver
{
    const string FirstProperty = "first";
    const string SecondProperty = "second";
    const string FirstValue = "First Value";
    const string SecondValue = "First Value";

    Key _result;

    ValueProvider<AppendedEvent> _first = _ => FirstValue;
    ValueProvider<AppendedEvent> _second = _ => SecondValue;

    void Establish()
    {
        _eventValueProviderResolvers.Resolve(Arg.Any<JsonSchemaProperty>(), "$first").Returns(_first);
        _eventValueProviderResolvers.Resolve(Arg.Any<JsonSchemaProperty>(), "$second").Returns(_second);
    }

    async Task Because()
    {
        var keyResult = await _resolver.Resolve(_projection, $"{WellKnownExpressions.Composite}({FirstProperty}=$first, {SecondProperty}=$second)", "target")(null!, null!, null!);
        _result = (keyResult as ResolvedKey)!.Key;
    }

    [Fact] void should_resolve_to_composite_containing_first_property_value() => ((IDictionary<string, object>)_result.Value)[FirstProperty].ShouldEqual(FirstValue);
    [Fact] void should_resolve_to_composite_containing_second_property_value() => ((IDictionary<string, object>)_result.Value)[SecondProperty].ShouldEqual(SecondValue);
}
