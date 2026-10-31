// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Projections.for_ProjectionPropertyPaths.given;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionPropertyPaths.when_converting_an_expression;

public class and_the_validated_member_has_a_derived_function_name : Specification
{
    Expression<Func<CalendarModel, int>> _expression;
    PropertyPath _result;

    void Establish() => _expression = model => model.Week;
    void Because() => _result = ProjectionPropertyPaths.FromExpression(_expression, new CamelCaseNamingPolicy(), ProjectionPathRole.Target, "the calendar week");

    [Fact] void should_name_the_member() => _result.Path.ShouldEqual("week");
    [Fact] void should_resolve_a_property_not_a_function() => _result.LastSegment.ShouldBeOfExactType<PropertyName>();
}
