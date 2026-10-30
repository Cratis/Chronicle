// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Expressions.ReadModelProperties.for_SubtractExpressionResolver;

public class when_resolving_a_snake_case_operand : given.an_appended_event
{
    IEventValueProviderExpressionResolvers _eventValueResolvers;
    SubtractExpressionResolver _resolver;
    JsonSchemaProperty _schema;
    ExpandoObject _target;

    void Establish()
    {
        _target = new();
        ((dynamic)_target).total = 10;
        _schema = new() { Type = JsonObjectType.Integer };
        _eventValueResolvers = Substitute.For<IEventValueProviderExpressionResolvers>();
        _eventValueResolvers.Resolve(_schema, "unit_price").Returns(_ => 3);
        _resolver = new(_eventValueResolvers, new TypeFormats());
    }

    void Because() => _resolver.Resolve("total", _schema, "$subtract(unit_price)")(@event, _target, ArrayIndexers.NoIndexers);

    [Fact] void should_accept_a_snake_case_operand() => _resolver.CanResolve("total", "$subtract(unit_price)").ShouldBeTrue();
    [Fact] void should_accept_a_nested_snake_case_operand() => _resolver.CanResolve("total", "$subtract(order_line.unit_price)").ShouldBeTrue();
    [Fact] void should_resolve_the_complete_operand() => _eventValueResolvers.Received(1).Resolve(_schema, "unit_price");
    [Fact] void should_subtract_the_event_value() => ((int)((dynamic)_target).total).ShouldEqual(7);
}
