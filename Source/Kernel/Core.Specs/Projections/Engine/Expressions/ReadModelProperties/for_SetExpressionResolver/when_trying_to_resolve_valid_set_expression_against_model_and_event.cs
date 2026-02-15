// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Projections.Expressions.EventValues;
using Cratis.Chronicle.Properties;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Engine.Expressions.ReadModelProperties.for_SetExpressionResolver;

public class when_trying_to_resolve_valid_set_expression_against_model_and_event : given.an_appended_event
{
    IEventValueProviderExpressionResolvers _eventValueResolvers;
    SetExpressionResolver _resolver;
    ExpandoObject _target;

    void Establish()
    {
        _target = new();
        _eventValueResolvers = Substitute.For<IEventValueProviderExpressionResolvers>();
        _eventValueResolvers.Resolve(Arg.Any<JsonSchemaProperty>(), Arg.Any<string>()).Returns(_ => my_event.Something);
        _resolver = new(_eventValueResolvers);
    }

    void Because() => _resolver.Resolve("targetProperty", new(), nameof(my_event.Something))(@event, _target, ArrayIndexers.NoIndexers);

    [Fact] void should_resolve_to_a_propertymapper_that_can_add_to_the_property() => ((int)((dynamic)_target).targetProperty).ShouldEqual(my_event.Something);
}
