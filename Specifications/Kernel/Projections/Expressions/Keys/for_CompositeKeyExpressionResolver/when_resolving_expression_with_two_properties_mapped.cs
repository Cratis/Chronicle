// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Properties;
using NJsonSchema;

namespace Aksio.Cratis.Kernel.Projections.Expressions.Keys.for_CompositeKeyExpressionResolver;

public class when_resolving_expression_with_two_properties_mapped : given.a_resolver
{
    const string first_property = "first";
    const string second_property = "second";
    const string first_value = "First Value";
    const string second_value = "First Value";

    Key result;

    ValueProvider<AppendedEvent> first = (AppendedEvent _) => first_value;
    ValueProvider<AppendedEvent> second = (AppendedEvent _) => second_value;

    void Establish()
    {
        event_value_provider_resolvers.Setup(_ => _.Resolve(IsAny<JsonSchemaProperty>(), "$first")).Returns(first);
        event_value_provider_resolvers.Setup(_ => _.Resolve(IsAny<JsonSchemaProperty>(), "$second")).Returns(second);
    }

    async Task Because() => result = await resolver.Resolve(projection.Object, $"$composite({first_property}=$first, {second_property}=$second)", "target")(null!, null!);

    [Fact] void should_resolve_to_composite_containing_first_property_value() => ((IDictionary<string, object>)result.Value)[first_property].ShouldEqual(first_value);
    [Fact] void should_resolve_to_composite_containing_second_property_value() => ((IDictionary<string, object>)result.Value)[second_property].ShouldEqual(second_value);
}
