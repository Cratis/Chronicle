// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Projections.for_ProjectionPropertyPaths.given;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionPropertyPaths.when_converting_an_expression;

public class and_the_accessor_calls_a_derived_function : Specification
{
    Expression<Func<CalendarModel, object>> _expression;
    PropertyPath _result;

    void Establish() => _expression = model => model.Occurred.Week();
    void Because() => _result = ProjectionPropertyPaths.FromExpression(_expression, new CamelCaseNamingPolicy(), ProjectionPathRole.Source, "the occurrence week");

    [Fact] void should_name_the_receiver_without_changing_the_function() => _result.Path.ShouldEqual("occurred.Week");
    [Fact] void should_resolve_a_function() => _result.LastSegment.ShouldBeOfExactType<DerivedPropertyFunctionSegment>();
}
