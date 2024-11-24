// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Expressions.EventValues;
using Cratis.Chronicle.Properties;
using Cratis.Strings;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Expressions.ModelProperties.for_SetExpressionResolver;

public class when_trying_to_resolve_valid_set_expression_against_model_and_event : given.an_appended_event
{
    Mock<IEventValueProviderExpressionResolvers> event_value_resolvers;
    SetExpressionResolver resolver;
    ExpandoObject target;

    void Establish()
    {
        target = new();
        event_value_resolvers = new();
        event_value_resolvers.Setup(_ => _.Resolve(Arg.Any<JsonSchemaProperty>(), Arg.Any<string>())).Returns((AppendedEvent _) => my_event.Something);
        resolver = new(event_value_resolvers.Object);
    }

    void Because() => resolver.Resolve("targetProperty", new(), nameof(my_event.Something).ToCamelCase())(@event, target, ArrayIndexers.NoIndexers);

    [Fact] void should_resolve_to_a_propertymapper_that_can_add_to_the_property() => ((int)((dynamic)target).targetProperty).ShouldEqual(my_event.Something);
}
