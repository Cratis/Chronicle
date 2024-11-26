// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Expressions.Keys.for_CompositeKeyExpressionResolver;

public class when_resolving_expression_with_two_properties_mapped : given.a_resolver
{
    const string _firstProperty = "first";
    const string _secondProperty = "second";
    const string _firstValue = "First Value";
    const string _secondValue = "First Value";

    Key _result;

    ValueProvider<AppendedEvent> first = (AppendedEvent _) => _firstValue;
    ValueProvider<AppendedEvent> second = (AppendedEvent _) => _secondValue;

    void Establish()
    {
        _eventValueProviderResolvers.Resolve(Arg.Any<JsonSchemaProperty>(), "$first").Returns(first);
        _eventValueProviderResolvers.Resolve(Arg.Any<JsonSchemaProperty>(), "$second").Returns(second);
    }

    async Task Because() => _result = await _resolver.Resolve(_projection, $"$composite({_firstProperty}=$first, {_secondProperty}=$second)", "target")(null!, null!);

    [Fact] void should_resolve_to_composite_containing_first_property_value() => ((IDictionary<string, object>)_result.Value)[_firstProperty].ShouldEqual(_firstValue);
    [Fact] void should_resolve_to_composite_containing_second_property_value() => ((IDictionary<string, object>)_result.Value)[_secondProperty].ShouldEqual(_secondValue);
}
