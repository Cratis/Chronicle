// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Events.Projections.Expressions.EventValues;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Strings;

namespace Aksio.Cratis.Events.Projections.Expressions.ModelProperties.for_AddExpressionResolver;

public class when_trying_to_resolve_valid_add_expression_against_model_and_event : given.an_appended_event
{
    Mock<IEventValueProviderExpressionResolvers> event_value_resolvers;
    AddExpressionResolver resolver;
    ExpandoObject target;

    void Establish()
    {
        target = new();
        event_value_resolvers = new();
        event_value_resolvers.Setup(_ => _.Resolve(IsAny<string>())).Returns((AppendedEvent _) => MyEvent.Something);
        resolver = new(event_value_resolvers.Object);
    }

    void Because() => resolver.Resolve("targetProperty", $"$add({nameof(MyEvent.Something).ToCamelCase()})")(@event, target, ArrayIndexers.NoIndexers);

    [Fact] void should_resolve_to_a_propertymapper_that_can_add_to_the_property() => ((int)((dynamic)target).targetProperty).ShouldEqual(MyEvent.Something);
}
